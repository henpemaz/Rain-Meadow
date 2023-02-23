using System;

namespace RainMeadow
{
    public partial class RoomSession
    {

        // A game entity has entered the room, check for corresponding online entity to be added
        internal void EntityEnteringRoom(AbstractPhysicalObject entity, WorldCoordinate pos)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) { return; } // throw new InvalidOperationException("not available"); }
            if (!entities.ContainsKey(entity)) // A new entity, presumably mine
            {
                // todo stronger checks if my entity or a leftover
                var oe = new OnlineEntity(entity, OnlineManager.mePlayer, entity.ID.number, pos, this);
                OnlineManager.mePlayer.recentEntities[oe.id] = oe; // funny how [] is safe on write, unsafe on read. Dumb as fuck
                entities.Add(entity, oe);
                RainMeadow.Debug("ADDED " + oe);
                NewOnlineEntity(oe);
            }
        }

        // A new OnlineEntity was added to the room locally, notify accordingly
        private void NewOnlineEntity(OnlineEntity oe)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) throw new InvalidOperationException("not available");
            if (isOwner) // I am responsible for notifying other players about it
            {
                foreach (var sub in subscriptions)
                {
                    if (sub.player == oe.owner) continue;
                    sub.player.QueueEvent(new NewEntityEvent(this, oe));
                }
            }
            else if (oe.owner.isMe) // I notify the owner about my entity in the room
            {
                owner.QueueEvent(new NewEntityEvent(this, oe));
                OnlineManager.AddFeed(this, oe);
            }
        }

        // I've been notified that a new creature has entered the room, and I must recreate the equivalent in my world
        public void OnNewEntity(NewEntityEvent newEntityEvent)
        {
            RainMeadow.Debug(this);
            // todo more than just creatures
            if (newEntityEvent.owner.isMe) { throw new InvalidOperationException("notified of my own creation"); }
            AbstractCreature creature;
            OnlineEntity oe;
            if (newEntityEvent.owner.recentEntities.TryGetValue(newEntityEvent.entityId, out oe))
            {
                RainMeadow.Debug("reusing existing entity " + oe);
                creature = oe.entity as AbstractCreature;
                if (oe.lastInRoom is RoomSession previousRoom && previousRoom != this)
                {
                    RainMeadow.Debug("removing from previous room " + previousRoom);
                    oe.lastInRoom = this;
                    previousRoom.EntityLeavingRoom(oe.entity); // left because added here
                }
                else if (oe.lastInRoom == this)
                {
                    throw new InvalidOperationException("already in room ??");
                }
                else
                {
                    oe.lastInRoom = this;
                }
                creature.slatedForDeletion = false;
                if (creature.realizedObject is PhysicalObject po) po.slatedForDeletetion = false;
                oe.ticksSinceSeen = 0;
                // todo check for creature already in room
            }
            else
            {
                RainMeadow.Debug("spawning new entity");
                // it is very tempting to switch to the generic tostring/fromstring from the savesystem, BUT
                // it would be almost impossible to sanitize input and who knows what someone could do through that
                if (!newEntityEvent.isCreature) throw new NotImplementedException("cant do non-creatures yet");
                CreatureTemplate.Type type = new CreatureTemplate.Type(newEntityEvent.template, false);
                if (type.Index == -1)
                {
                    RainMeadow.Debug(type);
                    RainMeadow.Debug(newEntityEvent.template);
                    throw new InvalidOperationException("invalid template");
                }
                EntityID id = absroom.world.game.GetNewID();
                id.altSeed = newEntityEvent.entityId;
                RainMeadow.Debug(id);
                RainMeadow.Debug(newEntityEvent.initialPos);
                creature = new AbstractCreature(absroom.world, StaticWorld.GetCreatureTemplate(type), null, newEntityEvent.initialPos, id);
                RainMeadow.Debug(creature);
                if (creature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat) // for some dumb reason it doesn't get a default
                {
                    creature.state = new PlayerState(creature, 0, RainMeadow.Ext_SlugcatStatsName.OnlineSessionRemotePlayer, false);
                }
                oe = new OnlineEntity(creature, newEntityEvent.owner, newEntityEvent.entityId, newEntityEvent.initialPos, this);
                newEntityEvent.owner.recentEntities.Add(newEntityEvent.entityId, oe);
            }
            entities.Add(creature, oe);
            RainMeadow.Debug("ADDED " + oe);
            NewOnlineEntity(oe);
            if (absroom.realizedRoom is Room room && creature.AllowedToExistInRoom(room))
            {
                RainMeadow.Debug("spawning creature " + creature);
                if (newEntityEvent.initialPos.TileDefined)
                {
                    RainMeadow.Debug("added directly to the room");
                    absroom.AddEntity(creature);
                    creature.RealizeInRoom(); // places in room
                }
                else if (newEntityEvent.initialPos.NodeDefined)
                {
                    RainMeadow.Debug("added directly to shortcut system");
                    creature.Realize();
                    creature.realizedCreature.inShortcut = true;
                    // this calls MOVE on the next tick which remove-adds?
                    absroom.world.game.shortcuts.CreatureEnterFromAbstractRoom(creature.realizedCreature, absroom, newEntityEvent.initialPos.abstractNode);
                }
                else
                {
                    RainMeadow.Debug("INVALID POS??");
                }
            }
            else
            {
                RainMeadow.Debug("not spawning creature " + creature);
                RainMeadow.Debug($"reasons {absroom.realizedRoom is not null} {(absroom.realizedRoom != null && creature.AllowedToExistInRoom(absroom.realizedRoom))}");
                absroom.AddEntity(creature);
            }
        }

        internal void EntityLeavingRoom(AbstractPhysicalObject entity)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug(entity);
            if (!isAvailable) throw new InvalidOperationException("not available");
            if (entities.TryGetValue(entity, out var oe))
            {
                if (oe.lastInRoom != null && oe.lastInRoom != this) throw new InvalidOperationException("already left the room");
                EntityLeftRoom(oe);
                if (!oe.owner.isMe)
                {
                    // external entity should be removed from the game until its re-added somewhere else
                    RainMeadow.Debug("Removing entity from the game: " + oe);
                    absroom.RemoveEntity(entity);
                    entity.slatedForDeletion = true;
                    if (entity.realizedObject is PhysicalObject po)
                    {
                        po.slatedForDeletetion = true;
                        if (absroom.realizedRoom is Room room) room.RemoveObject(po);
                    }
                    oe.lastInRoom = null;
                }
            }
            else
            {
                RainMeadow.Debug("untracked entity leaving: " + entity);
            }
        }

        private void EntityLeftRoom(OnlineEntity oe)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug(oe);
            entities.Remove(oe.entity);
            RainMeadow.Debug("REMOVED " + oe);
            if (isOwner) // I am responsible for notifying other players about it
            {
                RainMeadow.Debug("notifying others of entity leaving: " + oe);
                foreach (var sub in subscriptions)
                {
                    if (sub.player == oe.owner) continue;
                    sub.player.QueueEvent(new EntityLeftEvent(this, oe));
                }
            }
            else if (oe.owner.isMe) // I notify the owner about my entity in the room
            {
                RainMeadow.Debug("notifying owner of entity leaving: " + oe);
                owner.QueueEvent(new EntityLeftEvent(this, oe));
                OnlineManager.RemoveFeed(this, oe);
            }
            else
            {
                RainMeadow.Debug("external entity leaving, not notifying anyone: " + oe);
            }
        }

        // I've been notified that an entity has left
        internal void OnEntityLeft(EntityLeftEvent entityLeftEvent)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug(entityLeftEvent);
            if (entityLeftEvent.owner.isMe) { throw new InvalidOperationException("notified of my own creation"); }
            if (entityLeftEvent.owner.recentEntities.TryGetValue(entityLeftEvent.entityId, out OnlineEntity oe))
            {
                RainMeadow.Debug("entity found " + oe);
                if (oe.lastInRoom is RoomSession otherRoom && otherRoom != this)
                {
                    // already somewhere else, no op?
                }
                else
                {
                    EntityLeavingRoom(oe.entity);
                }
            }
            else
            {
                // not found!
                RainMeadow.Debug("entity not found");
            }
        }
    }
}

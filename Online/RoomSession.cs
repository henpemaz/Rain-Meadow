using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public class RoomSession : OnlineResource
    {
        public AbstractRoom absroom;
        public bool abstractOnDeactivate;
        private Dictionary<AbstractWorldEntity, OnlineEntity> entities = new();

        public RoomSession(WorldSession ws, AbstractRoom absroom)
        {
            super = ws;
            this.absroom = absroom;
            deactivateOnRelease = true;
        }

        protected override void ActivateImpl()
        {
            
        }

        protected override void DeactivateImpl()
        {
            if (abstractOnDeactivate)
            {
                absroom.Abstractize();
            }
        }

        protected override void AvailableImpl()
        {
            base.AvailableImpl();
            foreach (var ent in absroom.entities)
            {
                if(ent is AbstractPhysicalObject apo)
                {
                    EntityEnteringRoom(apo, apo.pos);
                }
            }
        }

        protected override void SubscribedImpl(OnlinePlayer player)
        {
            base.SubscribedImpl(player);
            foreach (var ent in entities.Values)
            {
                if (player == ent.owner) continue;
                player.QueueEvent(new NewEntityEvent(this, ent));
            }
        }

        internal override string Identifier()
        {
            return super.Identifier() + absroom.name;
        }

        public override void ReadState(ResourceState newState, ulong ts)
        {
            //throw new System.NotImplementedException();
        }

        protected override ResourceState MakeState(ulong ts)
        {
            return new RoomState(this, ts);
        }


        // A game entity has entered the room, check for corresponding online entity to be added
        internal void EntityEnteringRoom(AbstractPhysicalObject entity, WorldCoordinate pos)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) { return; } // throw new InvalidOperationException("not available"); }
            if (!entities.ContainsKey(entity)) // A new entity, presumably mine
            {
                // todo stronger checks if my entity or a leftover
                var oe = new OnlineEntity(entity, OnlineManager.mePlayer, entity.ID.number, pos, this);
                entities.Add(entity, oe);
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

        // I've been notified that a new creature has entered the room
        public void OnNewEntity(NewEntityEvent newEntityEvent)
        {
            RainMeadow.Debug(this);

            // todo more than just creatures
            if(newEntityEvent.owner.isMe) { throw new InvalidOperationException("notified of my own creation"); }
            AbstractCreature creature;
            OnlineEntity oe;
            if (newEntityEvent.from.recentEntities.TryGetValue(newEntityEvent.entityId, out oe))
            {
                RainMeadow.Debug("reusing existing entity " + oe);
                creature = oe.entity as AbstractCreature;
                if(oe.lastInRoom is RoomSession previousRoom && previousRoom != this)
                {
                    oe.lastInRoom = this;
                    previousRoom.EntityLeavingRoom(oe.entity); // left because added here
                }
                else if (oe.lastInRoom == this)
                {
                    throw new InvalidOperationException("already in room");
                }
                else
                {
                    oe.lastInRoom = this;
                }
                oe.entity.slatedForDeletion = false;
                creature.slatedForDeletion = false;
                oe.ticksSinceSeen = 0;
                // todo check for creature already in room
            }
            else
            {
                // it is very tempting to switch to the generic tostring/fromstring from the savesystem, BUT
                // it would be almost impossible to sanitize input and who knows what someone could do through that
                if (!newEntityEvent.isCreature) throw new NotImplementedException("cant do non-creatures yet");
                CreatureTemplate.Type type = new CreatureTemplate.Type(newEntityEvent.template, false);
                RainMeadow.Debug(type);
                RainMeadow.Debug(newEntityEvent.template);
                if (type.Index == -1)
                {
                    throw new InvalidOperationException("invalid template");
                }
                EntityID id = absroom.world.game.GetNewID();
                id.altSeed = newEntityEvent.entityId;
                RainMeadow.Debug(id);
                creature = new AbstractCreature(absroom.world, StaticWorld.GetCreatureTemplate(type), null, newEntityEvent.pos, id);
                RainMeadow.Debug(creature);
                if (creature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat) // for some dumb reason it doesn't get a default
                {
                    creature.state = new PlayerState(creature, 0, RainMeadow.Ext_SlugcatStatsName.OnlineSessionRemotePlayer, false);
                }
                oe = new OnlineEntity(creature, OnlineManager.mePlayer, newEntityEvent.entityId, newEntityEvent.pos, this);
                //entities.Add(creature, oe);
                newEntityEvent.from.recentEntities.Add(newEntityEvent.entityId, oe);
            }
            
            absroom.AddEntity(creature); // This calls EntityEnteringRoom but not NewOnlineEntity because it's already registered
            //NewOnlineEntity(oe);
            if (absroom.realizedRoom is Room room && room.shortCutsReady) 
            {
                RainMeadow.Debug("spawning creature " + creature);
                if(newEntityEvent.pos.TileDefined)
                {
                    creature.RealizeInRoom(); // places in room
                }
                else if(newEntityEvent.pos.NodeDefined)
                {
                    creature.Realize();
                    creature.realizedCreature.inShortcut = true;
                    absroom.world.game.shortcuts.CreatureEnterFromAbstractRoom(creature.realizedCreature, absroom, newEntityEvent.pos.abstractNode);
                }
            }
            else
            {
                RainMeadow.Debug("not spawning creature " + creature);
                RainMeadow.Debug($"{absroom.realizedRoom is not null}{(absroom.realizedRoom != null && creature.AllowedToExistInRoom(absroom.realizedRoom))}");
            }
        }

        internal void EntityLeavingRoom(AbstractPhysicalObject entity)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) throw new InvalidOperationException("not available");
            if (entities.TryGetValue(entity, out var oe))
            {
                EntityLeftRoom(oe);
                if (!oe.owner.isMe && oe.lastInRoom == this)
                {
                    // external entity should be removed from the game until its re-added somewhere else
                    absroom.RemoveEntity(entity);
                    entity.slatedForDeletion = true;
                    if(entity.realizedObject is PhysicalObject po)
                    {
                        po.slatedForDeletetion = true;
                        if (po is Creature creature && creature.inShortcut) creature.RemoveFromShortcuts();
                    }
                    oe.lastInRoom = null;
                }
            }
        }

        private void EntityLeftRoom(OnlineEntity oe)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) throw new InvalidOperationException("not available");
            entities.Remove(oe.entity);
            if (isOwner) // I am responsible for notifying other players about it
            {
                foreach (var sub in subscriptions)
                {
                    if (sub.player == oe.owner) continue;
                    sub.player.QueueEvent(new EntityLeftEvent(this, oe));
                }
            }
            else if (oe.owner.isMe) // I notify the owner about my entity in the room
            {
                owner.QueueEvent(new EntityLeftEvent(this, oe));
                OnlineManager.RemoveFeed(this, oe);
            }
        }

        // I've been notified that an entity has left
        internal void OnEntityLeft(EntityLeftEvent entityLeftEvent)
        {
            RainMeadow.Debug(this);
            if (entityLeftEvent.owner.isMe) { throw new InvalidOperationException("notified of my own creation"); }
            OnlineEntity oe;
            if (entityLeftEvent.from.recentEntities.TryGetValue(entityLeftEvent.entityId, out oe))
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

        public class RoomState : ResourceState
        {
            public RoomState(OnlineResource resource, ulong ts) : base(resource, ts)
            {

            }

            public override ResourceStateType stateType => ResourceStateType.RoomState;
        }
    }
}

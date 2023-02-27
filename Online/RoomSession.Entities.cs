using System;
using System.Linq;

namespace RainMeadow
{
    public partial class RoomSession
    {
        // called by hooks on absroom.addentity, as well as Available()
        // A game entity has entered the room, check for corresponding online entity to be added
        internal void EntityEnteringRoom(AbstractPhysicalObject entity, WorldCoordinate pos)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) { return; } // throw new InvalidOperationException("not available"); }
            if (!entities.Any(e=>e.entity == entity)) // A new entity, presumably mine
            {
                // todo stronger checks if my entity or a leftover
                RainMeadow.Debug("Registering new entity as owned by myself");
                var oe = new OnlineEntity(entity, OnlineManager.mePlayer, entity.ID.number, pos);
                RainMeadow.Debug(oe);
                OnlineManager.mePlayer.recentEntities[oe.id] = oe; // funny how [] is safe on write, unsafe on read. Dumb as fuck
                EntityEntered(oe);
            }
            else
            {
                RainMeadow.Debug("skipping already registered entity");
            }
        }

        // how to place a creature in the room in 19 simple steps
        protected override void EntityEntered(OnlineEntity oe)
        {
            base.EntityEntered(oe);
            if(oe.entity is not AbstractCreature creature) { throw new InvalidOperationException("entity not a creature"); }
            if (!oe.owner.isMe)
            {
                RainMeadow.Debug("A remote creature entered, adding it to the room");
                if (absroom.realizedRoom is Room room && creature.AllowedToExistInRoom(room))
                {
                    RainMeadow.Debug("spawning creature " + creature);
                    if (oe.initialPos.TileDefined)
                    {
                        RainMeadow.Debug("added directly to the room");
                        absroom.AddEntity(creature);
                        creature.RealizeInRoom(); // places in room
                    }
                    else if (oe.initialPos.NodeDefined)
                    {
                        RainMeadow.Debug("added directly to shortcut system");
                        creature.Realize();
                        creature.realizedCreature.inShortcut = true;
                        // this calls MOVE on the next tick which remove-adds
                        absroom.world.game.shortcuts.CreatureEnterFromAbstractRoom(creature.realizedCreature, absroom, oe.initialPos.abstractNode);
                    }
                    else
                    {
                        RainMeadow.Debug("INVALID POS??" + oe.initialPos);
                        throw new InvalidOperationException("entity must have a vaild position");
                    }
                }
                else
                {
                    RainMeadow.Debug("not spawning creature " + creature);
                    RainMeadow.Debug($"reasons {absroom.realizedRoom is not null} {(absroom.realizedRoom != null && creature.AllowedToExistInRoom(absroom.realizedRoom))}");
                    if(creature.realizedCreature != null)
                    {
                        if (!oe.initialPos.TileDefined && oe.initialPos.NodeDefined && absroom.realizedRoom != null && absroom.realizedRoom.shortCutsReady)
                        {
                            RainMeadow.Debug("added realized creature to shortcut system");
                            creature.realizedCreature.inShortcut = true;
                            // this calls MOVE on the next tick which remove-adds
                            absroom.world.game.shortcuts.CreatureEnterFromAbstractRoom(creature.realizedCreature, absroom, oe.initialPos.abstractNode);
                        }
                        else
                        {
                            // can't abstractize properly because previous location is lost
                            RainMeadow.Debug("cleared realized creature and added to absroom as abstract entity");
                            creature.realizedCreature = null;
                            absroom.AddEntity(creature);
                        }
                    }
                    else
                    {
                        RainMeadow.Debug("added to absroom as abstract entity");
                        absroom.AddEntity(creature);
                    }
                }
            }
            else
            {
                RainMeadow.Debug("My own entity has entered the room, not doing anything about it");
            }
        }

        internal void EntityLeavingRoom(AbstractPhysicalObject entity)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug(entity);
            if (!isAvailable) throw new InvalidOperationException("not available");
            if (entities.FirstOrDefault(e=>e.entity == entity) is OnlineEntity oe)
            {
                EntityLeft(oe);
            }
            else
            {
                RainMeadow.Debug("untracked entity leaving: " + entity);
            }
        }

        protected override void EntityLeft(OnlineEntity oe)
        {
            base.EntityLeft(oe);
            if (!oe.owner.isMe)
            {
                // external entity should be removed from the game until its re-added somewhere else
                RainMeadow.Debug("Removing entity from the game: " + oe);
                absroom.RemoveEntity(oe.entity);
                oe.entity.slatedForDeletion = true;
                if (oe.entity.realizedObject is PhysicalObject po)
                {
                    po.slatedForDeletetion = true;
                    if (absroom.realizedRoom is Room room) room.RemoveObject(po);
                }
            }
            else
            {
                RainMeadow.Debug("my own entity leaving");
            }
        }

        // I do not like this
        // How do I abstract this away without having this be like 4 different steps
        // this sucks
        // I need to be able to abstract an "entity" from an event
        // but actually instantiating the entity could be a separate step
        // so maybe "get a potentially empty entity, then maybe realize it"
        // maybe entity.HandleAddedToResource(resource)? this smells like a responsibility swap
        // maybe 
        protected override OnlineEntity CreateOrReuseEntity(NewEntityEvent newEntityEvent)
        {
            OnlineEntity oe = null;
            if (newEntityEvent.owner.recentEntities.TryGetValue(newEntityEvent.entityId, out oe))
            {
                RainMeadow.Debug("reusing existing entity " + oe);
                var creature = oe.entity as AbstractCreature;
                creature.slatedForDeletion = false;
                if (creature.realizedObject is PhysicalObject po) po.slatedForDeletetion = false;

                oe.initialPos = newEntityEvent.initialPos;
                oe.entity.pos = oe.initialPos;
                return oe;
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
                var creature = new AbstractCreature(absroom.world, StaticWorld.GetCreatureTemplate(type), null, newEntityEvent.initialPos, id);
                RainMeadow.Debug(creature);
                if (creature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat) // for some dumb reason it doesn't get a default
                {
                    creature.state = new PlayerState(creature, 0, RainMeadow.Ext_SlugcatStatsName.OnlineSessionRemotePlayer, false);
                }
                oe = new OnlineEntity(creature, newEntityEvent.owner, newEntityEvent.entityId, newEntityEvent.initialPos);
                newEntityEvent.owner.recentEntities.Add(newEntityEvent.entityId, oe);
                return oe;
            }
            return oe;
        }
    }
}

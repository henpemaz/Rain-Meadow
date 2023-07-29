using System;
using System.Runtime.CompilerServices;

namespace RainMeadow
{
    public class OnlinePhysicalObject : OnlineEntity
    {
        public readonly AbstractPhysicalObject apo;
        public readonly int seed;
        public bool realized;

        public bool beingMoved;
        public static ConditionalWeakTable<AbstractPhysicalObject, OnlinePhysicalObject> map = new();

        public RoomSession roomSession => this.currentlyJoinedResource as RoomSession; // shorthand

        public static OnlinePhysicalObject RegisterPhysicalObject(AbstractPhysicalObject apo)
        {
            OnlinePhysicalObject newOe = NewFromApo(apo);
            RainMeadow.Debug("Registering new entity - " + newOe.ToString());
            OnlineManager.recentEntities.Add(newOe.id, newOe);
            map.Add(apo, newOe);
            return newOe;
        }

        public static OnlinePhysicalObject NewFromApo(AbstractPhysicalObject apo)
        {
            if (apo is AbstractCreature ac) return new OnlineCreature(ac, apo.ID.RandomSeed, apo.realizedObject != null, OnlineManager.mePlayer, new OnlineEntity.EntityId(OnlineManager.mePlayer.inLobbyId, apo.ID.number), !RainMeadow.sSpawningPersonas);
            return new OnlinePhysicalObject(apo, apo.ID.RandomSeed, apo.realizedObject != null, OnlineManager.mePlayer, new OnlineEntity.EntityId(OnlineManager.mePlayer.inLobbyId, apo.ID.number), !RainMeadow.sSpawningPersonas);
        }

        public OnlinePhysicalObject(AbstractPhysicalObject apo, int seed, bool realized, OnlinePlayer owner, EntityId id, bool isTransferable) : base(owner, id, isTransferable)
        {
            this.apo = apo;
            this.seed = seed;
            this.realized = realized;
        }

        public override void NewOwner(OnlinePlayer newOwner)
        {
            base.NewOwner(newOwner);
            if (newOwner.isMe)
            {
                realized = apo.realizedObject != null; // owner is responsible for upkeeping this
            }
        }

        public override NewEntityEvent AsNewEntityEvent(OnlineResource inResource)
        {
            return new NewObjectEvent(seed, realized, apo.ToString(), inResource, this, null);
        }

        public static OnlineEntity FromEvent(NewObjectEvent newObjectEvent, OnlineResource inResource)
        {
            World world = inResource.World;
            EntityID id = world.game.GetNewID();
            id.altSeed = newObjectEvent.seed;

            RainMeadow.Debug("serializedObject: " + newObjectEvent.serializedObject);
            var apo = SaveState.AbstractPhysicalObjectFromString(world, newObjectEvent.serializedObject);
            apo.ID = id;
            
            var oe = new OnlinePhysicalObject(apo, newObjectEvent.seed, newObjectEvent.realized, OnlineManager.lobby.PlayerFromId(newObjectEvent.owner), newObjectEvent.entityId, newObjectEvent.isTransferable);
            map.Add(apo, oe);
            OnlineManager.recentEntities.Add(oe.id, oe);

            return oe;
        }

        public override void ReadState(EntityState entityState, OnlineResource inResource)
        {
            beingMoved = true;
            base.ReadState(entityState, inResource);
            beingMoved = false;
        }

        protected override EntityState MakeState(uint tick, OnlineResource resource)
        {
            if (resource is WorldSession ws && !OnlineManager.lobby.gameMode.ShouldSyncObjectInWorld(ws, apo)) throw new InvalidOperationException("asked for world state, not synched");
            if (resource is RoomSession rs && !OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, apo)) throw new InvalidOperationException("asked for room state, not synched");
            var realizedState = resource is RoomSession;
            if (realizedState && isMine && apo.realizedObject != null && !realized) { RainMeadow.Error($"have realized object, but not entity not marked as realized??: {this} in resource {resource}"); }
            if (realizedState && isMine && !realized)
            {
                //RainMeadow.Error($"asked for realized state, not realized: {this} in resource {resource}");
                realizedState = false;
            }
            return new PhysicalObjectEntityState(this, tick, realizedState);
        }

        public override void OnJoinedResource(OnlineResource inResource, EntityState initialState)
        {
            base.OnJoinedResource(inResource, initialState);
            if (isMine) return; // already moved
            RainMeadow.Debug($"{this} moving in {inResource}");
            if (inResource is WorldSession ws)
            {
                beingMoved = true;
                ws.world.GetAbstractRoom(this.apo.pos).AddEntity(apo);
                beingMoved = false;
            }
            else if (inResource is RoomSession newRoom)
            {
                if (apo is not AbstractCreature creature)
                {
                    if (newRoom.absroom.realizedRoom is Room realizedRoom)
                    {
                        if (apo.realizedObject != null && realizedRoom.updateList.Contains(apo.realizedObject))
                        {
                            RainMeadow.Debug($"Entity {apo.ID} already in the room {newRoom.absroom.name}, not adding!");
                            return;
                        }

                        RainMeadow.Debug($"Spawning entity: {apo.ID}");
                        beingMoved = true;
                        apo.RealizeInRoom();
                        beingMoved = false;
                    }
                    return;
                }

                if (newRoom.absroom.realizedRoom is Room realRoom && creature.AllowedToExistInRoom(realRoom))
                {
                    if (creature.realizedCreature != null && realRoom.updateList.Contains(creature.realizedCreature))
                    {
                        RainMeadow.Debug($"Creature {creature.ID} already in the room {newRoom.absroom.name}, not adding!");
                        return;
                    }

                    RainMeadow.Debug("spawning creature " + creature);
                    if (apo.pos.TileDefined)
                    {
                        RainMeadow.Debug("added directly to the room");
                        beingMoved = true;
                        creature.RealizeInRoom(); // places in room
                        beingMoved = false;
                    }
                    else if (apo.pos.NodeDefined)
                    {
                        RainMeadow.Debug("added directly to shortcut system");
                        beingMoved = true;
                        creature.Realize();
                        beingMoved = false;
                        creature.realizedCreature.inShortcut = true;
                        // this calls MOVE on the next tick which remove-adds
                        newRoom.absroom.world.game.shortcuts.CreatureEnterFromAbstractRoom(creature.realizedCreature, newRoom.absroom, apo.pos.abstractNode);
                    }
                    else
                    {
                        RainMeadow.Debug("INVALID POS??" + apo.pos);
                        throw new InvalidOperationException("entity must have a vaild position");
                    }
                }
                else
                {
                    RainMeadow.Debug("not spawning creature " + creature);
                    RainMeadow.Debug($"reasons {newRoom.absroom.realizedRoom is not null} {(newRoom.absroom.realizedRoom != null && creature.AllowedToExistInRoom(newRoom.absroom.realizedRoom))}");
                    if (creature.realizedCreature != null)
                    {
                        if (!apo.pos.TileDefined && apo.pos.NodeDefined && newRoom.absroom.realizedRoom != null && newRoom.absroom.realizedRoom.shortCutsReady)
                        {
                            RainMeadow.Debug("added realized creature to shortcut system");
                            creature.realizedCreature.inShortcut = true;
                            // this calls MOVE on the next tick which remove-adds, this could be bad?
                            newRoom.absroom.world.game.shortcuts.CreatureEnterFromAbstractRoom(creature.realizedCreature, newRoom.absroom, apo.pos.abstractNode);
                        }
                        else
                        {
                            // can't abstractize properly because previous location is lost
                            RainMeadow.Debug("cleared realized creature and added to absroom as abstract entity");
                            creature.realizedCreature = null;
                        }
                    }
                    else
                    {
                        RainMeadow.Debug("added to absroom as abstract entity");
                    }
                }
            }
        }

        public override void OnLeftResource(OnlineResource inResource)
        {
            bool unregister = inResource == primaryResource;
            base.OnLeftResource(inResource);
            if (!isMine)
            {
                if (inResource is RoomSession oldRoom)
                {
                    RainMeadow.Debug(this);
                    if (roomSession == oldRoom)
                    {
                        RainMeadow.Debug("Removing entity from room: " + this);
                        beingMoved = true;
                        oldRoom.absroom.RemoveEntity(apo);
                        if (apo.realizedObject is PhysicalObject po)
                        {
                            if (oldRoom.absroom.realizedRoom is Room room)
                            {
                                room.RemoveObject(po);
                                room.CleanOutObjectNotInThisRoom(po);
                            }
                            if (po is Creature c && c.inShortcut)
                            {
                                if (c.RemoveFromShortcuts()) c.inShortcut = false;
                            }
                        }
                        beingMoved = false;
                    }
                }
            }
            if(unregister)
            {
                map.Remove(apo);
                OnlineManager.recentEntities.Remove(id);
            }
        }

        public override string ToString()
        {
            return apo.ToString() + base.ToString();
        }
    }
}

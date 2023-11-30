using System;
using System.Linq;
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
            RainMeadow.Debug("Registered new entity - " + newOe.ToString());
            return newOe;
        }

        public static OnlinePhysicalObject NewFromApo(AbstractPhysicalObject apo)
        {
            if (apo is AbstractCreature ac)
            {
                var def = new OnlineCreatureDefinition(apo.ID.RandomSeed, apo.realizedObject != null, SaveState.AbstractCreatureToStringStoryWorld(ac), new OnlineEntity.EntityId(OnlineManager.mePlayer.inLobbyId, EntityId.IdType.apo, apo.ID.number), OnlineManager.mePlayer, !RainMeadow.sSpawningAvatar);
                return new OnlineCreature(def, ac);
            }
            else
            {
                var def = new OnlinePhysicalObjectDefinition(apo.ID.RandomSeed, apo.realizedObject != null, apo.ToString(), new OnlineEntity.EntityId(OnlineManager.mePlayer.inLobbyId, EntityId.IdType.apo, apo.ID.number), OnlineManager.mePlayer, !RainMeadow.sSpawningAvatar);
                return new OnlinePhysicalObject(def, apo);
            }
        }

        public OnlinePhysicalObject(OnlinePhysicalObjectDefinition entityDefinition, AbstractPhysicalObject apo) : base(entityDefinition)
        {
            this.apo = apo;
            this.seed = entityDefinition.seed;
            this.realized = entityDefinition.realized;
            map.Add(apo, this);
        }

        public override void NewOwner(OnlinePlayer newOwner)
        {
            base.NewOwner(newOwner);
            if (newOwner.isMe)
            {
                realized = apo.realizedObject != null; // owner is responsible for upkeeping this
            }
        }

        public static OnlineEntity FromDefinition(OnlinePhysicalObjectDefinition newObjectEvent, OnlineResource inResource)
        {
            World world = inResource.World;
            EntityID id = world.game.GetNewID();
            id.altSeed = newObjectEvent.seed;

            RainMeadow.Debug("serializedObject: " + newObjectEvent.serializedObject);
            var apo = SaveState.AbstractPhysicalObjectFromString(world, newObjectEvent.serializedObject);
            apo.ID = id;

            return new OnlinePhysicalObject(newObjectEvent, apo);
        }

        public override void ReadState(EntityState entityState, OnlineResource inResource)
        {
            beingMoved = true;
            base.ReadState(entityState, inResource);
            beingMoved = false;
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new PhysicalObjectEntityState(this, inResource, tick);
        }

        public override void OnJoinedResource(OnlineResource inResource)
        {
            base.OnJoinedResource(inResource);
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
                        creature.realizedCreature.RemoveFromShortcuts();
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
            base.OnLeftResource(inResource);
            if (!isMine)
            {
                if (inResource is RoomSession rs)
                {
                    RainMeadow.Debug("Removing entity from room: " + this);
                    beingMoved = true;
                    rs.absroom.RemoveEntity(apo);
                    if (apo.realizedObject is PhysicalObject po)
                    {
                        if (rs.absroom.realizedRoom is Room room)
                        {
                            room.RemoveObject(po);
                            room.CleanOutObjectNotInThisRoom(po);
                        }
                        if (po is Creature c && c.inShortcut && !joinedResources.Any(r => r is RoomSession))
                        {
                            if (c.RemoveFromShortcuts()) c.inShortcut = false;
                        }
                    }
                    beingMoved = false;
                }
                if (primaryResource == null) // gone
                {
                    RainMeadow.Debug("Removing entity from game: " + this);
                    beingMoved = true;
                    apo.Destroy();
                    apo.Room?.RemoveEntity(apo);
                    beingMoved = false;
                }
            }
            if (primaryResource == null)
            {
                RainMeadow.Debug("Removing entity from OnlinePhysicalObject.map: " + this);

                map.Remove(apo);
            }
        }

        public override string ToString()
        {
            return apo.ToString() + base.ToString();
        }
    }
}

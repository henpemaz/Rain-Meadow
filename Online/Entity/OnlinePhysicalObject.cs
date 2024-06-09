using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RainMeadow
{
    public class OnlinePhysicalObject : OnlineEntity
    {
        public class OnlinePhysicalObjectDefinition : EntityDefinition
        {
            [OnlineField]
            public string serializedObject;

            public OnlinePhysicalObjectDefinition() { }

            public OnlinePhysicalObjectDefinition(OnlinePhysicalObject onlinePhysicalObject, OnlineResource inResource) : base(onlinePhysicalObject, inResource)
            {
                serializedObject = onlinePhysicalObject.apo.ToString();
            }

            public override OnlineEntity MakeEntity(OnlineResource inResource, OnlineEntity.EntityState initialState)
            {
                return new OnlinePhysicalObject(this, inResource, (PhysicalObjectEntityState)initialState);
            }
        }

        public readonly AbstractPhysicalObject apo;
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
            EntityId entityId = new OnlineEntity.EntityId(OnlineManager.mePlayer.inLobbyId, EntityId.IdType.apo, apo.ID.number);
            if (OnlineManager.recentEntities.ContainsKey(entityId))
            {
                RainMeadow.Error($"entity with repeated ID: {entityId}");
                var origid = apo.ID;
                var newid = apo.world.game.GetNewID();
                newid.spawner = origid.spawner;
                newid.altSeed = origid.RandomSeed;
                apo.ID = newid;
                entityId = new OnlineEntity.EntityId(OnlineManager.mePlayer.inLobbyId, EntityId.IdType.apo, apo.ID.number);
                RainMeadow.Error($"set as: {entityId}");
            }

            switch (apo)
            {
                case AbstractMeadowCollectible:
                    return new OnlineMeadowCollectible(apo, entityId, OnlineManager.mePlayer, !RainMeadow.sSpawningAvatar);
                case AbstractCreature ac:
                    return new OnlineCreature(ac, entityId, OnlineManager.mePlayer, !RainMeadow.sSpawningAvatar);
                case AbstractConsumable acm:
                    return OnlineConsumableFromAcm(acm, entityId, OnlineManager.mePlayer, !RainMeadow.sSpawningAvatar);
                default:
                    return new OnlinePhysicalObject(apo, entityId, OnlineManager.mePlayer, !RainMeadow.sSpawningAvatar);
                case null:
                    throw new ArgumentNullException(nameof(apo));
            }
        }

        private static OnlineConsumable OnlineConsumableFromAcm(AbstractConsumable acm, EntityId entityId, OnlinePlayer owner, bool isTransferable) 
        {
            switch (acm) {
                case BubbleGrass.AbstractBubbleGrass abg:
                    return new OnlineBubbleGrass(abg, entityId, owner, isTransferable);
                case SeedCob.AbstractSeedCob asc:
                    return new OnlineSeedCob(asc, entityId, owner, isTransferable);
                case SporePlant.AbstractSporePlant asp:
                    return new OnlineSporePlant(asp, entityId, owner, isTransferable);
                case PebblesPearl.AbstractPebblesPearl app:
                    return new OnlinePebblesPearl(app, entityId, owner, isTransferable);
                default:
                    return new OnlineConsumable(acm, entityId, owner, isTransferable);
                case null:
                    throw new ArgumentNullException(nameof(acm));
            }
        }

        protected virtual AbstractPhysicalObject ApoFromDef(OnlinePhysicalObjectDefinition newObjectEvent, OnlineResource inResource, PhysicalObjectEntityState initialState)
        {
            World world = inResource is RoomSession rs ? rs.World : inResource is WorldSession ws ? ws.world : throw new InvalidProgrammerException("not room nor world");
            EntityID id = world.game.GetNewID();

            RainMeadow.Debug("serializedObject: " + newObjectEvent.serializedObject);

            var apo = SaveState.AbstractPhysicalObjectFromString(world, newObjectEvent.serializedObject);
            id.altSeed = apo.ID.RandomSeed;
            apo.ID = id;
            apo.pos = initialState.pos;
            return apo;
        }

        public OnlinePhysicalObject(AbstractPhysicalObject apo, EntityId id, OnlinePlayer owner, bool isTransferable) : base(id, owner, isTransferable)
        {
            this.apo = apo;
            realized = apo.realizedObject != null;
            map.Add(apo, this);
        }

        public OnlinePhysicalObject(OnlinePhysicalObjectDefinition entityDefinition, OnlineResource inResource, PhysicalObjectEntityState initialState) : base(entityDefinition, inResource, initialState)
        {
            this.apo = ApoFromDef(entityDefinition, inResource, initialState);
            realized = initialState.realized;
            map.Add(apo, this);
        }

        internal override EntityDefinition MakeDefinition(OnlineResource onlineResource)
        {
            return new OnlinePhysicalObjectDefinition(this, onlineResource);
        }

        public override void NewOwner(OnlinePlayer newOwner)
        {
            base.NewOwner(newOwner);
            if (newOwner.isMe)
            {
                realized = apo.realizedObject != null; // owner is responsible for upkeeping this
            }
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
                beingMoved = true;
                newRoom.World.GetAbstractRoom(this.apo.pos).AddEntity(apo);
                beingMoved = false;
                if (apo is not AbstractCreature creature)
                {
                    if (newRoom.absroom.realizedRoom is Room realizedRoom && realizedRoom.shortCutsReady)
                    {
                        if (apo.realizedObject != null && realizedRoom.updateList.Contains(apo.realizedObject))
                        {
                            RainMeadow.Debug($"Entity {this} already in the room {newRoom.absroom.name}, not adding!");
                            return;
                        }

                        // todo carried by other won't pick up if entering from abstract, how fix?
                        if(apo.realizedObject != null && apo.realizedObject.grabbedBy.Count > 0)
                        {
                            RainMeadow.Debug($"Entity {this} carried by other, not adding!");
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

                    // TODO creature carrying objects should marks objects as being moved so they run move code
                    //  right now this spits more errors than it should
                    // TODO creature spawning from abstract needs grasp data available to do the object-carrying

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
                    //apo.Destroy();
                    apo.LoseAllStuckObjects();
                    apo.Room?.RemoveEntity(apo);
                    beingMoved = false;
                }
            }
        }

        public override void Deregister()
        {
            base.Deregister();
            RainMeadow.Debug("Removing entity from OnlinePhysicalObject.map: " + this);
            map.Remove(apo);
        }

        public override string ToString()
        {
            return apo.ToString() + base.ToString();
        }

        [RPCMethod]
        public static void HitByWeapon(OnlinePhysicalObject objectHit, OnlinePhysicalObject weapon)
        {
            objectHit?.apo.realizedObject.HitByWeapon(weapon.apo.realizedObject as Weapon);
        }
        [RPCMethod]
        public static void HitByExplosion(OnlinePhysicalObject objectHit, float hitfac)
        {
            objectHit?.apo.realizedObject.HitByExplosion(hitfac,null,0);
        }
    }
}

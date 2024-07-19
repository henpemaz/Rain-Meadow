using System;
using System.Linq;
using System.Runtime.CompilerServices;
using static RainMeadow.OnlineCreature;
using System.Runtime.InteropServices.ComTypes;

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
                    OnlineCreatureDefinition acDef;
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

        protected void AllMoving(bool set)
        {
            var all = apo.GetAllConnectedObjects();
            for (int i = 0; i < all.Count; i++)
            {
                var otherapo = all[i];
                if (otherapo != null && map.TryGetValue(otherapo, out var otheropo))
                {
                    otheropo.beingMoved = set;
                }
            }
        }

        protected override void JoinImpl(OnlineResource inResource, EntityState initialState)
        {
            var poState = initialState as PhysicalObjectEntityState;
            var topos = poState.pos;
            var waspos = apo.pos;

            // so here I was thinking maybe we disconnect everything as things get moved so the game doesn't chain-move them?
            // basically on joinimpl AND leaveimpl always de-stuck any sticks both abstract and real
            // but right now we're missing most of the tech for recreating them though pretty much we only handle creaturegrasps?
            // if we want to re-stick things then that information needs to go through somehow and needs to be move versatile than the current thing
            
            RainMeadow.Debug($"{this} joining {inResource}");
            RainMeadow.Debug($"from {waspos} to {poState.pos}");
            try
            {
                AllMoving(true);
                if (inResource is WorldSession ws)
                {
                    RainMeadow.Debug($"world join");
                    apo.Move(topos);
                }
                else if (inResource is RoomSession newRoom)
                {
                    RainMeadow.Debug($"room join");
                    if (apo.realizedObject is PhysicalObject po)
                    {
                        RainMeadow.Debug($"already realized");
                        apo.Move(topos);
                        if (apo is AbstractCreature ac)
                        {
                            topos = QuickConnectivity.DefineNodeOfLocalCoordinate(poState.pos, ac.world, ac.creatureTemplate);
                            if (ac.AllowedToExistInRoom(newRoom.absroom.realizedRoom))
                            {
                                RainMeadow.Debug($"late creature");
                                ac.realizedCreature.inShortcut = true;
                                ac.world.game.shortcuts.CreatureEnterFromAbstractRoom(ac.realizedCreature, newRoom.absroom, topos.abstractNode);
                            }
                            else
                            {
                                RainMeadow.Debug($"early creature");
                                ac.Abstractize(topos);
                            }
                        }
                        else
                        {
                            RainMeadow.Debug($"object to place");
                            po.PlaceInRoom(newRoom.absroom.realizedRoom);
                        }
                    }
                    else // apo.realizedObject == null
                    {
                        RainMeadow.Debug($"not realized");
                        if (apo is AbstractCreature ac)
                        {
                            RainMeadow.Debug($"creature enter");
                            // creatures will properly enter from node, gnuff specially if we add spit-out-of-sc events
                            topos = QuickConnectivity.DefineNodeOfLocalCoordinate(poState.pos, ac.world, ac.creatureTemplate);
                            if (topos.CompareDisregardingTile(ac.pos))
                            {
                                ac.ChangeRooms(topos);
                            }
                            else { ac.Move(topos); }
                        }
                        else
                        {
                            RainMeadow.Debug($"obj enter");
                            // regular apos do not
                            apo.Move(topos);
                            if (apo.realizedObject == null && !apo.InDen && !apo.GetAllConnectedObjects().Any(other => other is AbstractCreature)) apo.RealizeInRoom();
                        }
                    }
                }
                AllMoving(false);
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                apo.world.GetAbstractRoom(apo.pos)?.RemoveEntity(apo); // safe enough
                apo.world.GetAbstractRoom(poState.pos)?.AddEntity(apo);
                apo.pos = poState.pos;
                AllMoving(false);
                //throw;
            }
        }

        protected override void LeaveImpl(OnlineResource inResource)
        {
            RainMeadow.Debug($"{this} leaving {inResource}");
            try
            {
                AllMoving(true);
                if (primaryResource == null) // gone
                {
                    RainMeadow.Debug("Removing entity from game: " + this);
                    apo.LoseAllStuckObjects();
                    apo.Room?.RemoveEntity(apo);
                }
                if (inResource is RoomSession rs)
                {
                    RainMeadow.Debug("Removing entity from room: " + this);
                    rs.absroom.RemoveEntity(apo);
                    if (apo.realizedObject is PhysicalObject po)
                    {
                        if (rs.absroom.realizedRoom is Room room)
                        {
                            room.RemoveObject(po);
                            room.CleanOutObjectNotInThisRoom(po);
                        }
                        if (po is Creature c && c.inShortcut)
                        {
                            c.RemoveFromShortcuts();
                        }
                    }
                }
                AllMoving(false);
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                apo.realizedObject?.RemoveFromRoom();
                apo.world.GetAbstractRoom(apo.pos)?.RemoveEntity(apo);
                AllMoving(false);
                //throw;
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
            objectHit?.apo.realizedObject.HitByExplosion(hitfac, null, 0);
        }
    }
}
using System;
using System.Linq;
using UnityEngine;
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
                var wasId = onlinePhysicalObject.apo.ID.number;
                onlinePhysicalObject.apo.ID.number = onlinePhysicalObject.apo.ID.RandomSeed;
                serializedObject = onlinePhysicalObject.apo.ToString();
                onlinePhysicalObject.apo.ID.number = wasId;
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
            bool transferable = !RainMeadow.sSpawningAvatar;
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
                    return new OnlineMeadowCollectible(apo, entityId, OnlineManager.mePlayer, transferable);
                case AbstractCreature ac:
                    return new OnlineCreature(ac, entityId, OnlineManager.mePlayer, transferable);
                case AbstractConsumable acm:
                    if (IsTypeConsumable(apo.type)) return OnlineConsumableFromAcm(acm, entityId, OnlineManager.mePlayer, transferable);
                    else
                    {
                        RainMeadow.Error("object has AbstractConsumable but type is not consumable: " + apo.type);
                        goto default; // screw you, trader-spawned scavengerbomb
                    }
                default:
                    return new OnlinePhysicalObject(apo, entityId, OnlineManager.mePlayer, transferable);
                case null:
                    throw new ArgumentNullException(nameof(apo));
            }
        }

        private static bool IsTypeConsumable(AbstractPhysicalObject.AbstractObjectType type)
        {
            if(AbstractConsumable.IsTypeConsumable(type)) return true;

            if(type == AbstractPhysicalObject.AbstractObjectType.SeedCob) return true; // un fucking believable

            return false;
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
            id.altSeed = apo.ID.RandomSeed; // this becomes a problem on transfers, the altSeed field is not used by vanila serialize, need to be sent as new field
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
            AllMoving(true);
            base.ReadState(entityState, inResource);
            AllMoving(false);
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
            // on joinimpl we've just read initialstate as well so some stuff is already set
            var poState = initialState as PhysicalObjectEntityState;
            var topos = poState.pos;
            
            RainMeadow.Debug($"{this} joining {inResource} at {poState.pos}");
            try
            {
                AllMoving(true);
                if (inResource is WorldSession ws)
                {
                    RainMeadow.Debug($"world join");
                    apo.world = ws.world;
                    apo.pos = poState.pos;
                    ws.world.GetAbstractRoom(topos)?.AddEntity(apo);
                    if (poState.inDen) ws.world.GetAbstractRoom(topos)?.MoveEntityToDen(apo);
                    apo.InDen = poState.inDen;
                }
                else if (inResource is RoomSession newRoom)
                {
                    RainMeadow.Debug($"room join");
                    if (!poState.inDen && apo.pos.room != -1) // inden entities are basically abstracted so not added to the room
                                                              // room == -1 signals swallowed item which shouldn't be in room
                    {
                        if (apo is AbstractCreature ac && !ac.AllowedToExistInRoom(newRoom.absroom.realizedRoom))
                        {
                            RainMeadow.Debug($"early creature");
                            apo.Move(topos);
                            if (apo.realizedObject is PhysicalObject po)
                            {
                                // this line might be problematic because room.cleanout calls apo.Destroy
                                // need a better way to guarantee a realized thing isn't added in 2 different rooms
                                po.slatedForDeletetion = true; // if it ends up in a room somehow (dragged by other?), duplicates = bad
                                po.RemoveFromRoom();
                            }
                            if (apo.realizedObject is Creature c)
                            {
                                c.RemoveFromShortcuts();
                            }
                            apo.Abstractize(topos);
                        }
                        else // creature allowed or notcreature
                        {
                            if (topos.TileDefined)
                            {
                                apo.Move(topos);
                                if (apo.realizedObject is Creature c)
                                {
                                    c.RemoveFromShortcuts();
                                }
                                if (newRoom.absroom.realizedRoom.shortCutsReady)
                                {
                                    RainMeadow.Debug($"spawning in room");
                                    apo.RealizeInRoom(); // placesinroom
                                }
                                else
                                {
                                    RainMeadow.Debug($"early entity"); // room loading will place it
                                }
                            }
                            else // nodedefined
                            {
                                RainMeadow.Debug("node defined");
                                apo.Move(topos);
                                if (apo.realizedObject is Creature c)
                                {
                                    c.RemoveFromShortcuts();
                                }
                                if (apo is AbstractCreature ac2) // Creature.ChangeRoom didn't run, so we do it manually
                                {
                                    RainMeadow.Debug("creature moved");
                                    if (ac2.realizedCreature == null || !ac2.realizedCreature.inShortcut)
                                    {
                                        RainMeadow.Debug($"spawning in shortcuts");
                                        ac2.Realize();
                                        ac2.realizedCreature.inShortcut = true;
                                        ac2.world.game.shortcuts.CreatureEnterFromAbstractRoom(ac2.realizedCreature, ac2.world.GetAbstractRoom(topos), topos.abstractNode);
                                    }
                                    else
                                    {
                                        RainMeadow.Debug($"supposedly already spawning in shortcuts");
                                        RainMeadow.Debug("found in shortcuts? " + (ac2.realizedCreature != null && apo.world.game.shortcuts.betweenRoomsWaitingLobby.Any(v => v.creature.abstractCreature.GetAllConnectedObjects().Any(o => o.realizedObject == ac2.realizedCreature))));
                                    }
                                }
                                else
                                {
                                    RainMeadow.Debug($"regular item, spawning off-room");
                                    apo.Realize();
                                    // and lets leave it at that, some creechur will connect to it and drag it in-room
                                }
                            }
                        }
                    } // inDen
                    // else not needed

                    if (apo.pos.room == -1)
                    {
                        // shouldn't happen, swallowed item leaves room
                        // might happen for a few frames during leave transac though?
                        RainMeadow.Error($"{this} in {newRoom} has room -1 assigned!");
                    }
                }
                AllMoving(false);
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
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
                    if(apo.realizedObject is PhysicalObject po)
                    {
                        if (apo.Room?.realizedRoom is Room room)
                        {
                            room.RemoveObject(po);
                            room.CleanOutObjectNotInThisRoom(po);
                        }
                        if (po is Creature c && c.inShortcut)
                        {
                            RainMeadow.Debug("removing from shortcuts");
                            c.RemoveFromShortcuts();
                        }
                    }
                }
                if (inResource is RoomSession rs)
                {
                    RainMeadow.Debug("Removing entity from room: " + this);
                    if (apo.realizedObject is PhysicalObject po)
                    {
                        if (rs.absroom.realizedRoom is Room room)
                        {
                            room.RemoveObject(po);
                            room.CleanOutObjectNotInThisRoom(po);
                        }
                        if (po is Creature c && c.inShortcut)
                        {
                            RainMeadow.Debug("removing from shortcuts");
                            c.RemoveFromShortcuts();
                        }
                    }
                    rs.absroom.RemoveEntity(apo);
                }
                AllMoving(false);
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                apo.realizedObject?.RemoveFromRoom();
                (apo.realizedObject as Creature)?.RemoveFromShortcuts();
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
            foreach (var item in apo.stuckObjects)
            {
                AbstractObjStickRepr.map.Remove(item);
            }
        }

        public override string ToString()
        {
            return $"{apo?.type} {base.ToString()}";
        }

        [RPCMethod]
        public static void HitByWeapon(OnlinePhysicalObject objectHit, OnlinePhysicalObject weapon)
        {
            objectHit?.apo.realizedObject?.HitByWeapon(weapon.apo.realizedObject as Weapon);
        }
        [RPCMethod]
        public static void HitByExplosion(OnlinePhysicalObject objectHit, float hitfac)
        {
            objectHit?.apo.realizedObject?.HitByExplosion(hitfac, null, 0);
        }

        [RPCMethod]
        public static void ScavengerBombExplode(OnlinePhysicalObject opo, Vector2 pos)
        {
            if (opo?.apo.realizedObject is not ScavengerBomb bomb) return;

            bomb.bodyChunks[0].pos = pos;
            bomb.Explode(null);
        }
    }
}

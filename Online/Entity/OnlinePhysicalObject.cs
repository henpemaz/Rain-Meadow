using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

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

            var opoDef = new OnlinePhysicalObjectDefinition(apo.ID.RandomSeed, apo.realizedObject != null, apo.ToString(), entityId, OnlineManager.mePlayer, !RainMeadow.sSpawningAvatar);

            switch (apo)
            {
                case AbstractSpear:
                    //may break with downpour
                    return new OnlinePhysicalObject(opoDef, apo);
                case VultureMask.AbstractVultureMask:
                    //May break with downpour
                    return new OnlinePhysicalObject(opoDef, apo);
                case AbstractCreature ac:
                    var acDef = new OnlineCreatureDefinition(ac.ID.RandomSeed, ac.realizedObject != null, SaveState.AbstractCreatureToStringStoryWorld(ac), entityId, OnlineManager.mePlayer, !RainMeadow.sSpawningAvatar);
                    return new OnlineCreature(acDef, ac);
                case AbstractConsumable acm:
                    return OnlineConsumableFromAcm(acm,opoDef);
                default:
                    return new OnlinePhysicalObject(opoDef, apo);
                case null:
                    throw new ArgumentNullException(nameof(apo));
            }
        }

        private static OnlineConsumable OnlineConsumableFromAcm(AbstractConsumable acm, OnlinePhysicalObjectDefinition opoDef) 
        {
            var ocmDef = new OnlineConsumableDefinition(opoDef, acm);
            switch (acm) {
                case BubbleGrass.AbstractBubbleGrass abg:
                    var abgDef = new OnlineBubbleGrassDefinition(ocmDef, abg);
                    return new OnlineBubbleGrass(abgDef, abg);
                case SeedCob.AbstractSeedCob asc:
                    var ascDef = new OnlineSeedCobDefinition(ocmDef, asc);
                    return new OnlineSeedCob(ascDef, asc);
                case SporePlant.AbstractSporePlant asp:
                    var aspDef = new OnlineSporePlantDefinition(ocmDef, asp);
                    return new OnlineSporePlant(aspDef, asp);
                case PebblesPearl.AbstractPebblesPearl app:
                    var appDef = new OnlinePebblesPearlDefinition(ocmDef, app);
                    return new OnlinePebblesPearl(appDef, app);
                default:
                    return new OnlineConsumable(ocmDef, acm);
                case null:
                    throw new ArgumentNullException(nameof(acm));
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
            World world = inResource is RoomSession rs ? rs.World : inResource is WorldSession ws ? ws.world : throw new InvalidProgrammerException("not room nor world");
            EntityID id = world.game.GetNewID();
            id.altSeed = newObjectEvent.seed;

            RainMeadow.Debug("serializedObject: " + newObjectEvent.serializedObject);

            var apo = SaveState.AbstractPhysicalObjectFromString(world, newObjectEvent.serializedObject);
            apo.ID = id;
            if (!world.IsRoomInRegion(apo.pos.room))
            {
                RainMeadow.Debug("Room not in region: " + apo.pos.room);
                // most common cause is gates which are ambiguous room names, solve for current region instead of global
                string[] obarray = Regex.Split(newObjectEvent.serializedObject, "<oA>");
                string[] wcarray = obarray[2].Split('.');
                AbstractRoom room = world.GetAbstractRoom(wcarray[0]);
                if (room != null)
                {
                    RainMeadow.Debug($"fixing room index -> {room.index}");
                    apo.pos.room = room.index;
                }
                else
                {
                    RainMeadow.Error("Couldn't find room in region: " + wcarray[0]);
                }
            }

            RainMeadow.Debug($"room index -> {apo.pos.room} in region? {world.IsRoomInRegion(apo.pos.room)}");
            

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
        public static void HitByExplosion(OnlinePhysicalObject objectHit, OnlinePhysicalObject sourceObject, Vector2 pos, int lifeTime, float rad, float force, float damage, float stun, float deafen, OnlinePhysicalObject killTagHolder, float killTagHolderDmgFactor, float minStun, float backgroundNoise, float hitfac)
        {
            var source = (sourceObject.apo.realizedObject);

            var creature = (killTagHolder.apo as AbstractCreature).realizedCreature;
            var explosion = new Explosion(source.room, source, pos, lifeTime, rad, force, damage, stun, deafen, creature, killTagHolderDmgFactor, minStun, backgroundNoise);
            objectHit?.apo.realizedObject.HitByExplosion(hitfac, explosion, 0);

        }
    }
}

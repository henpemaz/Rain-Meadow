using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    public class OnlinePhysicalObject : OnlineEntity
    {
        public readonly AbstractPhysicalObject apo;
        public readonly int seed;
        public bool realized;
        public WorldCoordinate enterPos; // todo keep this updated, currently loading with creatures mid-room still places them in shortcuts
        public bool beingMoved;
        public static ConditionalWeakTable<AbstractPhysicalObject, OnlineEntity> map = new();

        internal static OnlinePhysicalObject RegisterPhysicalObject(AbstractPhysicalObject apo)
        {
            RainMeadow.Debug("Registering new entity as owned by myself");
            var newOe = new OnlinePhysicalObject(apo, apo.ID.RandomSeed, apo.pos, PlayersManager.mePlayer, new OnlineEntity.EntityId(PlayersManager.mePlayer.id.m_SteamID, apo.ID.number), !RainMeadow.sSpawningPersonas);
            RainMeadow.Debug(newOe);
            OnlineManager.recentEntities[newOe.id] = newOe;
            OnlinePhysicalObject.map.Add(apo, newOe);
            return newOe;
        }

        public OnlinePhysicalObject(AbstractPhysicalObject apo, int seed, WorldCoordinate pos, OnlinePlayer owner, EntityId id, bool isTransferable) : base(owner, id, isTransferable)
        {
            this.apo = apo;
            this.seed = seed;
            this.enterPos = pos;
            this.realized = apo.realizedObject != null; // todo do we really initialize this
        }

        public override void NewOwner(OnlinePlayer newOwner)
        {
            base.NewOwner(newOwner);
            if (newOwner.isMe)
            {
                realized = apo.realizedObject != null; // owner is responsible for upkeeping this
            }
        }

        internal override NewEntityEvent AsNewEntityEvent(OnlineResource onlineResource)
        {
            throw new NotImplementedException();
        }
    }

    public abstract partial class OnlineEntity
    {
        public OnlinePlayer owner;
        public readonly EntityId id;
        public readonly bool isTransferable;

        public List<OnlineResource> joinedResources; // used like a stack
        public List<OnlineResource> locallyEnteredResources; // used like a stack
        public OnlineResource highestResource => locallyEnteredResources?[0];
        public OnlineResource lowestResource => locallyEnteredResources?[locallyEnteredResources.Count - 1];
        
        public bool isPending => pendingRequest != null;
        public OnlineEvent pendingRequest;
        public PlayerTickReference joinedAt;

        protected OnlineEntity(OnlinePlayer owner, EntityId id, bool isTransferable)
        {
            this.owner = owner;
            this.id = id;
            this.isTransferable = isTransferable;
        }

        public void EnterResourceLocally(OnlineResource resource)
        {
            // todo handle leaving same-level resource when joining (I guess if remote)
            // but why do we even keep track of this for non-local?
            if (locallyEnteredResources.Count != 0 && resource.super != lowestResource) throw new InvalidOperationException("not entering a subresource");
            locallyEnteredResources.Add(resource);
            if (owner.isMe) JoinPending();
        }

        private void JoinPending()
        {
            if (!owner.isMe) { throw new InvalidProgrammerException("not owner"); }
            if (isPending) { return; } // still pending
            var pending = locallyEnteredResources.FirstOrDefault(r => !r.entities.ContainsKey(this.id));
            if (pending != null)
            {
                pending.LocalEntityEntered(this);
            }
        }

        public void OnJoinedResource(OnlineResource inResource)
        {
            joinedResources.Add(inResource);
            if (owner.isMe) JoinPending();
        }

        internal static OnlineEntity FromNewEntityEvent(NewEntityEvent newEntityEvent, OnlineResource inResource)
        {
            OnlineEntity newOe = null;
            if (newEntityEvent is NewObjectEvent newObjectEvent)
            {
                if (newObjectEvent is NewCreaturetEvent newCreatureEvent)
                {

                }
                else
                {

                }
            }
            else if(newEntityEvent is NewGraspEvent newGraspEvent)
            {

            }
            else
            {

            }

            return newOe;
        }

        internal abstract NewEntityEvent AsNewEntityEvent(OnlineResource onlineResource);

        public virtual void NewOwner(OnlinePlayer newOwner)
        {
            RainMeadow.Debug(this);
            var wasOwner = owner;
            owner = newOwner;

            if (wasOwner.isMe)
            {
                foreach (var res in locallyEnteredResources)
                {
                    OnlineManager.RemoveFeed(res, this);
                }
            }
            if (newOwner.isMe)
            {
                foreach (var res in locallyEnteredResources)
                {
                    OnlineManager.AddFeed(res, this);
                }
            }
        }

        // I was in a resource and I was left behind as the resource was released
        public void Deactivated(OnlineResource onlineResource)
        {
            RainMeadow.Debug(this);
            if (onlineResource != this.lowestResource) throw new InvalidOperationException("still active in subresource");
            locallyEnteredResources.RemoveAt(locallyEnteredResources.Count - 1);
            if (owner.isMe) OnlineManager.RemoveFeed(onlineResource, this);
        }

        public override string ToString()
        {
            return $"{id} from {owner.name}";
        }















        public static OnlineEntity old_CreateOrReuseEntity(old_NewEntityEvent newEntityEvent, World world)
        {
            RainMeadow.DebugMe();
            OnlineEntity oe = null;


            if (world.GetAbstractRoom(newEntityEvent.initialPos) == null)
            {
                RainMeadow.Error($"Room not found!! {newEntityEvent.initialPos}");
            }
            if (OnlineManager.recentEntities.TryGetValue(newEntityEvent.entityId, out oe))
            {
                if (oe.entity.world.game != world.game) throw new InvalidOperationException($"Entity not cleared in last session!! {oe.id}");
                
                RainMeadow.Debug("reusing existing entity " + oe);

                oe.owner = newEntityEvent.owner;
                oe.enterPos = newEntityEvent.initialPos;
                oe.realized = newEntityEvent.realized;

                if (!world.IsRoomInRegion(oe.entity.pos.room))
                {
                    oe.entity.world = world;
                    oe.entity.pos = oe.enterPos;
                    WorldSession.registeringRemoteEntity = true;
                    world.GetAbstractRoom(oe.enterPos).AddEntity(oe.entity);
                    WorldSession.registeringRemoteEntity = false;
                }
                // we don't update other fields because they shouldn't change... in theory...
            }
            else
            {
                RainMeadow.Debug("spawning new entity");
                OnlineManager.recentEntities.Remove(newEntityEvent.entityId);
                // it is very tempting to switch to the generic tostring/fromstring from the savesystem, BUT
                // it would be almost impossible to sanitize input and who knows what someone could do through that
                // EDIT : screw it, we usin' generic string savesystem anyways B)
                
                EntityID id = world.game.GetNewID();
                id.altSeed = newEntityEvent.seed;
                WorldSession.registeringRemoteEntity = true;

                if (!newEntityEvent.isCreature)
                {
                    var abstractPhysicalObject = SaveState.AbstractPhysicalObjectFromString(world, newEntityEvent.saveString);
                    
                    world.GetAbstractRoom(newEntityEvent.initialPos).AddEntity(abstractPhysicalObject);
                    WorldSession.registeringRemoteEntity = false;
                    
                    oe = new OnlineEntity(abstractPhysicalObject, newEntityEvent.owner, newEntityEvent.entityId, newEntityEvent.seed, newEntityEvent.initialPos, newEntityEvent.isTransferable);
                    OnlineEntity.map.Add(abstractPhysicalObject, oe);
                    OnlineManager.recentEntities.Add(newEntityEvent.entityId, oe);
                }
                else
                {
                    AbstractCreature abstractCreature;
                    
                    CreatureTemplate.Type type = new CreatureTemplate.Type(newEntityEvent.template, false);
                    if (type.Index == -1)
                    {
                        RainMeadow.Debug(type);
                        RainMeadow.Debug(newEntityEvent.template);
                        throw new InvalidOperationException("invalid template");
                    }
                    
                    if (type == CreatureTemplate.Type.Slugcat) //todo: fix loading and serializing players?
                    {
                        abstractCreature = new AbstractCreature(world, StaticWorld.GetCreatureTemplate(type), null, newEntityEvent.initialPos, id);
                    }
                    else
                    {
                        abstractCreature = SaveState.AbstractCreatureFromString(world, newEntityEvent.saveString, false);
                    }

                    world.GetAbstractRoom(newEntityEvent.initialPos).AddEntity(abstractCreature);
                    WorldSession.registeringRemoteEntity = false;
                    if (abstractCreature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat) // for some dumb reason it doesn't get a default
                    {
                        abstractCreature.state = new PlayerState(abstractCreature, 0, RainMeadow.Ext_SlugcatStatsName.OnlineSessionRemotePlayer, false);
                    }

                    oe = new OnlineEntity(abstractCreature, newEntityEvent.owner, newEntityEvent.entityId, newEntityEvent.seed, newEntityEvent.initialPos, newEntityEvent.isTransferable);
                    OnlineEntity.map.Add(abstractCreature, oe);
                    OnlineManager.recentEntities.Add(newEntityEvent.entityId, oe);
                }
            }
            oe.realized = newEntityEvent.realized;

            return oe;
        }

        public void old_EnteredRoom(RoomSession newRoom)
        {
            RainMeadow.Debug(this);
            if (roomSession != null && roomSession != newRoom) // still in previous room
            {
                roomSession.old_EntityLeftResource(this);
            }
            roomSession = newRoom;
            if (!owner.isMe)
            {
                RainMeadow.Debug("A remote entity entered, adding it to the room");
                beingMoved = true;
                entity.Move(enterPos);
                beingMoved = false;
                
                if (entity is not AbstractCreature creature)
                {
                    if (newRoom.absroom.realizedRoom is Room realizedRoom)
                    {
                        if (entity.realizedObject != null && realizedRoom.updateList.Contains(entity.realizedObject))
                        {
                            RainMeadow.Debug($"Entity {entity.ID} already in the room {newRoom.absroom.name}, not adding!");
                            return;
                        }
                        
                        RainMeadow.Debug($"Spawning entity: {entity.ID}");
                        beingMoved = true;
                        entity.RealizeInRoom();
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
                    if (enterPos.TileDefined)
                    {
                        RainMeadow.Debug("added directly to the room");
                        beingMoved = true;
                        creature.RealizeInRoom(); // places in room
                        beingMoved = false;
                    }
                    else if (enterPos.NodeDefined)
                    {
                        RainMeadow.Debug("added directly to shortcut system");
                        beingMoved = true;
                        creature.Realize();
                        beingMoved = false;
                        creature.realizedCreature.inShortcut = true;
                        // this calls MOVE on the next tick which remove-adds
                        newRoom.absroom.world.game.shortcuts.CreatureEnterFromAbstractRoom(creature.realizedCreature, newRoom.absroom, enterPos.abstractNode);
                    }
                    else
                    {
                        RainMeadow.Debug("INVALID POS??" + enterPos);
                        throw new InvalidOperationException("entity must have a vaild position");
                    }
                }
                else
                {
                    RainMeadow.Debug("not spawning creature " + creature);
                    RainMeadow.Debug($"reasons {newRoom.absroom.realizedRoom is not null} {(newRoom.absroom.realizedRoom != null && creature.AllowedToExistInRoom(newRoom.absroom.realizedRoom))}");
                    if (creature.realizedCreature != null)
                    {
                        if (!enterPos.TileDefined && enterPos.NodeDefined && newRoom.absroom.realizedRoom != null && newRoom.absroom.realizedRoom.shortCutsReady)
                        {
                            RainMeadow.Debug("added realized creature to shortcut system");
                            creature.realizedCreature.inShortcut = true;
                            // this calls MOVE on the next tick which remove-adds, this could be bad?
                            newRoom.absroom.world.game.shortcuts.CreatureEnterFromAbstractRoom(creature.realizedCreature, newRoom.absroom, enterPos.abstractNode);
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

        public void old_LeftRoom(RoomSession oldRoom)
        {
            RainMeadow.Debug(this);
            if (roomSession == oldRoom)
            {
                if (!owner.isMe)
                {
                    RainMeadow.Debug("Removing entity from room: " + this);
                    beingMoved = true;
                    oldRoom.absroom.RemoveEntity(entity);
                    if (entity.realizedObject is PhysicalObject po)
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
                else
                {
                    RainMeadow.Debug("my own entity leaving");
                }
                roomSession = null;
            }
        }


        public void CreatureViolence(OnlineEntity onlineVillain, int hitchunkIndex, PhysicalObject.Appendage.Pos hitappendage, Vector2? directionandmomentum, Creature.DamageType type, float damage, float stunbonus)
        {
            this.owner.QueueEvent(new CreatureEvent.Violence(onlineVillain, this, hitchunkIndex, hitappendage, directionandmomentum, type, damage, stunbonus));
        }

        public void ForceGrab(OnlineEntity onlineGrabbed, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool pacifying)
        {
            var grabber = (Creature)this.entity.realizedObject;
            var grabbedThing = onlineGrabbed.entity.realizedObject;

            if (grabber.grasps[graspUsed] != null)
            {
                if (grabber.grasps[graspUsed].grabbed == grabbedThing) return;
                grabber.grasps[graspUsed].Release();
            }
            // Will I need to also include the shareability conflict here, too? Idk.
            grabber.grasps[graspUsed] = new Creature.Grasp(grabber, grabbedThing, graspUsed, chunkGrabbed, shareability, dominance, pacifying);
            grabbedThing.Grabbed(grabber.grasps[graspUsed]);
            new AbstractPhysicalObject.CreatureGripStick(grabber.abstractCreature, grabbedThing.abstractPhysicalObject, graspUsed, pacifying || grabbedThing.TotalMass < grabber.TotalMass);
        }
        public void ForceGrab(GraspRef graspRef)
        {
            var castShareability = new Creature.Grasp.Shareability(Creature.Grasp.Shareability.values.GetEntry(graspRef.Shareability));
            ForceGrab(graspRef.OnlineGrabbed, graspRef.GraspUsed, graspRef.ChunkGrabbed, castShareability, graspRef.Dominance, graspRef.Pacifying);
        }
    }
}

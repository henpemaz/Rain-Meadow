using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    public class OnlineCreature : OnlinePhysicalObject
    {
        public OnlineCreature(AbstractCreature ac, int seed, bool realized, WorldCoordinate pos, OnlinePlayer owner, EntityId id, bool isTransferable) : base(ac, seed, realized, pos, owner, id, isTransferable)
        {
            // ? anything special?
        }

        internal static OnlineEntity FromEvent(NewCreatureEvent newCreatureEvent, OnlineResource inResource)
        {
            World world = inResource.World;
            EntityID id = world.game.GetNewID();
            id.altSeed = newCreatureEvent.seed;

            AbstractCreature ac = SaveState.AbstractCreatureFromString(inResource.World, newCreatureEvent.serializedObject, false);
            ac.ID = id;
            if (ac.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat) // for some dumb reason it doesn't get a default
            {
                ac.state = new PlayerState(ac, 0, RainMeadow.Ext_SlugcatStatsName.OnlineSessionRemotePlayer, false);
            }

            var oe = new OnlineCreature(ac, newCreatureEvent.seed, newCreatureEvent.realized, newCreatureEvent.enterPos, newCreatureEvent.owner, newCreatureEvent.entityId, newCreatureEvent.isTransferable);
            OnlinePhysicalObject.map.Add(ac, oe);
            OnlineManager.recentEntities.Add(oe.id, oe);
            return oe;
        }

        public override EntityState GetState(ulong tick, OnlineResource resource)
        {
            if (resource is WorldSession ws && !OnlineManager.lobby.gameMode.ShouldSyncObjectInWorld(ws, apo)) throw new InvalidOperationException("asked for world state, not synched");
            if (resource is RoomSession rs && !OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, apo)) throw new InvalidOperationException("asked for room state, not synched");
            var realizedState = resource is RoomSession;
            if (realizedState) { if (apo.realizedObject != null && !realized) RainMeadow.Error($"have realized object, but not entity not marked as realized??: {this} in resource {resource}"); }
            if (realizedState && !realized)
            {
                //throw new InvalidOperationException("asked for realized state, not realized");
                RainMeadow.Error($"asked for realized state, not realized: {this} in resource {resource}");
                realizedState = false;
            }
            return new AbstractCreatureState(this, tick, realizedState);
        }

        public void CreatureViolence(OnlinePhysicalObject onlineVillain, int hitchunkIndex, PhysicalObject.Appendage.Pos hitappendage, Vector2? directionandmomentum, Creature.DamageType type, float damage, float stunbonus)
        {
            this.owner.QueueEvent(new CreatureEvent.Violence(onlineVillain, this, hitchunkIndex, hitappendage, directionandmomentum, type, damage, stunbonus));
        }

        public void ForceGrab(OnlinePhysicalObject onlineGrabbed, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool pacifying)
        {
            var grabber = (Creature)this.apo.realizedObject;
            var grabbedThing = onlineGrabbed.apo.realizedObject;

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
    public class OnlinePhysicalObject : OnlineEntity
    {
        public readonly AbstractPhysicalObject apo;
        public readonly int seed;
        public bool realized;
        public WorldCoordinate enterPos; // todo keep this updated, currently loading with creatures mid-room still places them in shortcuts

        public bool beingMoved;
        public static ConditionalWeakTable<AbstractPhysicalObject, OnlinePhysicalObject> map = new();

        public RoomSession roomSession => this.lowestResource as RoomSession; // shorthand

        internal static OnlinePhysicalObject RegisterPhysicalObject(AbstractPhysicalObject apo, WorldCoordinate pos)
        {
            RainMeadow.Debug("Registering new entity as owned by myself");
            var newOe = new OnlinePhysicalObject(apo, apo.ID.RandomSeed, apo.realizedObject != null, pos, PlayersManager.mePlayer, new OnlineEntity.EntityId(PlayersManager.mePlayer.id.m_SteamID, apo.ID.number), !RainMeadow.sSpawningPersonas);
            RainMeadow.Debug(newOe);
            OnlineManager.recentEntities[newOe.id] = newOe;
            OnlinePhysicalObject.map.Add(apo, newOe);
            return newOe;
        }

        public OnlinePhysicalObject(AbstractPhysicalObject apo, int seed, bool realized, WorldCoordinate pos, OnlinePlayer owner, EntityId id, bool isTransferable) : base(owner, id, isTransferable)
        {
            this.apo = apo;
            this.seed = seed;
            this.enterPos = pos;
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

        internal override NewEntityEvent AsNewEntityEvent(OnlineResource inResource)
        {
            return new NewObjectEvent(seed, enterPos, realized, apo.ToString(), inResource, this, null);
        }

        internal static OnlineEntity FromEvent(NewObjectEvent newObjectEvent, OnlineResource inResource)
        {
            World world = inResource.World;
            EntityID id = world.game.GetNewID();
            id.altSeed = newObjectEvent.seed;

            var apo = SaveState.AbstractPhysicalObjectFromString(world, newObjectEvent.serializedObject);
            var oe = new OnlinePhysicalObject(apo, newObjectEvent.seed, newObjectEvent.realized, newObjectEvent.enterPos, newObjectEvent.owner, newObjectEvent.entityId, newObjectEvent.isTransferable);
            OnlinePhysicalObject.map.Add(apo, oe);
            OnlineManager.recentEntities.Add(oe.id, oe);
            return oe;
        }

        public override void ReadState(EntityState entityState, ulong tick)
        {
            // todo easing??
            // might need to get a ref to the sender all the way here for lag estimations?
            // todo delta handling
            if (lowestResource is RoomSession && !entityState.realizedState) return; // We can skip abstract state if we're receiving state in a room as well
            beingMoved = true;
            entityState.ReadTo(this);
            beingMoved = false;
            latestState = entityState;
        }

        public override EntityState GetState(ulong tick, OnlineResource resource)
        {
            if (resource is WorldSession ws && !OnlineManager.lobby.gameMode.ShouldSyncObjectInWorld(ws, apo)) throw new InvalidOperationException("asked for world state, not synched");
            if (resource is RoomSession rs && !OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, apo)) throw new InvalidOperationException("asked for room state, not synched");
            var realizedState = resource is RoomSession;
            if (realizedState) { if (apo.realizedObject != null && !realized) RainMeadow.Error($"have realized object, but not entity not marked as realized??: {this} in resource {resource}"); }
            if (realizedState && !realized)
            {
                //throw new InvalidOperationException("asked for realized state, not realized");
                RainMeadow.Error($"asked for realized state, not realized: {this} in resource {resource}");
                realizedState = false;
            }
            return new PhysicalObjectEntityState(this, tick, realizedState);
        }
    }

    public abstract partial class OnlineEntity
    {
        public OnlinePlayer owner;
        public readonly EntityId id;
        public readonly bool isTransferable;

        public List<OnlineResource> joinedResources = new(); // used like a stack
        public List<OnlineResource> enteredResources = new(); // used like a stack
        public OnlineResource highestResource => joinedResources.Count != 0 ? joinedResources[0] : null;
        public OnlineResource lowestResource => joinedResources.Count != 0 ? joinedResources[joinedResources.Count - 1] : null;
        
        public bool isPending => pendingRequest != null;
        public OnlineEvent pendingRequest;

        protected OnlineEntity(OnlinePlayer owner, EntityId id, bool isTransferable)
        {
            this.owner = owner;
            this.id = id;
            this.isTransferable = isTransferable;
        }

        public void EnterResource(OnlineResource resource)
        {
            // todo handle joining same-level resource when joining (I guess if remote)
            // but why do we even keep track of this for non-local?
            if (enteredResources.Count != 0 && resource.super != lowestResource) throw new InvalidOperationException("not entering a subresource");
            enteredResources.Add(resource);
            if (owner.isMe) JoinOrLeavePending();
        }

        public void LeaveResource(OnlineResource resource)
        {
            // todo handle leaving same-level resource when joining (I guess if remote)
            // but why do we even keep track of this for non-local?
            if (enteredResources.Count == 0) throw new InvalidOperationException("not in a resource");
            if (resource != lowestResource) throw new InvalidOperationException("not the right resource");
            enteredResources.Remove(resource);
            if (owner.isMe) JoinOrLeavePending();
        }

        private void JoinOrLeavePending()
        {
            if (!owner.isMe) { throw new InvalidProgrammerException("not owner"); }
            if (isPending) { return; } // still pending
            // any resources to leave
            var pending = joinedResources.Except(enteredResources).FirstOrDefault(r => r.entities.ContainsKey(this));
            if (pending != null)
            {
                pending.LocalEntityLeft(this);
                return;
            }
            // any resources to join
            pending = enteredResources.FirstOrDefault(r => !r.entities.ContainsKey(this));
            if (pending != null)
            {
                pending.LocalEntityEntered(this);
                return;
            }
        }

        public virtual void OnJoinedResource(OnlineResource inResource)
        {
            joinedResources.Add(inResource);
            if (owner.isMe) JoinOrLeavePending();
        }

        public virtual void OnLeftResource(OnlineResource inResource)
        {
            joinedResources.Remove(inResource);
            if (owner.isMe) JoinOrLeavePending();
        }

        internal abstract NewEntityEvent AsNewEntityEvent(OnlineResource onlineResource);

        internal static OnlineEntity FromNewEntityEvent(NewEntityEvent newEntityEvent, OnlineResource inResource)
        {
            if (newEntityEvent is NewObjectEvent newObjectEvent)
            {
                if (newObjectEvent is NewCreatureEvent newCreatureEvent)
                {
                    return OnlineCreature.FromEvent(newCreatureEvent, inResource);
                }
                else
                {
                    return OnlinePhysicalObject.FromEvent(newObjectEvent, inResource);
                }
            }
            //else if(newEntityEvent is NewGraspEvent newGraspEvent)
            //{

            //}
            else
            {
                throw new InvalidOperationException("unknown entity event type");
            }
        }

        public virtual void NewOwner(OnlinePlayer newOwner)
        {
            RainMeadow.Debug(this);
            var wasOwner = owner;
            owner = newOwner;

            if (wasOwner.isMe)
            {
                foreach (var res in enteredResources)
                {
                    OnlineManager.RemoveFeed(res, this);
                }
            }
            if (newOwner.isMe)
            {
                foreach (var res in enteredResources)
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
            enteredResources.RemoveAt(enteredResources.Count - 1);
            if (owner.isMe) OnlineManager.RemoveFeed(onlineResource, this);
        }

        public EntityState latestState;

        public abstract void ReadState(EntityState entityState, ulong tick);

        public abstract EntityState GetState(ulong tick, OnlineResource resource);

        public override string ToString()
        {
            return $"{id} from {owner.name}";
        }
    }
}

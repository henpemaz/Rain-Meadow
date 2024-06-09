using HarmonyLib;
using RainMeadow.Generics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public abstract partial class OnlineEntity
    {
        [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
        public abstract class EntityDefinition : OnlineState, IIdentifiable<OnlineEntity.EntityId>
        {
            [OnlineField(always: true)]
            public OnlineEntity.EntityId entityId;
            [OnlineField]
            public ushort owner;
            [OnlineField]
            public bool isTransferable;
            [OnlineField]
            public TickReference tickReference;

            public EntityDefinition() : base() { }

            public EntityDefinition(OnlineEntity entity, OnlineResource inResource)
            {
                this.entityId = entity.id;
                this.owner = entity.owner.inLobbyId;
                this.isTransferable = entity.isTransferable;
                this.tickReference = inResource.supervisor.MakeTickReference();
            }

            public abstract OnlineEntity MakeEntity(OnlineResource inResource, OnlineEntity.EntityState initialState);

            public OnlineEntity.EntityId ID => entityId;

            public override string ToString()
            {
                return base.ToString() + ":" + entityId;
            }
        }

        internal abstract EntityDefinition MakeDefinition(OnlineResource onlineResource);

        public OnlinePlayer owner;
        public readonly EntityId id;
        public readonly bool isTransferable;

        public bool isMine => owner.isMe;

        /// <summary>
        /// stack of resources this entity has entered locally, updated imediately, kept in sync for remote entities
        /// </summary>
        public List<OnlineResource> enteredResources = new();
        /// <summary>
        /// stack of resources this entity has joined in online space, updated by callbacks
        /// </summary>
        public List<OnlineResource> joinedResources = new(); // used like a stack

        public OnlineResource primaryResource => joinedResources.Count != 0 ? joinedResources[0] : null;
        public OnlineResource currentlyJoinedResource => joinedResources.Count != 0 ? joinedResources[joinedResources.Count - 1] : null;
        public OnlineResource currentlyEnteredResource => enteredResources.Count != 0 ? enteredResources[enteredResources.Count - 1] : null;

        public bool isPending => pendingRequest != null;
        public OnlineEvent pendingRequest;

        protected OnlineEntity(EntityId id, OnlinePlayer owner, bool isTransferable)
        {
            this.id = id;
            this.owner = owner;
            this.isTransferable = isTransferable;

            OnlineManager.recentEntities.Add(id, this);
        }

        protected OnlineEntity(EntityDefinition entityDefinition, OnlineResource inResource, EntityState initialState)
        {
            this.id = entityDefinition.entityId;
            this.owner = OnlineManager.lobby.PlayerFromId(entityDefinition.owner);
            this.isTransferable = entityDefinition.isTransferable;

            OnlineManager.recentEntities.Add(id, this);
        }

        /// <summary>
        /// enter a resource locally, will automatically join in online space
        /// </summary>
        /// <param name="resource"></param>
        public void EnterResource(OnlineResource resource)
        {
            if (enteredResources.Contains(resource)) { if (isMine) JoinOrLeavePending(); return; }
            RainMeadow.Debug($"{this} entering {resource}");
            if (enteredResources.Count != 0 && resource.super != currentlyEnteredResource)
            {
                var primary = enteredResources[0];
                var commonAncestor = currentlyEnteredResource.CommonAncestor(resource, out List<OnlineResource> chainA, out List<OnlineResource> chainB);
                // roll up
                while (enteredResources.Count != 0 && currentlyEnteredResource != commonAncestor)
                {
                    enteredResources.Remove(currentlyEnteredResource);
                }
                // roll down
                var mergeTarget = chainB.Contains(currentlyEnteredResource) ? currentlyEnteredResource : chainB.First(e => e.IsSibling(primary));
                foreach (var res in chainB.SkipWhile(r => r != mergeTarget))
                {
                    enteredResources.Add(res);
                }
            }
            else
            {
                enteredResources.Add(resource);
            }
            if (isMine) JoinOrLeavePending();
        }

        /// <summary>
        /// leave a resource locally, will automatically leave in online space
        /// </summary>
        /// <param name="resource"></param>
        public void LeaveResource(OnlineResource resource)
        {
            if (!enteredResources.Contains(resource)) { return; }
            RainMeadow.Debug($"{this} left {resource}");
            if (resource != currentlyEnteredResource)
            {
                RainMeadow.Error($"Not the right resource {this} - {resource} - {currentlyEnteredResource}" + Environment.NewLine + Environment.StackTrace);
            }
            enteredResources.Remove(resource);
            if (isMine) JoinOrLeavePending();
        }

        public void JoinOrLeavePending()
        {
            if (!isMine) { throw new InvalidProgrammerException("not owner"); }
            if (isPending) { return; } // still pending
            RainMeadow.Debug(this);

            enteredResources.RemoveAll(r => !r.isActive);
            joinedResources.RemoveAll(r => !r.isAvailable || !r.entities.ContainsKey(this.id));

            // any resources to leave
            var pending = joinedResources.LastOrDefault(r => !enteredResources.Contains(r));
            if (pending != null)
            {
                pending.LocalEntityLeft(this);
                return;
            }
            // any resources to join
            pending = enteredResources.FirstOrDefault(r => !joinedResources.Contains(r));
            if (pending != null)
            {
                pending.LocalEntityEntered(this);
                return;
            }
        }

        public virtual void OnJoinedResource(OnlineResource inResource)
        {
            RainMeadow.Debug(this);
            if (inResource == currentlyJoinedResource) return;
            if (joinedResources.Contains(inResource))
            {
                RainMeadow.Error($"Not the right resource {this} - {inResource} - {currentlyEnteredResource}" + Environment.NewLine + Environment.StackTrace);
                return;
            }
            if (!isMine && this.currentlyJoinedResource != null && currentlyJoinedResource.IsSibling(inResource))
            {
                currentlyJoinedResource.EntityLeftResource(this);
            }
            joinedResources.Add(inResource);
            incomingState.Add(inResource, new Queue<EntityState>());
            if (isMine)
            {
                if (!inResource.isOwner)
                    OnlineManager.AddFeed(inResource, this);
                JoinOrLeavePending();
            }
        }

        public virtual void OnLeftResource(OnlineResource inResource)
        {
            RainMeadow.Debug($"{this} left {inResource}");
            if (!joinedResources.Contains(inResource))
            {
                RainMeadow.Debug($"Entity already left: {this} {inResource}");
                return;
            }

            if (inResource != currentlyJoinedResource)
            {
                RainMeadow.Debug($"Entity leaving resource in wrong order: {this} was in {currentlyJoinedResource} wants to leave {inResource}");
            }

            joinedResources.ToArray().Do(r => { if (r.IsSubresourceOf(inResource)) r.EntityLeftResource(this); });
            //while (currentlyJoinedResource != inResource) currentlyJoinedResource.EntityLeftResource(this);

            joinedResources.Remove(inResource);
            lastStates.Remove(inResource);
            incomingState.Remove(inResource);
            if (isMine)
            {
                OnlineManager.RemoveFeed(inResource, this);
                JoinOrLeavePending();
            }
            if (primaryResource == null && !isPending)
            {
                Deregister();
            }
        }

        public virtual void Deregister()
        {
            RainMeadow.Debug("Removing entity from recentEntities: " + this);
            OnlineManager.recentEntities.Remove(id);
        }

        public virtual void NewOwner(OnlinePlayer newOwner)
        {
            if(newOwner == null) { throw new InvalidProgrammerException("null owner for entity"); }
            RainMeadow.Debug($"{this} assigned to {newOwner}");
            var wasOwner = owner;
            if (wasOwner == newOwner) return;
            owner = newOwner;
            //definition.owner = newOwner.inLobbyId;
            primaryResource.registeredEntities[id].owner = newOwner.inLobbyId;

            if (wasOwner.isMe)
            {
                foreach (var res in joinedResources)
                {
                    OnlineManager.RemoveFeed(res, this);
                }
            }
            if (newOwner.isMe)
            {
                foreach (var res in joinedResources)
                {
                    if (!res.isOwner) OnlineManager.AddFeed(res, this);
                }
                JoinOrLeavePending();
            }
        }

        // I was in a resource and I was left behind as the resource was released
        public virtual void Deactivated(OnlineResource onlineResource)
        {
            if (!joinedResources.Contains(onlineResource)) return;
            RainMeadow.Debug($"{this} in {onlineResource}");
            enteredResources.Remove(onlineResource);
            joinedResources.Remove(onlineResource);
            incomingState.Remove(onlineResource);
            if (pendingRequest is RPCEvent rpc && rpc.target == onlineResource) pendingRequest = null;
            if (isMine)
            {
                OnlineManager.RemoveFeed(onlineResource, this);
                JoinOrLeavePending();
            }
            if (primaryResource == null && !isPending)
            {
                Deregister();
            }
        }

        public virtual void ReadState(EntityState entityState, OnlineResource inResource)
        {
            if (entityState == null) throw new InvalidProgrammerException("state is null");
            if (entityState.isDelta) throw new InvalidProgrammerException("state delta");
            lastStates[inResource] = entityState;
            if (inResource != currentlyEnteredResource)
            {
                // RainMeadow.Debug($"Skipping state for wrong resource" + Environment.StackTrace);
                // since we send both region state and room state even if it's the same guy owning both, this gets spammed a lot
                // todo supress sending if more specialized state being sent to the same person
                return;
            }
            entityState.ReadTo(this);
        }

        public Dictionary<OnlineResource, Queue<EntityState>> incomingState = new();
        public virtual void ReadState(EntityFeedState entityFeedState)
        {
            var newState = entityFeedState.entityState;
            var inResource = entityFeedState.inResource;
            if (!incomingState.ContainsKey(inResource))
            {
                RainMeadow.Trace($"Received state for resource the entity isn't in {this} {inResource}, currently in {this.currentlyJoinedResource}");
                return;
            }
            var stateQueue = incomingState[inResource];
            if (newState.isDelta)
            {
                RainMeadow.Trace($"received delta state for tick {newState.tick} referencing baseline {newState.baseline}");
                while (stateQueue.Count > 0 && (newState.from != stateQueue.Peek().from || NetIO.IsNewer(newState.baseline, stateQueue.Peek().tick)))
                {
                    var discarded = stateQueue.Dequeue();
                    RainMeadow.Trace("discarding old event from tick " + discarded.tick);
                }
                if (stateQueue.Count == 0 || newState.baseline != stateQueue.Peek().tick)
                {
                    RainMeadow.Error($"Received unprocessable delta for {this} in {entityFeedState.inResource} from {newState.from}, tick {newState.tick} referencing baseline {newState.baseline}");
                    RainMeadow.Error($"Available ticks are: [{string.Join(", ", stateQueue.Where(s => s.from == newState.from).Select(s => s.tick))}]");
                    if (!newState.from.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(RPCs.DeltaReset, inResource, this.id)))
                    {
                        newState.from.InvokeRPC(RPCs.DeltaReset, inResource, this.id);
                    }
                    return;
                }
                newState = (EntityState)stateQueue.Peek().ApplyDelta(newState);
            }
            else
            {
                RainMeadow.Trace("received absolute state for tick " + newState.tick);
            }
            stateQueue.Enqueue(newState);
            if(newState.from == owner)
            {
                ReadState(newState, inResource);
            }
        }

        protected abstract EntityState MakeState(uint tick, OnlineResource inResource);

        public Dictionary<OnlineResource, EntityState> lastStates = new();
        public EntityState GetState(uint ts, OnlineResource inResource)
        {
            if (!lastStates.TryGetValue(inResource, out var lastState) || lastState == null || (isMine && lastState.tick != ts))
            {
                try
                {
                    lastState = MakeState(ts, inResource);
                    lastStates[inResource] = lastState;
                }
                catch (Exception)
                {
                    RainMeadow.Error(this);
                    throw;
                }
            }
            if (lastState == null) throw new InvalidProgrammerException("state is null");
            return lastState;
        }

        private List<EntityData> entityData = new();
        internal bool everRegistered;

        internal T AddData<T>(bool ignoreDuplicate = false) where T : EntityData, new()
        {
            for (int i = 0; i < entityData.Count; i++)
            {
                if (entityData[i].GetType() == typeof(T))
                {
                    if (ignoreDuplicate) return (T)entityData[i];
                    throw new ArgumentException("type already in data");
                }
            }
            var v = new T();
            entityData.Add(v);
            return v;
        }

        internal T AddData<T>(T toAdd, bool ignoreDuplicate = false) where T : EntityData
        {
            for (int i = 0; i < entityData.Count; i++)
            {
                if (entityData[i].GetType() == typeof(T))
                {
                    if (ignoreDuplicate) return (T)entityData[i];
                    throw new ArgumentException("type already in data");
                }
            }
            entityData.Add(toAdd);
            return toAdd;
        }

        internal bool TryGetData<T>(out T d, bool addIfMissing = false) where T : EntityData
        {
            for (int i = 0; i < entityData.Count; i++)
            {
                if (entityData[i].GetType() == typeof(T))
                {
                    d = (T)entityData[i];
                    return true;
                }
            }
            if (addIfMissing)
            {
                d = Activator.CreateInstance<T>();
                entityData.Add(d);
                return true;
            }
            d = null;
            return false;
        }

        internal T GetData<T>(bool addIfMissing = false) where T : EntityData
        {
            if (!TryGetData<T>(out var d, addIfMissing)) throw new KeyNotFoundException();
            return d;
        }

        internal void RemoveData<T>() where T : EntityData
        {
            for (int i = 0; i < entityData.Count; i++)
            {
                if (entityData[i].GetType() == typeof(T))
                {
                    entityData.RemoveAt(i);
                }
            }
        }

        public abstract class EntityData
        {
            protected EntityData() { }

            internal abstract EntityDataState MakeState(OnlineResource inResource);

            [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
            public abstract class EntityDataState : OnlineState
            {
                protected EntityDataState() { }

                internal abstract void ReadTo(OnlineEntity onlineEntity);
            }
        }

        public abstract class EntityState : RootDeltaState, IIdentifiable<OnlineEntity.EntityId>
        {
            [OnlineField(always: true)]
            public OnlineEntity.EntityId entityId;
            [OnlineField(nullable: true, polymorphic: true)]
            public AddRemoveSortedStates<OnlineEntity.EntityData.EntityDataState> entityDataStates;
            public OnlineEntity.EntityId ID => entityId;

            protected EntityState() : base() { }
            protected EntityState(OnlineEntity onlineEntity, OnlineResource inResource, uint ts) : base(ts)
            {
                this.entityId = onlineEntity.id;
                this.entityDataStates = new(onlineEntity.entityData.Select(d => d.MakeState(inResource)).Where(s => s != null).ToList());
            }

            public virtual void ReadTo(OnlineEntity onlineEntity)
            {
                entityDataStates.list.ForEach(d => d.ReadTo(onlineEntity));
            }
        }


        public override string ToString()
        {
            return $"{id} from {owner.id}";
        }
    }
}

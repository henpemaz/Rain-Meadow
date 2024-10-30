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
            internal ushort version;

            internal bool failedToSpawn; // tracked locally

            public EntityDefinition() : base() { }

            public EntityDefinition(OnlineEntity entity, OnlineResource inResource)
            {
                this.entityId = entity.id;
                this.owner = entity.owner.inLobbyId;
                this.isTransferable = entity.isTransferable;
                this.version = entity.version;
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

        internal ushort version;
        internal bool everRegistered;

        public bool isMine => owner.isMe && !isTransfering;

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

        public bool isPending => pendingRequest != null || isTransfering;
        public OnlineEvent pendingRequest;
        public bool isTransfering; // set on request, cleared on error or ownership change (non-error comes from different player than ownership status)

        protected OnlineEntity(EntityId id, OnlinePlayer owner, bool isTransferable)
        {
            this.id = id;
            this.owner = owner;
            this.isTransferable = isTransferable;
            this.version = 1;

            OnlineManager.recentEntities.Add(id, this);
        }

        protected OnlineEntity(EntityDefinition entityDefinition, OnlineResource inResource, EntityState initialState)
        {
            this.id = entityDefinition.entityId;
            this.owner = OnlineManager.lobby.PlayerFromId(entityDefinition.owner);
            this.isTransferable = entityDefinition.isTransferable;
            this.version = entityDefinition.version;

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
                RainMeadow.Debug("migrating from " + currentlyEnteredResource);
                var primary = currentlyEnteredResource.chain.First(r => enteredResources.Contains(r));
                var commonAncestor = currentlyEnteredResource.CommonAncestor(resource, out List<OnlineResource> chainA, out List<OnlineResource> chainB);
                // roll up
                while (enteredResources.Count != 0 && currentlyEnteredResource != commonAncestor)
                {
                    RainMeadow.Debug("leaving from: " + currentlyEnteredResource);
                    enteredResources.Remove(currentlyEnteredResource);
                }
                // roll down
                var mergeTarget = chainB.Contains(currentlyEnteredResource) ? currentlyEnteredResource : chainB.First(e => e.IsSibling(primary));
                foreach (var res in chainB.SkipWhile(r => r != mergeTarget).SkipWhile(r => enteredResources.Contains(r)))
                {
                    RainMeadow.Debug("actually entering: " + res);
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
        public void ExitResource(OnlineResource resource)
        {
            if (!enteredResources.Contains(resource)) { return; }
            RainMeadow.Debug($"{this} exiting {resource}");
            var index = enteredResources.IndexOf(resource);
            if (index > -1) enteredResources.RemoveRange(index, enteredResources.Count - index);
            RainMeadow.Debug($"now in {currentlyEnteredResource}");
            if (isMine) JoinOrLeavePending();
        }

        public void JoinOrLeavePending()
        {
            if (!isMine) { throw new InvalidProgrammerException("not owner"); }

            // Sanitize
            for (int i = enteredResources.Count - 1; i >= 0; i--)
            {
                if (!enteredResources[i].isActive)
                {
                    enteredResources.RemoveAt(i);
                }
            }
            for (int i = joinedResources.Count - 1; i >= 0; i--)
            {
                if (!joinedResources[i].isActive)
                {
                    RainMeadow.Error("was joined in inactive resource: " + joinedResources[i]);
                    joinedResources.RemoveAt(i);
                    continue;
                }
                if (!joinedResources[i].joinedEntities.ContainsKey(this.id))
                {
                    RainMeadow.Error("was joined in resource but not in entities list: " + joinedResources[i]);
                    joinedResources.RemoveAt(i);
                    continue;
                }
            }
            if (pendingRequest is RPCEvent rpc && rpc.target is OnlineResource res)
            {
                if (!enteredResources.Contains(res) && !joinedResources.Contains(res))
                {
                    RainMeadow.Debug($"dismissing pending request {pendingRequest} for resource {res}");
                    pendingRequest = null;
                }
            }
            if (isPending) { return; } // still pending
            RainMeadow.Debug(this);


            // any resources to leave
            var pending = joinedResources.LastOrDefault(r => !enteredResources.Contains(r));
            if (pending != null)
            {
                version++;
                pending.LocalEntityLeft(this);
                return;
            }
            // any resources to join
            pending = enteredResources.FirstOrDefault(r => !joinedResources.Contains(r));
            if (pending != null)
            {
                version++;
                pending.LocalEntityEntered(this);
                return;
            }
        }

        public void OnJoinedResource(OnlineResource inResource, EntityState initialState)
        {
            RainMeadow.Debug($"{this} joining {inResource}");
            if (inResource == currentlyJoinedResource || joinedResources.Contains(inResource))
            {
                RainMeadow.Error($"Already in resource {this} - {inResource} - {currentlyEnteredResource}" + Environment.NewLine + Environment.StackTrace);
                return;
            }
            if (!isMine && this.currentlyJoinedResource != null && currentlyJoinedResource.IsSibling(inResource))
            {
                currentlyJoinedResource.EntityLeftResource(this);
            }
            joinedResources.Add(inResource);
            incomingState.Add(inResource, new Queue<EntityState>());

            if (!isMine)
            {
                EnterResource(inResource);
                ReadState(initialState, inResource);
                JoinImpl(inResource, initialState);
            }

            if (isMine)
            {
                if (!inResource.isOwner)
                    OnlineManager.AddFeed(inResource, this);
                JoinOrLeavePending();
            }
        }

        protected virtual void JoinImpl(OnlineResource inResource, EntityState initialState)
        {

        }

        public void OnLeftResource(OnlineResource inResource)
        {
            RainMeadow.Debug($"{this} leaving {inResource}");
            if (!joinedResources.Contains(inResource)) return;

            // if any subresources to leave do that first for consistency 
            joinedResources.Reverse<OnlineResource>().ToArray().Do(r => { if (r.IsSubresourceOf(inResource)) r.EntityLeftResource(this); });

            joinedResources.Remove(inResource);
            lastStates.Remove(inResource);
            incomingState.Remove(inResource);

            if (!isMine) LeaveImpl(inResource);

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

        protected virtual void LeaveImpl(OnlineResource inResource)
        {

        }

        public virtual void Deregister()
        {
            RainMeadow.Debug("Removing entity from recentEntities: " + this);
            OnlineManager.recentEntities.Remove(id);
        }

        public virtual void NewOwner(OnlinePlayer newOwner)
        {
            if (newOwner == null) { throw new InvalidProgrammerException("null owner for entity"); }
            RainMeadow.Debug($"{this} assigned to {newOwner}");
            var wasOwner = owner;
            isTransfering = false;
            if (wasOwner == newOwner) { RainMeadow.Debug($"assigned to same owner"); return; }
            owner = newOwner;
            primaryResource.registeredEntities[id] = MakeDefinition(primaryResource);

            foreach (var key in incomingState.Keys.ToList())
            {
                incomingState[key] = new Queue<EntityState>();
            }
            lastStates.Clear();

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
            }

            if (newOwner.isMe || wasOwner.isMe)
            {
                if (pendingRequest is RPCEvent rpc && rpc.target == this) // dismiss ongoing request/release
                {
                    RainMeadow.Debug("Dismissing pending request: " + pendingRequest);
                    pendingRequest.Abort();
                }
            }

            if (newOwner.isMe)
            {
                JoinOrLeavePending();
            }
        }

        // I was in a resource and I was left behind as the resource was released
        public void Deactivated(OnlineResource onlineResource)
        {
            RainMeadow.Debug($"{this} in {onlineResource}");
            if (pendingRequest is RPCEvent rpc && rpc.target == onlineResource)
            {
                RainMeadow.Debug($"dismissing pending request {pendingRequest}");
                pendingRequest = null;
            }

            var index = enteredResources.IndexOf(onlineResource);
            if (index > -1) enteredResources.RemoveRange(index, enteredResources.Count - index);

            if (!joinedResources.Contains(onlineResource)) return;
            // if any subresources to leave do that first for consistency 
            joinedResources.Reverse<OnlineResource>().ToArray().Do(r => { if (r.IsSubresourceOf(onlineResource)) Deactivated(r); });

            joinedResources.Remove(onlineResource);
            lastStates.Remove(onlineResource);
            incomingState.Remove(onlineResource);

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

        /// <summary>
        /// Runs RPC on owner and auto-retries with current owner
        /// </summary>
        /// // todo add While param??
        public void RunRPC(Delegate del, params object[] args)
        {
            if (primaryResource == null) { RainMeadow.Debug("deactivated"); return; }
            if (owner == null || owner.hasLeft) { RainMeadow.Debug("no owner"); return; }
            owner.InvokeRPC(del, args).Then(e => { if (e is not GenericResult.Ok) OnlineManager.RunDeferred(() => RunRPC(del, args)); });
        }

        /// <summary>
        /// Runs RPC for others in room
        /// </summary>
        public void BroadcastRPCInRoom(Delegate del, params object[] args)
        {
            RainMeadow.Debug($"{this} - {del}");
            if (currentlyJoinedResource is RoomSession room)
            {
                foreach (var participant in room.participants)
                {
                    if (!participant.isMe)
                    {
                        participant.InvokeRPC(del, args);
                    }
                }
            }
        }

        public virtual void ReadState(EntityState entityState, OnlineResource inResource)
        {
            lastStates[inResource] = entityState;
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
            if (newState.from != owner)
            {
                RainMeadow.Trace($"skipping state from {newState.from}, wanted {owner}");
                return;
            }
            RainMeadow.Trace($"processing received state {newState} in resource {inResource}");
            var stateQueue = incomingState[inResource];
            if (newState.isDelta)
            {
                RainMeadow.Trace($"received delta state for tick {newState.tick} referencing baseline {newState.baseline}");
                while (stateQueue.Count > 0 && NetIO.IsNewer(newState.baseline, stateQueue.Peek().tick))
                {
                    var discarded = stateQueue.Dequeue();
                    RainMeadow.Trace("discarding old event from tick " + discarded.tick);
                }
                if (stateQueue.Count == 0 || newState.baseline != stateQueue.Peek().tick)
                {
                    RainMeadow.Error($"Received unprocessable delta for {this} in {entityFeedState.inResource} from {newState.from}, tick {newState.tick} referencing baseline {newState.baseline}");
                    RainMeadow.Error($"Available ticks are: [{string.Join(", ", stateQueue.Select(s => s.tick))}]");
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
            if (inResource != currentlyJoinedResource)
            {
                RainMeadow.Trace($"Skipping state for wrong resource: received {inResource} wanted {currentlyJoinedResource}");
                lastStates[inResource] = newState;
                return;
            }
            ReadState(newState, inResource);
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

        public abstract class EntityState : RootDeltaState, IIdentifiable<OnlineEntity.EntityId>
        {
            [OnlineField(always: true)]
            public OnlineEntity.EntityId entityId;
            [OnlineField(group: "meta")]
            public ushort version;
            [OnlineField(nullable: true)]
            public DeltaDataStates<EntityData.EntityDataState> entityDataStates;
            public OnlineEntity.EntityId ID => entityId;

            protected EntityState() : base() { }
            protected EntityState(OnlineEntity onlineEntity, OnlineResource inResource, uint ts) : base(ts)
            {
                this.entityId = onlineEntity.id;
                this.version = onlineEntity.version;
                this.entityDataStates = new(onlineEntity.entityData.Values.Select(d => d.MakeState(onlineEntity, inResource)).Where(s => s != null).ToList());
            }

            public virtual void ReadTo(OnlineEntity onlineEntity)
            {
                onlineEntity.version = version;
                entityDataStates.list.ForEach(ds => ds.ReadTo(onlineEntity.TryGetData(ds.GetDataType(), out var d) ? d : onlineEntity.AddData(ds.MakeData(onlineEntity)), onlineEntity));
            }
        }

        public override string ToString()
        {
            return $"{id} from {owner.id}";
        }
    }
}

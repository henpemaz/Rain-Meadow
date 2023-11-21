﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public abstract partial class OnlineEntity
    {
        public OnlinePlayer owner => definition.owner; // can be updated
        public EntityDefinition definition;
        public readonly EntityId id;
        public readonly bool isTransferable;

        public bool isMine => owner.isMe;

        public List<OnlineResource> joinedResources = new(); // used like a stack
        public List<OnlineResource> enteredResources = new(); // used like a stack

        public OnlineResource primaryResource => joinedResources.Count != 0 ? joinedResources[0] : null;
        public OnlineResource currentlyJoinedResource => joinedResources.Count != 0 ? joinedResources[joinedResources.Count - 1] : null;
        public OnlineResource currentlyEnteredResource => enteredResources.Count != 0 ? enteredResources[enteredResources.Count - 1] : null;

        public bool isPending => pendingRequest != null;
        public OnlineEvent pendingRequest;

        protected OnlineEntity(EntityDefinition entityDefinition)
        {
            this.definition = entityDefinition;
            this.id = entityDefinition.entityId;
            this.isTransferable = entityDefinition.isTransferable;

            OnlineManager.recentEntities.Add(id, this);
        }

        public void EnterResource(OnlineResource resource)
        {
            RainMeadow.Debug($"{this} entered {resource}");
            if (enteredResources.Count != 0 && resource.super != currentlyEnteredResource)
            {
                if (resource == currentlyEnteredResource) { return; }
                RainMeadow.Error($"Not the right resource {this} - {resource} - {currentlyEnteredResource}" + Environment.NewLine + Environment.StackTrace);
                if (resource.IsSibling(currentlyEnteredResource)) { LeaveResource(currentlyEnteredResource); }
            }
            enteredResources.Add(resource);
            if (isMine) JoinOrLeavePending();
        }

        public void LeaveResource(OnlineResource resource)
        {
            RainMeadow.Debug($"{this} left {resource}");
            if (resource != currentlyEnteredResource)
            {
                RainMeadow.Error($"Not the right resource {this} - {resource} - {currentlyEnteredResource}" + Environment.NewLine + Environment.StackTrace);
            }
            enteredResources.Remove(resource);
            if (isMine) JoinOrLeavePending();
        }

        private void JoinOrLeavePending()
        {
            //RainMeadow.Debug(this);
            if (!isMine) { throw new InvalidProgrammerException("not owner"); }
            if (isPending) { return; } // still pending
            // any resources to leave
            var pending = joinedResources.Except(enteredResources).FirstOrDefault(r => r.entities.ContainsKey(this.id));
            if (pending != null)
            {
                pending.LocalEntityLeft(this);
                return;
            }
            // any resources to join
            pending = enteredResources.FirstOrDefault(r => !r.entities.ContainsKey(this.id));
            if (pending != null)
            {
                pending.LocalEntityEntered(this);
                return;
            }
        }

        public virtual void OnJoinedResource(OnlineResource inResource)
        {
            RainMeadow.Debug(this);
            if (!isMine && this.currentlyJoinedResource != null && currentlyJoinedResource.IsSibling(inResource))
            {
                currentlyJoinedResource.EntityLeftResource(this);
            }
            joinedResources.Add(inResource);
            if (isMine)
            {
                if (!inResource.isOwner)
                    OnlineManager.AddFeed(inResource, this);
                JoinOrLeavePending();
            }
        }

        public virtual void OnLeftResource(OnlineResource inResource)
        {
            RainMeadow.Debug(this);
            if (!joinedResources.Contains(inResource))
            {
                RainMeadow.Debug($"Entity already left: {this} {inResource}");
                return;
            }

            while (currentlyJoinedResource != inResource) currentlyJoinedResource.EntityLeftResource(this);

            joinedResources.Remove(inResource);
            lastStates.Remove(inResource);
            incomingState.Remove(inResource);
            if (isMine)
            {
                OnlineManager.RemoveFeed(inResource, this);
                JoinOrLeavePending();
                if (!isTransferable)
                    inResource.SubresourcesUnloaded(); // maybe you can release now
            }
            if (primaryResource == null)
            {
                RainMeadow.Debug("Removing entity from recentEntities: " + this);
                OnlineManager.recentEntities.Remove(id);
            }
        }

        public virtual void NewOwner(OnlinePlayer newOwner)
        {
            RainMeadow.Debug(this);
            var wasOwner = owner;
            if (wasOwner == newOwner) return;
            definition.owner = newOwner;
            RainMeadow.Debug(this);

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
            RainMeadow.Debug(this);
            enteredResources.Remove(onlineResource);
            joinedResources.Remove(onlineResource);
            incomingState.Remove(onlineResource);
            if (isMine) OnlineManager.RemoveFeed(onlineResource, this);
        }

        public virtual void ReadState(EntityState entityState, OnlineResource inResource)
        {
            if (entityState == null) throw new InvalidProgrammerException("state is null");
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
            if (!incomingState.ContainsKey(inResource)) incomingState.Add(inResource, new Queue<EntityState>());
            var stateQueue = incomingState[inResource];
            if (newState.isDelta)
            {
                //RainMeadow.Debug($"received delta state for tick {newState.tick} referencing baseline {newState.Baseline}");
                while (stateQueue.Count > 0 && (newState.from != stateQueue.Peek().from || NetIO.IsNewer(newState.baseline, stateQueue.Peek().tick)))
                {
                    var discarded = stateQueue.Dequeue();
                    //RainMeadow.Debug("discarding old event from tick " + discarded.tick);
                }
                if (stateQueue.Count == 0 || newState.baseline != stateQueue.Peek().tick)
                {
                    RainMeadow.Error($"Received unprocessable delta for {this} from {newState.from}, tick {newState.tick} referencing baseline {newState.baseline}");
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
                //RainMeadow.Debug("received absolute state for tick " + newState.tick);
            }
            stateQueue.Enqueue(newState);
            if (newState.from == owner)
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

        public override string ToString()
        {
            return $"{id} from {owner.id}";
        }
    }
}

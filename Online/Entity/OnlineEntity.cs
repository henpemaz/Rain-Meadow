﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public abstract partial class OnlineEntity
    {
        public OnlinePlayer owner;
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

        protected OnlineEntity(OnlinePlayer owner, EntityId id, bool isTransferable)
        {
            this.owner = owner;
            this.id = id;
            this.isTransferable = isTransferable;
        }

        public void EnterResource(OnlineResource resource)
        {
            RainMeadow.Debug($"{this} entered {resource}");
            // todo handle joining same-level resource when joining (I guess if remote)
            // but why do we even keep track of this for non-local?
            if (enteredResources.Count != 0 && resource.super != currentlyEnteredResource)
            {
                RainMeadow.Error($"Not the right resource {this} - {resource} - {currentlyEnteredResource}");
            }
            enteredResources.Add(resource);
            if (isMine) JoinOrLeavePending();
        }

        public void LeaveResource(OnlineResource resource)
        {
            RainMeadow.Debug($"{this} left {resource}");
            // todo handle leaving same-level resource when joining (I guess if remote)
            // but why do we even keep track of this for non-local?
            if (enteredResources.Count == 0) throw new InvalidOperationException("not in a resource");

            if (resource != currentlyEnteredResource)
            {
                RainMeadow.Error($"Not the right resource {this} - {resource} - {currentlyEnteredResource}");
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

        public virtual void OnJoinedResource(OnlineResource inResource, EntityState initialState)
        {
            RainMeadow.Debug(this);
            if (!isMine && this.currentlyJoinedResource != null && currentlyJoinedResource.IsSibling(inResource))
            {
                currentlyJoinedResource.EntityLeftResource(this);
            }
            joinedResources.Add(inResource);
            initialState.entityId = this.id;
            ReadState(initialState, inResource);
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
                RainMeadow.Error($"Entity leaving resource it wasn't in: {this} {inResource}");
                return;
            }

            while (currentlyJoinedResource != inResource) currentlyJoinedResource.EntityLeftResource(this);

            joinedResources.Remove(inResource);
            lastStates.Remove(inResource);
            if (isMine)
            {
                OnlineManager.RemoveFeed(inResource, this);
                JoinOrLeavePending();
                if (!isTransferable)
                    inResource.SubresourcesUnloaded(); // maybe you can release now
            }
        }

        public abstract NewEntityEvent AsNewEntityEvent(OnlineResource onlineResource);

        public static OnlineEntity FromNewEntityEvent(NewEntityEvent newEntityEvent, OnlineResource inResource)
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
            if (wasOwner == newOwner) return;
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
                    if (!res.isOwner)
                        OnlineManager.AddFeed(res, this);
                }
            }
        }

        // I was in a resource and I was left behind as the resource was released
        public virtual void Deactivated(OnlineResource onlineResource)
        {
            RainMeadow.Debug(this);
            if (onlineResource != this.currentlyJoinedResource) throw new InvalidOperationException("not leaving lowest resource");
            enteredResources.Remove(onlineResource);
            joinedResources.Remove(onlineResource);
            if (isMine) OnlineManager.RemoveFeed(onlineResource, this);
        }

        public virtual void ReadState(EntityState entityState, OnlineResource inResource)
        {
            if (lastStates.TryGetValue(inResource, out var existingState) && NetIO.IsNewer(existingState.tick, entityState.tick)) { RainMeadow.Debug($"Skipping stale state"); return; }
            lastStates[inResource] = entityState;
            if (inResource != currentlyJoinedResource)
            {
                // RainMeadow.Debug($"Skipping state for wrong resource" + Environment.StackTrace);
                // since we send both region state and room state even if it's the same guy owning both, this gets spammed a lot
                // todo supress sending if more specialized state being sent
                return;
            }
            if(!isMine)
            {
                entityState.ReadTo(this);
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

        public EntityFeedState GetFeedState(uint ts, OnlineResource inResource)
        {
            return new EntityFeedState(GetState(ts, inResource), inResource, ts);
        }

        public override string ToString()
        {
            return $"{id} from {owner.id}";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using static RainMeadow.OnlineResource;

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
            RainMeadow.Debug(this);
            // todo handle joining same-level resource when joining (I guess if remote)
            // but why do we even keep track of this for non-local?
            if (enteredResources.Count != 0 && resource.super != currentlyEnteredResource) throw new InvalidOperationException("not entering a subresource");
            enteredResources.Add(resource);
            if (isMine) JoinOrLeavePending();
        }

        public void LeaveResource(OnlineResource resource)
        {
            RainMeadow.Debug(this);
            // todo handle leaving same-level resource when joining (I guess if remote)
            // but why do we even keep track of this for non-local?
            if (enteredResources.Count == 0) throw new InvalidOperationException("not in a resource");

            // this is wrong, it's cheking for joinedresources(remote) but we're looking at enteredresources(local, pending)
            // todo fix this
            if (resource != currentlyEnteredResource) throw new InvalidOperationException("not the right resource");
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
                if(!inResource.isOwner)
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
                if(!isTransferable)
                    inResource.SubresourcesUnloaded(); // maybe you can release now
            }
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
                    if(!res.isOwner)
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
            if (lastStates.TryGetValue(inResource, out var existingState) && OnlineManager.IsNewer(existingState.tick, entityState.tick)) return; // skipped old data
            lastStates[inResource] = entityState;
            if (inResource != currentlyJoinedResource) return; // skip processing
            entityState.ReadTo(this);
        }

        protected abstract EntityState MakeState(ulong tick, OnlineResource inResource);


        public Dictionary<OnlineResource, EntityState> lastStates = new();
        public EntityState GetState(ulong ts, OnlineResource inResource)
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

        public EntityInResourceState GetStateInResource(ulong ts, OnlineResource inResource)
        {
            return new EntityInResourceState(GetState(ts, inResource), inResource, ts);
        }

        public override string ToString()
        {
            return $"{id} from {owner.id}";
        }
    }
}

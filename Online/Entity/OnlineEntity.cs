using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
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
            RainMeadow.Debug(this);
            // todo handle joining same-level resource when joining (I guess if remote)
            // but why do we even keep track of this for non-local?
            if (enteredResources.Count != 0 && resource.super != lowestResource) throw new InvalidOperationException("not entering a subresource");
            enteredResources.Add(resource);
            if (owner.isMe) JoinOrLeavePending();
        }

        public void LeaveResource(OnlineResource resource)
        {
            RainMeadow.Debug(this);
            // todo handle leaving same-level resource when joining (I guess if remote)
            // but why do we even keep track of this for non-local?
            if (enteredResources.Count == 0) throw new InvalidOperationException("not in a resource");
            if (resource != lowestResource) throw new InvalidOperationException("not the right resource");
            enteredResources.Remove(resource);
            if (owner.isMe) JoinOrLeavePending();
        }

        private void JoinOrLeavePending()
        {
            //RainMeadow.Debug(this);
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
            RainMeadow.Debug(this);
            joinedResources.Add(inResource);
            if (owner.isMe) JoinOrLeavePending();
        }

        public virtual void OnLeftResource(OnlineResource inResource)
        {
            RainMeadow.Debug(this);
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
        public virtual void Deactivated(OnlineResource onlineResource)
        {
            RainMeadow.Debug(this);
            if (onlineResource != this.lowestResource) throw new InvalidOperationException("not leaving lowest resource");
            enteredResources.Remove(onlineResource);
            joinedResources.Remove(onlineResource);
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

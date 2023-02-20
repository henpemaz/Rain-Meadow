using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace RainMeadow
{
    // OnlineResources are transferible, subscriptable resources, limited to a resource that others can consume (lobby, world, room)
    // The owner of the resource coordinates states, distributes subresources and solves conflicts
    // Distributed Hyerarchical Ownership Management System?
    // Distributed transaction management system?
    // Todo improve initial-state on subscribe for event-driven data
    public abstract class OnlineResource
    {
        public OnlineResource super;
        public OnlinePlayer owner;
        public List<OnlineResource> subresources;

        public ResourceEvent pendingRequest; // should this maybe be a list/queue?
        public List<OnlinePlayer> subscribers; // this could be a dict of subscriptions, but how relevant is to access them through here anyways

        public bool isFree => owner == null;
        public bool isOwner => owner != null && owner.id == OnlineManager.me;
        public bool isSuper => super != null && super.isOwner;
        public bool isActive { get; protected set; }
        public bool isAvailable { get; protected set; }
        public bool isPending => pendingRequest != null;

        public override string ToString()
        {
            return $"<Resource {Identifier()} - o:{owner?.name}>";
        }

        // The game resource this corresponds to has loaded
        public virtual void Activate()
        {
            RainMeadow.Debug(this);
            if(isActive) { throw new InvalidOperationException("Resource is already active"); }
            if(!isAvailable) { throw new InvalidOperationException("Resource is not available"); }
            isActive = true;
            subresources = new List<OnlineResource>();
        }

        // The online resource has been leased and is available for use
        protected virtual void Available()
        {
            RainMeadow.Debug(this);
            if (isActive) { throw new InvalidOperationException("Resource is already active"); }
            isAvailable = true;
            subscribers = new List<OnlinePlayer>();
        }

        // The game resource has been unloaded
        public virtual void Deactivate()
        {
            RainMeadow.Debug(this);
            if (!isActive) { throw new InvalidOperationException("Resource is already inactive"); }
            if (subresources.Any(s=>s.isActive)) throw new InvalidOperationException("has active subresources");
            isActive = false;
            subresources.Clear();
        }

        // The online resource has been unleased
        protected virtual void Unavailable()
        {
            RainMeadow.Debug(this);
            if (!isActive) { throw new InvalidOperationException("resource is inactive, should be unleased first"); }
            OnlineManager.RemoveSubscriptions(this);
            subscribers.Clear();
            isAvailable = false;
        }

        private void Claimed(OnlinePlayer player)
        {
            RainMeadow.Debug(this.ToString() + " - " + player);
            if (player == owner && player != null) throw new InvalidOperationException("Re-assigned to the same owner");
            var oldOwner = owner;
            owner = player;
            if (isSuper && oldOwner != owner) // I am responsible for notifying lease changes to this
            {
                super.SubresourceNewOwner(this);
            }
        }

        private void Unclaimed() // could get rid of this and embrace player null
        {
            RainMeadow.Debug(this);
            if(owner == null) { throw new InvalidOperationException("Resource was already unassigned"); }
            var oldOwner = owner;
            owner = null;
            if (isSuper && oldOwner != null) // I am responsible for notifying lease changes to this
            {
                super.SubresourceNewOwner(this);
            }
        }

        private void SubresourceNewOwner(OnlineResource onlineResource)
        {
            foreach (var s in subscribers)
            {
                if (s == onlineResource.owner) continue; // they already know
                s.NewOwnerEvent(onlineResource, onlineResource.owner);
            }
        }

        private void Subscribed(OnlinePlayer player)
        {
            RainMeadow.Debug(this.ToString() + " - " + player.name);
            if (!isActive) throw new InvalidOperationException("inactive");
            if (subscribers == null) throw new InvalidOperationException("nill");
            if (player.isMe) throw new InvalidOperationException("Can't subscribe to self");
            OnlineManager.AddSubscription(this, player);
            this.subscribers.Add(player);

            // initial lease state
            // TODO initial state?
            // This will break if a user subscribes between Available and Activate
            foreach (var onlineResource in subresources)
            {
                player.NewOwnerEvent(onlineResource, onlineResource.owner);
            }
        }

        private void Unsubscribed(OnlinePlayer player)
        {
            RainMeadow.Debug(this.ToString() + " - " + player.name);
            if (player.isMe) throw new InvalidOperationException("Can't unsubscribe from self");
            OnlineManager.RemoveSubscription(this, player);
            this.subscribers.Remove(player);
        }

        // I request, possibly to someone else
        public virtual void Request()
        {
            RainMeadow.Debug(this);
            if (isPending) throw new InvalidOperationException("pending");
            if (isActive) throw new InvalidOperationException("active");

            if (owner != null)
            {
                pendingRequest = owner.RequestResource(this);
            }
            else if (super?.owner != null)
            {
                pendingRequest = super.owner.RequestResource(this);
            }
            else
            {
                throw new InvalidOperationException("cant be requested");
            }
        }

        // I release, possibly to someone else
        public void Release()
        {
            RainMeadow.Debug(this);
            if (isPending) throw new InvalidOperationException("pending");
            if (!isActive) throw new InvalidOperationException("inactive");
            if (isOwner) // let go
            {
                if (super?.owner != null) // return to super
                {
                    pendingRequest = super.owner.ReleaseResource(this);
                }
                else
                {
                    throw new InvalidOperationException("cant be released");
                }
            }
            else if (owner != null) // unsubscribe
            {
                pendingRequest = owner.ReleaseResource(this);
            }
            else
            {
                throw new InvalidOperationException("cant be released");
            }
        }

        // Someone requested me, maybe myself
        public void Requested(ResourceRequest request)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Requested by : " + request.from.name);

            if (isFree && isSuper) // I can lease
            {
                // Leased to player
                request.from.QueueEvent(new RequestResult.Leased(request));
                Claimed(request.from);
            }
            else if (isOwner) // I am the current owner and others can subscribe
            {
                if (request.from.isMe) throw new InvalidOperationException("requested, but already own");
                request.from.QueueEvent(new RequestResult.Subscribed(request)); // result first, so they Activate before getting new state
                // Player subscribed to resource
                Subscribed(request.from);
            }
            else // Not mine, can't lease
            {
                request.from.QueueEvent(new RequestResult.Error(request));
            }
        }

        // Someone released from me, maybe myself
        public void Released(ReleaseRequest request)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Released by : " + request.from.name);
            RainMeadow.Debug("Has subscribers : " + (request.subscribers.Count > 0));

            if (isSuper && owner == request.from)
            {
                if (request.subscribers.Count > 0)
                {
                    var newOwner = PlayersManager.BestTransferCandidate(this, request.subscribers);
                    newOwner.TransferResource(this, request.subscribers.Where(s => s != newOwner).ToList()); // This notifies the new owner
                    Claimed(newOwner); // This notifies all users
                }
                else
                {
                    Unclaimed();
                }
                request.from.QueueEvent(new ReleaseResult.Released(request));
                return;
            }
            if (isOwner)
            {
                Unsubscribed(request.from);
                request.from.QueueEvent(new ReleaseResult.Unsubscribed(request));
                return;
            }
            request.from.QueueEvent(new ReleaseResult.Error(request));
            return;
        }

        // The previous owner has left and I've been assigned as the new owner
        public void Transfered(TransferRequest request)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Transfered by : " + request.from.name);
            if (isActive && !isOwner && request.from == super?.owner) // I am a subscriber who now owns this resource
            {
                Claimed(OnlineManager.mePlayer);
                foreach(var subscriber in request.subscribers)
                {
                    if (subscriber.isMe) continue;
                    Subscribed(subscriber);
                }
                request.from.QueueEvent(new TransferResult.Ok(request));
                return;
            }
            RainMeadow.Debug($"Transfer error : {isActive} {!isOwner} {request.from == super?.owner}");
            request.from.QueueEvent(new TransferResult.Error(request));
            return;
        }

        // A pending request was answered to
        public void ResolveRequest(RequestResult requestResult)
        {
            RainMeadow.Debug(this);
            if (requestResult is RequestResult.Leased) // I'm the new owner of a previously-free resource
            {
                if (!requestResult.from.isMe) // don't claim twice
                {
                    Claimed(OnlineManager.mePlayer);
                }
                Available();
            }
            else if (requestResult is RequestResult.Subscribed) // I'm subscribed to a resource's state and events
            {
                Available();
            }
            else if (requestResult is RequestResult.Error) // I should retry
            {
                // todo retry logic
                RainMeadow.Error("request failed for " + this);
            }
            pendingRequest = null;
        }

        // A pending release was answered to
        internal void ResolveRelease(ReleaseResult releaseResult)
        {
            RainMeadow.Debug(this);
            
            if (releaseResult is ReleaseResult.Released) // I've let go
            {
                if (!releaseResult.from.isMe) // dont unclaim twice
                {
                    Unclaimed();
                }
                Unavailable();
            }
            else if (releaseResult is ReleaseResult.Unsubscribed) // I'm clear
            {
                Unavailable();
            } 
            else if (releaseResult is ReleaseResult.Error) // I should retry
            {
                RainMeadow.Error("released failed for " + this);
            }
            pendingRequest = null;
        }

        // A pending transfer was asnwered to
        internal void ResolveTransfer(TransferResult transferResult)
        {
            RainMeadow.Debug(this);
            
            if (transferResult is TransferResult.Ok) // New owner accepted it
            {
                // no op
            }
            else if (transferResult is TransferResult.Error) // I should retry
            {
                RainMeadow.Error("transfer failed for " + this);
            }
            pendingRequest = null;
        }

        // Super says there is a new owner for this resource
        internal void OnNewOwner(NewOwnerEvent newOwnerEvent)
        {
            RainMeadow.Debug(this);
            Claimed(newOwnerEvent.newOwner);
        }


        private ResourceState lastState;
        public virtual ResourceState GetState(ulong ts)
        {
            if (lastState == null || lastState.ts != ts)
            {
                lastState = MakeState(ts);
            }

            return lastState;
        }

        protected abstract ResourceState MakeState(ulong ts);
        public abstract void ReadState(ResourceState newState, ulong ts);

        internal virtual byte SizeOfIdentifier()
        {
            return (byte)Identifier().Length;
        }

        internal abstract string Identifier();        
    }
}

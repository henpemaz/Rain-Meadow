using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    // OnlineResources are transferible, subscriptable resources, limited to a resource that others can consume (lobby, world, room)
    // The owner of the resource coordinates states, distributes subresources and solves conflicts
    // Distributed Hyerarchical Ownership Management System?
    // Distributed transaction management system?
    public abstract class OnlineResource
    {
        public OnlineResource super;
        public OnlinePlayer owner;

        public ResourceEvent pendingRequest; // should this maybe be a list/queue?
        public List<OnlinePlayer> subscribers; // this could be a dict of subscriptions, but how relevant is to access them through here anyways

        public bool isFree => owner == null;
        public bool isOwner => owner != null && owner.id == OnlineManager.me;
        public bool isSuper => super != null && super.isOwner;
        public bool isActive { get; protected set; }
        public bool isPending => pendingRequest != null;

        public override string ToString()
        {
            return $"<Resource {Identifier()} - o:{owner?.name}>";
        }

        public virtual void Activate()
        {
            RainMeadow.Debug(this);
            isActive = true;
            subscribers = new List<OnlinePlayer>();
        }

        public virtual void Deactivate()
        {
            RainMeadow.Debug(this);
            isActive = false;
            subscribers.Clear();
            OnlineManager.RemoveSubscriptions(this);
        }

        private void Claimed(OnlinePlayer player)
        {
            RainMeadow.Debug(this.ToString() + " - " + player);
            owner = player;

            // todo this should only activate if mePlayer claimed it
            // active -> locally active
            // or do we need a second flag?
            Activate();
        }

        private void Unclaimed()
        {
            RainMeadow.Debug(this);
            owner = null;
            Deactivate();
        }

        private void Subscribed(OnlinePlayer player)
        {
            RainMeadow.Debug(this.ToString() + " - " + player);
            OnlineManager.AddSubscription(this, player);
            this.subscribers.Add(player);
        }

        private void Unsubscribed(OnlinePlayer player)
        {
            RainMeadow.Debug(this.ToString() + " - " + player.name);
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
                if (subscribers.Count > 0 && super?.owner != null) // transfer
                {
                    pendingRequest = super.owner.TransferResource(this);
                }
                else if (super?.owner != null) // return to super
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

        private OnlinePlayer BestTransferCandidate(List<OnlinePlayer> subscribers)
        {
            return subscribers[0];
        }

        // Someone requested me, maybe myself
        public RequestResult Requested(ResourceRequest request)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Requested by : " + request.from.name);

            if (isOwner) // I decide
            {
                if (request.from.isMe) throw new InvalidOperationException("requested, but already own");
                // Player subscribed to resource
                Subscribed(request.from);
                return new RequestResult.Subscribed(request);
            }
            if (isFree && isSuper)
            {
                // Leased to player
                Claimed(request.from);
                return new RequestResult.Leased(request);
            }
            // Not mine, can't lease
            return new RequestResult.Error(request);
        }

        // Someone released from me, maybe myself
        public ReleaseResult Released(ReleaseRequest request)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Released by : " + request.from.name);

            if (pendingRequest is TransferRequest tr && tr.to == request.from)
            {
                // uh oh
                // retry transfer to someone else?
                // prioritize releasing over transfering
                throw new NotImplementedException();
            }
            if(isSuper && owner == request.from)
            {
                Unclaimed();
                return new ReleaseResult.Released(request);
            }
            if (isOwner)
            {
                Unsubscribed(request.from);
                return new ReleaseResult.Unsubscribed(request);
            }
            return new ReleaseResult.Error(request);
        }

        // Someone I manage needs a resource transfered
        public TransferResult Transfered(TransferRequest request)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Transfered by : " + request.from.name);

            if (owner == request.from)
            {
                owner = BestTransferCandidate(request.subscribers);
                Claimed(owner);
                foreach (var x in request.subscribers)
                {
                    x.NewOwnerEvent(this, owner);
                }
                return new TransferResult.Ok(request);
            }
            return new TransferResult.Error(request);
        }

        // A pending request was answered to
        public void ResolveRequest(RequestResult requestResult)
        {
            RainMeadow.Debug(this);
            if (requestResult is RequestResult.Leased)
            {
                Claimed(OnlineManager.mePlayer);
            }
            else if (requestResult is RequestResult.Subscribed)
            {
                Activate();
            }
            else if (requestResult is RequestResult.Error)
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
            if (releaseResult is ReleaseResult.Released)
            {
                Unclaimed();
            }
            else if (releaseResult is ReleaseResult.Unsubscribed)
            {
                Deactivate();
            } 
            else if (releaseResult is ReleaseResult.Error)
            {
                RainMeadow.Error("released failed for " + this);
            }
            pendingRequest = null;
        }

        // A pending transfer was asnwered to
        internal void ResolveTransfer(TransferResult transferResult)
        {
            RainMeadow.Debug(this);
            if (transferResult is TransferResult.Ok)
            {
                Deactivate();
            }
            else if (transferResult is TransferResult.Error)
            {
                RainMeadow.Error("transfer failed for " + this);
            }
            pendingRequest = null;
        }

        internal void NewOwner(NewOwnerEvent newOwnerEvent)
        {
            RainMeadow.Debug(this);
            if (isActive)
            {
                Claimed(newOwnerEvent.newOwner);
            }
            if(isPending)
            {
                if (pendingRequest is ReleaseRequest) return;
                if (pendingRequest is TransferRequest) return;
            }
            else
            {
                pendingRequest = owner.RequestResource(this);
            }
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

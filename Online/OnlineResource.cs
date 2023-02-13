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

        private bool subscribedToOther;

        public RequestEvent pendingRequest; // should this maybe be a list/queue?
        public List<OnlinePlayer> subscribers; // this could be a dict of subscriptions, but how relevant is to access them through here anyways

        public bool isFree => owner == null;
        public bool isOwner => owner != null && owner.id == OnlineManager.me;
        public bool isSuper => super != null && super.isOwner;
        public bool isActive { get; protected set; }

        public void Activate()
        {

        }

        public void Deactivate()
        {

        }

        private void Claimed(OnlinePlayer player)
        {
            owner = player;
        }

        private void Unclaimed()
        {
            owner = null;
        }

        private void Subscribed(OnlinePlayer player)
        {
            OnlineManager.AddSubscription(this, player);
            this.subscribers.Add(player);
        }

        private void Unsubscribed(OnlinePlayer player)
        {
            OnlineManager.RemoveSubscription(this, player);
            this.subscribers.Remove(player);
        }

        // I request, possibly to someone else
        public virtual void Request()
        {
            if (isOwner) return;
            if (isFree && isSuper)
            {
                Claimed(OnlineManager.mePlayer);
                return;
            }
            if (pendingRequest != null) return;

            pendingRequest = owner.RequestResource(this);
        }

        // I release, possibly to someone else
        public void Release()
        {
            if (isFree) return;
            if (pendingRequest != null) return;
            if (isOwner) // let go
            {
                if (subscribers.Count > 0) // transfer to active
                {
                    pendingRequest = subscribers[0].TransferResource(this); // todo select based on ping
                }
                else // release
                {
                    if (!isSuper) // return to super
                    {
                        pendingRequest = super.owner.ReleaseResource(this);
                    }
                    else
                    {
                        Unclaimed();
                    }
                }
            }
            else // unsubscribe
            {
                pendingRequest = owner.ReleaseResource(this);
            }
        }

        // Someone else requested me
        public RequestResult Requested(ResourceRequest request)
        {
            if (isOwner) // I decide
            {
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

        // Someone else released from me
        public ReleaseResult Released(ReleaseRequest request)
        {
            if (pendingRequest is TransferRequest tr && tr.to == request.from)
            {
                // uh oh
                // retry transfer to someone else?
                // prioritize releasing over transfering
                throw new NotImplementedException();
            }
            if(isOwner)
            {
                Unsubscribed(request.from);
                return new ReleaseResult.Unsubscribed(request);
            }
            if(isSuper && owner == request.from)
            {
                Unclaimed();
                return new ReleaseResult.Released(request);
            }
            return new ReleaseResult.Error(request);
        }

        // Someone else transfered an active resource to me
        public TransferResult Transfered(TransferRequest request)
        {
            if (owner == request.from)
            {
                Claimed(OnlineManager.mePlayer);
                foreach (var x in request.subscribers.Where(x => x != OnlineManager.mePlayer))
                {
                    Subscribed(x);
                }
                return new TransferResult.Ok(request);
            }
            return new TransferResult.Error(request);
        }

        // A pending request was answered to
        public void ResolveRequest(RequestResult requestResult)
        {
            if (requestResult is RequestResult.Leased)
            {
                Claimed(OnlineManager.mePlayer);
            }
            else if (requestResult is RequestResult.Subscribed)
            {
                subscribedToOther = true;
            }
            else if (requestResult is RequestResult.Error)
            {
                RainMeadow.Error("request failed for " + this);
            }
            pendingRequest = null;
        }

        // A pending release was answered to
        internal void ResolveRelease(ReleaseResult releaseResult)
        {
            if(releaseResult is ReleaseResult.Released)
            {
                Unclaimed();
            }
            else if (releaseResult is ReleaseResult.Unsubscribed)
            {
                subscribedToOther = false;
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
            if (transferResult is TransferResult.Ok)
            {
                var transfer = (transferResult.referencedRequest as TransferRequest);
                transfer.subscribers.ForEach(s=>Unsubscribed(s));
                // TODO send subs a notification? when to they get to know that onwership changed?
                Claimed(transfer.to);
            }
            else if (transferResult is TransferResult.Error)
            {
                RainMeadow.Error("transfer failed for " + this);
            }
            pendingRequest = null;
        }

        private ResourceState lastState;
        public virtual ResourceState GetState(long ts)
        {
            if (lastState == null || lastState.ts != ts)
            {
                lastState = MakeState(ts);
            }

            return lastState;
        }

        protected abstract ResourceState MakeState(long ts);
        public abstract void ReadState(ResourceState newState, long ts);

        internal virtual byte SizeOfIdentifier()
        {
            return (byte)Identifier().Length;
        }

        internal abstract string Identifier();
    }
}

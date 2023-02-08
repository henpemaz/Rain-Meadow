using System;
using System.Collections.Generic;
using System.Linq;
using static Expedition.ExpeditionProgression;

namespace RainMeadow
{
    // OnlineResources are transferible, subscriptable resources, limited to a resource that others can consume (lobby, world, room)
    // The owner of the resource coordinates states, distributes subresources and solves conflicts
    public abstract class OnlineResource
    {
        public OnlineResource super;
        public OnlinePlayer owner;

        public PlayerEvent pendingRequest; // should this maybe be a list/queue?
        public List<OnlinePlayer> subscribers; // this could be a dict of tasks

        public bool isFree => owner == null;
        public bool isOwner => owner != null && owner.id == OnlineManager.me;
        public bool isSuper => super != null && super.isOwner;

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
            OnlineManager.subscriptions.Add(new Subscription(this, player));
            this.subscribers.Add(player);
        }

        private void Unsubscribed(OnlinePlayer player)
        {
            this.subscribers.Remove(player);
        }

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
                    Unclaimed();
                }
            }
            else // unsubscribe
            {
                pendingRequest = owner.ReleaseResource(this);
            }
        }

        public RequestResult Requested(ResourceEvent request)
        {
            if (isOwner) // I decide
            {
                // Player subscribed to resource
                Subscribed(request.from);
                return new RequestResult.Subscribed();
            }
            if (isFree && isSuper)
            {
                // Leased to player
                Claimed(request.from);
                return new RequestResult.Leased();
            }
            // Not mine, can't lease
            return new RequestResult.Error();
        }

        public ReleaseResult Released(ReleaseRequest request)
        {
            if(isOwner)
            {
                Unsubscribed(request.from);
                return new ReleaseResult.Unsubscribed();
            }
            if(isSuper && owner == request.from)
            {
                Unclaimed();
                return new ReleaseResult.Released();
            }
            return new ReleaseResult.Error();
        }

        public TransferResult Transfered(TransferRequest request)
        {
            if (owner == request.from)
            {
                Claimed(OnlineManager.mePlayer);
                subscribers.AddRange(request.subscribers.Where(x => x != OnlineManager.mePlayer));
                return new TransferResult.Ok();
            }
            return new TransferResult.Error();
        }

        public abstract ResourceState GetState(long ts);

        public abstract void SetState(ResourceState newState, long ts);

        internal virtual long SizeOfIdentifier()
        {
            throw new NotImplementedException();
        }
    }
}

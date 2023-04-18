using System.Linq;
using System;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        // I request, possibly to someone else
        public virtual void Request()
        {
            RainMeadow.Debug(this);
            if (isPending) throw new InvalidOperationException("pending");
            if (isAvailable && !isFree) throw new InvalidOperationException("available");
            if (isActive && !isFree) throw new InvalidOperationException("active");

            if (owner != null && !owner.hasLeft)
            {
                pendingRequest = (ResourceEvent)owner.QueueEvent(new ResourceRequest(this));
            }
            else if (super?.owner != null)
            {
                pendingRequest = (ResourceEvent)super.owner.QueueEvent(new ResourceRequest(this));
            }
            else
            {
                throw new InvalidOperationException("cant be requested");
            }
        }

        // I release, possibly to someone else
        private void Release()
        {
            RainMeadow.Debug(this);
            if (isPending) throw new InvalidOperationException("pending");
            if (!isAvailable) throw new InvalidOperationException("not available");
            if (!canRelease) throw new InvalidOperationException("cant be released in current state");
            if (isOwner) // let go
            {
                if (super?.owner != null) // return to super
                {
                    participants.Remove(PlayersManager.mePlayer);
                    NewLeaseState();
                    this.pendingRequest = (ResourceEvent)super.owner.QueueEvent(new ResourceRelease(this,
                        participants,
                        entities.Where(e => e.isTransferable && !e.isPending && e.owner.isMe).Select(e => e.id).ToList()));
                }
                else
                {
                    throw new InvalidOperationException("cant be released");
                }
            }
            else if (owner != null) // unsubscribe
            {
                this.pendingRequest = (ResourceEvent)owner.QueueEvent(new ResourceRelease(this,
                    participants,
                    entities.Where(e => e.isTransferable && !e.isPending && e.owner.isMe).Select(e => e.id).ToList()));
            }
            else
            {
                throw new InvalidOperationException("no owner");
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
                NewOwner(request.from); // set owner first
                request.from.QueueEvent(new RequestResult.Leased(request)); // then make available
            }
            else if (isOwner) // I am the current owner and others can subscribe
            {
                if (request.from.isMe) throw new InvalidOperationException("requested, but already own");
                if (participants.Contains(request.from)) // they are already in this
                {
                    request.from.QueueEvent(new RequestResult.Error(request));
                    return;
                }
                // Player subscribed to resource
                request.from.QueueEvent(new RequestResult.Subscribed(request)); // result first, so they Activate before getting new state
                Subscribed(request.from);
            }
            else // Not mine, can't lease
            {
                request.from.QueueEvent(new RequestResult.Error(request));
            }
        }

        // Someone released from me, maybe myself, a resource that I own or supervise
        public void Released(ResourceRelease request)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Released by : " + request.from.name);

            if (isSuper && owner == request.from) // The owner is returning this resource to super, but it might still have participants
            {
                var newParticipants = request.participants.Where(p => p != owner).ToList();
                var newOwner = PlayersManager.BestTransferCandidate(this, newParticipants);
                NewOwner(newOwner); // This notifies all users, if the new owner is active they'll restore the state
                if (newOwner != null)
                {
                    this.pendingRequest = (ResourceEvent)newOwner.QueueEvent(new ResourceTransfer(this, newParticipants, request.abandonedEntities));
                }
                request.from.QueueEvent(new ReleaseResult.Released(request)); // this notifies the old owner that the release was a success
                return;
            }
            if (isOwner) // A participant is unsubscribing from the resource
            {
                Unsubscribed(request.from);
                ClaimAbandonedEntities();
                request.from.QueueEvent(new ReleaseResult.Unsubscribed(request));
                return;
            }
            request.from.QueueEvent(new ReleaseResult.Error(request));
            return;
        }

        // The previous owner has left and I've been assigned (by super) as the new owner
        public void Transfered(ResourceTransfer request)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Transfered by : " + request.from.name);
            if (isAvailable && isActive && isOwner && request.from == super?.owner) // I am a subscriber with a valid state who now owns this resource
            {
                request.from.QueueEvent(new TransferResult.Ok(request));
                return;
            }
            RainMeadow.Debug($"Transfer error : {isAvailable} {isActive} {isOwner} {request.from == super?.owner}");
            request.from.QueueEvent(new TransferResult.Error(request)); // super should retry with someone else
            return;
        }

        // A pending request was answered to
        public void ResolveRequest(RequestResult requestResult)
        {
            RainMeadow.Debug(this);
            if (requestResult is RequestResult.Leased) // I'm the new owner of a previously-free resource
            {
                if (isAvailable) // this was transfered to me because the previous owner left
                {
                    RainMeadow.Debug("Claimed abandoned resource");
                }
                else
                {
                    RainMeadow.Debug("Claimed free resource");
                    Available();
                }
            }
            else if (requestResult is RequestResult.Subscribed) // I'm subscribed to a resource's state and events
            {
                RainMeadow.Debug("Subscribed to resource");
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
        public void ResolveRelease(ReleaseResult releaseResult)
        {
            RainMeadow.Debug(this);

            if (releaseResult is ReleaseResult.Released) // I've let go
            {
                Unavailable();
            }
            else if (releaseResult is ReleaseResult.Unsubscribed) // I'm clear
            {
                Unavailable();
            }
            else if (releaseResult is ReleaseResult.Error) // I should retry
            {
                // todo retry logic
                RainMeadow.Error("released failed for " + this);
            }
            pendingRequest = null;
        }

        // A pending transfer was asnwered to
        public void ResolveTransfer(TransferResult transferResult)
        {
            RainMeadow.Debug(this);

            if (transferResult is TransferResult.Ok) // New owner accepted it
            {
                // no op
            }
            else if (transferResult is TransferResult.Error) // I should retry
            {
                // todo retry logic
                RainMeadow.Error("transfer failed for " + this);
            }
            pendingRequest = null;
        }
    }
}

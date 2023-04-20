using System.Linq;
using System;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        // I request this resource, so I can have either ownership or subscription
        public virtual void Request()
        {
            RainMeadow.Debug(this);
            if (isPending) throw new InvalidOperationException("pending");
            if (isAvailable) throw new InvalidOperationException("available");

            pendingRequest = (ResourceEvent)supervisor.QueueEvent(new ResourceRequest(this));
        }

        // I no longer need this resource, supervisor can coordinate its transfer if needed
        private void Release()
        {
            RainMeadow.Debug(this);
            if (isPending) throw new InvalidOperationException("pending");
            if (!isAvailable) throw new InvalidOperationException("not available");
            if (!canRelease) throw new InvalidOperationException("cant be released in current state");

            pendingRequest = (ResourceEvent)supervisor.QueueEvent(new ResourceRelease(this));
        }

        // Someone requested this resource, if I supervise it I'll lease it
        public void Requested(ResourceRequest request)
        {
            RainMeadow.Debug(this);
            if (isSupervisor)
            {
                if (memberships.ContainsKey(request.from)) // they are already in this
                {
                    request.from.QueueEvent(new RequestResult.Error(request));
                    return;
                }

                if (isFree)
                {
                    // Leased to player
                    request.from.QueueEvent(new RequestResult.Leased(request)); // make available
                    NewOwner(request.from); // then new lease state
                    return;
                }
                else
                {
                    // Already leased, player subscribed
                    request.from.QueueEvent(new RequestResult.Subscribed(request));
                    NewParticipant(request.from);
                    return;
                }
            }

            request.from.QueueEvent(new RequestResult.Error(request));
        }

        // Someone is trying to release this resource, if I supervise it, I'll handle it
        public void Released(ResourceRelease request)
        {
            RainMeadow.Debug(this);
            if(isSupervisor)
            {
                if (!memberships.ContainsKey(request.from)) // they are already out?
                {
                    request.from.QueueEvent(new ReleaseResult.Error(request));
                    return;
                }

                if(request.from == owner) // Owner left, might need a transfer
                {
                    request.from.QueueEvent(new ReleaseResult.Released(request)); // this notifies the old owner that the release was a success
                    ParticipantLeft(request.from);
                    var newOwner = PlayersManager.BestTransferCandidate(this, memberships);
                    NewOwner(newOwner); // This notifies all users, if the new owner is active they'll restore the state
                    if (newOwner != null)
                    {
                        this.pendingRequest = (ResourceEvent)newOwner.QueueEvent(new ResourceTransfer(this));
                    }
                    return;
                }
                else
                {
                    request.from.QueueEvent(new ReleaseResult.Unsubscribed(request)); // non-owner unsubscribed
                    ParticipantLeft(request.from);
                    return;
                }
            }

            request.from.QueueEvent(new ReleaseResult.Error(request)); // I do not manage this resource
        }

        // The previous owner has left and I've been assigned (by super) as the new owner
        public void Transfered(ResourceTransfer request)
        {
            RainMeadow.Debug(this);
            if (isAvailable && isActive && isOwner && request.from == supervisor) // I am a subscriber with a valid state who now owns this resource
            {
                request.from.QueueEvent(new TransferResult.Ok(request));
                return;
            }
            RainMeadow.Debug($"Transfer error : {isAvailable} {isActive} {isOwner} {request.from == supervisor}");
            request.from.QueueEvent(new TransferResult.Error(request)); // super should retry with someone else
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
            if (requestResult.referencedEvent == pendingRequest) pendingRequest = null;
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
            if (pendingRequest == releaseResult.referencedEvent) pendingRequest = null;
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

            if (pendingRequest == transferResult.referencedEvent) pendingRequest = null;
        }
    }
}

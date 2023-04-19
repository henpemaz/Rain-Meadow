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
            if (isAvailable) throw new InvalidOperationException("available");

            pendingRequest = (ResourceEvent)supervisor.QueueEvent(new ResourceRequest(this));
        }

        // I release, possibly to someone else
        private void Release()
        {
            RainMeadow.Debug(this);
            if (isPending) throw new InvalidOperationException("pending");
            if (!isAvailable) throw new InvalidOperationException("not available");
            if (!canRelease) throw new InvalidOperationException("cant be released in current state");

            pendingRequest = (ResourceEvent)supervisor.QueueEvent(new ResourceRelease(this));
        }

        // Someone requested me, maybe myself
        public void Requested(ResourceRequest request)
        {
            RainMeadow.Debug(this);
            if (isSuper)
            {
                if (participants.Contains(request.from)) // they are already in this
                {
                    request.from.QueueEvent(new RequestResult.Error(request));
                    return;
                }
                if (isFree)
                {
                    // Leased to player
                    NewOwner(request.from); // set owner first
                    request.from.QueueEvent(new RequestResult.Leased(request)); // then make available?
                    return;
                }
                else
                {
                    // Already leased, player subscribed
                    NewMember(request.from);
                    request.from.QueueEvent(new RequestResult.Subscribed(request));
                    return;
                }
            }

            request.from.QueueEvent(new RequestResult.Error(request));
        }

        // Someone released from me, maybe myself, a resource that I own or supervise
        public void Released(ResourceRelease request)
        {
            RainMeadow.Debug(this);
            if(isSuper)
            {
                if (!participants.Contains(request.from)) // they are already out?
                {
                    request.from.QueueEvent(new ReleaseResult.Error(request));
                    return;
                }

                if(request.from == owner)
                {
                    MemberLeft(request.from);
                    var newOwner = PlayersManager.BestTransferCandidate(this, participants);
                    NewOwner(newOwner); // This notifies all users, if the new owner is active they'll restore the state
                    if (newOwner != null)
                    {
                        this.pendingRequest = (ResourceEvent)newOwner.QueueEvent(new ResourceTransfer(this));
                    }
                    request.from.QueueEvent(new ReleaseResult.Released(request)); // this notifies the old owner that the release was a success
                    return;
                }
                else
                {
                    MemberLeft(request.from);
                    request.from.QueueEvent(new ReleaseResult.Unsubscribed(request)); // this notifies the old owner that the release was a success
                    return;
                }
            }

            request.from.QueueEvent(new ReleaseResult.Error(request));
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

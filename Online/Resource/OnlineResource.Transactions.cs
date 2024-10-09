using System;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        // I request this resource, so I can have either ownership or subscription
        private void Request()
        {
            RainMeadow.Debug(this);
            if (isPending)
            {
                throw new InvalidOperationException("pending");
            }
            if (isAvailable)
            {
                throw new InvalidOperationException("available");
            }

            ClearIncommingBuffers();
            if(supervisor == null)
            {
                RainMeadow.Debug("Resolving request with no supervisor");
                Available();
            }
            else
            {
                isRequesting = true;
                supervisor.InvokeRPC(this.Requested).Then(this.ResolveRequest);
            }
        }

        // I no longer need this resource, supervisor can coordinate its transfer if needed
        private void Release()
        {
            RainMeadow.Debug(this);
            if (isPending)
            {
                throw new InvalidOperationException("pending");
            }
            if (!isAvailable)
            {
                throw new InvalidOperationException("not available");
            }
            if (!canRelease)
            {
                throw new InvalidOperationException("cant be released in current state");
            }

            if (supervisor == null)
            {
                RainMeadow.Debug("Resolving release with no supervisor");
                Unavailable();
            }
            else
            {
                isReleasing = true;
                supervisor.InvokeRPC(this.Released).Then(this.ResolveRelease);
            }
        }

        // Ask the engine architects before using this
        // emergency release for resources I wasn't meant to be subscribed to
        private void ForceRelease()
        {
            isReleasing = true;
            supervisor.InvokeRPC(this.Released).Then(this.ResolveRelease);
        }

        // Someone requested this resource, if I supervise it I'll lease it
        [RPCMethod]
        protected void Requested(RPCEvent request)
        {
            RainMeadow.Debug(this);
            if (isSupervisor && !super.isReleasing)
            {
                if (owner == null)
                {
                    // Leased to player
                    if (OnlineManager.lobby.gameMode.PlayerCanOwnResource(request.from, this))
                    {
                        request.from.QueueEvent(new GenericResult.Ok(request));
                        NewOwner(request.from);
                        return;
                    } // else fail
                }
                else
                {
                    // Already leased, player subscribed
                    request.from.QueueEvent(new GenericResult.Ok(request));
                    NewParticipant(request.from);
                    return;
                }
            }
            request.from.QueueEvent(new GenericResult.Error(request));
        }

        // Someone is trying to release this resource, if I supervise it, I'll handle it
        [RPCMethod]
        private void Released(RPCEvent request)
        {
            RainMeadow.Debug(this);
            if (isSupervisor && !super.isReleasing)
            {
                request.from.QueueEvent(new GenericResult.Ok(request));
                ParticipantLeft(request.from);
                return;
            }
            request.from.QueueEvent(new GenericResult.Error(request)); // I do not manage this resource
        }

        // The previous owner has left and I've been assigned (by super) as the new owner
        [RPCMethod]
        private void Transfered(RPCEvent request)
        {
            RainMeadow.Debug(this);
            if (isAvailable && !isReleasing && (isActive || participants.Count == 1) && request.from == supervisor) // I am a subscriber with a valid state who now owns this resource
            {
                request.from.QueueEvent(new GenericResult.Ok(request));
                return;
            }

            RainMeadow.Error($"Transfer error : {isAvailable} {!isReleasing} {(isActive || participants.Count == 1)} {request.from == supervisor}");
            request.from.QueueEvent(new GenericResult.Error(request)); // super should retry with someone else
        }

        // A pending request was answered to
        private void ResolveRequest(GenericResult requestResult)
        {
            isRequesting = false;
            if (requestResult is GenericResult.Ok)
            {
                RainMeadow.Debug("Joined resource: " + this);
            }
            else if (requestResult is GenericResult.Error) // I should retry
            {
                RainMeadow.Error("Request failed for " + this);
                PerformRequests();
            }
        }

        // A pending release was answered to
        private void ResolveRelease(GenericResult releaseResult)
        {
            isReleasing = false;
            if (releaseResult is GenericResult.Ok) // I've let go
            {
                RainMeadow.Debug("Left resource: " + this);
            }
            else if (releaseResult is GenericResult.Error) // I should retry
            {
                RainMeadow.Error("Release failed for " + this);
                PerformRequests();
            }
        }

        // A pending transfer was asnwered to
        private void ResolveTransfer(GenericResult transferResult)
        {
            RainMeadow.Debug(this);
            if (transferResult is GenericResult.Ok) // New owner accepted it
            {
                // no op
            }
            else if (transferResult is GenericResult.Error) // I should retry
            {
                RainMeadow.Error("transfer failed for " + this);
                if (super.isActive && isSupervisor)
                {
                    // re-add problematic guy at end of list so not picked again
                    if (participants.Contains(transferResult.from))
                    {
                        participants.Remove(transferResult.from);
                        participants.Add(transferResult.from);
                    }
                    PickNewOwner();
                }
            }
        }
    }
}

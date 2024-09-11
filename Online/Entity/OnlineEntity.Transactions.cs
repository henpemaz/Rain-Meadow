namespace RainMeadow
{
    public abstract partial class OnlineEntity
    {
        // I request, to someone else
        public void Request()
        {
            RainMeadow.Debug(this);
            if (isMine) throw new InvalidProgrammerException("this entity is already mine");
            if (!isTransferable) throw new InvalidProgrammerException("cannot be transfered");
            if (isPending) throw new InvalidProgrammerException("this entity has a pending request");
            if (!currentlyJoinedResource.isAvailable) throw new InvalidProgrammerException("in unavailable resource");
            if (!owner.hasLeft && currentlyJoinedResource.participants.Contains(owner))
            {
                pendingRequest = owner.InvokeRPC(this.Requested).Then(this.ResolveRequest);
            }
            else if (primaryResource.owner != null)
            {
                pendingRequest = primaryResource.owner.InvokeRPC(this.Requested).Then(this.ResolveRequest);
            }
        }

        // I've been requested and I'll pass the entity on
        [RPCMethod]
        public void Requested(RPCEvent request)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Requested by : " + request.from.id);
            if (isTransferable && this.isMine)
            {
                request.from.QueueEvent(new GenericResult.Ok(request)); // your request was well received, now please be patient while I transfer it
                this.primaryResource.LocalEntityTransfered(this, request.from);
            }
            else if (isTransferable && (owner.hasLeft || !currentlyJoinedResource.participants.Contains(owner)) && this.primaryResource.isOwner)
            {
                request.from.QueueEvent(new GenericResult.Ok(request));
                this.primaryResource.LocalEntityTransfered(this, request.from);
            }
            else
            {
                if (!isTransferable) RainMeadow.Debug("Denied because not transferable");
                else if (!isMine) RainMeadow.Debug("Denied because not mine");
                request.from.QueueEvent(new GenericResult.Error(request));
            }
        }

        // my request has been answered to
        // is this really needed?
        // I thought of stuff like "breaking grasps" if a request for the grasped object failed
        public void ResolveRequest(GenericResult requestResult)
        {
            RainMeadow.Debug(this);
            if (requestResult.referencedEvent == pendingRequest) pendingRequest = null;
            if (requestResult is GenericResult.Ok) // I'm the new owner of this entity
            {
                NewOwner(OnlineManager.mePlayer);
            }
            else if (requestResult is GenericResult.Error) // Something went wrong, I should retry
            {
                // todo retry logic
                // abort pending grasps?
                RainMeadow.Error("request failed for " + this);
            }
        }

        // I release this entity (to room host or world host)
        public void Release()
        {
            RainMeadow.Debug(this);
            if (!isMine) throw new InvalidProgrammerException("not mine");
            if (!isTransferable) throw new InvalidProgrammerException("cannot be transfered");
            if (isPending) throw new InvalidProgrammerException("this entity has a pending request");
            if (primaryResource is null) return; // deactivated

            if (primaryResource.isOwner)
            {
                RainMeadow.Debug("Staying as supervisor");
            }
            else if (primaryResource.owner != null)
            {
                this.pendingRequest = primaryResource.owner.InvokeRPC(Released, currentlyJoinedResource).Then(ResolveRelease);
            }
        }

        // someone released "to me"
        [RPCMethod]
        public void Released(RPCEvent rpcEvent, OnlineResource inResource)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Released by : " + rpcEvent.from.id);
            if (isTransferable && this.owner == rpcEvent.from && this.primaryResource.isOwner) // theirs and I can transfer
            {
                rpcEvent.from.QueueEvent(new GenericResult.Ok(rpcEvent)); // ok to them
                if (inResource.isAvailable || inResource.super.isActive)
                {
                    if (this.owner != inResource.owner) this.primaryResource.LocalEntityTransfered(this, inResource.owner);
                }
                else
                {
                    if (!this.isMine) this.primaryResource.LocalEntityTransfered(this, OnlineManager.mePlayer);
                }
            }
            else
            {
                if (!isTransferable) RainMeadow.Error("Denied because not transferable");
                else if (owner != rpcEvent.from) RainMeadow.Error("Denied because not theirs");
                else if (!primaryResource.isOwner) RainMeadow.Error("Denied because I don't supervise it");
                else if (isPending) RainMeadow.Error("Denied because pending");
                rpcEvent.from.QueueEvent(new GenericResult.Error(rpcEvent));
            }
        }

        // got an answer back from my release
        public void ResolveRelease(GenericResult result)
        {
            RainMeadow.Debug(this);
            if (result.referencedEvent == pendingRequest) pendingRequest = null;
            if (result is GenericResult.Ok)
            {
                // no op
            }
            else if (result is GenericResult.Error) // Something went wrong, I should retry
            {
                RainMeadow.Error("request failed for " + this);
                if (isTransferable && isMine && !isPending)
                {
                    Release();
                }
            }
        }
    }
}

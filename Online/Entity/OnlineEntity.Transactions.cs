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
            if (!owner.hasLeft && currentlyJoinedResource.participants.ContainsKey(owner))
            {
                pendingRequest = owner.QueueEvent(new EntityRequest(this));
            }
            else
            {
                pendingRequest = primaryResource.owner.QueueEvent(new EntityRequest(this));
            }
        }

        // I've been requested and I'll pass the entity on
        public void Requested(EntityRequest request)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Requested by : " + request.from.id);
            if (isTransferable && this.isMine)
            {
                request.from.QueueEvent(new GenericResult.Ok(request)); // your request was well received, now please be patient while I transfer it
                this.primaryResource.LocalEntityTransfered(this, request.from);
            }
            else if (isTransferable && (owner.hasLeft || !currentlyJoinedResource.participants.ContainsKey(owner)) && this.primaryResource.isOwner)
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
            if (requestResult is GenericResult.Ok) // I'm the new owner of this entity (comes as separate event though)
            {
                // confirm pending grasps?
            }
            else if (requestResult is GenericResult.Error) // Something went wrong, I should retry
            {
                // todo retry logic
                // abort pending grasps?
                RainMeadow.Error("request failed for " + this);
            }
            pendingRequest = null;
        }

        // I release this entity (to room host or world host)
        public void Release()
        {
            RainMeadow.Debug(this);
            if (!isMine) throw new InvalidProgrammerException("not mine");
            if (!isTransferable) throw new InvalidProgrammerException("cannot be transfered");
            if (isPending && pendingRequest is not EntityLeaveRequest) throw new InvalidProgrammerException("this entity has a pending request");
            if (primaryResource is null) return; // deactivated

            if (primaryResource.isOwner)
            {
                RainMeadow.Debug("Staying as supervisor");
            }
            else
            {
                this.pendingRequest = primaryResource.owner.InvokeRPC(Release, currentlyJoinedResource).Then(ResolveRelease);
            }
        }

        // someone released "to me"
        [RPCMethod]
        // todo maybe arg1 is rpcevent

        // todo friendly way to set a result for iresolvable
        // todo auto result for iresolvable
        public void Released(OnlineResource inResource)
        {
            RPCEvent entityRelease = RPCManager.currentEvent;
            RainMeadow.Debug(this);
            RainMeadow.Debug("Released by : " + entityRelease.from.id);
            if (isTransferable && this.owner == entityRelease.from && this.primaryResource.isOwner) // theirs and I can transfer
            {
                entityRelease.from.QueueEvent(new GenericResult.Ok(entityRelease)); // ok to them
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
                else if (owner != entityRelease.from) RainMeadow.Error("Denied because not theirs");
                else if (!primaryResource.isOwner) RainMeadow.Error("Denied because I don't supervise it");
                else if (isPending) RainMeadow.Error("Denied because pending");
                entityRelease.from.QueueEvent(new GenericResult.Error(entityRelease));
            }
        }

        // got an answer back from my release
        public void ResolveRelease(GenericResult result)
        {
            RainMeadow.Debug(this);
            if (result is GenericResult.Ok)
            {
                // ?
            }
            else if (result is GenericResult.Error) // Something went wrong, I should retry
            {
                // todo retry logic
                RainMeadow.Error("request failed for " + this);
            }
            pendingRequest = null;
        }
    }
}

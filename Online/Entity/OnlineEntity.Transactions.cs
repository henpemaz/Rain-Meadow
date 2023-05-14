namespace RainMeadow
{

    public abstract partial class OnlineEntity
    {
        // I request, to someone else
        public void Request()
        {
            RainMeadow.Debug(this);
            if (owner.isMe) throw new InvalidProgrammerException("this entity is already mine");
            if (!isTransferable) throw new InvalidProgrammerException("cannot be transfered");
            if (isPending) throw new InvalidProgrammerException("this entity has a pending request");
            if (!lowestResource.isAvailable) throw new InvalidProgrammerException("in unavailable resource");
            if (!owner.hasLeft && lowestResource.participants.ContainsKey(owner))
            {
                pendingRequest = owner.QueueEvent(new EntityRequest(this));
            }
            else
            {
                pendingRequest = highestResource.owner.QueueEvent(new EntityRequest(this));
            }
        }

        // I've been requested and I'll pass the entity on
        public void Requested(EntityRequest request)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Requested by : " + request.from.name);
            if (isTransferable && this.owner.isMe)
            {
                request.from.QueueEvent(new EntityRequestResult.Ok(request)); // your request was well received, now please be patient while I transfer it
                this.highestResource.old_EntityNewOwner(this, request.from);
            }
            else if (isTransferable && (owner.hasLeft || !lowestResource.participants.ContainsKey(owner)) && this.highestResource.owner.isMe)
            {
                request.from.QueueEvent(new EntityRequestResult.Ok(request));
                this.highestResource.old_EntityNewOwner(this, request.from);
            }
            else
            {
                if (!isTransferable) RainMeadow.Debug("Denied because not transferable");
                else if (!owner.isMe) RainMeadow.Debug("Denied because not mine");
                request.from.QueueEvent(new EntityRequestResult.Error(request));
            }
        }

        // my request has been answered to
        // is this really needed?
        // I thought of stuff like "breaking grasps" if a request for the grasped object failed
        public void ResolveRequest(EntityRequestResult requestResult)
        {
            RainMeadow.Debug(this);
            if (requestResult is EntityRequestResult.Ok) // I'm the new owner of this entity (comes as separate event though)
            {
                // confirm pending grasps?
            }
            else if (requestResult is EntityRequestResult.Error) // Something went wrong, I should retry
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
            if (!owner.isMe) throw new InvalidProgrammerException("not mine");
            if (!isTransferable) throw new InvalidProgrammerException("cannot be transfered");
            if (isPending) throw new InvalidProgrammerException("this entity has a pending request");
            if (highestResource is null) return; // deactivated

            if (highestResource.owner.isMe)
            {
                RainMeadow.Debug("Staying as supervisor");
            }
            else
            {
                this.pendingRequest = highestResource.owner.QueueEvent(new EntityReleaseEvent(this, lowestResource));
            }
        }

        // someone released "to me"
        public void Released(EntityReleaseEvent entityRelease)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Released by : " + entityRelease.from.name);
            if (isTransferable && this.owner == entityRelease.from && this.highestResource.owner.isMe) // theirs and I can transfer
            {
                entityRelease.from.QueueEvent(new EntityReleaseResult.Ok(entityRelease)); // ok to them
                var res = entityRelease.inResource;
                if (res.isAvailable || res.super.isActive)
                {
                    if (this.owner != res.owner) this.highestResource.old_EntityNewOwner(this, res.owner);
                }
                else
                {
                    if (!this.owner.isMe) this.highestResource.old_EntityNewOwner(this, PlayersManager.mePlayer);
                }
            }
            else
            {
                if (!isTransferable) RainMeadow.Error("Denied because not transferable");
                else if (owner != entityRelease.from) RainMeadow.Error("Denied because not theirs");
                else if (!highestResource.owner.isMe) RainMeadow.Error("Denied because I don't supervise it");
                else if (isPending) RainMeadow.Error("Denied because pending");
                entityRelease.from.QueueEvent(new EntityReleaseResult.Error(entityRelease));
            }
        }

        // got an answer back from my release
        public void ResolveRelease(EntityReleaseResult result)
        {
            RainMeadow.Debug(this);
            if (result is EntityReleaseResult.Ok)
            {
                // ?
            }
            else if (result is EntityReleaseResult.Error) // Something went wrong, I should retry
            {
                // todo retry logic
                RainMeadow.Error("request failed for " + this);
            }
            pendingRequest = null;
        }
    }
}

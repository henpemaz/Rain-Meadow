using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        public abstract World World { get; }
        public Dictionary<OnlineEntity.EntityId, NewEntityEvent> registeredEntities;
        public Dictionary<OnlineEntity.EntityId, EntityMembership> entities;

        // An entity I control has entered the resource, consider registering or joining
        // called from entity join logic - entities join on a queue of resources they need to join
        public void LocalEntityEntered(OnlineEntity oe)
        {
            RainMeadow.Debug(this);
            if (!isActive) throw new InvalidOperationException("not active");
            if (!oe.isMine) throw new InvalidOperationException("not mine");
            if (entities.ContainsKey(oe.id)) throw new InvalidOperationException("already in entities");
            RainMeadow.Debug($"{this} - joining with {oe}");
            if (oe.primaryResource != null)
            {
                EntityJoin(oe);
            }
            else // brand new
            {
                EntityRegister(oe);
            }
        }

        // An entity I control is being added to this resource for the first time, to be sent in full
        protected void EntityRegister(OnlineEntity oe)
        {
            RainMeadow.Debug(this);
            if (!isActive) throw new InvalidProgrammerException("not active");
            if (oe.primaryResource != null) { throw new InvalidOperationException("already in a resource"); }

            if (isOwner) // enter right away
            {
                EntityRegisteredInResource(oe.AsNewEntityEvent(this));
            }
            else // request to register
            {
                oe.pendingRequest = owner.InvokeRPC(this.OnEntityRegisterRequest, oe.AsNewEntityEvent(this)).Then(OnRegisterResolve);
            }
        }

        // as owner, from other
        [RPCMethod]
        public void OnEntityRegisterRequest(RPCEvent rpcEvent, NewEntityEvent newEntityEvent)
        {
            RainMeadow.Debug(this);
            if (isOwner && isActive)
            {
                EntityRegisteredInResource(newEntityEvent);
                rpcEvent.from.QueueEvent(new GenericResult.Ok(rpcEvent));
            }
            else
            {
                rpcEvent.from.QueueEvent(new GenericResult.Error(rpcEvent));
            }
        }

        // ok from owner
        public void OnRegisterResolve(GenericResult registerResult)
        {
            RainMeadow.Debug(this);
            var nee = ((registerResult.referencedEvent as RPCEvent).args[0]) as NewEntityEvent;
            var oe = nee.entityId.FindEntity();
            if (oe.pendingRequest == registerResult.referencedEvent) oe.pendingRequest = null;

            if (registerResult is GenericResult.Ok) // success
            {
                
            }
            else if (registerResult is GenericResult.Error) // retry
            {
                // todo retry
            }
        }

        // registering new entity
        private void EntityRegisteredInResource(NewEntityEvent newEntityEvent)
        {
            RainMeadow.Debug(this);
            if (!isOwner) throw new InvalidProgrammerException("not owner");

            // todo, I'm re-creating my own entities here if i'm owner running the direct path. how fix with minimun lookups?
            registeredEntities.Add(newEntityEvent.entityId, newEntityEvent);
            OnlineEntity oe = OnlineEntity.FromNewEntityEvent(newEntityEvent, this);
            entities.Add(oe.id, new EntityMembership(oe, this));
            if (!oe.isMine) oe.EnterResource(this);
            oe.OnJoinedResource(this);
        }


        // An entity of mine previously created, joining this resource
        protected void EntityJoin(OnlineEntity oe)
        {
            RainMeadow.Debug(this);
            if (!isActive) throw new InvalidProgrammerException("not active");
            if (oe.currentlyJoinedResource != super) { throw new InvalidOperationException("trying to join but not in super"); }

            if (isOwner) // join right away
            {
                EntityJoinedResource(oe);
            }
            else // request to join
            {
                RequestJoinEntity(oe);
            }
        }

        public void RequestJoinEntity(OnlineEntity oe)
        {
            RainMeadow.Debug(this);
            if (oe.isPending) throw new InvalidOperationException("can't enter subresource if pending");

            oe.pendingRequest = owner.InvokeRPC(this.OnEntityJoinRequest, oe, oe.GetState(oe.owner.tick, this)).NotBefore(super.entities[oe.id].memberSinceTick).Then(this.OnJoinResolve);
        }

        [RPCMethod]
        public void OnEntityJoinRequest(RPCEvent rpcEvent, OnlineEntity oe)
        {
            RainMeadow.Debug(this);
            if (isOwner && isActive)
            {
                EntityJoinedResource(oe);
                rpcEvent.from.QueueEvent(new GenericResult.Ok(rpcEvent));
            }
            else
            {
                rpcEvent.from.QueueEvent(new GenericResult.Error(rpcEvent));
            }
        }

        public void OnJoinResolve(GenericResult entityJoinResult)
        {
            RainMeadow.Debug(this);
            var oe = ((entityJoinResult.referencedEvent as RPCEvent).args[0] as OnlineEntity);
            if (oe.pendingRequest == entityJoinResult.referencedEvent) oe.pendingRequest = null;

            if (entityJoinResult is GenericResult.Ok) // success
            {
                EntityJoinedResource(oe);
            }
            else if (entityJoinResult is GenericResult.Error) // retry
            {
                // todo retry
            }
        }

        // existing entity joins
        private void EntityJoinedResource(OnlineEntity oe)
        {
            RainMeadow.Debug(this);
            entities.Add(oe.id, new EntityMembership(oe, this));
            if (!oe.isMine) oe.EnterResource(this);
            oe.OnJoinedResource(this);
        }

        public void LocalEntityLeft(OnlineEntity oe)
        {
            RainMeadow.Debug(this);
            if (!isActive) throw new InvalidProgrammerException("not active");
            if (oe.currentlyJoinedResource != this) { throw new InvalidOperationException("trying to leave but not lowest"); }

            if (isOwner) // leave right away
            {
                EntityLeftResource(oe);
            }
            else // request to leave
            {
                RequestEntityLeave(oe);
            }
        }

        public void RequestEntityLeave(OnlineEntity oe)
        {
            RainMeadow.Debug(this);
            if (oe.isPending) throw new InvalidOperationException("can't leave if pending");
            oe.pendingRequest = owner.InvokeRPC(this.OnEntityLeaveRequest, oe).Then(this.OnEntityLeaveResolve).NotBefore(entities[oe.id].memberSinceTick);
        }

        [RPCMethod]
        public void OnEntityLeaveRequest(RPCEvent rpcEvent, OnlineEntity oe)
        {
            RainMeadow.Debug(this);
            if (isOwner && isActive)
            {
                EntityLeftResource(oe);
                rpcEvent.from.QueueEvent(new GenericResult.Ok(rpcEvent));
            }
            else
            {
                rpcEvent.from.QueueEvent(new GenericResult.Error(rpcEvent));
            }
        }

        public void OnEntityLeaveResolve(GenericResult entityLeaveResult)
        {
            RainMeadow.Debug(this);
            var oe = (entityLeaveResult.referencedEvent as RPCEvent).args[0] as OnlineEntity;
            if (oe.pendingRequest == entityLeaveResult.referencedEvent) oe.pendingRequest = null;

            if (entityLeaveResult is GenericResult.Ok) // success
            {

            }
            else if (entityLeaveResult is GenericResult.Error) // retry
            {
                // todo retry
            }
        }

        public void EntityLeftResource(OnlineEntity oe)
        {
            RainMeadow.Debug(this);
            registeredEntities.Remove(oe.id);
            entities.Remove(oe.id);
            if (!oe.isMine) oe.LeaveResource(this);
            oe.OnLeftResource(this);
        }

        public void LocalEntityTransfered(OnlineEntity oe, OnlinePlayer to)
        {
            RainMeadow.Debug(this);
            if (!isActive) throw new InvalidOperationException("not active");
            if (oe.primaryResource != this) throw new InvalidOperationException("transfered in wrong resource");
            if (!oe.isMine && !isOwner) throw new InvalidOperationException("not my business");

            if (isOwner) // transfer right away
            {
                EntityTransfered(oe, to);
            }
            else // request to transfer
            {
                RequestEntityTransfer(oe, to);
            }
        }

        public void RequestEntityTransfer(OnlineEntity oe, OnlinePlayer to)
        {
            RainMeadow.Debug(this);
            if (oe.isPending) throw new InvalidOperationException("can't trandfer if pending");
            owner.InvokeRPC(this.OnEntityTransferRequest, oe, to).Then(this.ResolveTransfer);
        }

        [RPCMethod]
        public void OnEntityTransferRequest(RPCEvent entityTransferRequest, OnlineEntity oe, OnlinePlayer newOwner)
        {
            RainMeadow.Debug(this);
            if (isOwner && isActive)
            {
                EntityTransfered(oe, newOwner);
                entityTransferRequest.from.QueueEvent(new GenericResult.Ok(entityTransferRequest));
            }
            else
            {
                entityTransferRequest.from.QueueEvent(new GenericResult.Error(entityTransferRequest));
            }
        }

        public void OnEntityTransferResolve(GenericResult entityTransferResult)
        {
            RainMeadow.Debug(this);
            var oe = (entityTransferResult.referencedEvent as RPCEvent).args[0] as OnlineEntity;
            if (oe.pendingRequest == entityTransferResult.referencedEvent) oe.pendingRequest = null;

            if (entityTransferResult is GenericResult.Ok) // success
            {

            }
            else if (entityTransferResult is GenericResult.Error) // retry
            {
                // todo retry
            }
        }

        public void EntityTransfered(OnlineEntity oe, OnlinePlayer to)
        {
            RainMeadow.Debug(this);
            oe.NewOwner(to);
        }
    }
}

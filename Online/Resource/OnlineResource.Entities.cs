using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        public Dictionary<OnlineEntity.EntityId, EntityDefinition> registeredEntities;
        public Dictionary<OnlineEntity.EntityId, EntityMembership> entities;

        // An entity I control has entered the resource, consider registering or joining
        // called from entity join logic - entities join on a queue of resources they need to join
        public void LocalEntityEntered(OnlineEntity oe)
        {
            RainMeadow.Debug($"{oe} : {this}");
            if (!isActive) throw new InvalidOperationException("not active");
            if (!oe.isMine) throw new InvalidOperationException("not mine");
            if (entities.ContainsKey(oe.id)) throw new InvalidOperationException("already in entities");
            RainMeadow.Debug($"{this} - joining with {oe}");
            if (oe.primaryResource != null && !oe.primaryResource.IsSibling(this))
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
            RainMeadow.Debug($"{oe} : {this}");
            if (!isActive) throw new InvalidProgrammerException("not active");
            if (oe.primaryResource != null && !oe.primaryResource.IsSibling(this)) { throw new InvalidOperationException("already in a resource"); }

            OnlineManager.lobby.gameMode.NewEntity(oe, this);

            if (isOwner) // I control this, enter right away
            {
                EntityRegisteredInResource(oe, oe.definition, null);
            }
            else // request to register
            {
                oe.pendingRequest = owner.InvokeRPC(this.OnEntityRegisterRequest, oe.definition, oe.GetState(oe.owner.tick, this)).Then(OnRegisterResolve);
            }
        }

        // as owner, from other
        [RPCMethod]
        public void OnEntityRegisterRequest(RPCEvent rpcEvent, EntityDefinition newEntityEvent, OnlineEntity.EntityState initialState)
        {
            RainMeadow.Debug($"{newEntityEvent} : {this}");
            if (isOwner && isActive)
            {
                OnNewRemoteEntity(newEntityEvent, initialState);
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
            var nee = ((registerResult.referencedEvent as RPCEvent).args[0]) as EntityDefinition;
            var oe = nee.entityId.FindEntity();
            RainMeadow.Debug($"{oe} : {this}");
            if (oe.pendingRequest == registerResult.referencedEvent) oe.pendingRequest = null;
            else RainMeadow.Error($"Weird event situation, pending is {oe.pendingRequest} and referenced is {registerResult.referencedEvent}");

            if (registerResult is GenericResult.Ok) // success
            {
                
            }
            else if (registerResult is GenericResult.Error) // retry
            {
                // todo retry
            }
        }

        // recreate from event
        public void OnNewRemoteEntity(EntityDefinition entityDefinition, OnlineEntity.EntityState initialState)
        {
            if (OnlineManager.lobby.PlayerFromId(entityDefinition.owner) == null)
            {
                RainMeadow.Error($"Owner not found for entitydefinition: {entityDefinition} : {entityDefinition.owner}");
                return;
            }
            OnlineEntity oe = entityDefinition.entityId.FindEntity(quiet: true) ?? entityDefinition.MakeEntity(this);
            RainMeadow.Debug($"{oe} : {this}");
            if (!oe.isMine && !registeredEntities.ContainsKey(entityDefinition.entityId)) OnlineManager.lobby.gameMode.NewEntity(oe, this);
            EntityRegisteredInResource(oe, entityDefinition, initialState);
        }

        // registering new entity
        private void EntityRegisteredInResource(OnlineEntity oe, EntityDefinition newEntityEvent, OnlineEntity.EntityState initialState)
        {
            RainMeadow.Debug($"{oe} : {this}");
            registeredEntities.Add(newEntityEvent.entityId, newEntityEvent);
            EntityJoinedResource(oe, initialState);
        }


        // An entity of mine previously created, joining this resource
        protected void EntityJoin(OnlineEntity oe)
        {
            RainMeadow.Debug($"{oe} : {this}");
            if (!isActive) throw new InvalidProgrammerException("not active");
            if (oe.currentlyJoinedResource != super) { throw new InvalidOperationException("trying to join but not in super"); }

            if (isOwner) // join right away
            {
                EntityJoinedResource(oe, null);
            }
            else // request to join
            {
                RequestJoinEntity(oe);
            }
        }

        public void RequestJoinEntity(OnlineEntity oe)
        {
            RainMeadow.Debug($"{oe} : {this}");
            if (oe.isPending) throw new InvalidOperationException("can't enter subresource if pending");

            oe.pendingRequest = owner.InvokeRPC(this.OnEntityJoinRequest, oe, oe.GetState(oe.owner.tick, this)).NotBefore(super.entities[oe.id].memberSinceTick).Then(this.OnJoinResolve);
        }

        [RPCMethod]
        public void OnEntityJoinRequest(RPCEvent rpcEvent, OnlineEntity oe, OnlineEntity.EntityState initialState)
        {
            RainMeadow.Debug($"{oe} : {this}");
            if (isOwner && isActive)
            {
                EntityJoinedResource(oe, initialState);
                rpcEvent.from.QueueEvent(new GenericResult.Ok(rpcEvent));
            }
            else
            {
                rpcEvent.from.QueueEvent(new GenericResult.Error(rpcEvent));
            }
        }

        public void OnJoinResolve(GenericResult entityJoinResult)
        {
            var oe = ((entityJoinResult.referencedEvent as RPCEvent).args[0] as OnlineEntity);
            RainMeadow.Debug($"{oe} : {this}");
            if (oe.pendingRequest == entityJoinResult.referencedEvent) oe.pendingRequest = null;
            else RainMeadow.Error($"Weird event situation, pending is {oe.pendingRequest} and referenced is {entityJoinResult.referencedEvent}");

            if (entityJoinResult is GenericResult.Ok) // success
            {

            }
            else if (entityJoinResult is GenericResult.Error) // retry
            {
                // todo retry
            }
        }

        // existing entity joins
        private void EntityJoinedResource(OnlineEntity oe, OnlineEntity.EntityState initialState)
        {
            RainMeadow.Debug($"{oe} : {this}");
            entities.Add(oe.id, new EntityMembership(oe, this));
            if (isOwner) lastModified = OnlineManager.mePlayer.tick;
            if (!oe.isMine)
            {
                oe.EnterResource(this);
                oe.ReadState(initialState, this);
            }
            oe.OnJoinedResource(this);
        }

        public void LocalEntityLeft(OnlineEntity oe)
        {
            RainMeadow.Debug($"{oe} : {this}");
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
            RainMeadow.Debug($"{oe} : {this}");
            if (oe.isPending) throw new InvalidOperationException("can't leave if pending");
            oe.pendingRequest = owner.InvokeRPC(this.OnEntityLeaveRequest, oe).Then(this.OnEntityLeaveResolve).NotBefore(entities[oe.id].memberSinceTick);
        }

        [RPCMethod]
        public void OnEntityLeaveRequest(RPCEvent rpcEvent, OnlineEntity oe)
        {
            RainMeadow.Debug($"{oe} : {this}");
            if (oe.owner == rpcEvent.from && isOwner && isActive)
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
            var oe = (entityLeaveResult.referencedEvent as RPCEvent).args[0] as OnlineEntity;
            RainMeadow.Debug($"{oe} : {this}");
            if (oe.pendingRequest == entityLeaveResult.referencedEvent) oe.pendingRequest = null;
            else RainMeadow.Error($"Weird event situation, pending is {oe.pendingRequest} and referenced is {entityLeaveResult.referencedEvent}");

            if (entityLeaveResult is GenericResult.Ok) // success
            {

            }
            else if (entityLeaveResult is GenericResult.Error) // retry
            {
                if (oe.isMine)
                {
                    if (isActive)
                    {
                        LocalEntityLeft(oe);
                    }
                    else // resource deactivated
                    {
                        oe.JoinOrLeavePending();
                    }
                }
            }
        }

        public void EntityLeftResource(OnlineEntity oe)
        {
            RainMeadow.Debug($"{oe} : {this}");
            if (oe.primaryResource == this && !registeredEntities.ContainsKey(oe.id)) throw new InvalidProgrammerException("wasn't registered in resource");
            if (!entities.ContainsKey(oe.id)) throw new InvalidProgrammerException("wasn't joined in resource");
            registeredEntities.Remove(oe.id);
            entities.Remove(oe.id);
            if (isOwner) lastModified = OnlineManager.mePlayer.tick;
            if (!oe.isMine) oe.LeaveResource(this);
            oe.OnLeftResource(this);
        }

        public void LocalEntityTransfered(OnlineEntity oe, OnlinePlayer to)
        {
            RainMeadow.Debug($"{oe} : {this} : to {to}");
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
            RainMeadow.Debug($"{oe} : {this} : to {to}");
            if (oe.isPending) throw new InvalidOperationException("can't trandfer if pending");
            oe.pendingRequest = owner.InvokeRPC(this.OnEntityTransferRequest, oe, to).Then(this.OnEntityTransferResolve);
        }

        [RPCMethod]
        public void OnEntityTransferRequest(RPCEvent entityTransferRequest, OnlineEntity oe, OnlinePlayer newOwner)
        {
            RainMeadow.Debug($"{oe} : {this} : to {newOwner}");
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
            var oe = (entityTransferResult.referencedEvent as RPCEvent).args[0] as OnlineEntity;
            RainMeadow.Debug($"{oe} : {this}");
            if (oe.pendingRequest == entityTransferResult.referencedEvent) oe.pendingRequest = null;
            else RainMeadow.Error($"Weird event situation, pending is {oe.pendingRequest} and referenced is {entityTransferResult.referencedEvent}");

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
            RainMeadow.Debug($"{oe} : {this} : to {to}");
            oe.NewOwner(to);
            if (isOwner) lastModified = OnlineManager.mePlayer.tick;
        }
    }
}

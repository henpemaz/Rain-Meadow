using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        public Dictionary<OnlineEntity.EntityId, OnlineEntity.EntityDefinition> registeredEntities;
        public Dictionary<OnlineEntity.EntityId, EntityMembership> joinedEntities;
        public List<OnlineEntity> activeEntities;

        // An entity I control has entered the resource, consider registering or joining
        // called from entity join logic - entities join on a queue of resources they need to join
        public void LocalEntityEntered(OnlineEntity oe)
        {
            RainMeadow.Debug($"{oe} : {this}");
            if (!isActive) throw new InvalidOperationException("not active");
            if (!oe.isMine) throw new InvalidOperationException("not mine");
            if (joinedEntities.ContainsKey(oe.id)) throw new InvalidOperationException("already in entities");
            RainMeadow.Debug($"{this} - joining with {oe}");
            if (oe.primaryResource != null && !oe.primaryResource.IsSibling(this))
            {
                EntityJoin(oe);
            }
            else // brand new
            {
                if (!oe.everRegistered)
                {
                    oe.everRegistered = true;
                    OnlineManager.lobby.gameMode.NewEntity(oe, this);
                }
                EntityRegister(oe);
            }
        }

        // An entity I control is being added to this resource for the first time, to be sent in full
        protected void EntityRegister(OnlineEntity oe)
        {
            RainMeadow.Debug($"{oe} : {this}");
            if (!isActive) throw new InvalidProgrammerException("not active");
            if (oe.primaryResource != null && !oe.primaryResource.IsSibling(this)) { throw new InvalidOperationException("already in a resource"); }

            if (isOwner) // I control this, enter right away
            {
                EntityRegisteredInResource(oe, oe.MakeDefinition(this), null);
            }
            else if (owner != null && !owner.hasLeft) // request to register
            {
                oe.pendingRequest = owner.InvokeRPC(this.OnEntityRegisterRequest, oe.MakeDefinition(this), oe.GetState(oe.owner.tick, this)).Then(OnRegisterResolve);
            }
        }

        // as owner, from other
        [RPCMethod]
        public void OnEntityRegisterRequest(RPCEvent rpcEvent, OnlineEntity.EntityDefinition newEntityEvent, OnlineEntity.EntityState initialState)
        {
            RainMeadow.Debug($"{newEntityEvent} : {this}");
            if (isOwner && isActive && !isReleasing)
            {
                OnNewRemoteEntity(newEntityEvent, initialState);
                rpcEvent.from.QueueEvent(new GenericResult.Ok(rpcEvent));
            }
            else
            {
                RainMeadow.Error($"Failed with reasons {isOwner} {isActive} {!isReleasing}");
                rpcEvent.from.QueueEvent(new GenericResult.Error(rpcEvent));
            }
        }

        // ok from owner
        public void OnRegisterResolve(GenericResult registerResult)
        {
            var nee = ((registerResult.referencedEvent as RPCEvent).args[0]) as OnlineEntity.EntityDefinition;
            var ini = ((registerResult.referencedEvent as RPCEvent).args[1]) as OnlineEntity.EntityState;
            var oe = nee.entityId.FindEntity();
            RainMeadow.Debug($"{oe} : {this}");
            if (oe.pendingRequest == registerResult.referencedEvent) oe.pendingRequest = null;
            else if (isActive) RainMeadow.Error($"Weird event situation, pending is {oe.pendingRequest} and referenced is {registerResult.referencedEvent}");

            if (registerResult is GenericResult.Ok) // success
            {
                if (isActive)
                {
                    EntityRegisteredInResource(oe, nee, ini);
                }
            }
            else if (registerResult is GenericResult.Error) // retry
            {
                oe.pendingRequest = null;
                if (oe.isMine) oe.JoinOrLeavePending();
            }
        }

        // recreate from event
        public void OnNewRemoteEntity(OnlineEntity.EntityDefinition entityDefinition, OnlineEntity.EntityState initialState)
        {
            if (OnlineManager.lobby.PlayerFromId(entityDefinition.owner) == null)
            {
                // might be just due to bad timming on player join/leave
                RainMeadow.Error($"Owner not found for entitydefinition: {entityDefinition} : {entityDefinition.owner}");
                return;
            }
            OnlineEntity oe = null;
            try
            {
                oe = entityDefinition.entityId.FindEntity(quiet: true) ?? entityDefinition.MakeEntity(this, initialState);
            }
            catch (Exception e)
            {
                RainMeadow.Error($"Failed to spawn new remote entity in {this} : {entityDefinition}");
                RainMeadow.Error(e);
                OnlineManager.recentEntities.Remove(entityDefinition.entityId); // no spurious entries
                entityDefinition.failedToSpawn = true;
                return;
            }
            if (oe.primaryResource == this)
            {
                RainMeadow.Error($"Already registered: " + oe);
                return;
            }

            if (oe.primaryResource is OnlineResource otherResource && otherResource != this && NetIO.IsNewer(otherResource.registeredEntities[oe.id].version, entityDefinition.version))
            {
                // recently registered elsewhere, to not move back here
                RainMeadow.Trace($"Entity moved recently: " + oe);
                return;
            }
            RainMeadow.Debug($"{oe} : {this}");
            if (!oe.everRegistered)
            {
                oe.everRegistered = true;
                OnlineManager.lobby.gameMode.NewEntity(oe, this);
            }
            EntityRegisteredInResource(oe, entityDefinition, initialState);
        }

        // registering new entity
        private void EntityRegisteredInResource(OnlineEntity oe, OnlineEntity.EntityDefinition newEntityEvent, OnlineEntity.EntityState initialState)
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
            else if (owner != null && !owner.hasLeft) // request to join
            {
                RequestJoinEntity(oe);
            }
        }

        public void RequestJoinEntity(OnlineEntity oe)
        {
            RainMeadow.Debug($"{oe} : {this}");
            if (oe.isPending) throw new InvalidOperationException("can't enter subresource if pending");

            oe.pendingRequest = owner.InvokeRPC(this.OnEntityJoinRequest, oe, oe.GetState(oe.owner.tick, this)).Then(this.OnJoinResolve);
        }

        [RPCMethod]
        public void OnEntityJoinRequest(RPCEvent rpcEvent, OnlineEntity oe, OnlineEntity.EntityState initialState)
        {
            RainMeadow.Debug($"{oe} : {this}");
            if (oe != null && isOwner && isActive && !isReleasing)
            {
                EntityJoinedResource(oe, initialState);
                rpcEvent.from.QueueEvent(new GenericResult.Ok(rpcEvent));
            }
            else
            {
                RainMeadow.Debug($"failed with reasons: {oe != null} {isOwner} {isActive} {!isReleasing}");
                rpcEvent.from.QueueEvent(new GenericResult.Error(rpcEvent));
            }
        }

        public void OnJoinResolve(GenericResult entityJoinResult)
        {
            var oe = ((entityJoinResult.referencedEvent as RPCEvent).args[0] as OnlineEntity);
            var ini = ((entityJoinResult.referencedEvent as RPCEvent).args[1] as OnlineEntity.EntityState);
            RainMeadow.Debug($"{oe} : {this}");
            if (oe.pendingRequest == entityJoinResult.referencedEvent) oe.pendingRequest = null;
            else if (isActive) RainMeadow.Error($"Weird event situation, pending is {oe.pendingRequest} and referenced is {entityJoinResult.referencedEvent}");

            if (entityJoinResult is GenericResult.Ok) // success
            {
                if (isActive)
                {
                    EntityJoinedResource(oe, ini);
                }
            }
            else if (entityJoinResult is GenericResult.Error) // retry
            {
                if (oe.isMine) oe.JoinOrLeavePending();
            }
        }

        // existing entity joins
        private void EntityJoinedResource(OnlineEntity oe, OnlineEntity.EntityState initialState)
        {
            RainMeadow.Debug($"{oe} : {this}");
            joinedEntities.Add(oe.id, new EntityMembership(oe));
            activeEntities.Add(oe);
            EntitiesModified();

            oe.OnJoinedResource(this, initialState);
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
            else if (owner != null && !owner.hasLeft) // request to leave
            {
                RequestEntityLeave(oe);
            }
        }

        public void RequestEntityLeave(OnlineEntity oe)
        {
            RainMeadow.Debug($"{oe} : {this}");
            if (oe.isPending) throw new InvalidOperationException("can't leave if pending");
            oe.pendingRequest = owner.InvokeRPC(this.OnEntityLeaveRequest, oe).Then(this.OnEntityLeaveResolve);
        }

        [RPCMethod]
        public void OnEntityLeaveRequest(RPCEvent rpcEvent, OnlineEntity oe)
        {
            RainMeadow.Debug($"{oe} : {this}");
            if (oe != null && oe.owner == rpcEvent.from && isOwner && isActive && !isReleasing)
            {
                EntityLeftResource(oe);
                rpcEvent.from.QueueEvent(new GenericResult.Ok(rpcEvent));
            }
            else
            {
                RainMeadow.Debug($"failed with reasons: {oe != null} {oe?.owner == rpcEvent.from} {isOwner} {isActive} {!isReleasing}");
                rpcEvent.from.QueueEvent(new GenericResult.Error(rpcEvent));
            }
        }

        public void OnEntityLeaveResolve(GenericResult entityLeaveResult)
        {
            var oe = (entityLeaveResult.referencedEvent as RPCEvent).args[0] as OnlineEntity;
            RainMeadow.Debug($"{oe} : {this}");
            if (oe.pendingRequest == entityLeaveResult.referencedEvent) oe.pendingRequest = null;
            else if (isActive) RainMeadow.Error($"Weird event situation, pending is {oe.pendingRequest} and referenced is {entityLeaveResult.referencedEvent}");

            if (entityLeaveResult is GenericResult.Ok) // success
            {
                if (isActive)
                {
                    EntityLeftResource(oe);
                }
            }
            else if (entityLeaveResult is GenericResult.Error) // retry
            {
                if (oe.isMine) oe.JoinOrLeavePending();
            }
        }

        public void EntityLeftResource(OnlineEntity oe)
        {
            RainMeadow.Debug($"{oe} : {this}");
            if (oe.primaryResource == this && !registeredEntities.ContainsKey(oe.id)) throw new InvalidProgrammerException("wasn't registered in resource");
            if (!joinedEntities.ContainsKey(oe.id)) throw new InvalidProgrammerException("wasn't joined in resource");
            registeredEntities.Remove(oe.id);
            joinedEntities.Remove(oe.id);
            activeEntities.Remove(oe);
            EntitiesModified();
            if (!oe.isMine) oe.ExitResource(this);
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
            else if (owner != null && !owner.hasLeft) // request to transfer
            {
                RequestEntityTransfer(oe, to);
            }
            else
            {
                RainMeadow.Error("Can't transfer");
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
            if (oe != null && entityTransferRequest.from == oe.owner && isOwner && isActive && !isReleasing)
            {
                OnlineManager.RunDeferred(() => { // deferred so we receive the incoming state first
                    EntityTransfered(oe, newOwner);
                });
                entityTransferRequest.from.QueueEvent(new GenericResult.Ok(entityTransferRequest));
            }
            else
            {
                RainMeadow.Debug($"failed with reasons: {oe != null} {entityTransferRequest.from == oe?.owner} {isOwner} {isActive} {!isReleasing}");
                entityTransferRequest.from.QueueEvent(new GenericResult.Error(entityTransferRequest));
            }
        }

        public void OnEntityTransferResolve(GenericResult entityTransferResult)
        {
            var oe = (entityTransferResult.referencedEvent as RPCEvent).args[0] as OnlineEntity;
            var to = (entityTransferResult.referencedEvent as RPCEvent).args[1] as OnlinePlayer;
            RainMeadow.Debug($"{oe} : {this}");
            if (!isActive) throw new InvalidOperationException("not active");
            if (oe.pendingRequest == entityTransferResult.referencedEvent) oe.pendingRequest = null;
            else if (isActive) RainMeadow.Error($"Weird event situation, pending is {oe.pendingRequest} and referenced is {entityTransferResult.referencedEvent}");

            if (entityTransferResult is GenericResult.Ok) // success
            {
                // no op, state comes with updated info
            }
            else if (entityTransferResult is GenericResult.Error) // retry
            {
                oe.pendingRequest = null;
                if (oe.isMine && joinedEntities.ContainsKey(oe.id) && participants.Contains(to)) LocalEntityTransfered(oe, to);
                else if (oe.isMine) oe.JoinOrLeavePending();
            }
        }

        public void EntityTransfered(OnlineEntity oe, OnlinePlayer to)
        {
            RainMeadow.Debug($"{oe} : {this} : to {to}");
            oe.NewOwner(to);
            EntitiesModified();
        }
    }
}

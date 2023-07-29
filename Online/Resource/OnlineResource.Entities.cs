﻿using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        public abstract World World { get; }
        public Dictionary<OnlineEntity, EntityMembership> entities;
        private List<EntityResourceEvent> incomingEntityEvents; // entities coming from onwer but that I can't process yet

        // An entity I control has entered the resource, consider registering or joining
        // called from entity join logic - entities join on a queue of resources they need to join
        public void LocalEntityEntered(OnlineEntity oe)
        {
            RainMeadow.Debug(this);
            if (!isActive) throw new InvalidOperationException("not active");
            if (!oe.isMine) throw new InvalidOperationException("not mine");
            if (entities.ContainsKey(oe)) throw new InvalidOperationException("already in entities");
            RainMeadow.Debug($"{this} - joining with {oe}");
            if (this is not Lobby && super.entities.ContainsKey(oe)) // already in super, therefore known
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
                EntityRegisteredInResource(oe, oe.GetState(OnlineManager.mePlayer.tick, this));
            }
            else // request to register
            {
                RequestRegisterEntity(oe);
            }
        }

        // Local-to-owner, tell them about my entity
        private void RequestRegisterEntity(OnlineEntity oe)
        {
            RainMeadow.Debug(this);
            oe.pendingRequest = owner.QueueEvent(new RegisterNewEntityRequest(oe.AsNewEntityEvent(this)));
        }

        // as owner, from other
        public void OnEntityRegisterRequest(RegisterNewEntityRequest registerEntityRequest)
        {
            RainMeadow.Debug(this);
            if (isOwner && isActive && (registerEntityRequest.dependsOnTick?.ChecksOut() ?? true)) // tick checked here because needs an answer this frame
            {
                OnlineEntity oe = OnlineEntity.FromNewEntityEvent(registerEntityRequest.newEntityEvent, this);
                EntityRegisteredInResource(oe, registerEntityRequest.newEntityEvent.initialState);
                registerEntityRequest.from.QueueEvent(new GenericResult.Ok(registerEntityRequest));
            }
            else
            {
                registerEntityRequest.from.QueueEvent(new GenericResult.Error(registerEntityRequest));
            }
        }

        // ok from owner
        public void OnRegisterResolve(GenericResult registerResult)
        {
            RainMeadow.Debug(this);
            var nee = (registerResult.referencedEvent as RegisterNewEntityRequest).newEntityEvent;
            var oe = nee.entityId.FindEntity();
            if (oe.pendingRequest == registerResult.referencedEvent) oe.pendingRequest = null;

            if (registerResult is GenericResult.Ok) // success
            {
                EntityRegisteredInResource(oe, nee.initialState);
            }
            else if (registerResult is GenericResult.Error) // retry
            {
                // todo retry
            }
        }

        // registering new entity
        private void EntityRegisteredInResource(OnlineEntity oe, EntityState initialState)
        {
            RainMeadow.Debug(this);
            entities.Add(oe, new EntityMembership(oe, this));
            oe.OnJoinedResource(this, initialState);
            if (isOwner)
            {
                foreach (var part in participants)
                {
                    if (part.Key.isMe || part.Key == oe.owner) { continue; }
                    part.Key.QueueEvent(oe.AsNewEntityEvent(this));
                }
            }
        }

        // from owner as third-party
        public void OnNewRemoteEntity(NewEntityEvent newEntityEvent)
        {
            if (!isActive)
            {
                incomingEntityEvents.Add(newEntityEvent);
                return;
            }
            RainMeadow.Debug(this);
            OnlineEntity oe = OnlineEntity.FromNewEntityEvent(newEntityEvent, this);
            EntityRegisteredInResource(oe, newEntityEvent.initialState);
        }


        // An entity of mine previously created, joining this resource
        protected void EntityJoin(OnlineEntity oe)
        {
            RainMeadow.Debug(this);
            if (!isActive) throw new InvalidProgrammerException("not active");
            if (oe.currentlyJoinedResource != super) { throw new InvalidOperationException("trying to join but not in super"); }

            if (isOwner) // join right away
            {
                EntityJoinedResource(oe, oe.GetState(OnlineManager.mePlayer.tick, this));
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
            oe.pendingRequest = owner.QueueEvent(new EntityJoinRequest(this, oe, super.entities[oe].memberSinceTick));
        }

        public void OnEntityJoinRequest(EntityJoinRequest entityJoinRequest)
        {
            RainMeadow.Debug(this);
            if (isOwner && isActive && (entityJoinRequest.dependsOnTick?.ChecksOut() ?? true)) // tick checked here because needs an answer this frame
            {
                OnlineEntity oe = entityJoinRequest.entityId.FindEntity();
                entityJoinRequest.initialState.ReadTo(oe);
                EntityJoinedResource(oe, entityJoinRequest.initialState);
                entityJoinRequest.from.QueueEvent(new GenericResult.Ok(entityJoinRequest));
            }
            else
            {
                entityJoinRequest.from.QueueEvent(new GenericResult.Error(entityJoinRequest));
            }
        }

        public void OnJoinResolve(GenericResult entityJoinResult)
        {
            RainMeadow.Debug(this);
            var oe = (entityJoinResult.referencedEvent as EntityJoinRequest).entityId.FindEntity();
            if (oe.pendingRequest == entityJoinResult.referencedEvent) oe.pendingRequest = null;

            if (entityJoinResult is GenericResult.Ok) // success
            {
                EntityJoinedResource(oe, (entityJoinResult.referencedEvent as EntityJoinRequest).initialState);
            }
            else if (entityJoinResult is GenericResult.Error) // retry
            {
                // todo retry
            }
        }

        public void OnEntityJoined(EntityJoinedEvent entityJoinedEvent)
        {
            if (!isActive)
            {
                incomingEntityEvents.Add(entityJoinedEvent);
                return;
            }
            RainMeadow.Debug(this);
            var oe = entityJoinedEvent.entityId.FindEntity();
            EntityJoinedResource(oe, entityJoinedEvent.initialState);
        }

        // existing entity joins
        private void EntityJoinedResource(OnlineEntity oe, EntityState initialState)
        {
            RainMeadow.Debug(this);
            entities.Add(oe, new EntityMembership(oe, this));
            oe.OnJoinedResource(this, initialState);
            if (isOwner)
            {
                var inSuper = super.entities[oe];
                foreach (var part in participants)
                {
                    if (part.Key.isMe || part.Key == oe.owner) { continue; }
                    part.Key.QueueEvent(new EntityJoinedEvent(this, oe, TickReference.NewestOfMemberships(part.Value, inSuper)));
                }
            }
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
            oe.pendingRequest = owner.QueueEvent(new EntityLeaveRequest(this, oe.id, entities[oe].memberSinceTick));
        }

        public void OnEntityLeaveRequest(EntityLeaveRequest entityLeaveRequest)
        {
            RainMeadow.Debug(this);
            if (isOwner && isActive && (entityLeaveRequest.dependsOnTick?.ChecksOut() ?? true)) // tick checked here because needs an answer this frame
            {
                OnlineEntity oe = entityLeaveRequest.entityId.FindEntity();
                EntityLeftResource(oe);
                entityLeaveRequest.from.QueueEvent(new GenericResult.Ok(entityLeaveRequest));
            }
            else
            {
                entityLeaveRequest.from.QueueEvent(new GenericResult.Error(entityLeaveRequest));
            }
        }

        public void OnEntityLeaveResolve(GenericResult entityLeaveResult)
        {
            RainMeadow.Debug(this);
            var oe = (entityLeaveResult.referencedEvent as EntityLeaveRequest).entityId.FindEntity();
            if (oe.pendingRequest == entityLeaveResult.referencedEvent) oe.pendingRequest = null;

            if (entityLeaveResult is GenericResult.Ok) // success
            {
                EntityLeftResource(oe);
            }
            else if (entityLeaveResult is GenericResult.Error) // retry
            {
                // todo retry
            }
        }

        public void OnEntityLeft(EntityLeftEvent entityLeftEvent)
        {
            if (!isActive)
            {
                incomingEntityEvents.Add(entityLeftEvent);
                return;
            }
            RainMeadow.Debug(this);
            var oe = entityLeftEvent.entityId.FindEntity();
            EntityLeftResource(oe);
        }

        public void EntityLeftResource(OnlineEntity oe)
        {
            RainMeadow.Debug(this);
            entities.Remove(oe);
            oe.OnLeftResource(this);
            if (isOwner)
            {
                super.entities.TryGetValue(oe, out EntityMembership inSuper);
                foreach (var part in participants)
                {
                    if (part.Key.isMe || part.Key == oe.owner) { continue; }
                    part.Key.QueueEvent(new EntityLeftEvent(this, oe, TickReference.NewestOfMemberships(part.Value, inSuper)));
                }
            }
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
            owner.QueueEvent(new EntityTransferRequest(this, oe.id, to));
        }

        public void OnEntityTransferRequest(EntityTransferRequest entityTransferRequest)
        {
            RainMeadow.Debug(this);
            if (isOwner && isActive)
            {
                OnlineEntity oe = entityTransferRequest.entityId.FindEntity();
                EntityTransfered(oe, OnlineManager.lobby.PlayerFromId(entityTransferRequest.newOwner));
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
            var oe = (entityTransferResult.referencedEvent as EntityTransferRequest).entityId.FindEntity();
            if (oe.pendingRequest == entityTransferResult.referencedEvent) oe.pendingRequest = null;

            if (entityTransferResult is GenericResult.Ok) // success
            {
                EntityTransfered(oe, OnlineManager.lobby.PlayerFromId((entityTransferResult.referencedEvent as EntityTransferRequest).newOwner));
            }
            else if (entityTransferResult is GenericResult.Error) // retry
            {
                // todo retry
            }
        }

        public void OnEntityTransfered(EntityTransferedEvent entityTransferedEvent)
        {
            RainMeadow.Debug(this);
            var oe = entityTransferedEvent.entityId.FindEntity();
            EntityTransfered(oe, OnlineManager.lobby.PlayerFromId(entityTransferedEvent.newOwner));
        }

        public void EntityTransfered(OnlineEntity oe, OnlinePlayer to)
        {
            RainMeadow.Debug(this);
            oe.NewOwner(to);
            if (isOwner)
            {
                foreach (var part in participants)
                {
                    if (part.Key.isMe) { continue; }
                    part.Key.QueueEvent(new EntityTransferedEvent(this, oe.id, to, part.Value.memberSinceTick));
                }
            }
        }
    }
}

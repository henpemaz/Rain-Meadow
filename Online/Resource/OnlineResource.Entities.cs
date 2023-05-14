using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        protected abstract World World { get; }
        public Dictionary<OnlineEntity.EntityId, OnlineEntity> entities;
        private List<EntityResourceEvent> incomingEntityEvents; // entities coming from onwer but that I can't process yet

        /*
         * My entity
         * I can do whatever with it
         * I enter resources, whether boss allows it or not
         * I leave resources whetheer boss allows it or not
         * ^ if I were to say no to these, how would i "hold" those entities?
         *  store orig an call later?
         *  nah this seems a bit absurd
         *  
         * entities move as they please, fire join events, retry as needed, resolve pendencies as possible
         * entities "enter resources" in two ways
         *  entity.inResource marks the entity entered it... physically/locally
         *  and then resource.entities has the actual formal thing
         *  talk to resource owner to end up there
         *  entity.onjoined callback to tidyup and maybe check if pending in other resources
         * 
         * 
         * people know im here through events and the list of entites they keep on their own
         * 
         * maybe owner should keep a list of those entities
         *  maybe list of entities should be in lease info
         * 
         * maybe with clear creation-removal, short-ids are back in the menu
         *  registered at this level -> map of id/shortid
         *  joined at this level -> list of shortid
         * 
         * same for players, short-id for known players
         *  map at lobby level
         * 
         * 
         */

        // An entity I control has entered the resource, consider registering or joining
        public void LocalEntityEntered(OnlineEntity oe)
        {
            if (!isActive) throw new InvalidOperationException("not active");
            if (!oe.owner.isMe) throw new InvalidOperationException("not mine");
            if (entities.ContainsKey(oe.id)) throw new InvalidOperationException("already in entities");
            RainMeadow.Debug($"{this} - joining with {oe}");
            if (this is not Lobby && super.entities.ContainsKey(oe.id)) // already in super, therefore known
            {
                RequestJoinEntity(oe);
            }
            else // brand new
            {
                EntityRegister(oe);
            }
        }

        // An entity I control is being added to this resource for the first time, to be sent in full
        protected void EntityRegister(OnlineEntity oe)
        {
            if (!isActive) throw new InvalidProgrammerException("not active");
            if (oe.highestResource != null) { throw new InvalidOperationException("already in a resource"); }

            if (isOwner) // enter right away
            {
                EntityRegisteredInResource(oe);
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
            oe.pendingRequest = owner.QueueEvent(new RegisterNewEntityEvent(oe.AsNewEntityEvent(this)));
        }

        // as owner, from other
        public void OnEntityRegistering(RegisterNewEntityEvent registerEntityEvent)
        {
            if(isOwner && isActive && registerEntityEvent.dependsOnTick.ChecksOut()) // tick checked here because needs an answer this frame
            {
                OnlineEntity oe = OnlineEntity.FromNewEntityEvent(registerEntityEvent.newEntityEvent, this);
                EntityRegisteredInResource(oe);
                registerEntityEvent.from.QueueEvent(new RegisterNewEntityResult.Ok(registerEntityEvent));
            }
            else
            {
                registerEntityEvent.from.QueueEvent(new RegisterNewEntityResult.Error(registerEntityEvent));
            }
        }

        // ok from owner
        internal void OnRegisterResolve(RegisterNewEntityResult registerResult)
        {
            RainMeadow.Debug(this);
            var oe = ((registerResult.referencedEvent as RegisterNewEntityEvent).newEntityEvent as NewEntityEvent).entityId.FindEntity();
            if (oe.pendingRequest == registerResult.referencedEvent) oe.pendingRequest = null;

            if (registerResult is RegisterNewEntityResult.Ok) // success
            {
                EntityRegisteredInResource(oe);
            }
            else if (registerResult is RegisterNewEntityResult.Error) // retry
            {
                // todo retry
            }
        }

        // registering new entity
        private void EntityRegisteredInResource(OnlineEntity oe)
        {
            entities.Add(oe.id, oe);
            oe.joinedAt = new PlayerTickReference(owner); // might need one of these per resource we join, membership-like
                                                          // specially for when we leave and unwind
            if (isOwner)
            {
                foreach (var part in participants)
                {
                    if (part.Key.isMe || part.Key == oe.owner) { continue; }
                    part.Key.QueueEvent(oe.AsNewEntityEvent(this));
                }
            }
            oe.OnJoinedResource(this);
        }

        // from owner as third-party
        internal void OnNewRemoteEntity(NewEntityEvent newEntityEvent)
        {
            OnlineEntity oe = OnlineEntity.FromNewEntityEvent(newEntityEvent, this);
            EntityRegisteredInResource(oe);
        }


        // todo handshake this
        // join request - onjoinrequest - joinrequestresolve - onjoined
        // An entity of mine previously created, joining this resource
        public void RequestJoinEntity(OnlineEntity oe)
        {
            if (oe.isPending) throw new InvalidOperationException("can't enter subresource if pending");
            
            owner.QueueEvent(new EntityJoinRequest(oe, this, PlayerTickReference.NewestOf(ownerSinceTick, this, oe.joinedAt, super)));
        }

        public void OnEntityJoinRequest(EntityJoinRequest entityJoinRequest)
        {
            if (isOwner && isActive && entityJoinRequest.dependsOnTick.ChecksOut()) // tick checked here because needs an answer this frame
            {
                OnlineEntity oe = entityJoinRequest.entityId.FindEntity();
                EntityJoinedResource(oe);
                entityJoinRequest.from.QueueEvent(new EntityJoinResult.Ok(entityJoinRequest));
            }
            else
            {
                entityJoinRequest.from.QueueEvent(new EntityJoinResult.Error(entityJoinRequest));
            }
        }

        public void OnJoinResolve(EntityJoinResult entityJoinResult)
        {

        }

        internal void OnEntityJoined(EntityJoinedEvent entityJoinedEvent)
        {
            var oe = entityJoinedEvent.entityId.FindEntity();
            EntityJoinedResource(oe);
        }

        // existing entity joins
        private void EntityJoinedResource(OnlineEntity oe)
        {
            entities.Add(oe.id, oe);
            oe.joinedAt = new PlayerTickReference(owner); // might need one of these per resource we join, membership-like
                                                          // specially for when we leave and unwind
            if (isOwner)
            {
                foreach (var part in participants)
                {
                    if (part.Key.isMe || part.Key == oe.owner) { continue; }
                    part.Key.QueueEvent(new );
                }
            }
            oe.OnJoinedResource(this);
        }


        // todo leave resource















        // OLD


        // A new OnlineEntity was added, notify accordingly
        public virtual void old_EntityEnteredResource(OnlineEntity oe)
        {
            RainMeadow.Debug($"{this} - {oe}");
            if (!isAvailable) throw new InvalidOperationException("not available");
            if (entities.Contains(oe)) throw new InvalidOperationException("already in entities");
            entities.Add(oe);
            if (isOwner) // I am responsible for notifying other players about it
            {
                RainMeadow.Debug("notifying others of entity joining");
                foreach (var member in participants.Values)
                {
                    if (member.player.isMe || member.player == oe.owner) continue;
                    member.player.QueueEvent(new old_NewEntityEvent(this, oe, member.memberSinceTick));
                }
            }
            else if (oe.owner.isMe) // I notify the owner about my entity in the room
            {
                RainMeadow.Debug("notifying owner of entity joining");
                // todo should this be handshaked? owner might change
                owner.QueueEvent(new old_NewEntityEvent(this, oe, participants[PlayersManager.mePlayer].memberSinceTick));
                OnlineManager.AddFeed(this, oe);
            }
            else
            {
                RainMeadow.Debug("externally controlled entity joining, not notifying anyone");
            }
        }

        // I've been notified that a new creature has entered, and I must recreate the equivalent in my game
        public void old_OnNewEntity(old_NewEntityEvent newEntityEvent)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) { throw new InvalidOperationException("not available"); }
            if (!isActive)
            {
                RainMeadow.Debug("queueing for later");
                this.incomingEntityEvents.Add(newEntityEvent);
                return;
            }
            if (newEntityEvent.owner.isMe) { throw new InvalidOperationException("notified of my own creation"); }
            OnlineEntity oe = OnlineEntity.old_CreateOrReuseEntity(newEntityEvent, this.World);
            old_EntityEnteredResource(oe);
        }

        public virtual void old_EntityLeftResource(OnlineEntity oe)
        {
            RainMeadow.Debug($"{this} - {oe}");
            if (!isAvailable) { RainMeadow.Debug("not available, skipping"); return; }
            if (!entities.Contains(oe)) throw new InvalidOperationException("not in entities");
            entities.Remove(oe);
            if (isOwner) // I am responsible for notifying other players about it
            {
                RainMeadow.Debug("notifying others of entity leaving");
                foreach (var member in participants.Values)
                {
                    if (member.player.isMe || member.player == oe.owner) continue;
                    member.player.QueueEvent(new EntityLeftEvent(this, oe, member.memberSinceTick));
                }
            }
            else if (oe.owner.isMe) // I notify the owner about my entity in the room
            {
                RainMeadow.Debug("notifying owner of entity leaving");
                // todo should this be handshaked? owner might change
                owner.QueueEvent(new EntityLeftEvent(this, oe, participants[PlayersManager.mePlayer].memberSinceTick));
                OnlineManager.RemoveFeed(this, oe);
            }
            else
            {
                RainMeadow.Debug("external entity leaving, not notifying anyone");
            }
        }

        // I've been notified that an entity has left
        public void old_OnEntityLeft(EntityLeftEvent entityLeftEvent)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) { RainMeadow.Debug("not available, skipping"); return; }
            if (!isActive)
            {
                RainMeadow.Debug("queueing for later");
                this.incomingEntityEvents.Add(entityLeftEvent);
                return;
            }
            OnlineEntity oe = entities.FirstOrDefault(e => e.id == entityLeftEvent.entityId);
            if (oe.owner.isMe) { throw new InvalidOperationException("notified of my own creation"); }
            old_EntityLeftResource(oe);
        }

        // Assign a new owner to this entity, if I own or supervise it, I must notify accordingly
        public void old_EntityNewOwner(OnlineEntity oe, OnlinePlayer newOwner, bool notifyPreviousOwner = false)
        {
            RainMeadow.Debug($"{this} - {oe} - {newOwner}");
            if (!isAvailable) { throw new InvalidOperationException("not available"); }
            if (oe.owner == newOwner) throw new InvalidOperationException("reasigned to same owner");
            if (oe.highestResource != this) throw new InvalidOperationException("asigned owner in wrong resource");
            var wasOwner = oe.owner; // will be null in transfer-as-super situations

            if (isOwner) // I am responsible for notifying other players about it
            {
                RainMeadow.Debug("notifying others in resource of entity transfer");
                foreach (var member in participants.Values)
                {
                    if (member.player.isMe || (member.player == wasOwner && !notifyPreviousOwner)) continue; // on transfers, we need to notify previous owner
                    member.player.QueueEvent(new EntityNewOwnerEvent(this, oe.id, newOwner, member.memberSinceTick));
                }
            }
            else if (wasOwner.isMe) // I notify the owner about my entity ive donated to someone else
            {
                RainMeadow.Debug("notifying resource owner of entity transfer");
                owner.QueueEvent(new EntityNewOwnerEvent(this, oe.id, newOwner, participants[PlayersManager.mePlayer].memberSinceTick));
            }

            oe.NewOwner(newOwner); // handles the entity actually changing owner
                                          // also calls to SubresourcesUnloaded which might call Release
                                          // which is why I moved it down here :3
        }

        // I'm notified of a new owner for an entity in this resource
        public void old_OnEntityNewOwner(EntityNewOwnerEvent entityNewOwner)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) { throw new InvalidOperationException("not available"); }
            if (!isActive)
            {
                RainMeadow.Debug("queueing for later");
                this.incomingEntityEvents.Add(entityNewOwner);
                return;
            }
            OnlineEntity oe = entities.FirstOrDefault(e => e.id == entityNewOwner.entityId);
            if (oe != null)
            {
                old_EntityNewOwner(oe, entityNewOwner.newOwner);
            }
            else
            {
                RainMeadow.Error("entity mentioned could not be found");
            }
        }
    }
}

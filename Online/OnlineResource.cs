using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO.Ports;
using System.Linq;

namespace RainMeadow
{
    // OnlineResources are transferible, subscriptable resources, limited to a resource that others can consume (lobby, world, room)
    // The owner of the resource coordinates states, distributes subresources and solves conflicts
    public abstract partial class OnlineResource
    {
        public OnlineResource super;
        public OnlinePlayer owner;
        public List<OnlineResource> subresources;
        public List<OnlinePlayer> participants;

        public ResourceEvent pendingRequest; // should this maybe be a list/queue? Will it be any more manageable if multiple events can cohexist?

        public bool isFree => owner == null;
        public bool isOwner => owner != null && owner.id == OnlineManager.me;
        public bool isSuper => super != null && super.isOwner;
        public bool isActive { get; protected set; } // The respective in-game resource is loaded
        public bool isAvailable { get; protected set; } // The resource was leased or subscribed to
        public bool isPending => pendingRequest != null;
        public bool canRelease => !isPending && !subresources.Any(s => s.isAvailable) && !entities.Any(e => e.owner.isMe && !e.isTransferable);

        public void FullyReleaseResource()
        {
            RainMeadow.Debug(this);
            if (isActive)
            {
                foreach (var sub in subresources)
                {
                    if (sub.isAvailable && !sub.isPending) sub.FullyReleaseResource();
                }
                foreach (var item in entities.ToList())
                {
                    if(!item.isTransferable && item.owner.isMe) EntityLeftResource(item); // force remove
                }
            }

            if(canRelease) { Release(); releaseWhenPossible = false; }
            else if (pendingRequest is not ReleaseRequest) { releaseWhenPossible = true; }
        }

        public void SubresourcesUnloaded() // callback-ish for resources freeing up
        {
            if (releaseWhenPossible && canRelease)
            {
                Release();
                releaseWhenPossible = false;
            }
        }

        protected abstract void ActivateImpl();

        // The game resource this corresponds to has loaded, and subresources can be enumerated
        public void Activate()
        {
            RainMeadow.Debug(this);
            if(isActive) { throw new InvalidOperationException("Resource is already active"); }
            if(!isAvailable) { throw new InvalidOperationException("Resource is not available"); }
            isActive = true;
            subresources = new List<OnlineResource>();
            entities = new();

            ActivateImpl();

            if (incomingLease != null) // lease changes that couldn't be processed yet (needed the subresources listed, iow resource active)
            {
                ProcessLease(incomingLease);
                incomingLease = null;
            }
            if (isOwner)
            {
                NewLeaseState();
            }

            foreach (var item in incomingEntities) // entities that couldn't be processed yet (needed the resource active)
            {
                item.Process();
            }
            incomingEntities.Clear();

            if (releaseWhenPossible) FullyReleaseResource(); // my bad I don't want it anymore
        }

        protected virtual void AvailableImpl() { }

        // The online resource has been leased and its state is available
        protected void Available()
        {
            RainMeadow.Debug(this);
            if (isAvailable) { throw new InvalidOperationException("Resource is already available"); }
            if (isActive) { throw new InvalidOperationException("Resource is already active"); }
            isAvailable = true;
            participants = new() { OnlineManager.mePlayer };
            incomingLease = null;
            incomingEntities = new();

            AvailableImpl();
        }

        public bool deactivateOnRelease;
        public bool releaseWhenPossible;
        protected abstract void DeactivateImpl();

        // The game resource this corresponds to has been unloaded
        public void Deactivate()
        {
            RainMeadow.Debug(this);
            if (!isActive) { throw new InvalidOperationException("resource is already inactive"); }
            if (isAvailable) { throw new InvalidOperationException("resource is still available"); }
            if (subresources.Any(s=>s.isActive)) throw new InvalidOperationException("has active subresources");
            isActive = false;
            DeactivateImpl();
            subresources.Clear();
            subresources = null;
        }

        protected virtual void UnavailableImpl() { }

        // The online resource has been unleased
        protected void Unavailable()
        {
            RainMeadow.Debug(this);
            if (!isActive) { throw new InvalidOperationException("resource is inactive, should have been unleased first"); }
            if (!isAvailable) { throw new InvalidOperationException("resource is already not available"); }
            if (subresources.Any(s => s.isAvailable)) throw new InvalidOperationException("has available subresources");
            isAvailable = false;
            UnavailableImpl();

            participants.Clear();
            participants = null;
            
            OnlineManager.RemoveSubscriptions(this);


            foreach (var ent in entities)
            {
                ent.Deactivated(this);
            }
            OnlineManager.RemoveFeeds(this);
            entities.Clear();
            entities = null;

            if (deactivateOnRelease)
            {
                Deactivate();
            }

            super?.SubresourcesUnloaded(); // I've released, notify super if super is waiting
        }

        protected void NewOwner(OnlinePlayer player)
        {
            RainMeadow.Debug(this.ToString() + " - " + (player != null ? player : "null"));
            //if (player == owner && player != null) throw new InvalidOperationException("Re-assigned to the same owner"); // this breaks in transfers, as the transferee doesnt have the delta
            var oldOwner = owner;
            owner = player;
            
            if (isSuper && oldOwner != owner) // I am responsible for notifying lease changes to this
            {
                super.NewLeaseState();
            }
        }

        protected virtual void SubscribedImpl(OnlinePlayer player) { }
        private void Subscribed(OnlinePlayer player)
        {
            RainMeadow.Debug(this.ToString() + " - " + player.ToString());
            if (!isAvailable) throw new InvalidOperationException("not available");
            if (!isOwner) throw new InvalidOperationException("not owner");
            if (player.isMe) throw new InvalidOperationException("Can't subscribe to self");

            participants.Add(player);
            OnlineManager.AddSubscription(this, player);
            SubscribedImpl(player);

            if (isActive)
            {
                NewLeaseState(player);
                foreach (var ent in entities)
                {
                    if (player == ent.owner) continue;
                    player.QueueEvent(new NewEntityEvent(this, ent));
                }
            }
        }

        private void Unsubscribed(OnlinePlayer player)
        {
            RainMeadow.Debug(this.ToString() + " - " + player.name);
            if (player.isMe) throw new InvalidOperationException("Can't unsubscribe from self");

            participants.Remove(player);
            OnlineManager.RemoveSubscription(this, player);

            if (isActive)
            {
                NewLeaseState();
            }
        }

        // I request, possibly to someone else
        public virtual void Request()
        {
            RainMeadow.Debug(this);
            if (isPending) throw new InvalidOperationException("pending");
            if (isAvailable) throw new InvalidOperationException("available");
            if (isActive) throw new InvalidOperationException("active");

            if (owner != null)
            {
                owner.RequestResource(this);
            }
            else if (super?.owner != null)
            {
                super.owner.RequestResource(this);
            }
            else
            {
                throw new InvalidOperationException("cant be requested");
            }
        }

        // I release, possibly to someone else
        private void Release()
        {
            RainMeadow.Debug(this);
            if (isPending) throw new InvalidOperationException("pending");
            if (!isAvailable) throw new InvalidOperationException("not available");
            if (!canRelease) throw new InvalidOperationException("cant be released in current state");
            if (isOwner) // let go
            {
                if (super?.owner != null) // return to super
                {
                    super.owner.ReleaseResource(this);
                }
                else
                {
                    throw new InvalidOperationException("cant be released");
                }
            }
            else if (owner != null) // unsubscribe
            {
                owner.ReleaseResource(this);
            }
            else
            {
                throw new InvalidOperationException("no owner");
            }
        }

        // Someone requested me, maybe myself
        public void Requested(ResourceRequest request)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Requested by : " + request.from.name);

            if (isFree && isSuper) // I can lease
            {
                // Leased to player
                NewOwner(request.from); // set owner first
                request.from.QueueEvent(new RequestResult.Leased(request)); // then make available
            }
            else if (isOwner) // I am the current owner and others can subscribe
            {
                if (request.from.isMe) throw new InvalidOperationException("requested, but already own");
                // Player subscribed to resource
                request.from.QueueEvent(new RequestResult.Subscribed(request)); // result first, so they Activate before getting new state
                Subscribed(request.from);
            }
            else // Not mine, can't lease
            {
                request.from.QueueEvent(new RequestResult.Error(request));
            }
        }

        // Someone released from me, maybe myself
        public void Released(ReleaseRequest request)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Released by : " + request.from.name);

            if (isSuper && owner == request.from)
            {
                var newParticipants = request.participants.Where(p => p != owner).ToList();
                var newOwner = PlayersManager.BestTransferCandidate(this, newParticipants);
                NewOwner(newOwner); // This notifies all users

                var theEntitiesTheySpeakOf = request.abandonedEntities.Select(eid => entities.FirstOrDefault(en => en.id == eid)).Where(ent => ent != null && ent.owner == request.from).ToList();
                var controlledEntities = theEntitiesTheySpeakOf.Where(e => e.highestResource.isOwner).ToList();
                var entitiesTo = newOwner ?? OnlineManager.mePlayer;
                foreach (var ent in controlledEntities) //assign myself what I can
                {
                    if(ent.owner != entitiesTo)
                    {
                        ent.owner = null;
                        ent.highestResource.EntityNewOwner(ent, entitiesTo, true);
                    }
                }
                // todo what to do with the "rest" of entities? there should be none if it's just room/world stuff, but...
                // currently the receiving side will try and Request entities they receive through this and dont own
                foreach( var ent in theEntitiesTheySpeakOf.Except(controlledEntities))
                {
                    RainMeadow.Error($"Unhandled entity: {ent}");
                }

                if (newOwner != null)
                {
                    newOwner.TransferResource(this, newParticipants, request.abandonedEntities); // This notifies the new owner to apply subscriptions
                }
                request.from.QueueEvent(new ReleaseResult.Released(request)); // this notifies the old owner that the release was a success
                return;
            }
            if (isOwner)
            {
                request.from.QueueEvent(new ReleaseResult.Unsubscribed(request));
                Unsubscribed(request.from);
                var theEntitiesTheySpeakOf = request.abandonedEntities.Select(eid => entities.FirstOrDefault(en => en.id == eid)).Where(ent => ent != null && ent.owner == request.from).ToList();
                var controlledEntities = theEntitiesTheySpeakOf.Where(e => e.highestResource.isOwner).ToList();
                foreach (var ent in controlledEntities) //assign myself what I can
                {
                    ent.owner = null;
                    ent.highestResource.EntityNewOwner(ent, OnlineManager.mePlayer, true);
                }
                foreach(var ent in theEntitiesTheySpeakOf.Except(controlledEntities)) // request the rest
                {
                    ent.Request();
                }
                return;
            }
            request.from.QueueEvent(new ReleaseResult.Error(request));
            return;
        }

        // The previous owner has left and I've been assigned (by super) as the new owner
        public void Transfered(TransferRequest request)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Transfered by : " + request.from.name);
            if (isAvailable && isActive && isOwner && request.from == super?.owner) // I am a subscriber with a valid state who now owns this resource
            {
                participants = new();
                isActive = false; // we tell a little lie while we re-add everyone
                foreach(var subscriber in request.participants)
                {
                    if (subscriber.isMe) continue;
                    Subscribed(subscriber);
                }
                isActive = true;
                currentLeaseState = null;
                NewLeaseState();

                foreach (var entId in request.abandonedEntities)
                {
                    var ent = this.entities.FirstOrDefault(e => e.id == entId);
                    if (ent != null)
                    {
                        if (!ent.owner.isMe) // it couldn't be transfered by super for some reason?
                        {
                            if (ent.highestResource.isOwner)
                            {
                                ent.owner = null; // so it also notifies the previous owner, because they didn't initiate this change
                                ent.highestResource.EntityNewOwner(ent, OnlineManager.mePlayer, true);
                            }
                            else if(!ent.isPending)
                            {
                                ent.Request();
                            }
                            else
                            {
                                RainMeadow.Error("Couldn't request entitity because pending: " + ent);
                            }
                        }
                    }
                    else
                    {
                        RainMeadow.Error("donated entity not found: " + entId);
                    }
                }

                OnlineManager.RemoveFeeds(this);

                request.from.QueueEvent(new TransferResult.Ok(request));
                return;
            }
            RainMeadow.Debug($"Transfer error : {isAvailable} {isActive} {isOwner} {request.from == super?.owner}");
            request.from.QueueEvent(new TransferResult.Error(request)); // super should retry
            return;
        }

        // A pending request was answered to
        public void ResolveRequest(RequestResult requestResult)
        {
            RainMeadow.Debug(this);
            if (requestResult is RequestResult.Leased) // I'm the new owner of a previously-free resource
            {
                Available();
            }
            else if (requestResult is RequestResult.Subscribed) // I'm subscribed to a resource's state and events
            {
                Available();
            }
            else if (requestResult is RequestResult.Error) // I should retry
            {
                // todo retry logic
                RainMeadow.Error("request failed for " + this);
            }
            pendingRequest = null;
        }

        // A pending release was answered to
        internal void ResolveRelease(ReleaseResult releaseResult)
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
            pendingRequest = null;
        }

        // A pending transfer was asnwered to
        internal void ResolveTransfer(TransferResult transferResult)
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
            pendingRequest = null;
        }

        public override string ToString()
        {
            return $"<Resource {Identifier()} - o:{owner?.name} - av:{isAvailable} - ac:{isActive}>";
        }

        internal virtual byte SizeOfIdentifier()
        {
            return (byte)Identifier().Length;
        }

        internal abstract string Identifier();
    }
}

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

        public List<Subscription> subscriptions; // I kind of want to delete this, but leasestate needs sorting out

        public ResourceEvent pendingRequest; // should this maybe be a list/queue? Will it be any more manageable if multiple events can cohexist?

        public bool isFree => owner == null;
        public bool isOwner => owner != null && owner.id == OnlineManager.me;
        public bool isSuper => super != null && super.isOwner;
        public bool isActive { get; protected set; } // The respective in-game resource is loaded
        public bool isAvailable { get; protected set; } // The resource was leased or subscribed to
        public bool isPending => pendingRequest != null;
        public bool canRelease => !isPending && !(subresources.Any(s => s.isAvailable) || entities.Any(e => e.owner.isMe));

        public void FullyReleaseResource()
        {
            RainMeadow.Debug(this);
            if (isActive)
            {
                foreach (var sub in subresources)
                {
                    if (sub.isAvailable && !sub.isPending) sub.FullyReleaseResource();
                }
                foreach(var ent in entities.ToList())
                {
                    if (ent.owner.isMe)
                    {
                        if (ent.isTransferable)
                        {
                            ent.Release();
                        }
                        else
                        {
                            throw new InvalidOperationException("non transferable entity in resource being released");
                        }
                    }
                }
            }

            if(canRelease) { Release(); releaseWhenPossible = false; }
            else if (pendingRequest is not ReleaseRequest) { releaseWhenPossible = true; }
        }

        public void SubresourcesUnloaded()
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
            subscriptions = new();
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
            subscriptions.Clear();
            subscriptions = null;
            
            OnlineManager.RemoveFeeds(this); // there shouldn't be any leftovers but might as well
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
            if (subscriptions == null) throw new InvalidOperationException("nill");
            if (player.isMe) throw new InvalidOperationException("Can't subscribe to self");

            participants.Add(player);
            var sub = OnlineManager.AddSubscription(this, player);
            this.subscriptions.Add(sub);

            SubscribedImpl(player);

            foreach (var ent in entities)
            {
                if (player == ent.owner) continue;
                player.QueueEvent(new NewEntityEvent(this, ent));
            }

            if (isActive)
            {
                NewLeaseState();
            }
        }

        private void Unsubscribed(OnlinePlayer player)
        {
            RainMeadow.Debug(this.ToString() + " - " + player.name);
            if (player.isMe) throw new InvalidOperationException("Can't unsubscribe from self");

            participants.Remove(player);
            var sub = subscriptions.First(s => s.player == player);
            this.subscriptions.Remove(sub);
            OnlineManager.RemoveSubscription(sub);

            NewLeaseState();
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
                throw new InvalidOperationException("cant be released");
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
                var newParticipants = request.participants.Where(p => p != request.from).ToList();
                if (newParticipants.Count > 0)
                {
                    var newOwner = PlayersManager.BestTransferCandidate(this, newParticipants);
                    NewOwner(newOwner); // This notifies all users
                    newOwner.TransferResource(this, newParticipants); // This notifies the new owner to apply subscriptions
                }
                else
                {
                    NewOwner(null);
                }
                request.from.QueueEvent(new ReleaseResult.Released(request)); // this notifies the old owner that the release was a success
                return;
            }
            if (isOwner)
            {
                request.from.QueueEvent(new ReleaseResult.Unsubscribed(request));
                Unsubscribed(request.from);
                return;
            }
            request.from.QueueEvent(new ReleaseResult.Error(request));
            return;
        }

        // The previous owner has left and I've been assigned as the new owner
        public void Transfered(TransferRequest request)
        {
            RainMeadow.Debug(this);
            RainMeadow.Debug("Transfered by : " + request.from.name);
            if (isAvailable && isActive && isOwner && request.from == super?.owner) // I am a subscriber with a valid state who now owns this resource
            {
                participants = request.participants;
                foreach(var subscriber in request.participants)
                {
                    if (subscriber.isMe) continue;
                    Subscribed(subscriber);
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

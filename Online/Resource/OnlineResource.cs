using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    // OnlineResources are transferible, subscriptable resources, limited to a resource that others can consume (lobby, world, room)
    // The owner of the resource coordinates states, distributes subresources and solves conflicts
    public abstract partial class OnlineResource
    {
        public OnlineResource super; // the resource above this (ie lobby for a world, world for a room)
        public OnlinePlayer owner; // the current owner of this resource, can perform certain operations
        //public List<OnlinePlayer> participants = new(); // all the players in the resource, current owner included
        public Dictionary<OnlinePlayer, ResourceMembership> memberships = new();
        public List<OnlineResource> subresources;

        public ResourceEvent pendingRequest; // should this maybe be a list/queue? Will it be any more manageable if multiple events can cohexist?

        public bool isFree => owner == null || owner.hasLeft;
        public bool isOwner => owner != null && owner.isMe;
        public bool isSupervisor => super.isOwner;
        public OnlinePlayer supervisor => super.owner;
        public bool isActive { get; protected set; } // The respective in-game resource is loaded
        public bool isAvailable { get; protected set; } // The resource was leased or subscribed to
        public bool isPending => pendingRequest != null;
        public bool canRelease => !isPending && isActive && !subresources.Any(s => s.isAvailable);

        public void FullyReleaseResource()
        {
            RainMeadow.Debug(this);
            if (!isAvailable) { throw new InvalidOperationException("not available"); }
            if (isActive)
            {
                foreach (var sub in subresources)
                {
                    if (sub.isAvailable) sub.FullyReleaseResource();
                }
                foreach (var item in entities.ToList())
                {
                    if (!item.isTransferable && item.owner.isMe)
                    {
                        //RainMeadow.Debug($"Foce-remove entity {item} from resource {this}");
                        //EntityLeftResource(item); // force remove
                        throw new InvalidOperationException("Not isTransferable: " + item);
                    }
                }
            }

            if (canRelease) { Release(); releaseWhenPossible = false; }
            else if (pendingRequest is not ResourceRelease) { releaseWhenPossible = true; }
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
            if (isActive) { throw new InvalidOperationException("Resource is already active"); }
            if (!isAvailable) { throw new InvalidOperationException("Resource is not available"); }
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
                NewLeaseState(); // subresources are now available for leasing
            }

            foreach (var item in incomingEntities) // entities that couldn't be processed yet (needed the resource active)
            {
                item.Process();
            }
            incomingEntities.Clear();

            if (releaseWhenPossible) FullyReleaseResource(); // my bad I don't want it anymore
            else if (owner.hasLeft) OnPlayerDisconnect(owner); // I might be late to the party but if I'm the only one here I can claim it now
        }

        protected virtual void AvailableImpl() { }

        // The online resource has been leased and its state is available
        protected void Available()
        {
            RainMeadow.Debug(this);
            if (isAvailable) { throw new InvalidOperationException("Resource is already available"); }
            if (isActive) { throw new InvalidOperationException("Resource is already active"); }
            isAvailable = true;
            incomingLease = null;
            incomingEntities = new();

            AvailableImpl();
        }

        public bool deactivateOnRelease = true; // hmm turns out we always do this
        public bool releaseWhenPossible;

        protected abstract void DeactivateImpl();

        // The game resource this corresponds to has been unloaded
        // or so I thought but: game tring to unload -> release, then deactivate, then let game unload
        public void Deactivate()
        {
            RainMeadow.Debug(this);
            if (!isActive) { throw new InvalidOperationException("resource is already inactive"); }
            if (isAvailable) { throw new InvalidOperationException("resource is still available"); }
            if (subresources.Any(s => s.isActive)) throw new InvalidOperationException("has active subresources");
            isActive = false;
            DeactivateImpl();
            subresources.Clear();
            subresources = null;
            releaseWhenPossible = false;
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

            incomingLease = null;
            currentLeaseState = null;

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

        protected void NewOwner(OnlinePlayer newOwner)
        {
            RainMeadow.Debug($"{this} - '{(newOwner != null ? newOwner : "null")}'");
            if (newOwner == owner && newOwner != null) throw new InvalidOperationException("Re-assigned to the same owner");
            var oldOwner = owner;
            owner = newOwner;

            if(isOwner) { this.ownerSinceTick = new PlayerTickReference(supervisor, supervisor.tick); } // "since when" so I can tell others since when

            if (newOwner != null && !memberships.ContainsKey(newOwner)) memberships.Add(newOwner, new ResourceMembership(newOwner, this));

            if (isSupervisor && this is not Lobby && oldOwner != owner) // I am responsible for notifying lease changes to this
            {
                super.NewLeaseState();
            }

            if (isAvailable && isActive && isOwner) // transfered / claimed by me
            {
                RainMeadow.Debug($"Transfer received!");
                foreach (var membership in memberships.Values)
                {
                    if (membership.player.isMe || membership.player.hasLeft) continue;
                    Subscribed(membership.player, true);
                    membership.everSentLease = false;
                }
                NewLeaseState();
                ClaimAbandonedEntities();
            }

            if (isOwner)
            { 
                OnlineManager.RemoveFeeds(this);
            }
            else if (oldOwner != null && oldOwner.isMe)
            {
                OnlineManager.RemoveSubscriptions(this);
            }

            if (oldOwner != null && oldOwner.hasLeft)
            {
                RainMeadow.Debug($"Old owner has left, checking...");
                OnPlayerDisconnect(oldOwner); // we might be able to sort out things now
            }
        }

        public void UpdateParticipants(List<OnlinePlayer> newParticipants)
        {
            //RainMeadow.Debug(this);
            var originalParticipants = memberships.Keys.ToArray();
            foreach(var p in newParticipants.Except(originalParticipants))
            {
                NewParticipant(p);
            }
            foreach(var p in originalParticipants.Except(newParticipants))
            {
                ParticipantLeft(p);
            }
        }

        private void NewParticipant(OnlinePlayer newParticipant)
        {
            RainMeadow.Debug($"{this}-{newParticipant}");
            memberships.Add(newParticipant, new ResourceMembership(newParticipant, this));
            if(isAvailable && isOwner)
            {
                Subscribed(newParticipant, false);
            }
            if (isSupervisor) // I am responsible for notifying lease changes to this
            {
                super.NewLeaseState();
            }
        }

        private void ParticipantLeft(OnlinePlayer participant)
        {
            RainMeadow.Debug($"{this}-{participant}");
            memberships.Remove(participant);
            if (isAvailable && isOwner && !participant.isMe)
            {
                Unsubscribed(participant);
                if (isActive) ClaimAbandonedEntities();
            }
            if (isSupervisor) // I am responsible for notifying lease changes to this
            {
                super.NewLeaseState();
            }
        }

        public void ClaimAbandonedEntities()
        {
            RainMeadow.Debug(this);
            if (!isActive) throw new InvalidOperationException("not active");
            for (int i = entities.Count - 1; i >= 0; i--)
            {
                OnlineEntity ent = entities[i];
                if (ent.owner.hasLeft || !memberships.ContainsKey(ent.owner)) // abandoned
                {
                    RainMeadow.Debug($"Abandoned entity: {ent}");
                    if (ent.isTransferable)
                    {
                        if (!ent.isPending)
                        {
                            ent.Request();
                        }
                        else
                        {
                            RainMeadow.Error("Couldn't request entitity because pending: " + ent);
                        }
                    }
                    else if (isOwner) // untransferable, kick it out
                    {
                        EntityLeftResource(ent);
                    }
                }
            }
        }

        // There is a race condition here
        // One of the participants of the room will try and reach super with a Request()
        // but absolutely nothing guarantees that they'll reach super before a random new user requesting the resource does
        // (if the new user gets first, the previous state is lost and these guys are left out wihout a subscription)
        // moreso, super has no way to detect whether it should wait for an old participant
        // and... waiting is bad
        public void OnPlayerDisconnect(OnlinePlayer player)
        {
            //RainMeadow.Debug(this);
            if (this is Lobby lobby && owner == player) // lobby owner has left
            {
                RainMeadow.Debug($"Lobby owner {player} left!!!");
                var newOwner = SteamMatchmaking.GetLobbyOwner(lobby.id);
                NewOwner(PlayersManager.PlayerFromId(newOwner));
            }

            if (memberships.ContainsKey(player))
            {
                RainMeadow.Debug($"Member was in resource {this}");
                if (isSupervisor)
                {
                    RainMeadow.Debug($"Kicking out member");
                    ParticipantLeft(player);
                    if (owner == player) // Ooops we'll need a new host
                    {
                        RainMeadow.Debug($"Member was the owner");
                        var newOwner = PlayersManager.BestTransferCandidate(this, memberships);
                        
                        if (newOwner != null && !isPending)
                        {
                            NewOwner(newOwner); // This notifies all users, if the new owner is active they'll restore the state
                            this.pendingRequest = (ResourceEvent)newOwner.QueueEvent(new ResourceTransfer(this));
                        }
                        else
                        {
                            if (newOwner != null) RainMeadow.Error("Can't assign because pending");
                            else NewOwner(null);
                        }
                    }
                }
            }

            if (isActive && subresources.Count > 0 && owner != null && !owner.hasLeft) // has subresources, check when this one is sorted
            {
                RainMeadow.Debug($"Checking subresources for {this}");
                for (int i = 0; i < subresources.Count; i++)
                {
                    subresources[i].OnPlayerDisconnect(player);
                }
            }
        }

        protected virtual void SubscribedImpl(OnlinePlayer player, bool fromTransfer) { }
        private void Subscribed(OnlinePlayer player, bool fromTransfer)
        {
            RainMeadow.Debug(this.ToString() + " - " + player.ToString());
            if (!isAvailable) throw new InvalidOperationException("not available");
            if (!isOwner) throw new InvalidOperationException("not owner");
            if (player.isMe) throw new InvalidOperationException("Can't subscribe to self");

            OnlineManager.AddSubscription(this, player);
            SubscribedImpl(player, fromTransfer);

            if (isActive && !fromTransfer)
            {
                var tickReference = new PlayerTickReference(supervisor, supervisor.tick);
                foreach (var ent in entities)
                {
                    if (player == ent.owner) continue;
                    player.QueueEvent(new NewEntityEvent(this, ent, tickReference));
                }
            }
        }

        private void Unsubscribed(OnlinePlayer player)
        {
            RainMeadow.Debug(this.ToString() + " - " + player);
            if (player.isMe) throw new InvalidOperationException("Can't unsubscribe from self");

            OnlineManager.RemoveSubscription(this, player);
        }

        public override string ToString()
        {
            return $"<Resource {Id()} - o:`{owner}` - av:{(isAvailable ? 1 : 0)} - ac:{(isActive ? 1 : 0)} - m:{memberships.Count}>";
        }

        public virtual byte SizeOfIdentifier()
        {
            return (byte)Id().Length;
        }

        public abstract string Id();

        public abstract ushort ShortId();

        public abstract OnlineResource SubresourceFromShortId(ushort shortId);
    }
}

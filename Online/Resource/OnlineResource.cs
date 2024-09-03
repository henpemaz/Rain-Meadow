using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    // OnlineResources are transferible, non-dynamic resources, that others can consume (lobby, world, room)
    // The owner of the resource coordinates states, distributes subresources and solves conflicts
    public abstract partial class OnlineResource
    {
        public readonly OnlineResource super; // the resource above this (ie lobby for a world, world for a room)
        public readonly List<OnlineResource> chain; // cached tree chain
        public OnlinePlayer owner; // the current owner of this resource, can perform certain operations
        public List<OnlinePlayer> participants = new(); // all the players in the resource, owner included

        public List<OnlineResource> subresources;

        public bool isOwner => owner != null && owner.isMe;
        public bool isSupervisor => super.isOwner;
        public OnlinePlayer supervisor => super.owner;

        public bool isRequesting { get; protected set; } // Ongoing request op
        public bool isActive { get; protected set; } // The respective in-game resource is loaded
        public bool isAvailable { get; protected set; } // The resource state is available
        public bool isWaitingForState { get; protected set; } // The resource was leased or subscribed to by supervisor, waiting for owner
        public bool isReleasing { get; protected set; } // Ongoing release op
        public bool isPending => isRequesting || isReleasing || isWaitingForState; // Ongoing op

        public bool canRelease => !isPending // no ongoing transaction
            && (!isActive || !subresources.Any(s => s.isAvailable || s.isPending)) // no subresource available or pending
            && (!isOwner || participants.All(p => p.isMe || p.recentlyAckdTicks.Any(rt => NetIO.IsNewer(rt, lastModified)))); // state broadcasted

        public uint lastModified; // local tick used locally by owner only to ensure state is broadcasted

        public OnlineResource(OnlineResource super)
        {
            if(super != null)
            {
                this.super = super;
                this.chain = super.chain.Append(this).ToList();
            }
            else
            {
                this.super = this;
                chain = new() { this };
            }
        }

        internal virtual void Tick(uint tick)
        {
            if (isActive)
            {
                foreach (var subresource in subresources)
                {
                    if (subresource.isAvailable)
                    {
                        subresource.Tick(tick);
                    }
                }
            }

            if (releaseWhenPossible && canRelease) // delayed release, typical if owner needs to wait for changes to broadcast
            {
                Release();
            }
        }

        // The online resource has been leased
        public void WaitingForState()
        {
            RainMeadow.Debug(this);
            if (isAvailable) { throw new InvalidOperationException("Resource is already available"); }
            if (isActive) { throw new InvalidOperationException("Resource is already active"); }
            isRequesting = false;
            isWaitingForState = true;
        }

        protected abstract void AvailableImpl();

        // The online resource has been leased and its state is available
        protected void Available()
        {
            RainMeadow.Debug(this);
            if (isAvailable) { throw new InvalidOperationException("Resource is already available"); }
            if (isActive) { throw new InvalidOperationException("Resource is already active"); }
            isWaitingForState = false;
            isAvailable = true;
            AvailableImpl();

            OnlineManager.lobby.gameMode.ResourceAvailable(this);

            if (this.activateOnAvailable) Activate();

            if (releaseWhenPossible) FullyReleaseResource(); // my bad I don't want it anymore
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
            registeredEntities = new();
            joinedEntities = new();
            activeEntities = new();

            ActivateImpl();

            if (latestState != null && !isOwner)
            {
                if (latestState is ResourceWithSubresourcesState withSubresources && withSubresources.subleaseState.list.Count != subresources.Count)
                {
                    OnlineManager.QuitWithError("subresources missmatch");
                    return;
                }
                latestState.ReadTo(this);

                // sanitize subresources
                for (int i = 0; i < subresources.Count; i++)
                {
                    OnlineResource res = subresources[i];
                    if (res.participants.Contains(OnlineManager.mePlayer))
                    {
                        res.ForceRelease();
                    }
                }
            }
            else if (!isOwner)
            {
                RainMeadow.Error($"Active but no state available! {this}");
            }

            OnlineManager.lobby.gameMode.ResourceActive(this);
        }

        public bool deactivateOnRelease = true; // hmm turns out we always do this
        public bool activateOnAvailable;
        public bool releaseWhenPossible;

        protected virtual void UnavailableImpl() { }

        // The online resource has been unleased
        protected void Unavailable()
        {
            RainMeadow.Debug(this);
            if (!isAvailable) { throw new InvalidOperationException("resource is already not available"); }
            if (isActive && subresources.Any(s => s.isAvailable)) throw new InvalidOperationException("has available subresources");
            isAvailable = false;
            isReleasing = false;
            releaseWhenPossible = false;

            UnavailableImpl();

            ClearIncommingBuffers();
            OnlineManager.RemoveSubscriptions(this);
            latestState = null;

            if (isActive && deactivateOnRelease)
            {
                Deactivate();
            }
            super.SubresourcesUnloaded(); // I've released, notify super if super is waiting
        }

        protected abstract void DeactivateImpl();

        // The game resource this corresponds to has been unloaded
        // or so I thought but: game tring to unload -> release, then deactivate, then let game unload
        public void Deactivate()
        {
            RainMeadow.Debug(this);
            if (!isActive) { throw new InvalidOperationException("resource is already inactive"); }
            if (isAvailable) { throw new InvalidOperationException("resource is still available"); }
            isActive = false;
            DeactivateImpl();

            foreach (var ent in activeEntities)
            {
                ent.Deactivated(this);
            }
            OnlineManager.RemoveFeeds(this);

            subresources.Clear();
            registeredEntities.Clear();
            joinedEntities.Clear();
            activeEntities.Clear();
            subresources = null;
            registeredEntities = null;
            joinedEntities = null;
            activeEntities = null;
        }

        // Recursivelly release resources
        public void FullyReleaseResource()
        {
            RainMeadow.Debug(this);
            if (isActive)
            {
                foreach (var sub in subresources)
                {
                    if (sub.isAvailable || sub.isPending) sub.FullyReleaseResource();
                }
            }

            if (canRelease) { Release(); }
            else { releaseWhenPossible = true; }
        }

        public void SubresourcesUnloaded() // callback-ish for resources freeing up
        {
            if (releaseWhenPossible && canRelease)
            {
                Release();
            }
        }

        protected virtual void ClearIncommingBuffers()
        {
            incomingState = new(8);
            resourceData = new();
        }

        protected void NewOwner(OnlinePlayer newOwner)
        {
            RainMeadow.Debug($"{this} - '{(newOwner != null ? newOwner : "null")}'");
            if (newOwner == owner && newOwner != null)
            {
                if (RainMeadow.isArenaMode(out var _))
                {
                    RainMeadow.Debug("Assigned to host"); // Lobby owner control
                }
                else
                {
                    throw new InvalidOperationException("Re-assigned to the same owner");
                }
            }

            if (isAvailable && newOwner == null && !isReleasing) throw new InvalidOperationException("No owner for available resource");
            var oldOwner = owner;
            owner = newOwner;
            LeaseModified();

            incomingState = new(8); // used for delta-encoding stream

            if (owner != null) NewParticipant(owner);

            if (isOwner)
            {
                NewVersion();
                OnlineManager.RemoveFeeds(this); // do not send data to myself

                if (isAvailable) // transfered / claimed by me while already active
                {
                    RainMeadow.Debug($"Transfer received!");
                    foreach (var player in participants)
                    {
                        if (player.isMe || player.hasLeft) continue;
                        Subscribed(player);
                    }
                    if (isActive) ClaimAbandonedEntitiesAndResources(); // can this cause a recursion that I didn't intend when sorting out a leaving player?
                }

                if (isWaitingForState) // I am the authority for the state of this
                {
                    Available();
                }

            }
            else if (oldOwner != null && oldOwner.isMe) // no longer responsible for sending data
            {
                OnlineManager.RemoveSubscriptions(this);
            }

            // cleanup
            if (oldOwner != null && oldOwner.hasLeft)
            {
                RainMeadow.Debug($"Old owner has left, checking...");
                OnPlayerDisconnect(oldOwner); // we might be able to sort out things now
            }
        }

        protected void LeaseModified()
        {
            if (super.isOwner) super.NewVersion();
        }

        protected void EntitiesModified()
        {
            if (isOwner) NewVersion();
        }

        private void NewVersion()
        {
            lastModified = OnlineManager.mePlayer.tick;
        }

        public void UpdateParticipants(List<OnlinePlayer> newParticipants)
        {
            var originalParticipants = participants.ToArray();
            foreach (var p in newParticipants.Except(originalParticipants))
            {
                NewParticipant(p);
            }
            foreach (var p in originalParticipants.Except(newParticipants))
            {
                ParticipantLeft(p);
            }
        }

        protected virtual void NewParticipantImpl(OnlinePlayer player) { }

        private void NewParticipant(OnlinePlayer newParticipant)
        {
            if (participants.Contains(newParticipant)) return;
            RainMeadow.Debug($"{this}-{newParticipant}");
            if (super != this)
            {
                super.NewParticipant(newParticipant);
            }
            participants.Add(newParticipant);
            LeaseModified();
            if (isAvailable && isOwner && !newParticipant.isMe)
            {
                Subscribed(newParticipant);
            }
            NewParticipantImpl(newParticipant);
        }

        protected virtual void ParticipantLeftImpl(OnlinePlayer player) { }
        private void ParticipantLeft(OnlinePlayer participant)
        {
            if (!participants.Contains(participant)) return;
            RainMeadow.Debug($"{this}-{participant}");
            if (isActive)
            {
                foreach (var resource in subresources)
                {
                    resource.ParticipantLeft(participant);
                }
            }
            participants.Remove(participant);
            LeaseModified();
            if (RainMeadow.isArenaMode(out var _))
            {
                if (isSupervisor && participant == owner)
                {
                    RainMeadow.Debug("Abandoning Arena resource and not assigning new owner");
                }
                if (isAvailable && !participant.isMe)
                {
                    Unsubscribed(participant);
                }
            }
            else
            {
                if (isSupervisor && participant == owner)
                {
                    PickNewOwner();
                }
                if (isAvailable && isOwner && !participant.isMe)
                {
                    Unsubscribed(participant);
                    if (isActive) ClaimAbandonedEntitiesAndResources();
                }
            }
            ParticipantLeftImpl(participant);
        }

        public void ClaimAbandonedEntitiesAndResources()
        {
            RainMeadow.Debug(this);
            if (!isActive) throw new InvalidOperationException("not active");
            if (!isOwner) throw new InvalidOperationException("not owner");
            foreach (var resource in subresources)
            {
                if (resource.owner != null && resource.owner.hasLeft) // abandoned
                {
                    RainMeadow.Debug($"Abandoned resource: {resource}");
                    resource.ParticipantLeft(resource.owner);
                }
            }
            var entities = this.activeEntities.ToList();
            for (int i = entities.Count - 1; i >= 0; i--)
            {
                OnlineEntity ent = entities[i];
                if (ent.owner.hasLeft || !participants.Contains(ent.owner)) // abandoned
                {
                    RainMeadow.Debug($"Abandoned entity: {ent}");
                    if (ent.isTransferable)
                    {
                        if (!ent.primaryResource.participants.Contains(ent.owner) || ent.owner.hasLeft) // owner really just left if behind
                        {
                            if (ent.primaryResource == this) // we're in control
                            {
                                EntityTransfered(ent, OnlineManager.mePlayer);
                            }
                        }
                        else if (!ent.isPending) // they're still around, request
                        {
                            ent.Request();
                        }
                        else
                        {
                            RainMeadow.Error("Couldn't request entitity because pending: " + ent);
                        }
                    }
                    else // untransferable, kick it out
                    {
                        EntityLeftResource(ent);
                    }
                }
            }
        }

        public void OnPlayerDisconnect(OnlinePlayer player)
        {
            //RainMeadow.Debug(this);
            if (this is Lobby lobby && owner == player) // lobby owner has left
            {
                RainMeadow.Debug($"Lobby owner {player} left!!!");
                NewOwner(MatchmakingManager.instance.GetLobbyOwner());
            }

            // first transfer recursivelly, then remove recursivelly

            // transfer this resource if possible
            if (isSupervisor && owner != null && owner.hasLeft)
            {
                RainMeadow.Debug($"Transfering abandoned resource {this}");
                PickNewOwner();
            }

            // transfer subresources after (we might be super and it might be easier)
            if (isActive && subresources.Count > 0) // has subresources, check when this one is sorted
            {
                RainMeadow.Debug($"Checking subresources for {this}");
                for (int i = 0; i < subresources.Count; i++)
                {
                    subresources[i].OnPlayerDisconnect(player);
                }
            }

            if (this is Lobby) // topmost resource, removes recursivelly
            {
                ParticipantLeft(player);
            }
        }

        private void PickNewOwner()
        {
            if (!isSupervisor) throw new InvalidProgrammerException("not supervisor");
            OnlinePlayer newOwner;

            if (RainMeadow.isArenaMode(out var _))
            {
                newOwner = OnlineManager.lobby.owner; // Host always owns
            }
            else
            {
                newOwner = MatchmakingManager.instance.BestTransferCandidate(this, participants);
            }
            if (newOwner != owner) NewOwner(newOwner);
            if (newOwner != null)
            {
                newOwner.InvokeRPC(this.Transfered).Then(this.ResolveTransfer);
            }
        }

        private void Subscribed(OnlinePlayer player)
        {
            RainMeadow.Debug(this.ToString() + " - " + player.ToString());
            if (!isAvailable) throw new InvalidOperationException("not available");
            if (!isOwner) throw new InvalidOperationException("not owner");
            if (player.isMe) throw new InvalidOperationException("Can't subscribe to self");

            OnlineManager.AddSubscription(this, player);
        }

        private void Unsubscribed(OnlinePlayer player)
        {
            RainMeadow.Debug(this.ToString() + " - " + player);
            if (player.isMe) throw new InvalidOperationException("Can't unsubscribe from self");

            OnlineManager.RemoveSubscription(this, player);
        }

        public override string ToString()
        {
            return $"<Resource {Id()} - o:`{owner}` - av:{(isAvailable ? 1 : 0)} - ac:{(isActive ? 1 : 0)} - m:{participants.Count}>";
        }

        public virtual byte SizeOfIdentifier()
        {
            return (byte)Id().Length;
        }

        public abstract string Id();

        public abstract ushort ShortId();

        public bool IsSibling(OnlineResource other)
        {
            return other == this || (this is not Lobby && other is not Lobby && this.super == other.super);
        }

        public OnlineResource CommonAncestor(OnlineResource other, out List<OnlineResource> chainA, out List<OnlineResource> chainB)
        {
            var root = chain.Last(x => other.chain.Contains(x));
            chainA = chain.SkipWhile(x => x != root).ToList();
            chainB = other.chain.SkipWhile(x => x != root).ToList();
            return root;
        }

        public bool IsSubresourceOf(OnlineResource other)
        {
            if (other == this) return false;
            if (this == super) return false;
            if (other == super) return true;
            return chain.Contains(other);
        }

        public abstract OnlineResource SubresourceFromShortId(ushort shortId);
    }
}

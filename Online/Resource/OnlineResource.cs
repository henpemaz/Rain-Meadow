using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    // OnlineResources are transferible, subscriptable resources, limited to a resource that others can consume (lobby, world, room)
    // The owner of the resource coordinates states, distributes subresources and solves conflicts
    public abstract partial class OnlineResource
    {
        public readonly OnlineResource super; // the resource above this (ie lobby for a world, world for a room)
        public readonly List<OnlineResource> chain; // cached tree chain
        public OnlinePlayer owner; // the current owner of this resource, can perform certain operations
        public List<OnlinePlayer> participants = new(); // all the players in the resource, owner included

        public List<OnlineResource> subresources;

        public OnlineEvent pendingRequest; // should this maybe be a list/queue? Will it be any more manageable if multiple events can cohexist?

        public bool isFree => owner == null || owner.hasLeft;
        public bool isOwner => owner != null && owner.isMe;
        public bool isSupervisor => super.isOwner;
        public OnlinePlayer supervisor => super.owner;
        public bool isActive { get; protected set; } // The respective in-game resource is loaded
        public bool isAvailable { get; protected set; } // The resource state is available
        public bool isWaitingForState { get; protected set; } // The resource was leased or subscribed to
        public bool isPending => pendingRequest != null || isWaitingForState;
        public bool canRelease => !isPending // no ongoing transaction
            && isActive && !subresources.Any(s => s.isAvailable || s.isPending) // no subresource available or pending
            && (!isOwner || participants.All(p => p.isMe || p.recentlyAckdTicks.Any(rt => NetIO.IsNewer(rt, lastModified)))); // state broadcasted

        public uint lastModified; // local tick used locally only
        // if it were to be sync'd for 'versioning' then instead should be a tickref (player+tick)

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
            foreach (var subresource in subresources)
            {
                if (subresource.isActive)
                {
                    subresource.Tick(tick);
                }
            }

            if (releaseWhenPossible && canRelease)
            {
                Release();
                releaseWhenPossible = false;
            }
            if (releaseWhenPossible && !canRelease)
            {
                RainMeadow.Trace($"Can't release {this} from {owner}, reasons: {!isPending} {isActive} {!subresources.Any(s => s.isAvailable || s.isPending)} {(!isOwner || participants.All(p => p.isMe || p.recentlyAckdTicks.Any(rt => NetIO.IsNewer(rt, lastModified))))}");
            }
        }

        // The online resource has been leased
        public void WaitingForState()
        {
            RainMeadow.Debug(this);
            if (isAvailable) { throw new InvalidOperationException("Resource is already available"); }
            if (isActive) { throw new InvalidOperationException("Resource is already active"); }
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
            }
            else if (!isOwner)
            {
                RainMeadow.Error($"Active but no state available! {this}");
            }

            OnlineManager.lobby.gameMode.ResourceActive(this);

            if (releaseWhenPossible) FullyReleaseResource(); // my bad I don't want it anymore
            else if (owner.hasLeft) OnPlayerDisconnect(owner); // I might be late to the party but if I'm the only one here I can claim it now
        }

        public bool deactivateOnRelease = true; // hmm turns out we always do this
        public bool activateOnAvailable;
        public bool releaseWhenPossible;

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

            ClearIncommingBuffers();
            OnlineManager.RemoveSubscriptions(this);

            foreach (var ent in activeEntities)
            {
                ent.Deactivated(this);
            }
            OnlineManager.RemoveFeeds(this);

            registeredEntities.Clear();
            joinedEntities.Clear();
            activeEntities.Clear();
            registeredEntities = null;
            joinedEntities = null;
            activeEntities = null;

            latestState = null;

            if (deactivateOnRelease)
            {
                Deactivate();
            }
            releaseWhenPossible = false;
            super.SubresourcesUnloaded(); // I've released, notify super if super is waiting
        }

        protected abstract void DeactivateImpl();

        // The game resource this corresponds to has been unloaded
        // or so I thought but: game tring to unload -> release, then deactivate, then let game unload
        public void Deactivate()
        {
            RainMeadow.Debug(this);
            if (!isActive) { throw new InvalidOperationException("resource is already inactive"); }
            if (isAvailable)
            {
                if (RainMeadow.isArenaMode(out var _))
                {
                    this.releaseWhenPossible = true;
                } else
                {
                    throw new InvalidOperationException("resource is still available");
                }
            }
            if (subresources.Any(s => s.isActive)) throw new InvalidOperationException("has active subresources");
            isActive = false;
            DeactivateImpl();
            subresources.Clear();
            subresources = null;
            releaseWhenPossible = false;
        }

        // Recursivelly release resources
        public void FullyReleaseResource()
        {
            RainMeadow.Debug(this);
            if (!isAvailable) { throw new InvalidOperationException("not available"); }
            if (isActive)
            {
                foreach (var sub in subresources)
                {
                    if (sub.isPending) { sub.releaseWhenPossible = true; }
                    if (sub.isAvailable) sub.FullyReleaseResource();
                }
                //foreach (var entm in entities.Values.ToArray())
                //{
                //    entm.entity.Deactivated(this);
                //}
            }

            if (canRelease) { Release(); releaseWhenPossible = false; }
            else if (pendingRequest is not RPCEvent rc || rc.handler.method.Name != nameof(this.Released)) { releaseWhenPossible = true; }
        }

        public void SubresourcesUnloaded() // callback-ish for resources freeing up
        {
            if (releaseWhenPossible && canRelease)
            {
                Release();
                releaseWhenPossible = false;
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
                if (RainMeadow.isArenaMode(out var _))
                {

                    RainMeadow.Debug("Assigned to host"); // Lobby owner control

                }
                else
                {
                    throw new InvalidOperationException("Re-assigned to the same owner");
                }

            if (isAvailable && newOwner == null && (pendingRequest is not RPCEvent rc || rc.handler.method.Name != nameof(this.Released))) throw new InvalidOperationException("No owner for available resource");
            var oldOwner = owner;
            owner = newOwner;

            if (owner != null) NewParticipant(owner);

            if (isAvailable && isActive && isOwner) // transfered / claimed by me while already active
            {
                RainMeadow.Debug($"Transfer received!");
                foreach (var player in participants)
                {
                    if (player.isMe || player.hasLeft) continue;
                    Subscribed(player, true);
                }

                ClaimAbandonedEntitiesAndResources();

            }

            if (isWaitingForState && isOwner) // I am the authority for the state of this
            {
                Available();
            }

            if (isActive) // maybe has subresources, notify
            {
                if (this is Lobby) NewSupervisor(owner);
                foreach (var res in subresources)
                {
                    if (res.isAvailable) res.NewSupervisor(owner);
                }
            }

            if (isOwner) lastModified = OnlineManager.mePlayer.tick;

            if (isOwner) // do not send data to myself
            {
                OnlineManager.RemoveFeeds(this);
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

        private void NewSupervisor(OnlinePlayer owner)
        {
            if (!isAvailable) { throw new InvalidOperationException("not available"); }
            var newTick = new TickReference(supervisor, supervisor.tick);
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
            if (isAvailable && isOwner && !newParticipant.isMe)
            {
                Subscribed(newParticipant, false);
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

                    // so im thinking, make entity leaving figure out "x is subsubsubresource of y" autoleave correctly
                    // needs resource.issubresource(parent)

                    // claiming ent/res works better top down because claiming res makes us able to claim subres as well
                    // claiming/kicking ent should technically be bottom up but we can improve it
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

            NewOwner(newOwner);
            if (newOwner != null && !isPending)
            {
                newOwner.InvokeRPC(this.Transfered).Then(this.ResolveTransfer);
            }
        }

        private void Subscribed(OnlinePlayer player, bool fromTransfer)
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

﻿using System;
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
        public Dictionary<OnlinePlayer, PlayerMemebership> participants = new(); // all the players in the resource, owner included

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
        public bool canRelease => !isPending && isActive && !subresources.Any(s => s.isAvailable);

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
            entities = new();

            ActivateImpl();

            if (latestState != null)
            {
                latestState.ReadTo(this);
            }
            else if (!isOwner)
            {
                RainMeadow.Error($"Active but no state available! {this}");
            }

            if (releaseWhenPossible) FullyReleaseResource(); // my bad I don't want it anymore
            else if (owner.hasLeft) OnPlayerDisconnect(owner); // I might be late to the party but if I'm the only one here I can claim it now
        }

        public bool deactivateOnRelease = true; // hmm turns out we always do this
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

            foreach (var ent in entities)
            {
                ent.Value.entity.Deactivated(this);
            }
            OnlineManager.RemoveFeeds(this);
            entities.Clear();
            entities = null;

            if (deactivateOnRelease)
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
                    if (sub.isAvailable) sub.FullyReleaseResource();
                }
                foreach (var entm in entities.Values.ToArray())
                {
                    var ent = entm.entity;
                    if (!ent.isTransferable && ent.isMine)
                    {
                        RainMeadow.Debug($"Foce-remove entity {ent} from resource {this}");
                        EntityLeftResource(ent); // force remove
                    }
                }
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
            //incomingEntityEvents = new();
            incomingState = new(32);
        }

        protected void NewOwner(OnlinePlayer newOwner)
        {
            RainMeadow.Debug($"{this} - '{(newOwner != null ? newOwner : "null")}'");
            if (newOwner == owner && newOwner != null) throw new InvalidOperationException("Re-assigned to the same owner");
            if (isAvailable && newOwner == null && (pendingRequest is not RPCEvent rc || rc.handler.method.Name != nameof(this.Released))) throw new InvalidOperationException("No owner for available resource");
            var oldOwner = owner;
            owner = newOwner;

            if (owner != null) NewParticipant(owner);

            if (isAvailable && isActive && isOwner) // transfered / claimed by me while already active
            {
                RainMeadow.Debug($"Transfer received!");
                foreach (var membership in participants.Values)
                {
                    if (membership.player.isMe || membership.player.hasLeft) continue;
                    Subscribed(membership.player, true);
                }
                ClaimAbandonedEntitiesAndResources();
            }

            if (isWaitingForState) // I am the authority for the state of this
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
            foreach (var part in participants.Values)
            {
                part.memberSinceTick = newTick;
            }
        }

        public void UpdateParticipants(List<OnlinePlayer> newParticipants)
        {
            //RainMeadow.Debug(this);
            var originalParticipants = participants.Keys.ToArray();
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
            if (participants.ContainsKey(newParticipant)) return;
            RainMeadow.Debug($"{this}-{newParticipant}");
            if (super != this && super.isActive && super.isOwner)
            {
                super.NewParticipant(newParticipant);
            }
            participants.Add(newParticipant, new PlayerMemebership(newParticipant, this));
            if (isAvailable && isOwner && !newParticipant.isMe)
            {
                Subscribed(newParticipant, false);
            }
            NewParticipantImpl(newParticipant);
        }

        protected virtual void ParticipantLeftImpl(OnlinePlayer player) { }
        private void ParticipantLeft(OnlinePlayer participant)
        {
            if (!participants.ContainsKey(participant)) return;
            RainMeadow.Debug($"{this}-{participant}");
            if (isActive && isOwner)
            {
                foreach (var resource in subresources)
                {
                    resource.ParticipantLeft(participant);
                }
            }
            participants.Remove(participant);
            if (isAvailable && isOwner && !participant.isMe)
            {
                Unsubscribed(participant);
                if (isActive) ClaimAbandonedEntitiesAndResources();
            }
            ParticipantLeftImpl(participant);
        }

        public void ClaimAbandonedEntitiesAndResources()
        {
            RainMeadow.Debug(this);
            if (!isActive) throw new InvalidOperationException("not active");
            if (!isOwner) throw new InvalidOperationException("not owner");
            var entities = this.entities.Values.Select(em => em.entity).ToList();
            for (int i = entities.Count - 1; i >= 0; i--)
            {
                OnlineEntity ent = entities[i];
                if (ent.owner.hasLeft || !participants.ContainsKey(ent.owner)) // abandoned
                {
                    RainMeadow.Debug($"Abandoned entity: {ent}");
                    if (ent.isTransferable)
                    {
                        if (!ent.primaryResource.participants.ContainsKey(ent.owner) || ent.owner.hasLeft) // owner really just left if behind
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
            foreach (var resource in subresources)
            {
                if (resource.owner != null && ((resource.owner.hasLeft) || !participants.ContainsKey(resource.owner))) // abandoned
                {
                    RainMeadow.Debug($"Abandoned resource: {resource}");
                    resource.ParticipantLeft(resource.owner);
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

            if (participants.ContainsKey(player))
            {
                RainMeadow.Debug($"Member was in resource {this}");
                if (isSupervisor)
                {
                    RainMeadow.Debug($"Kicking out member");
                    ParticipantLeft(player);
                    if (owner == player) // Ooops we'll need a new host
                    {
                        RainMeadow.Debug($"Member was the owner");
                        var newOwner = MatchmakingManager.instance.BestTransferCandidate(this, participants);

                        if (newOwner != null && !isPending)
                        {
                            NewOwner(newOwner); // This notifies all users, if the new owner is active they'll restore the state
                            newOwner.InvokeRPC(this.Transfered);
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

        public abstract OnlineResource SubresourceFromShortId(ushort shortId);
    }
}

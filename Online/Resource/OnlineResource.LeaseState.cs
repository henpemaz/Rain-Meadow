using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        // Lease is the participants and ownership of subresources
        // If lobby, includes participants in self
        private LeaseState incomingLease; // lease to be processed on activate
        protected LeaseState currentLeaseState;

        private void NewLeaseState()
        {
            RainMeadow.Debug(this);
            if (!isActive) { throw new InvalidOperationException("not active"); }
            if (!isOwner) { throw new InvalidOperationException("not owner"); }
            if (ownerSinceTick is null) { throw new InvalidOperationException("no tick reference"); }
            if (subresources.Count == 0) { return; } // nothing to be sent
            var newLeaseState = this is Lobby ? new LobbyLeaseState(this) : new LeaseState(this);
            var delta = newLeaseState.Delta(currentLeaseState);
            foreach (var membership in participants)
            {
                if (membership.Key.isMe) continue;
                
                if (!membership.Value.everSentLease)
                {
                    var tickReference = TickReference.NewestOf(membership.Value.memberSinceTick, this, ownerSinceTick, super);
                    membership.Key.QueueEvent(this is Lobby ? new LobbyLeaseChangeEvent(this, newLeaseState, tickReference) : new LeaseChangeEvent(this, newLeaseState, tickReference)); // its their first time here
                    membership.Value.everSentLease = true;
                }
                else if(!delta.isEmptyDelta)
                {
                    membership.Key.QueueEvent(this is Lobby ? new LobbyLeaseChangeEvent(this, delta, null) : new LeaseChangeEvent(this, delta, null)); // send the delta
                }
            }
            currentLeaseState = newLeaseState; // store in full
        }

        public void OnLeaseChange(LeaseChangeEvent leaseEvent)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) { throw new InvalidOperationException("not available"); }
            if (isOwner) { throw new InvalidOperationException("I am owner"); }
            if (leaseEvent.from != owner) { throw new InvalidOperationException("not from owner"); }
            if (!isActive) // store it for later
            {
                RainMeadow.Debug("Too early, saving for later");
                if (incomingLease != null && leaseEvent.leaseState.isDelta) { incomingLease.AddDelta(leaseEvent.leaseState); }
                else incomingLease = leaseEvent.leaseState;
            }
            else
            {
                ProcessLease(leaseEvent.leaseState);
            }
        }

        public void OnLobbyLeaseChange(LobbyLeaseChangeEvent leaseEvent)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) { throw new InvalidOperationException("not available"); }
            if (isOwner) { throw new InvalidOperationException("I am owner"); }
            if (leaseEvent.from != owner) { throw new InvalidOperationException("not from owner"); }
            if (!isActive) // store it for later
            {
                RainMeadow.Debug("Too early, saving for later");
                if (incomingLease != null && leaseEvent.leaseState.isDelta) { incomingLease.AddDelta(leaseEvent.leaseState); }
                else incomingLease = leaseEvent.leaseState;
            }
            else
            {
                ProcessLease(leaseEvent.leaseState);
            }
        }

        private void ProcessLease(LeaseState leaseState)
        {
            RainMeadow.Debug(this);
            if(currentLeaseState != null)
            {
                currentLeaseState.AddDelta(leaseState);
            }
            else
            {
                if (leaseState.isDelta) { throw new InvalidOperationException("delta"); }
                this.currentLeaseState = leaseState;
            }
            if (leaseState is LobbyLeaseState lobbyLease) { UpdateParticipants(lobbyLease.players.list.Select(id => LobbyManager.instance.GetPlayer(id)).ToList()); }
            foreach (var item in currentLeaseState.sublease)
            {
                var resource = SubresourceFromShortId(item.resourceId);
                var itemOwner = LobbyManager.lobby.PlayerFromId(item.owner);
                if (resource.owner != itemOwner) resource.NewOwner(itemOwner);
                resource.UpdateParticipants(item.participants.list.Select(u=>LobbyManager.lobby.PlayerFromId(u)).ToList());
            }
        }

        public class SubleaseState : Serializer.ICustomSerializable
        {
            public bool isDelta;
            public ushort resourceId;
            public ushort owner;
            public SerializableIDeltaUnsortedListOfUShorts participants;
            public bool isEmptyDelta;

            public SubleaseState() { }
            public SubleaseState(OnlineResource resource)
            {
                this.resourceId = resource.ShortId();
                this.owner = resource.owner?.inLobbyId ?? default;
                this.participants = new SerializableIDeltaUnsortedListOfUShorts(resource.participants.Keys.Select(p => p.inLobbyId).ToList());
            }

            // update from other
            public void AddDelta(SubleaseState other)
            {
                if (isDelta) throw new InvalidProgrammerException("is already delta");
                if (!other.isDelta) throw new InvalidProgrammerException("other not delta");
                if (resourceId != other.resourceId) throw new InvalidProgrammerException("wrong resource");
                owner = other.owner;
                participants.AddDelta(other.participants);
            }

            // difference from other
            public SubleaseState Delta(SubleaseState other)
            {
                if (isDelta) throw new InvalidProgrammerException("is already delta");
                if (other == null) { return this; }
                if (other.isDelta) throw new InvalidProgrammerException("other is delta");
                return new SubleaseState() { 
                    isDelta = true,
                    resourceId = resourceId,
                    owner = owner,
                    participants = participants.Delta(other.participants),
                    isEmptyDelta = (owner == other.owner && participants.isEmptyDelta)  
                };
            }

            public void CustomSerialize(Serializer serializer)
            {
                serializer.Serialize(ref isDelta);
                serializer.Serialize(ref resourceId);
                serializer.Serialize(ref owner);
                serializer.Serialize(ref participants);
            }
        }

        public class LeaseState : Serializer.ICustomSerializable // its it's own weird thing, sent around as events because critical yet too big to send fully every frame
        {
            public bool isDelta;
            public List<SubleaseState> sublease; // owner and participants in subresources
            public bool isEmptyDelta; // do not send

            public LeaseState() { }
            public LeaseState(OnlineResource onlineResource)
            {
                sublease = onlineResource.subresources.Select(r => new SubleaseState(r)).ToList();
            }

            // update from other
            public virtual void AddDelta(LeaseState other)
            {
                if (isDelta) throw new InvalidProgrammerException("is already delta");
                if (!other.isDelta) throw new InvalidProgrammerException("other not delta");
                foreach (var otherLease in other.sublease)
                {
                    foreach (var lease in sublease)
                    {
                        if (lease.resourceId == otherLease.resourceId)
                        {
                            lease.AddDelta(otherLease);
                            break;
                        }
                    }
                }
            }

            // difference from other
            public virtual LeaseState Delta(LeaseState other)
            {
                if (isDelta) throw new InvalidProgrammerException("is already delta");
                if (other == null) { return this; }
                if (other.isDelta) throw new InvalidProgrammerException("other is delta");
                var deltasublease = sublease.Select(sl => sl.Delta(other.sublease.FirstOrDefault(osl => osl.resourceId == sl.resourceId))).Where(sl => !sl.isEmptyDelta).ToList();
                return new LeaseState()
                {
                    isDelta = true,
                    sublease = deltasublease,
                    isEmptyDelta = deltasublease.Count == 0
                };
            }

            public virtual void CustomSerialize(Serializer serializer)
            {
                serializer.Serialize(ref isDelta);
                serializer.Serialize(ref sublease);
            }
        }

        public class LobbyLeaseState : LeaseState
        {
            public SerializableIDeltaSortedListOfPlayerId players;

            public LobbyLeaseState() { }
            public LobbyLeaseState(OnlineResource onlineResource) : base(onlineResource)
            {
                players = new(onlineResource.participants.Keys.Select(p=>p.id).ToList());
            }

            // update from other
            public override void AddDelta(LeaseState _other)
            {
                base.AddDelta(_other);
                var other = _other as LobbyLeaseState;
                players.AddDelta(other.players);
                
            }

            // difference from other
            public override LeaseState Delta(LeaseState _other)
            {
                var other = _other as LobbyLeaseState;
                if (isDelta) throw new InvalidProgrammerException("is already delta");
                if (other == null) { return this; }
                if (other.isDelta) throw new InvalidProgrammerException("other is delta");
                var delta = new LobbyLeaseState()
                {
                    isDelta = true,
                    players = players.Delta(other.players),
                    sublease = sublease.Select(sl => sl.Delta(other.sublease.FirstOrDefault(osl => osl.resourceId == sl.resourceId))).Where(sl => !sl.isEmptyDelta).ToList()
                };
                delta.isEmptyDelta = delta.sublease.Count == 0 && delta.players.isEmptyDelta;
                return delta;
            }

            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                serializer.Serialize(ref players);
            }
        }
    }
}

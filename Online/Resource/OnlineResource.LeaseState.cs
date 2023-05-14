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
        private LeaseState currentLeaseState;

        private void NewLeaseState()
        {
            RainMeadow.Debug(this);
            if (!isActive) { throw new InvalidOperationException("not active"); }
            if (!isOwner) { throw new InvalidOperationException("not owner"); }
            if (ownerSinceTick is null) { throw new InvalidOperationException("no tick reference"); }
            if (subresources.Count == 0) { return; } // nothing to be sent
            var newLeaseState = new LeaseState(this);
            var delta = newLeaseState.Delta(currentLeaseState);
            foreach (var membership in participants)
            {
                if (membership.Key.isMe) continue;
                
                if (!membership.Value.everSentLease)
                {
                    var tickReference = PlayerTickReference.NewestOf(membership.Value.memberSinceTick, this, ownerSinceTick, super);
                    membership.Key.QueueEvent(new LeaseChangeEvent(this, newLeaseState, tickReference)); // its their first time here
                    membership.Value.everSentLease = true;
                }
                else if(!delta.isEmptyDelta)
                {
                    membership.Key.QueueEvent(new LeaseChangeEvent(this, delta, null)); // send the delta
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
            if(this is Lobby) this.UpdateParticipants(currentLeaseState.participants.participants); // only lobby gets self-member list remember
            foreach (var item in currentLeaseState.sublease)
            {
                var resource = SubresourceFromShortId(item.resourceId);
                if (resource.owner != item.owner) resource.NewOwner(item.owner);
                resource.UpdateParticipants(item.participants.participants);
            }
        }

        public class OnlinePlayerGroup : Serializer.ICustomSerializable
        {
            public List<OnlinePlayer> participants;
            public List<OnlinePlayer> left;
            public bool isDelta;
            public bool isEmptyDelta;

            public OnlinePlayerGroup() { }

            public OnlinePlayerGroup(List<OnlinePlayer> participants)
            {
                this.participants = participants ?? throw new ArgumentNullException(nameof(participants));
                left = new();
            }

            // update from other
            public void AddDelta(OnlinePlayerGroup other)
            {
                if (isDelta) throw new InvalidProgrammerException("is already delta");
                if (!other.isDelta) throw new InvalidProgrammerException("other not delta");
                participants = participants.Union(other.participants).Except(other.left).ToList();
            }

            // difference from other
            public OnlinePlayerGroup Delta(OnlinePlayerGroup other)
            {
                if (isDelta) throw new InvalidProgrammerException("is already delta");
                if (other == null) { return this; }
                if (other.isDelta) throw new InvalidProgrammerException("other is delta");
                return new OnlinePlayerGroup()
                {
                    isDelta = true,
                    participants = participants.Except(other.participants).ToList(),
                    left = other.participants.Except(participants).ToList(),
                    isEmptyDelta = participants.Count == 0 && left.Count == 0
                };
            }

            public void CustomSerialize(Serializer serializer)
            {
                serializer.Serialize(ref isDelta);
                serializer.Serialize(ref participants);
                if (isDelta) serializer.Serialize(ref left);
            }
        }

        public class SubleaseState : Serializer.ICustomSerializable
        {
            public bool isDelta;
            public ushort resourceId;
            public OnlinePlayer owner;
            public OnlinePlayerGroup participants;
            public bool isEmptyDelta;

            public SubleaseState() { }
            public SubleaseState(OnlineResource resource)
            {
                this.resourceId = resource.ShortId();
                this.owner = resource.owner;
                this.participants = new OnlinePlayerGroup(resource.participants.Keys.ToList());
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
            public OnlinePlayerGroup participants; // participants in current resource, only sent for Lobby
            public List<SubleaseState> sublease; // owner and participants in subresources
            public bool isEmptyDelta;

            public LeaseState() { }
            public LeaseState(OnlineResource onlineResource)
            {
                participants = new OnlinePlayerGroup(onlineResource is Lobby ? onlineResource.participants.Keys.ToList() : new());
                sublease = onlineResource.subresources.Select(r => new SubleaseState(r)).ToList();
            }

            // update from other
            public void AddDelta(LeaseState other)
            {
                if (isDelta) throw new InvalidProgrammerException("is already delta");
                if (!other.isDelta) throw new InvalidProgrammerException("other not delta");
                participants.AddDelta(other.participants);
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
            public LeaseState Delta(LeaseState other)
            {
                if (isDelta) throw new InvalidProgrammerException("is already delta");
                if (other == null) { return this; }
                if (other.isDelta) throw new InvalidProgrammerException("other is delta");
                var deltaparticipants = participants.Delta(other.participants);
                var deltasublease = sublease.Select(sl => sl.Delta(other.sublease.FirstOrDefault(osl => osl.resourceId == sl.resourceId))).Where(sl => !sl.isEmptyDelta).ToList();
                return new LeaseState()
                {
                    isDelta = true,
                    participants = deltaparticipants,
                    sublease = deltasublease,
                    isEmptyDelta = deltaparticipants.isEmptyDelta && deltasublease.Count == 0
                };
            }

            public void CustomSerialize(Serializer serializer)
            {
                serializer.Serialize(ref isDelta);
                serializer.Serialize(ref participants);
                serializer.Serialize(ref sublease);
            }
        }
    }
}

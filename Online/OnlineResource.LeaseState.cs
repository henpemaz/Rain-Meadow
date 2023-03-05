using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        private LeaseState incomingLease; // lease to be processed on activate

        private void NewLeaseState()
        {
            RainMeadow.Debug(this);
            foreach (var s in subscriptions)
            {
                s.NewLeaseState(this);
            }
        }

        internal LeaseState GetLeaseState()
        {
            return new LeaseState(this);
        }

        internal void LeaseChange(LeaseState leaseState)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) { throw new InvalidOperationException("not available"); }
            if (isOwner) { throw new InvalidOperationException("owner"); }
            if (!isActive) // store it for later
            {
                if (incomingLease != null) { incomingLease.AddDelta(leaseState); }
                else incomingLease = leaseState;
            }
            else
            {
                ProcessLease(leaseState);
            }
        }

        private void ProcessLease(LeaseState leaseState)
        {
            RainMeadow.Debug(this);
            foreach (var item in leaseState.ownership)
            {
                // todo fix the security hole in this ;3
                // should really only be able to reference subresources here, not any resource
                OnlineManager.ResourceFromIdentifier(item.Key).NewOwner(OnlineManager.PlayerFromId(item.Value));
            }
            this.participants = participants
                .Union(leaseState.entered.Select(OnlineManager.PlayerFromId))
                .Except(leaseState.left.Select(OnlineManager.PlayerFromId)).ToList();
        }

        public class LeaseState // its it's own weird thing, sent around as events because critical yet too big to send fully every frame
            // if the state delta-from-last-acknowledged mechanism works properly, this could then be sent as a 1-tick, but would achieve the same?
        {
            public Dictionary<string,ulong> ownership;
            public List<ulong> entered;
            public List<ulong> left;

            public LeaseState() { }
            public LeaseState(OnlineResource onlineResource)
            {
                ownership = new(onlineResource.subresources.Count);
                foreach (var sub in onlineResource.subresources)
                {
                    ownership[sub.Identifier()] = sub.owner is OnlinePlayer p ? p.id.m_SteamID : 0ul;
                }
                entered = onlineResource.participants.Select(p => p.id.m_SteamID).ToList();
                left = new();
            }

            // update from other
            internal void AddDelta(LeaseState leaseState)
            {
                foreach (var item in leaseState.ownership)
                {
                    ownership[item.Key] = item.Value;
                }

                entered = entered.Union(leaseState.entered).Except(leaseState.left).ToList();
            }

            internal void CustomSerialize(Serializer serializer)
            {
                serializer.Serialize(ref ownership);
                serializer.Serialize(ref entered);
                serializer.Serialize(ref left);
            }

            // difference from other
            internal LeaseState Delta(LeaseState previousLeaseState)
            {
                if(previousLeaseState == null) { return this; }
                var delta = new LeaseState();
                delta.ownership = ownership.Except(previousLeaseState.ownership).ToDictionary(); // linq why no dict from kvp

                delta.entered = entered.Except(previousLeaseState.entered).ToList();
                delta.left = previousLeaseState.entered.Except(entered).ToList();

                return delta;
            }
        }
    }
}

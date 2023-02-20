using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        internal LeaseState GetLeaseState()
        {
            return new LeaseState(this);
        }

        internal void LeaseChange(LeaseState leaseState)
        {
            RainMeadow.Debug(this);
            if (!isAvailable) { throw new InvalidOperationException("not available"); }
            if (!isActive)
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
                OnlineManager.ResourceFromIdentifier(item.Key).NewOwner(OnlineManager.PlayerFromId(new Steamworks.CSteamID(item.Value)));
            }
        }

        public class LeaseState
        {
            public Dictionary<string,ulong> ownership;

            public LeaseState() { }
            public LeaseState(OnlineResource onlineResource)
            {
                ownership = new(onlineResource.subresources.Count);
                foreach (var sub in onlineResource.subresources)
                {
                    ownership[sub.Identifier()] = sub.owner is OnlinePlayer p ? p.id.m_SteamID : 0ul;
                }
            }

            // update from other
            internal void AddDelta(LeaseState leaseState)
            {
                foreach (var item in leaseState.ownership)
                {
                    ownership[item.Key] = item.Value;
                }
            }

            // difference from other
            internal LeaseState Delta(LeaseState previousLeaseState)
            {
                if(previousLeaseState == null) { return this; }
                var delta = new LeaseState();
                delta.ownership = ownership.Except(previousLeaseState.ownership).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return delta;
            }
        }
    }
}

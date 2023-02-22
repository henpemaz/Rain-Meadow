using System;

namespace RainMeadow
{
    public class Subscription
    {
        public OnlineResource onlineResource;
        public OnlinePlayer player;
        private OnlineResource.LeaseState previousLeaseState;

        public Subscription(OnlineResource onlineResource, OnlinePlayer player)
        {
            this.onlineResource = onlineResource;
            this.player = player;
        }

        public void Update(ulong ts)
        {
            // Todo delta
            player.OutgoingStates.Enqueue(onlineResource.GetState(ts));
        }

        internal void NewLeaseState(OnlineResource onlineResource) // Lease changes are critical and thus sent as events
        {
            var newLeaseState = onlineResource.GetLeaseState();
            player.QueueEvent(new LeaseChangeEvent(onlineResource, newLeaseState.Delta(previousLeaseState))); // send the delta
            previousLeaseState = newLeaseState; // store in full
        }
    }
}
using System;

namespace RainMeadow
{
    internal class Subscription
    {
        private OnlineResource onlineResource;
        private OnlinePlayer player;

        public Subscription(OnlineResource onlineResource, OnlinePlayer player)
        {
            this.onlineResource = onlineResource;
            this.player = player;
        }

        internal void Update(int ts)
        {
            player.OutgoingStates.Enqueue(onlineResource.GetState(ts));
        }
    }
}
using System;

namespace RainMeadow
{
    internal class Subscription
    {
        private OnlineResource onlineResource;
        private OnlinePlayer player;
        private int tick;

        public Subscription(OnlineResource onlineResource, OnlinePlayer player)
        {
            this.onlineResource = onlineResource;
            this.player = player;
        }

        internal void Update(int ts)
        {
            player.outgoingStates.Add(onlineResource.GetState(ts));
        }
    }
}
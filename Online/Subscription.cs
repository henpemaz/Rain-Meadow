using System;

namespace RainMeadow
{
    internal class Subscription
    {
        internal OnlineResource onlineResource;
        internal OnlinePlayer player;

        public Subscription(OnlineResource onlineResource, OnlinePlayer player)
        {
            this.onlineResource = onlineResource;
            this.player = player;
        }

        internal void Update(ulong ts)
        {
            player.OutgoingStates.Enqueue(onlineResource.GetState(ts));
        }
    }
}
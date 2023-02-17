using System;

namespace RainMeadow
{
    public class Subscription
    {
        public OnlineResource onlineResource;
        public OnlinePlayer player;

        public Subscription(OnlineResource onlineResource, OnlinePlayer player)
        {
            this.onlineResource = onlineResource;
            this.player = player;
        }

        public void Update(ulong ts)
        {
            player.OutgoingStates.Enqueue(onlineResource.GetState(ts));
        }
    }
}
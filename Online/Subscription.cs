using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public class Subscription // Feed resource state to player
    {
        public OnlineResource resource;
        public OnlinePlayer player;
        public Queue<OnlineState> OutgoingStates = new(32);
        public OnlineState lastAcknoledgedState;

        public Subscription(OnlineResource resource, OnlinePlayer player)
        {
            this.resource = resource;
            this.player = player;
        }

        public void Update(ulong tick)
        {
            if (!resource.isAvailable) throw new InvalidOperationException("not available");
            if (!resource.isActive) return; // resource not ready yet

            while (OutgoingStates.Count > 0 && OnlineManager.IsNewerOrEqual(player.lastAckdTick, OutgoingStates.Peek().ts))
            {
                lastAcknoledgedState = OutgoingStates.Dequeue();
            }

            var newState = resource.GetState(tick);
            player.OutgoingStates.Enqueue(newState.Delta(lastAcknoledgedState));
            OutgoingStates.Enqueue(newState);
        }
    }
}
using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public class ResourceSubscription // Feed resource state to player
    {
        public OnlineResource resource;
        public OnlinePlayer player;
        public Queue<OnlineState> OutgoingStates = new(32);
        public OnlineState lastAcknoledgedState;

        public ResourceSubscription(OnlineResource resource, OnlinePlayer player)
        {
            this.resource = resource;
            this.player = player;
            if (!resource.isAvailable) throw new InvalidOperationException("not available");
            if (player.isMe) throw new InvalidOperationException("subscribed to self");
        }

        public void Update(uint tick)
        {
            if (!resource.isAvailable) throw new InvalidOperationException("not available");
            if (!resource.isOwner) throw new InvalidOperationException("not owner");
            if (!resource.isActive) return; // resource not ready yet

            while (OutgoingStates.Count > 0 && NetIO.IsNewerOrEqual(player.lastAckdTick, OutgoingStates.Peek().tick))
            {
                lastAcknoledgedState = OutgoingStates.Dequeue();
            }
            if (lastAcknoledgedState != null && lastAcknoledgedState.tick != player.lastAckdTick) lastAcknoledgedState = null;

            var newState = resource.GetState(tick);
            if (lastAcknoledgedState != null)
            {
                //RainMeadow.Debug($"sending delta for tick {newState.tick} from reference {lastAcknoledgedState.tick} ");
                player.OutgoingStates.Enqueue(newState.Delta(lastAcknoledgedState));
            }
            else
            {
                //RainMeadow.Debug($"sending absolute state for tick {newState.tick}");
                player.OutgoingStates.Enqueue(newState);
            }
            OutgoingStates.Enqueue(newState);
        }
    }
}
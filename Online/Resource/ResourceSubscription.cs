using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public class ResourceSubscription // Feed resource state to player
    {
        public OnlineResource resource;
        public OnlinePlayer player;
        public Queue<OnlineResource.ResourceState> OutgoingStates = new(32);
        public OnlineResource.ResourceState lastAcknoledgedState;

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

            if (player.recentlyAckdTicks.Count > 0) while (OutgoingStates.Count > 0 && NetIO.IsNewerOrEqual(player.oldestTickToConsider, OutgoingStates.Peek().tick)) OutgoingStates.Dequeue(); // discard obsolete
            if (player.recentlyAckdTicks.Count > 0) while (OutgoingStates.Count > 0 && player.recentlyAckdTicks.Contains(OutgoingStates.Peek().tick)) lastAcknoledgedState = OutgoingStates.Dequeue(); // use most recent available
            if (lastAcknoledgedState != null && !player.recentlyAckdTicks.Contains(lastAcknoledgedState.tick)) lastAcknoledgedState = null; // not available

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
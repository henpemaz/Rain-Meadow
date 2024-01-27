using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public class ResourceSubscription : OnlineStateMessage.IStateSource // Feed resource state to player
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

            if (player.recentlyAckdTicks.Count > 0) while (OutgoingStates.Count > 0 && NetIO.IsNewer(player.oldestTickToConsider, OutgoingStates.Peek().tick))
                {
                    RainMeadow.Trace("Discarding obsolete:" + OutgoingStates.Peek().tick);
                    OutgoingStates.Dequeue(); // discard obsolete
                }
            if (player.recentlyAckdTicks.Count > 0) while (OutgoingStates.Count > 0 && player.recentlyAckdTicks.Contains(OutgoingStates.Peek().tick))
                {
                    RainMeadow.Trace("Considering candidate:" + OutgoingStates.Peek().tick);
                    lastAcknoledgedState = OutgoingStates.Dequeue(); // use most recent available
                }
            if (lastAcknoledgedState != null && !player.recentlyAckdTicks.Contains(lastAcknoledgedState.tick))
            {
                RainMeadow.Trace("invalid:" + lastAcknoledgedState.tick);
                lastAcknoledgedState = null; // not available
            }

            var newState = resource.GetState(tick);
            if (lastAcknoledgedState != null)
            {
                RainMeadow.Trace($"sending delta for tick {newState.tick} from reference {lastAcknoledgedState.tick} ");
                var delta = newState.Delta(lastAcknoledgedState);
                //RainMeadow.Trace("Sending delta:\n" + delta.DebugPrint(0));
                player.QueueStateMessage(new OnlineStateMessage(delta, this));
                //player.OutgoingStates.Enqueue(delta);
            }
            else
            {
                RainMeadow.Trace($"sending absolute state for tick {newState.tick}");
                //RainMeadow.Trace("Sending full:\n" + newState.DebugPrint(0));
                player.QueueStateMessage(new OnlineStateMessage(newState, this));
                //player.OutgoingStates.Enqueue(newState);
            }
            OutgoingStates.Enqueue(newState);
        }

        public void ResetDeltas()
        {
            RainMeadow.Debug($"delta reset for {resource} -> {player}");
            OutgoingStates = new Queue<OnlineResource.ResourceState>(OutgoingStates.Where(x => !x.isDelta && x.tick > player.latestTickAck));
        }

        public void Sent(OnlineState state)
        {
            // no op
        }

        public void Failed(OnlineState state)
        {
            var failedTick = (state as OnlineResource.ResourceState).tick;
            OutgoingStates = new(OutgoingStates.Where(e => e.tick != failedTick));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public class EntityFeed // Feed entity state to resource owner
    {
        public OnlineResource resource;
        public OnlineEntity entity;
        public OnlinePlayer player;
        public Queue<EntityState> OutgoingStates = new(32);
        public EntityState lastAcknoledgedState;

        public EntityFeed(OnlineResource resource, OnlineEntity oe)
        {
            this.resource = resource;
            this.player = resource.owner;
            this.entity = oe;
            if (resource.isOwner) throw new InvalidOperationException("feeding myself");
        }

        public void Update(uint tick)
        {
            if (!resource.isAvailable) throw new InvalidOperationException("not available");
            if (resource.isOwner)
            {
                RainMeadow.Error($"Self-feeding entity {entity} for resource {resource}");
                throw new InvalidOperationException("feeding myself");
            }
            if (resource.owner != player) // they don't know
            {
                OutgoingStates.Clear();
                lastAcknoledgedState = null;
            }
            player = resource.owner;

            if (player.recentlyAckdTicks.Count > 0) while (OutgoingStates.Count > 0 && NetIO.IsNewer(player.oldestTickToConsider, OutgoingStates.Peek().tick))
                {
                    //RainMeadow.Debug("Discarding obsolete:" + OutgoingStates.Peek().tick);
                    OutgoingStates.Dequeue(); // discard obsolete
                }
            if (player.recentlyAckdTicks.Count > 0) while (OutgoingStates.Count > 0 && player.recentlyAckdTicks.Contains(OutgoingStates.Peek().tick))
                {
                    //RainMeadow.Debug("Considering candidate:" + OutgoingStates.Peek().tick);
                    lastAcknoledgedState = OutgoingStates.Dequeue(); // use most recent available
                }
            if (lastAcknoledgedState != null && !player.recentlyAckdTicks.Contains(lastAcknoledgedState.tick))
            {
                //RainMeadow.Debug("invalid:" + lastAcknoledgedState.tick);
                lastAcknoledgedState = null; // not available
            }

            var newState = entity.GetState(tick, resource);
            if (lastAcknoledgedState != null)
            {
                //RainMeadow.Debug($"sending delta for tick {newState.tick} from reference {lastAcknoledgedState.tick} ");
                var delta = newState.Delta(lastAcknoledgedState);
                //RainMeadow.Debug("Sending delta:\n" + delta.DebugPrint(0));
                player.OutgoingStates.Enqueue(new EntityFeedState(delta, resource));
            }
            else
            {
                // RainMeadow.Debug($"sending absolute state for tick {newState.tick}");
                //RainMeadow.Debug("Sending full:\n" + newState.DebugPrint(0));
                player.OutgoingStates.Enqueue(new EntityFeedState(newState, resource));
            }
            OutgoingStates.Enqueue(newState);
        }
    }
}
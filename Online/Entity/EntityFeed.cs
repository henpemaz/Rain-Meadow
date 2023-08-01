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
        public Queue<EntityFeedState> OutgoingStates = new(32);
        public EntityFeedState lastAcknoledgedState;

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

            if (player.recentlyAckdTicks.Count > 0) while (OutgoingStates.Count > 0 && NetIO.IsNewerOrEqual(player.oldestTickToConsider, OutgoingStates.Peek().tick)) OutgoingStates.Dequeue(); // discard obsolete
            if (player.recentlyAckdTicks.Count > 0) while (OutgoingStates.Count > 0 && player.recentlyAckdTicks.Contains(OutgoingStates.Peek().tick)) lastAcknoledgedState = OutgoingStates.Dequeue(); // use most recent available
            if (lastAcknoledgedState != null && !player.recentlyAckdTicks.Contains(lastAcknoledgedState.tick)) lastAcknoledgedState = null; // not available

            var newState = entity.GetFeedState(tick, resource);
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
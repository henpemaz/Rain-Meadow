using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public class EntityFeed : OnlineStateMessage.IStateSource // Feed entity state to resource owner
    {
        public OnlineResource resource;
        public OnlineEntity entity;
        public OnlinePlayer player;
        public Queue<OnlineStateMessage> OutgoingStates = new(32);
        public OnlineEntity.EntityState lastAcknoledgedState;

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

            if (player.recentlyAckdTicks.Count > 0)
            {
                while (OutgoingStates.Count > 0 && NetIO.IsNewer(player.oldestTickToConsider, OutgoingStates.Peek().tick))
                {
                    RainMeadow.Trace("Discarding obsolete:" + OutgoingStates.Peek().tick);
                    OutgoingStates.Dequeue(); // discard obsolete
                }
                while (OutgoingStates.Count > 0 && player.recentlyAckdTicks.Contains(OutgoingStates.Peek().tick))
                {
                    RainMeadow.Trace("Considering candidate:" + OutgoingStates.Peek().tick);
                    lastAcknoledgedState = (OnlineEntity.EntityState)OutgoingStates.Dequeue().sourceState; // use most recent available
                }
            }

            var newState = entity.GetState(tick, resource);
            if (lastAcknoledgedState != null)
            {
                RainMeadow.Trace($"sending delta for tick {newState.tick} from reference {lastAcknoledgedState.tick}");
                var delta = (OnlineEntity.EntityState)newState.Delta(lastAcknoledgedState);
                //RainMeadow.Trace("Sending delta:\n" + delta.DebugPrint(0));
                OutgoingStates.Enqueue(player.QueueStateMessage(new OnlineStateMessage(new EntityFeedState(delta, resource), newState, this, true, tick, delta.baseline)));
            }
            else
            {
                RainMeadow.Trace($"sending absolute state for tick {newState.tick}");
                //RainMeadow.Trace("Sending full:\n" + newState.DebugPrint(0));
                OutgoingStates.Enqueue(player.QueueStateMessage(new OnlineStateMessage(new EntityFeedState(newState, resource), newState, this, false, tick, 0)));
            }
        }

        public void ResetDeltas()
        {
            RainMeadow.Debug($"delta reset for {entity} in {resource} -> {player}");
            RainMeadow.Debug($"recent states were [{string.Join(", ", OutgoingStates.Select(s => s.sentAsDelta ? $"{s.tick}d{s.baseline}" : $"{s.tick}"))}]");
            lastAcknoledgedState = null;
            OutgoingStates = new Queue<OnlineStateMessage>(OutgoingStates.Where(x => !x.sentAsDelta && x.tick > player.latestTickAck));
        }

        public void Sent(OnlineStateMessage stateMessage)
        {
            // no op
        }

        public void Failed(OnlineStateMessage stateMessage)
        {
            OutgoingStates = new(OutgoingStates.Where(e => e.tick != stateMessage.tick));
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;

namespace RainMeadow
{
    public class EntityFeed // Feed entity state to resource owner
    {
        public OnlineResource resource;
        public OnlineEntity entity;
        public Queue<OnlineState> OutgoingStates = new(32);
        public OnlineState lastAcknoledgedState;

        public EntityFeed(OnlineResource resource, OnlineEntity oe)
        {
            this.resource = resource;
            this.entity = oe;
        }

        public void Update(ulong tick)
        {
            if (!resource.isAvailable) throw new InvalidOperationException("not available");
            if (resource.owner.isMe) throw new InvalidOperationException("feeding myself");

            while (OutgoingStates.Count > 0 && OnlineManager.IsNewerOrEqual(resource.owner.lastAckdTick, OutgoingStates.Peek().ts))
            {
                lastAcknoledgedState = OutgoingStates.Dequeue();
            }

            // todo detect owner changed and reset? maybe get rid of reset and just remove feed add feed on ownership changes?
            var newState = entity.GetState(tick, resource);
            resource.owner.OutgoingStates.Enqueue(newState.Delta(lastAcknoledgedState));
            OutgoingStates.Enqueue(newState);
        }

        public void Reset()
        {
            OutgoingStates.Clear();
            lastAcknoledgedState = null;
        }
    }
}
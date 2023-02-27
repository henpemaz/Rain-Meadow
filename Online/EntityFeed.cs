using System;
using System.Collections;
using System.Collections.Generic;

namespace RainMeadow
{
    public class EntityFeed
    {
        public OnlineResource resource;
        public OnlineEntity entity;
        public Queue<OnlineState> OutgoingStates = new(128);
        private OnlineState lastAcknoledgedState;

        public EntityFeed(OnlineResource resource, OnlineEntity oe)
        {
            this.resource = resource;
            this.entity = oe;
        }

        internal void Update(ulong tick)
        {
            if(!resource.isAvailable) throw new InvalidOperationException("not available");

            while (OutgoingStates.Count > 0 && OnlineManager.IsNewerOrEqual(resource.owner.lastAckdTick, OutgoingStates.Peek().ts))
            {
                var e = OutgoingStates.Dequeue();
                lastAcknoledgedState = e;
            }

            var newState = entity.GetState(tick, resource);
            resource.owner.OutgoingStates.Enqueue(newState.Delta(lastAcknoledgedState));
            OutgoingStates.Enqueue(newState);
        }
    }
}
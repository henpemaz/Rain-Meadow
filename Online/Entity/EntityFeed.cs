﻿using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public class EntityFeed // Feed entity state to resource owner
    {
        public OnlineResource resource;
        public OnlineEntity entity;
        public Queue<EntityFeedState> OutgoingStates = new(32);
        public EntityFeedState lastAcknoledgedState;

        public EntityFeed(OnlineResource resource, OnlineEntity oe)
        {
            this.resource = resource;
            this.entity = oe;
        }

        public void Update(uint tick)
        {
            if (!resource.isAvailable) throw new InvalidOperationException("not available");
            if (resource.isOwner)
            {
                RainMeadow.Error($"Self-feeding entity {entity} for resource {resource}");
                throw new InvalidOperationException("feeding myself");
            }

            while (OutgoingStates.Count > 0 && NetIO.IsNewerOrEqual(resource.owner.lastAckdTick, OutgoingStates.Peek().tick))
            {
                lastAcknoledgedState = OutgoingStates.Dequeue();
            }

            // todo detect owner changed and reset? maybe get rid of reset and just remove feed add feed on ownership changes?
            var newState = entity.GetFeedState(tick, resource);
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
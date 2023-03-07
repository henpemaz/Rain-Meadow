using System;
using System.Linq;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        private ResourceState lastState;

        public virtual ResourceState GetState(ulong ts)
        {
            if (lastState == null || lastState.ts != ts)
            {
                lastState = MakeState(ts);
            }

            return lastState;
        }

        protected abstract ResourceState MakeState(ulong ts);
        public virtual void ReadState(ResourceState newState, ulong ts)
        {
            foreach (var entityState in newState.entityStates)
            {
                if (entityState != null)
                {
                    if (entityState.onlineEntity == null || entityState.onlineEntity.owner.isMe) continue;
                    entityState.onlineEntity.ReadState(entityState, ts);
                }
                else
                {
                    throw new InvalidCastException("got null state, maybe it was not an EntityState");
                }
            }
        }

        public abstract class ResourceState : OnlineState
        {
            public OnlineResource resource;
            public OnlineEntity.EntityState[] entityStates;

            protected ResourceState() : base() { }
            protected ResourceState(OnlineResource resource, ulong ts) : base(ts)
            {
                this.resource = resource;
                entityStates = resource.entities.Select(e => e.GetState(ts, resource)).ToArray();
            }

            public override long EstimatedSize => resource.SizeOfIdentifier();
            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                serializer.Serialize(ref resource);
                serializer.Serialize(ref entityStates);
            }
        }
    }
}

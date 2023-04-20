using System;
using System.Linq;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        private ResourceState lastState;

        public ResourceState GetState(ulong ts)
        {
            if (lastState == null || lastState.ts != ts)
            {

                try
                {
                    lastState = MakeState(ts);
                }
                catch (Exception)
                {
                    RainMeadow.Error(this);
                    throw;
                }
            }

            return lastState;
        }

        protected abstract ResourceState MakeState(ulong ts);
        public virtual void ReadState(ResourceState newState, ulong ts)
        {
            if(isActive)
            {
                foreach (var entityState in newState.entityStates)
                {
                    if (entityState != null)
                    {
                        if (entityState.onlineEntity == null)
                        {
                            RainMeadow.Error("got state for missing onlineEntity");
                            continue;
                        }
                        if (entityState.onlineEntity.owner.isMe) continue; // not interested
                        if (entityState.onlineEntity.lowestResource == this) // this resource is the most "detailed" provider
                        {
                            entityState.onlineEntity.ReadState(entityState, ts);
                        }
                    }
                    else
                    {
                        throw new InvalidCastException("got null state, maybe it was not an EntityState");
                    }
                }
            }
        }

        public abstract class ResourceState : OnlineState
        {
            public OnlineResource resource;
            public EntityState[] entityStates;

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

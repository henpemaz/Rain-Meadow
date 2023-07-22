using System;
using System.Linq;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        private ResourceState lastState;

        public ResourceState GetState(ulong ts)
        {
            if (lastState == null || lastState.tick != ts)
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
        public virtual void ReadState(ResourceState newState)
        {
            if(isActive)
            {
                foreach (var entityState in newState.entityStates)
                {
                    if (entityState != null)
                    {
                        var entity = entityState.entityId.FindEntity();
                        if (entity == null)
                        {
                            RainMeadow.Error("got state for missing onlineEntity " + entityState.entityId);
                            continue;
                        }
                        if (entity.isMine) continue; // not interested
                        if (entity.currentlyJoinedResource == this) // this resource is the most "detailed" provider
                        {
                            entity.ReadState(entityState, this);
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
                entityStates = resource.entities.Select(e => e.Key.GetState(ts, resource)).ToArray();
                if (entityStates.Any(es => es == null)) throw new InvalidProgrammerException("here");
            }

            public override long EstimatedSize => resource.SizeOfIdentifier();
            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                serializer.Serialize(ref resource);
                serializer.SerializePolyStates(ref entityStates);
            }
        }
    }
}

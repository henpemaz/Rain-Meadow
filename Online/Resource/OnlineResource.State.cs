using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        private ResourceState latestState;

        public ResourceState GetState(ulong ts)
        {
            if (latestState == null || latestState.tick != ts)
            {
                try
                {
                    latestState = MakeState(ts);
                }
                catch (Exception)
                {
                    RainMeadow.Error(this);
                    throw;
                }
            }

            return latestState;
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
                serializer.SerializeResourceByReference(ref resource);
                serializer.SerializePolyStates(ref entityStates);
            }
        }

        public abstract class ResourceWithSubresourcesState : ResourceState
        {
            public IdentifiablesDeltaList<SubleaseState, ushort> subleaseState;

            protected ResourceWithSubresourcesState() { }
            protected ResourceWithSubresourcesState(OnlineResource resource, ulong ts) : base(resource, ts)
            {
                subleaseState = new IdentifiablesDeltaList<SubleaseState, ushort>(resource.subresources.Select(r => new SubleaseState(r)).ToList());
            }


            public override OnlineState ApplyDelta(OnlineState newState)
            {
                // todo
                return base.ApplyDelta(newState);
            }

            public override OnlineState Delta(OnlineState lastAcknoledgedState)
            {
                return base.Delta(lastAcknoledgedState);
            }

            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                serializer.Serialize(ref subleaseState);
            }

        }

        public class SubleaseState : Serializer.ICustomSerializable, IDelta<SubleaseState>, IIdentifiable<ushort>
        {
            public ushort resourceId;
            public ushort owner;
            public AddRemoveUnsortedUshorts participants;

            public ushort ID => resourceId;

            public SubleaseState() { }
            public SubleaseState(OnlineResource resource)
            {
                this.resourceId = resource.ShortId();
                this.owner = resource.owner?.inLobbyId ?? default;
                this.participants = new AddRemoveUnsortedUshorts(resource.participants.Keys.Select(p => p.inLobbyId).ToList());
            }

            public void ApplyDelta(SubleaseState other)
            {
                if (resourceId != other.resourceId) throw new InvalidProgrammerException("wrong resource");
                owner = other.owner;
                participants.ApplyDelta(other.participants);
            }

            public SubleaseState Delta(SubleaseState other)
            {
                if (other == null) { return this; }
                var delta = new SubleaseState()
                {
                    resourceId = resourceId,
                    owner = owner,
                    participants = participants.Delta(other.participants),
                };
                return (owner == other.owner && delta == null) ? null : delta;
            }

            public void CustomSerialize(Serializer serializer)
            {
                serializer.Serialize(ref resourceId);
                serializer.Serialize(ref owner);
                serializer.Serialize(ref participants);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        protected ResourceState latestState;

        public ResourceState GetState(uint ts)
        {
            if (!isOwner) { throw new InvalidProgrammerException("not owner"); }
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

        protected Queue<ResourceState> incomingState;
        protected abstract ResourceState MakeState(uint ts);
        public void ReadState(ResourceState newState)
        {
            if (newState.from != owner) { RainMeadow.Debug($"Skipping state resource for {this} from wrong owner {newState.from}"); return; }
            if (newState.IsDelta)
            {
                //RainMeadow.Debug($"received delta state for tick {newState.tick} referencing baseline {newState.DeltaFromTick}");
                while (incomingState.Count > 0 && NetIO.IsNewer(newState.DeltaFromTick, incomingState.Peek().tick))
                {
                    var discarded = incomingState.Dequeue();
                    //RainMeadow.Debug("discarding old event from tick " + discarded.tick);
                }
                if (incomingState.Count == 0 || newState.DeltaFromTick != incomingState.Peek().tick)
                {
                    throw new InvalidProgrammerException($"Unprocessable delta");
                }
                newState = (ResourceState)incomingState.Peek().ApplyDelta(newState);
            }
            else
            {
                //RainMeadow.Debug("received absolute state for tick " + newState.tick);
            }
            incomingState.Enqueue(newState);
            newState.ReadTo(this);
            if(isWaitingForState) { Available(); }
        }

        public abstract class ResourceState : OnlineState
        {
            public OnlineResource resource;
            public Generics.DeltaStates<EntityState, OnlineEntity.EntityId> entityStates;

            protected ResourceState() : base() { }
            protected ResourceState(OnlineResource resource, uint ts) : base(ts)
            {
                this.resource = resource;
                entityStates = new(resource.entities.Select(e => e.Key.GetState(ts, resource)).ToList());
            }
            protected abstract ResourceState NewInstance();

            public override long EstimatedSize => resource.SizeOfIdentifier();
            public override bool SupportsDelta => true;

            public override OnlineState ApplyDelta(OnlineState newState)
            {
                if (!newState.IsDelta) throw new InvalidProgrammerException("other isn't delta");
                var result = NewInstance();
                result.tick = newState.tick;
                result.resource = resource;
                result.entityStates = (Generics.DeltaStates<EntityState, OnlineEntity.EntityId>)entityStates.ApplyDelta(((ResourceState)newState)?.entityStates);
                return result;
            }

            public override OnlineState Delta(OnlineState lastAcknoledgedState)
            {
                if (lastAcknoledgedState == null) throw new InvalidProgrammerException("null");
                var delta = NewInstance();
                delta.IsDelta = true;
                delta.DeltaFromTick = lastAcknoledgedState.tick;
                delta.resource = resource;
                delta.entityStates = (Generics.DeltaStates<EntityState, OnlineEntity.EntityId>)entityStates.Delta(((ResourceState)lastAcknoledgedState)?.entityStates);
                return delta;
            }

            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                serializer.SerializeResourceByReference(ref resource);
                serializer.SerializeNullable(ref entityStates);
            }

            public virtual void ReadTo(OnlineResource resource)
            {
                if (resource.isActive)
                {
                    foreach (var entityState in entityStates.list)
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
                            if (entity.currentlyJoinedResource == resource) // this resource is the most "detailed" provider
                            {
                                entity.ReadState(entityState, resource);
                            }
                        }
                        else
                        {
                            throw new InvalidCastException("got null state, maybe it was not an EntityState");
                        }
                    }
                }
            }
        }

        public abstract class ResourceWithSubresourcesState : ResourceState
        {
            public Generics.IdentifiablesDeltaList<SubleaseState, ushort, SubleaseState> subleaseState;

            protected ResourceWithSubresourcesState() { }
            protected ResourceWithSubresourcesState(OnlineResource resource, uint ts) : base(resource, ts)
            {
                subleaseState = new Generics.IdentifiablesDeltaList<SubleaseState, ushort, SubleaseState>(resource.subresources.Select(r => new SubleaseState(r)).ToList());
            }

            public override OnlineState ApplyDelta(OnlineState newState)
            {
                var result = (ResourceWithSubresourcesState)base.ApplyDelta(newState);
                result.subleaseState = subleaseState.ApplyDelta(((ResourceWithSubresourcesState)newState).subleaseState);
                return result;
            }

            public override OnlineState Delta(OnlineState lastAcknoledgedState)
            {
                var delta = (ResourceWithSubresourcesState)base.Delta(lastAcknoledgedState);
                delta.subleaseState = subleaseState.Delta((lastAcknoledgedState as ResourceWithSubresourcesState).subleaseState);
                return delta;
            }

            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                serializer.SerializeNullable(ref subleaseState);
            }

            public override void ReadTo(OnlineResource resource)
            {
                base.ReadTo(resource);
                if (resource.isActive)
                {
                    foreach (var item in subleaseState.list)
                    {
                        var subresource = resource.SubresourceFromShortId(item.resourceId);
                        var itemOwner = OnlineManager.lobby.PlayerFromId(item.owner);
                        if (subresource.owner != itemOwner) subresource.NewOwner(itemOwner);
                        subresource.UpdateParticipants(item.participants.list.Select(u => OnlineManager.lobby.PlayerFromId(u)).ToList());
                    }
                }
            }
        }

        public class SubleaseState : Serializer.ICustomSerializable, Generics.IDelta<SubleaseState>, Generics.IIdentifiable<ushort>
        {
            public ushort resourceId;
            public ushort owner;
            public Generics.AddRemoveUnsortedUshorts participants;

            public ushort ID => resourceId;

            public SubleaseState() { }
            public SubleaseState(OnlineResource resource)
            {
                this.resourceId = resource.ShortId();
                this.owner = resource.owner?.inLobbyId ?? default;
                this.participants = new Generics.AddRemoveUnsortedUshorts(resource.participants.Keys.Select(p => p.inLobbyId).ToList());
            }
            public virtual SubleaseState EmptyInstance() => new();

            public SubleaseState ApplyDelta(SubleaseState other)
            {
                var result = EmptyInstance();
                result.resourceId = resourceId;
                result.owner = other?.owner ?? owner;
                result.participants = (Generics.AddRemoveUnsortedUshorts)participants.ApplyDelta(other?.participants);
                return result;
            }

            public SubleaseState Delta(SubleaseState other)
            {
                if (other == null) { return this; }
                var delta = EmptyInstance();
                delta.resourceId = resourceId;
                delta.owner = owner;
                delta.participants = (Generics.AddRemoveUnsortedUshorts)participants.Delta(other.participants);
                return (owner == other.owner && delta.participants == null) ? null : delta;
            }

            public void CustomSerialize(Serializer serializer)
            {
                serializer.Serialize(ref resourceId);
                serializer.Serialize(ref owner);
                serializer.SerializeNullable(ref participants);
            }
        }
    }
}

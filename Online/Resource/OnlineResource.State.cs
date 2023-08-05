using RainMeadow.Generics;
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
            // this has a flaw when there's multiple players talking to me.
            if (newState.IsDelta)
            {
                //RainMeadow.Debug($"received delta state from {newState.from} for tick {newState.tick} referencing baseline {newState.DeltaFromTick}");
                while (incomingState.Count > 0 && (owner != incomingState.Peek().from || NetIO.IsNewer(newState.DeltaFromTick, incomingState.Peek().tick)))
                {
                    var discarded = incomingState.Dequeue();
                    //RainMeadow.Debug("discarding old event from tick " + discarded.tick);
                }
                if (incomingState.Count == 0 || newState.DeltaFromTick != incomingState.Peek().tick)
                {
                    RainMeadow.Error($"Received unprocessable delta for {this} from {newState.from}, tick {newState.tick} referencing baseline {newState.DeltaFromTick}");
                    return;
                }
                newState = incomingState.Peek().ApplyDelta(newState);
            }
            else
            {
                //RainMeadow.Debug($"received absolute state from {newState.from} for tick " + newState.tick);
            }
            incomingState.Enqueue(newState);
            if (latestState == null || latestState.from != owner || NetIO.IsNewer(newState.tick, latestState.tick))
            {
                latestState = newState;
                newState.ReadTo(this);
            }
            if(isWaitingForState) { Available(); }
        }

        public abstract class ResourceState : RootDeltaState, IPrimaryDelta<ResourceState>
        {
            public OnlineResource resource;
            public DeltaStates<EntityState, OnlineEntity.EntityId> entityStates;

            protected ResourceState() : base() { }
            protected ResourceState(OnlineResource resource, uint ts) : base(ts)
            {
                this.resource = resource;
                entityStates = new(resource.entities.Select(e => e.Key.GetState(ts, resource)).ToList());
            }
            public abstract ResourceState EmptyDelta();

            public override long EstimatedSize(bool inDeltaContext)
            {
                return base.EstimatedSize(inDeltaContext) + resource.SizeOfIdentifier() + (entityStates != null ? (2 + entityStates.list.Sum(e => e.EstimatedSize(inDeltaContext))) : 1);
            }

            public bool IsEmptyDelta { get ; set; }

            public virtual ResourceState ApplyDelta(ResourceState newState)
            {
                if (!newState.IsDelta) throw new InvalidProgrammerException("other isn't delta");
                var result = EmptyDelta();
                result.tick = newState.tick;
                result.resource = resource;
                result.entityStates = entityStates.ApplyDelta(newState.entityStates);
                return result;
            }

            public virtual ResourceState Delta(ResourceState lastAcknoledgedState)
            {
                if (lastAcknoledgedState == null) throw new InvalidProgrammerException("null");
                var delta = EmptyDelta();
                delta.IsDelta = true;
                delta.DeltaFromTick = lastAcknoledgedState.tick;
                delta.resource = resource;
                delta.entityStates = entityStates.Delta(lastAcknoledgedState.entityStates);
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

        public class LeaseList : IdentifiablesDeltaList<SubleaseState, ushort, SubleaseState, LeaseList>
        {
            public LeaseList() { }

            public LeaseList(List<SubleaseState> list) : base(list) { }

            public override void CustomSerialize(Serializer serializer)
            {
                serializer.Serialize(ref list);
            }
        }

        public abstract class ResourceWithSubresourcesState : ResourceState
        {
            public LeaseList subleaseState;

            protected ResourceWithSubresourcesState() { }
            protected ResourceWithSubresourcesState(OnlineResource resource, uint ts) : base(resource, ts)
            {
                subleaseState = new LeaseList(resource.subresources.Select(r => new SubleaseState(r)).ToList());
            }

            public override ResourceState ApplyDelta(ResourceState newState)
            {
                var result = (ResourceWithSubresourcesState)base.ApplyDelta(newState);
                result.subleaseState = subleaseState.ApplyDelta(((ResourceWithSubresourcesState)newState).subleaseState);
                return result;
            }

            public override ResourceState Delta(ResourceState lastAcknoledgedState)
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
            public virtual SubleaseState EmptyDelta() => new();

            public SubleaseState ApplyDelta(SubleaseState other)
            {
                var result = EmptyDelta();
                result.resourceId = resourceId;
                result.owner = other?.owner ?? owner;
                result.participants = (Generics.AddRemoveUnsortedUshorts)participants.ApplyDelta(other?.participants);
                return result;
            }

            public SubleaseState Delta(SubleaseState other)
            {
                if (other == null) { return this; }
                var delta = EmptyDelta();
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

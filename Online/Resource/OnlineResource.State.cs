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

        protected Queue<ResourceState> incomingState = new(32);
        protected abstract ResourceState MakeState(uint ts);
        public void ReadState(ResourceState newState)
        {
            // this has a flaw when there's multiple players talking to me.
            if (newState.isDelta)
            {
                //RainMeadow.Debug($"received delta state from {newState.from} for tick {newState.tick} referencing baseline {newState.Baseline}");
                while (incomingState.Count > 0 && (owner != incomingState.Peek().from || NetIO.IsNewer(newState.baseline, incomingState.Peek().tick)))
                {
                    var discarded = incomingState.Dequeue();
                    //RainMeadow.Debug("discarding old state from tick " + discarded.tick);
                }
                if (incomingState.Count == 0 || newState.baseline != incomingState.Peek().tick)
                {
                    RainMeadow.Error($"Received unprocessable delta for {this} from {newState.from}, tick {newState.tick} referencing baseline {newState.baseline}");
                    if (!newState.from.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(RPCs.DeltaReset, this, null)))
                    {
                        newState.from.InvokeRPC(RPCs.DeltaReset, this, null);
                    }
                    return;
                }
                newState = (ResourceState)incomingState.Peek().ApplyDelta(newState);
            }
            else
            {
                //RainMeadow.Debug($"received absolute state from {newState.from} for tick " + newState.tick);
            }
            incomingState.Enqueue(newState);
            if (newState.from == owner)
            {
                latestState = newState;
                newState.ReadTo(this);
                if (isWaitingForState) { Available(); }
            }
        }

        public abstract class ResourceState : RootDeltaState
        {
            [OnlineField]
            public OnlineResource resource;
            [OnlineField(nullable = true)]
            public Generics.AddRemoveSortedCustomSerializables<OnlineEntity.EntityId> entitiesJoined;
            [OnlineField(nullable = true)]
            public DeltaStates<EntityDefinition, OnlineState, OnlineEntity.EntityId> registeredEntities;
            [OnlineField(nullable = true)]
            public DeltaStates<EntityState, OnlineState, OnlineEntity.EntityId> entityStates;

            protected ResourceState() : base() { }
            protected ResourceState(OnlineResource resource, uint ts) : base(ts)
            {
                this.resource = resource;
                entitiesJoined = new(resource.entities.Keys.ToList());
                registeredEntities = new(resource.registeredEntities.Values.Select(def => def.Clone() as EntityDefinition).ToList());
                entityStates = new(resource.entities.Select(e => e.Value.entity.GetState(ts, resource)).ToList());
            }
            public virtual void ReadTo(OnlineResource resource)
            {
                if (resource.isActive)
                {
                    foreach (var def in registeredEntities.list)
                    {
                        if (!resource.registeredEntities.ContainsKey(def.entityId))
                        {
                            resource.OnNewRemoteEntity(def, entityStates.list.Find(es => es.entityId == def.entityId));
                        }
                    }

                    foreach (var entityId in entitiesJoined.list)
                    {
                        if (!resource.entities.ContainsKey(entityId))
                        {
                            // there might be some timing considerations to this, entity from higher up not being available locally yet
                            var ent = entityId.FindEntity();
                            if (ent != null)
                            {
                                resource.EntityJoinedResource(ent, entityStates.list.Find(es => es.entityId == entityId));
                            }
                            else
                            {
                                RainMeadow.Error($"Entity in resource {this} missing: " + entityId);
                            }
                        }
                    }

                    foreach (var kvp in resource.entities.ToList())
                    {
                        // this would be better as a set not a list
                        if (!entitiesJoined.list.Contains(kvp.Key))
                        {
                            resource.EntityLeftResource(kvp.Value.entity);
                        }
                    }

                    foreach (var def in registeredEntities.list)
                    {
                        if (resource.entities.TryGetValue(def.entityId, out var ent)) // hmm
                        {
                            if (def.owner != ent.entity.owner)
                            {
                                ent.entity.NewOwner(def.owner);
                            }
                        }
                        else
                        {
                            RainMeadow.Error($"Entity in resource {this} missing: " + def.entityId);
                        }
                    }

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
            [OnlineField(nullable = true)]
            public LeaseList subleaseState;

            protected ResourceWithSubresourcesState() { }
            protected ResourceWithSubresourcesState(OnlineResource resource, uint ts) : base(resource, ts)
            {
                subleaseState = new LeaseList(resource.subresources.Select(r => new SubleaseState(r)).ToList());
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

        // TODO improve this kind of data object
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
                result.participants = participants.ApplyDelta(other?.participants);
                return result;
            }

            public SubleaseState Delta(SubleaseState other)
            {
                if (other == null) { return this; }
                var delta = EmptyDelta();
                delta.resourceId = resourceId;
                delta.owner = owner;
                delta.participants = participants.Delta(other.participants);
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

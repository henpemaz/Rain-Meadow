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
                RainMeadow.Trace($"received delta state from {newState.from} for tick {newState.tick} referencing baseline {newState.baseline}");
                while (incomingState.Count > 0 && (owner != incomingState.Peek().from || NetIO.IsNewer(newState.baseline, incomingState.Peek().tick)))
                {
                    var discarded = incomingState.Dequeue();
                    RainMeadow.Trace("discarding old state from tick " + discarded.tick);
                }
                if (incomingState.Count == 0 || newState.baseline != incomingState.Peek().tick)
                {
                    RainMeadow.Error($"Received unprocessable delta for {this} from {newState.from}, tick {newState.tick} referencing baseline {newState.baseline}");
                    RainMeadow.Error($"Available ticks are: [{string.Join(", ", incomingState.Where(s => s.from == newState.from).Select(s => s.tick))}]");
                    if (!newState.from.OutgoingEvents.Any(e=>e is RPCEvent rpc && rpc.IsIdentical(RPCs.DeltaReset, this, null)))
                    {
                        newState.from.InvokeRPC(RPCs.DeltaReset, this, null);
                    }
                    return;
                }
                newState = (ResourceState)incomingState.Peek().ApplyDelta(newState);
            }
            else
            {
                RainMeadow.Trace($"received absolute state from {newState.from} for tick " + newState.tick);
            }
            incomingState.Enqueue(newState);
            if (newState.from == owner)
            {
                latestState = newState;
                if (isWaitingForState || isAvailable) newState.ReadTo(this);
                if(isWaitingForState) { Available(); }
            }
            else
            {
                RainMeadow.Trace($"received state from {newState.from} but owner is {owner}");
            }
        }

        private List<ResourceData> resourceData = new();

        internal T AddData<T>(bool ignoreDuplicate = false) where T : ResourceData
        {
            for (int i = 0; i < resourceData.Count; i++)
            {
                if (resourceData[i].GetType() == typeof(T))
                {
                    if (ignoreDuplicate) return (T)resourceData[i];
                    throw new ArgumentException("type already in data");
                }
            }
            var v = (T)Activator.CreateInstance(typeof(T), new[] { this });
            resourceData.Add(v);
            return v;
        }

        internal T AddData<T>(T toAdd, bool ignoreDuplicate = false) where T : ResourceData
        {
            for (int i = 0; i < resourceData.Count; i++)
            {
                if (ignoreDuplicate) return (T)resourceData[i];
                throw new ArgumentException("type already in data");
            }
            resourceData.Add(toAdd);
            return toAdd;
        }

        internal bool TryGetData<T>(out T d, bool addIfMissing = false) where T : ResourceData
        {
            for (int i = 0; i < resourceData.Count; i++)
            {
                if (resourceData[i].GetType() == typeof(T))
                {
                    d = (T)resourceData[i];
                    return true;
                }
            }
            if (addIfMissing)
            {
                d = (T)Activator.CreateInstance(typeof(T), new[] { this });
                resourceData.Add(d);
                return true;
            }
            d = null;
            return false;
        }

        internal bool TryGetData(Type T, out ResourceData d, bool addIfMissing = false)
        {
            for (int i = 0; i < resourceData.Count; i++)
            {
                if (resourceData[i].GetType() == T)
                {
                    d = resourceData[i];
                    return true;
                }
            }
            if (addIfMissing)
            {
                d = (ResourceData)Activator.CreateInstance(T, new[] { this });
                resourceData.Add(d);
                return true;
            }
            d = null;
            return false;
        }

        internal T GetData<T>(bool addIfMissing = false) where T : ResourceData
        {
            if (!TryGetData<T>(out var d, addIfMissing)) throw new KeyNotFoundException();
            return d;
        }


        internal ResourceData GetData(Type T, bool addIfMissing = false)
        {
            if (!TryGetData(T, out var d, addIfMissing)) throw new KeyNotFoundException();
            return d;
        }

        /// <summary>
        /// Gamemode-specific data for a resource.
        /// Must have ctor(OnlineResource)
        /// </summary>
        public abstract class ResourceData
        {
            public readonly OnlineResource resource;

            public ResourceData(OnlineResource resource) // required constructor signature
            {
                this.resource = resource;
            }

            internal virtual ResourceDataState MakeState() { return null; }

            [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
            public abstract class ResourceDataState : OnlineState
            {
                public ResourceDataState() { }

                internal abstract void ReadTo(ResourceData data);
                internal abstract Type GetDataType();
            }
        }

        public abstract class ResourceState : RootDeltaState
        {
            [OnlineField(always = true)]
            public OnlineResource resource;
            [OnlineField(nullable = true, group = "entitydefs")]
            public AddRemoveSortedCustomSerializables<OnlineEntity.EntityId> entitiesJoined;
            [OnlineField(nullable = true, group = "entitydefs")]
            public DeltaStates<EntityDefinition, OnlineState, OnlineEntity.EntityId> registeredEntities;
            [OnlineField(nullable = true, group = "entities")]
            public DeltaStates<OnlineEntity.EntityState, OnlineState, OnlineEntity.EntityId> entityStates;
            [OnlineField(nullable = true, group = "data")]
            public AddRemoveSortedStates<ResourceData.ResourceDataState> resourceDataStates;

            protected ResourceState() : base() { }
            protected ResourceState(OnlineResource resource, uint ts) : base(ts)
            {
                this.resource = resource;
                entitiesJoined = new(resource.entities.Keys.ToList());
                registeredEntities = new(resource.registeredEntities.Values.Select(def => def.Clone() as EntityDefinition).ToList());
                entityStates = new(resource.entities.Select(e => e.Value.entity.GetState(ts, resource)).ToList());
                resourceDataStates = new(resource.resourceData.Select(d => d.MakeState()).Where(s => s != null).ToList());
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
                            if (def.owner != ent.entity.owner.inLobbyId)
                            {
                                ent.entity.NewOwner(OnlineManager.lobby.PlayerFromId(def.owner));
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
                resourceDataStates.list.ForEach(d => d.ReadTo(resource.GetData(d.GetDataType(), true)));
            }
        }

        public class LeaseList : IdentifiablesDeltaList<SubleaseState, ushort, SubleaseState, LeaseList>
        {
            public LeaseList() { }

            public LeaseList(List<SubleaseState> list) : base(list) { }

            public override void CustomSerialize(Serializer serializer)
            {
                serializer.SerializeByte(ref list); // up to 255 subresources
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
                        subresource.UpdateParticipants(item.participants.list.Select(OnlineManager.lobby.PlayerFromId).Where(p => p != null).ToList());
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

using RainMeadow.Generics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        protected ResourceState latestState;

        public ResourceState? state { get => isAvailable ? latestState : null; }

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

        protected Queue<ResourceState> incomingState = new(8);
        protected abstract ResourceState MakeState(uint ts);
        public void ReadState(ResourceState newState)
        {
            RainMeadow.Trace(this);
            if (!isAvailable && !isWaitingForState && !isPending)
            {
                RainMeadow.Trace($"received state for inactive resource");
                return;
            }
            if (newState.from != owner)
            {
                RainMeadow.Trace($"received state from {newState.from} but owner is {owner}");
                return;
            }
            RainMeadow.Trace($"processing received state {newState} in resource {this}");
            if (newState.isDelta)
            {
                RainMeadow.Trace($"received delta state from {newState.from} for tick {newState.tick} referencing baseline {newState.baseline}");
                while (incomingState.Count > 0 && EventMath.IsNewer(newState.baseline, incomingState.Peek().tick))
                {
                    var discarded = incomingState.Dequeue();
                    RainMeadow.Trace("discarding old state from tick " + discarded.tick);
                }
                if (incomingState.Count == 0 || newState.baseline != incomingState.Peek().tick)
                {
                    RainMeadow.Error($"Received unprocessable delta for {this} from {newState.from}, tick {newState.tick} referencing baseline {newState.baseline}");
                    RainMeadow.Error($"Available ticks are: [{string.Join(", ", incomingState.Select(s => s.tick))}]");
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
                RainMeadow.Trace($"received absolute state from {newState.from} for tick " + newState.tick);
            }
            incomingState.Enqueue(newState);
            latestState = newState;
            if (isWaitingForState || isAvailable) newState.ReadTo(this);
            if (isWaitingForState)
            {
                Available();
            }
        }

        public abstract class ResourceState : RootDeltaState
        {
            [OnlineField(always = true)]
            public OnlineResource resource;
            [OnlineField(group = "entitydefs")]
            public DynamicIdentifiablesICustomSerializables<EntityMembership, OnlineEntity.EntityId>? entitiesJoined;
            [OnlineField(group = "entitydefs")]
            public DeltaStates<OnlineEntity.EntityDefinition, OnlineEntity.EntityId>? registeredEntities;
            [OnlineField(group = "entities")]
            public DeltaStates<OnlineEntity.EntityState, OnlineEntity.EntityId>? entityStates;
            [OnlineField(group = "data")]
            public DeltaDataStates<ResourceData.ResourceDataState>? resourceDataStates;

            protected ResourceState() : base() { }
            protected ResourceState(OnlineResource resource, uint ts) : base(ts)
            {
                this.resource = resource;
                registeredEntities = new(resource.registeredEntities.Values.ToList());
                entitiesJoined = new(resource.joinedEntities.Values.ToList());
                entityStates = new(resource.activeEntities.Select(e => e.GetState(ts, resource)).ToList());
                resourceDataStates = new(resource.resourceData.Values.Select(d => d.MakeState(resource)).Where(s => s != null).ToList());
            }
            public virtual void ReadTo(OnlineResource resource)
            {
                if (resource.isActive)
                {
                    foreach (var def in registeredEntities.list)
                    {
                        if (!resource.registeredEntities.ContainsKey(def.entityId) && !def.failedToSpawn)
                        {
                            try
                            {
                                resource.OnNewRemoteEntity(def, entityStates.list.Find(es => es.entityId == def.entityId));
                            }
                            catch (Exception e)
                            {
                                RainMeadow.Error($"Failed to read new remote entity in {resource} : {def}");
                                RainMeadow.Error(e);
                            }
                        }
                    }

                    foreach (var entityJoin in entitiesJoined.list)
                    {
                        if (!resource.joinedEntities.ContainsKey(entityJoin.entityId) && resource.super.joinedEntities.ContainsKey(entityJoin.entityId))
                        {
                            // todo preventing re-adding if inhabiting sibling resource "more rencetly"
                            var ent = entityJoin.entityId.FindEntity();

                            if (ent != null)
                            {
                                var inResource = ent.currentlyJoinedResource;
                                // in super, or in other but older there
                                if (inResource == resource.super || (resource.IsSibling(inResource) && inResource.joinedEntities.TryGetValue(ent.id, out var otherJoin) && EventMath.IsNewer(entityJoin.version, otherJoin.version)))
                                {
                                    try
                                    {
                                        resource.EntityJoinedResource(ent, entityStates.list.Find(es => es.entityId == entityJoin.entityId));
                                    }
                                    catch (Exception e)
                                    {
                                        RainMeadow.Error($"Failed to join with entity in {resource} : {ent}");
                                        RainMeadow.Error(e);
                                    }
                                }
                                else
                                {
                                    RainMeadow.Trace($"Entity not added. in super? {inResource == resource.super} - in sibling? {resource.IsSibling(inResource)}");
                                }
                            }
                            else
                            {
                                RainMeadow.Error($"Entity in resource {this} missing: " + entityJoin.entityId);
                            }
                        }
                    }

                    var joinedHash = entitiesJoined.lookup;
                    foreach (var kvp in resource.joinedEntities.ToList())
                    {
                        if (!joinedHash.ContainsKey(kvp.Key))
                        {
                            try
                            {
                                resource.EntityLeftResource(kvp.Key.FindEntity());
                            }
                            catch (Exception e)
                            {
                                RainMeadow.Error($"Failed to leave with entity in {resource} : {kvp.Key.FindEntity()}");
                                RainMeadow.Error(e);
                            }
                        }
                    }

                    foreach (var def in registeredEntities.list)
                    {
                        if (def.entityId.FindEntity() is var ent && ent != null)
                        {
                            if (def.owner != ent.owner.inLobbyId && OnlineManager.lobby.PlayerFromId(def.owner) is OnlinePlayer newOwner)
                            {
                                RainMeadow.Debug("new owner for " + ent);
                                try
                                {
                                    if (newOwner.isMe) ent.ReadState(entityStates.lookup[ent.id], resource);
                                    ent.NewOwner(newOwner);
                                }
                                catch (Exception e)
                                {
                                    RainMeadow.Error($"Failed assign new owner to entity in {resource} : {ent}");
                                    RainMeadow.Error(e);
                                }
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
                            if (!entity.isMine && !entity.isTransfering)
                            {
                                if (entity.currentlyJoinedResource == resource) // this resource is the most "detailed" provider
                                {
                                    try
                                    {
                                        entity.ReadState(entityState, resource);
                                    }
                                    catch (Exception e)
                                    {
                                        RainMeadow.Error($"Failed to read state to entity in {resource} : {entity}");
                                        RainMeadow.Error(e);
                                    }
                                }
                            }
                        }
                        else
                        {
                            RainMeadow.Error("got null state, maybe it was not an EntityState");
                        }
                    }
                }
                resourceDataStates.list.ForEach(ds => ds.ReadTo(resource.TryGetData(ds.GetDataType(), out var d) ? d : resource.AddData(ds.MakeData(resource)), resource));
            }
        }

        public class LeaseList : FixedIdentifiablesDeltaList<SubleaseState, ushort, SubleaseState, LeaseList>
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
            [OnlineField]
            public LeaseList? subleaseState;

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
            public Generics.DynamicUnorderedUshorts participants;

            public ushort ID => resourceId;

            public SubleaseState() { }
            public SubleaseState(OnlineResource resource)
            {
                this.resourceId = resource.ShortId();
                this.owner = resource.owner?.inLobbyId ?? default;
                this.participants = new Generics.DynamicUnorderedUshorts(resource.participants.Select(p => p.inLobbyId).ToList());
            }
            public virtual SubleaseState EmptyDelta() => new();

            public SubleaseState ApplyDelta(SubleaseState other)
            {
                var result = EmptyDelta();
                result.resourceId = resourceId;
                result.owner = other?.owner ?? owner;
                result.participants = (Generics.DynamicUnorderedUshorts)participants.ApplyDelta(other?.participants);
                return result;
            }

            public SubleaseState Delta(SubleaseState other)
            {
                if (other == null) { return this; }
                var delta = EmptyDelta();
                delta.resourceId = resourceId;
                delta.owner = owner;
                delta.participants = (Generics.DynamicUnorderedUshorts)participants.Delta(other.participants);
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

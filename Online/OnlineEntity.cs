using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    // Welcome to polymorphism hell
    public class OnlineEntity
    {
        internal static ConditionalWeakTable<AbstractPhysicalObject, OnlineEntity> map = new();

        // todo abstract this into "entity" and "game entity"
        public AbstractPhysicalObject entity;
        public OnlinePlayer owner;
        public int id;
        public WorldCoordinate initialPos;
        internal RoomSession inRoom;

        public OnlineEntity(AbstractPhysicalObject entity, OnlinePlayer owner, int id, WorldCoordinate pos)
        {
            this.entity = entity;
            this.owner = owner;
            this.id = id;
            this.initialPos = pos;
        }

        public override string ToString()
        {
            return $"{entity}:{id} from {owner}";
        }

        internal void ReadState(EntityState entityState, ulong tick)
        {
            // todo easing??
            entityState.ReadTo(this);
        }

        internal OnlineState GetState(ulong tick, OnlineResource resource)
        {
            // todo return different data based on resource being fed (world vs room)
            if (entity is AbstractCreature)
            {
                return new CreatureEntityState(this, tick);
            }
            return new PhysicalObjectEntityState(this, tick);
        }

        // I do not like this
        // How do I abstract this away without having this be like 4 different steps
        // this sucks
        // I need to be able to abstract an "entity" from an event
        // but actually instantiating the entity could be a separate step
        // so maybe "get a potentially empty entity, then maybe realize it"
        // maybe entity.HandleAddedToResource(resource)? this smells like a responsibility swap
        // maybe 
        public static OnlineEntity CreateOrReuseEntity(NewEntityEvent newEntityEvent, World world)
        {
            OnlineEntity oe = null;
            WorldSession.registeringRemoteEntity = true;
            if (newEntityEvent.owner.recentEntities.TryGetValue(newEntityEvent.entityId, out oe))
            {
                RainMeadow.Debug("reusing existing entity " + oe);
                var creature = oe.entity as AbstractCreature;
                creature.slatedForDeletion = false;
                if (creature.realizedObject is PhysicalObject po) po.slatedForDeletetion = false;

                oe.initialPos = newEntityEvent.initialPos;
                oe.entity.pos = oe.initialPos;
            }
            else
            {
                RainMeadow.Debug("spawning new entity");
                // it is very tempting to switch to the generic tostring/fromstring from the savesystem, BUT
                // it would be almost impossible to sanitize input and who knows what someone could do through that
                if (!newEntityEvent.isCreature) throw new NotImplementedException("cant do non-creatures yet");
                CreatureTemplate.Type type = new CreatureTemplate.Type(newEntityEvent.template, false);
                if (type.Index == -1)
                {
                    RainMeadow.Debug(type);
                    RainMeadow.Debug(newEntityEvent.template);
                    throw new InvalidOperationException("invalid template");
                }
                EntityID id = world.game.GetNewID();
                id.altSeed = newEntityEvent.entityId;
                RainMeadow.Debug(id);
                RainMeadow.Debug(newEntityEvent.initialPos);
                var creature = new AbstractCreature(world, StaticWorld.GetCreatureTemplate(type), null, newEntityEvent.initialPos, id);
                RainMeadow.Debug(creature);
                if (creature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat) // for some dumb reason it doesn't get a default
                {
                    creature.state = new PlayerState(creature, 0, RainMeadow.Ext_SlugcatStatsName.OnlineSessionRemotePlayer, false);
                }
                oe = new OnlineEntity(creature, newEntityEvent.owner, newEntityEvent.entityId, newEntityEvent.initialPos);
                OnlineEntity.map.Add(creature, oe);
                newEntityEvent.owner.recentEntities.Add(newEntityEvent.entityId, oe);
            }
            WorldSession.registeringRemoteEntity = false;
            return oe;
        }

        public abstract class EntityState : OnlineState
        {
            public OnlineEntity onlineEntity;

            protected EntityState() : base () { }
            protected EntityState(OnlineEntity onlineEntity, ulong ts) : base(ts)
            {
                this.onlineEntity = onlineEntity;
            }

            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                serializer.Serialize(ref onlineEntity);
            }

            public abstract void ReadTo(OnlineEntity onlineEntity);
        }

        internal class PhysicalObjectEntityState : EntityState
        {
            public WorldCoordinate pos;
            public OnlineState realizedState;

            public PhysicalObjectEntityState() : base() { }
            public PhysicalObjectEntityState(OnlineEntity onlineEntity, ulong ts) : base(onlineEntity, ts)
            {
                if(onlineEntity != null)
                {
                    this.pos = onlineEntity.entity.pos;
                    this.realizedState = GetRealizedState();
                }
            }

            protected virtual RealizedObjectState GetRealizedState()
            {
                if (onlineEntity.entity.realizedObject == null) return null;
                return new RealizedObjectState(onlineEntity);
            }

            public override StateType stateType => StateType.PhysicalObjectEntityState;

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                onlineEntity.entity.pos = pos;
                (realizedState as RealizedObjectState)?.ReadTo(onlineEntity);
            }

            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                serializer.SerializeNoStrings(ref pos);
                serializer.SerializeNullable(ref realizedState);
            }
        }

        internal class CreatureEntityState : PhysicalObjectEntityState
        {
            // what do I even put here for AbstractCreature? inDen?
            public CreatureEntityState() : base() { }
            public CreatureEntityState(OnlineEntity onlineEntity, ulong ts) : base(onlineEntity, ts)
            {
                
            }

            protected override RealizedObjectState GetRealizedState()
            {
                if (onlineEntity.entity.realizedObject == null) return null;
                if (onlineEntity.entity.realizedObject is Player) return new RealizedPlayerState(onlineEntity);
                if (onlineEntity.entity.realizedObject is Creature) return new RealizedCreatureState(onlineEntity);
                return base.GetRealizedState();
            }

            public override StateType stateType => StateType.CreatureEntityState;
        }

        internal class RealizedObjectState : OnlineState
        {
            ChunkState[] chunkStates;
            public RealizedObjectState(OnlineEntity onlineEntity)
            {
                if(onlineEntity!=null)
                {
                    chunkStates = onlineEntity.entity.realizedObject.bodyChunks.Select(c=>new ChunkState(c)).ToArray();
                }
            }

            public override StateType stateType => StateType.RealizedObjectState;

            internal virtual void ReadTo(OnlineEntity onlineEntity)
            {
                if(onlineEntity.entity.realizedObject is PhysicalObject po)
                {
                    if (chunkStates.Length == po.bodyChunks.Length)
                    {
                        for (int i = 0; i < chunkStates.Length; i++)
                        {
                            chunkStates[i].ReadTo(po.bodyChunks[i]);
                        }
                    }
                }
            }

            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                serializer.Serialize(ref chunkStates);
            }
        }

        public class ChunkState// : OnlineState
        {
            private Vector2 pos;
            private Vector2 vel;

            public ChunkState(BodyChunk c)
            {
                if (c != null)
                {
                    pos = c.pos;
                    vel = c.vel;
                }
            }

            public void CustomSerialize(Serializer serializer)
            {
                serializer.Serialize(ref pos);
                serializer.Serialize(ref vel);
            }

            public void ReadTo(BodyChunk c)
            {
                c.pos = pos;
                c.vel = vel;
            }
        }

        internal class RealizedCreatureState : RealizedObjectState
        {
            public RealizedCreatureState(OnlineEntity onlineEntity) : base(onlineEntity)
            {
            }
            public override StateType stateType => StateType.RealizedCreatureState;
        }

        internal class RealizedPlayerState : RealizedCreatureState
        {
            public RealizedPlayerState(OnlineEntity onlineEntity) : base(onlineEntity)
            {
            }
            public override StateType stateType => StateType.RealizedPlayerState;
        }
    }
}

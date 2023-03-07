using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public partial class OnlineEntity
    {
        internal void ReadState(EntityState entityState, ulong tick)
        {
            // todo easing??
            // might need to get a ref to the sender all the way here for lag estimations?
            entityState.ReadTo(this);
        }

        internal EntityState GetState(ulong tick, OnlineResource resource)
        {
            if(resource is WorldSession ws && !OnlineManager.lobby.session.ShouldSyncObjectInWorld(ws, entity)) throw new InvalidOperationException("asked for world state, not synched");
            if (resource is RoomSession rs && !OnlineManager.lobby.session.ShouldSyncObjectInRoom(rs, entity)) throw new InvalidOperationException("asked for room state, not synched");
            var realizedState = resource is RoomSession;
            if (realizedState && !realized)
            {
                //throw new InvalidOperationException("asked for realized state, not realized");
                RainMeadow.Error("asked for realized state, not realized");
                realizedState = false;
            }
            if (entity is AbstractCreature)
            {
                return new CreatureEntityState(this, tick, realizedState);
            }
            return new PhysicalObjectEntityState(this, tick, realizedState);
        }

        public abstract class EntityState : OnlineState // Is this class completely redundant? everything inherits from PhysicalObjectEntityState
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
            public bool realized;
            public OnlineState realizedState;

            public PhysicalObjectEntityState() : base() { }
            public PhysicalObjectEntityState(OnlineEntity onlineEntity, ulong ts, bool realizedState) : base(onlineEntity, ts)
            {
                this.pos = onlineEntity.entity.pos;
                this.realized = onlineEntity.realized; // now now, oe.realized means its realized in the owners world
                                                       // not necessarily whether we're getting a real state or not
                if (realizedState) this.realizedState = GetRealizedState();
            }

            protected virtual RealizedObjectState GetRealizedState()
            {
                if (onlineEntity.entity.realizedObject == null) throw new InvalidOperationException("not realized");
                return new RealizedObjectState(onlineEntity);
            }

            public override StateType stateType => StateType.PhysicalObjectEntityState;

            public override void ReadTo(OnlineEntity onlineEntity) // idk why this has a param if it also stores a ref to it
            {
                //onlineEntity.entity.pos = pos;
                onlineEntity.entity.Move(pos);
                onlineEntity.realized = this.realized;
                (realizedState as RealizedObjectState)?.ReadTo(onlineEntity);
            }

            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                serializer.SerializeNoStrings(ref pos);
                serializer.Serialize(ref realized);
                serializer.SerializeNullable(ref realizedState);
            }
        }

        internal class CreatureEntityState : PhysicalObjectEntityState
        {
            // what do I even put here for AbstractCreature? inDen?
            public CreatureEntityState() : base() { }
            public CreatureEntityState(OnlineEntity onlineEntity, ulong ts, bool realizedState) : base(onlineEntity, ts, realizedState)
            {

            }

            protected override RealizedObjectState GetRealizedState()
            {
                if (onlineEntity.entity.realizedObject == null) throw new InvalidOperationException("not realized");
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
                if (onlineEntity != null)
                {
                    chunkStates = onlineEntity.entity.realizedObject.bodyChunks.Select(c => new ChunkState(c)).ToArray();
                }
            }

            public override StateType stateType => StateType.RealizedObjectState;

            internal virtual void ReadTo(OnlineEntity onlineEntity)
            {
                if (onlineEntity.entity.realizedObject is PhysicalObject po)
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

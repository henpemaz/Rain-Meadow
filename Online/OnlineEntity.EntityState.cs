using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public partial class OnlineEntity
    {
        public EntityState latestState;

        public void ReadState(EntityState entityState, ulong tick)
        {
            // todo easing??
            // might need to get a ref to the sender all the way here for lag estimations?
            // todo delta handling
            if (lowestResource is RoomSession && !entityState.realizedState) return; // We can skip abstract state if we're receiving state in a room as well
            entityState.ReadTo(this);
            latestState = entityState;
        }

        public EntityState GetState(ulong tick, OnlineResource resource)
        {
            if (resource is WorldSession ws && !OnlineManager.lobby.session.ShouldSyncObjectInWorld(ws, entity)) throw new InvalidOperationException("asked for world state, not synched");
            if (resource is RoomSession rs && !OnlineManager.lobby.session.ShouldSyncObjectInRoom(rs, entity)) throw new InvalidOperationException("asked for room state, not synched");
            var realizedState = resource is RoomSession;
            if(realizedState) { if(entity.realizedObject != null && !realized) RainMeadow.Error("have realized object, but not entity not marked as realized??"); }
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
            public bool realizedState;

            protected EntityState() : base () { }
            protected EntityState(OnlineEntity onlineEntity, ulong ts, bool realizedState) : base(ts)
            {
                this.onlineEntity = onlineEntity;
                this.realizedState = realizedState;
            }

            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                serializer.Serialize(ref onlineEntity);
                serializer.Serialize(ref realizedState);
            }

            public abstract void ReadTo(OnlineEntity onlineEntity);
        }


        public class PhysicalObjectEntityState : EntityState
        {
            public WorldCoordinate pos;
            public bool realized;
            public OnlineState realizedObjectState;

            public PhysicalObjectEntityState() : base() { }
            public PhysicalObjectEntityState(OnlineEntity onlineEntity, ulong ts, bool realizedState) : base(onlineEntity, ts, realizedState)
            {
                this.pos = onlineEntity.entity.pos;
                this.realized = onlineEntity.realized; // now now, oe.realized means its realized in the owners world
                                                       // not necessarily whether we're getting a real state or not
                if (realizedState) this.realizedObjectState = GetRealizedState();
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
                (realizedObjectState as RealizedObjectState)?.ReadTo(onlineEntity);
            }

            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                serializer.SerializeNoStrings(ref pos);
                serializer.Serialize(ref realized);
                serializer.SerializeNullable(ref realizedObjectState);
            }
        }

        public class CreatureEntityState : PhysicalObjectEntityState
        {
            // what do I even put here for AbstractCreature? inDen?
            public CreatureEntityState() : base() { }
            public CreatureEntityState(OnlineEntity onlineEntity, ulong ts, bool realizedState) : base(onlineEntity, ts, realizedState)
            {

            }

            protected override RealizedObjectState GetRealizedState()
            {
                if (onlineEntity.entity.realizedObject is Player) return new RealizedPlayerState(onlineEntity);
                if (onlineEntity.entity.realizedObject is Creature) return new RealizedCreatureState(onlineEntity);
                return base.GetRealizedState();
            }

            public override StateType stateType => StateType.CreatureEntityState;
        }

        public class RealizedObjectState : OnlineState
        {
            ChunkState[] chunkStates;

            public RealizedObjectState() { }
            public RealizedObjectState(OnlineEntity onlineEntity)
            {
                if (onlineEntity != null)
                {
                    chunkStates = onlineEntity.entity.realizedObject.bodyChunks.Select(c => new ChunkState(c)).ToArray();
                }
            }

            public override StateType stateType => StateType.RealizedObjectState;

            public virtual void ReadTo(OnlineEntity onlineEntity)
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

        public class ChunkState// : OnlineState // no need for serializing its type, its just always the same data
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

        public class RealizedCreatureState : RealizedObjectState
        {
            public RealizedCreatureState() { }
            public RealizedCreatureState(OnlineEntity onlineEntity) : base(onlineEntity)
            {

            }
            public override StateType stateType => StateType.RealizedCreatureState;
        }

        public class RealizedPlayerState : RealizedCreatureState
        {
            private byte animationIndex;
            private short animationFrame;
            private byte bodyModeIndex;
            private ushort inputs;
            private Vector2 analogInput;
            public RealizedPlayerState() { }
            public RealizedPlayerState(OnlineEntity onlineEntity) : base(onlineEntity)
            {
                Player p = onlineEntity.entity.realizedObject as Player;
                animationIndex = (byte)p.animation.Index;
                animationFrame = (short)p.animationFrame;
                bodyModeIndex = (byte)p.bodyMode.Index;

                var i = p.input[0];
                inputs = (ushort)(
                      (i.x == 1 ? 1 << 0 : 0)
                    | (i.x == -1 ? 1 << 1 : 0)
                    | (i.y == 1 ? 1 << 2 : 0)
                    | (i.y == -1 ? 1 << 3 : 0)
                    | (i.downDiagonal == 1 ? 1 << 4 : 0)
                    | (i.downDiagonal == -1 ? 1 << 5 : 0)
                    | (i.pckp ? 1 << 6 : 0)
                    | (i.jmp ? 1 << 7 : 0)
                    | (i.thrw ? 1 << 8 : 0)
                    | (i.mp ? 1 << 9 : 0));

                analogInput = i.analogueDir;
            }
            public Player.InputPackage GetInput()
            {
                Player.InputPackage i = default;
                if (((inputs >> 0) & 1) != 0) i.x = 1;
                if (((inputs >> 1) & 1) != 0) i.x = -1;
                if (((inputs >> 2) & 1) != 0) i.y = 1;
                if (((inputs >> 3) & 1) != 0) i.y = -1;
                if (((inputs >> 4) & 1) != 0) i.downDiagonal = 1;
                if (((inputs >> 5) & 1) != 0) i.downDiagonal = -1;
                if (((inputs >> 6) & 1) != 0) i.pckp = true;
                if (((inputs >> 7) & 1) != 0) i.jmp = true;
                if (((inputs >> 8) & 1) != 0) i.thrw = true;
                if (((inputs >> 9) & 1) != 0) i.mp = true;
                i.analogueDir = analogInput;
                return i;
            }

            public override StateType stateType => StateType.RealizedPlayerState;

            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                serializer.Serialize(ref animationIndex);
                serializer.Serialize(ref animationFrame);
                serializer.Serialize(ref bodyModeIndex);
                serializer.Serialize(ref inputs);
                serializer.Serialize(ref analogInput);
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                if(onlineEntity.entity.realizedObject is Player pl)
                {
                    pl.animation = new Player.AnimationIndex(Player.AnimationIndex.values.GetEntry(animationIndex));
                    pl.animationFrame = animationFrame;
                    pl.bodyMode = new Player.BodyModeIndex(Player.BodyModeIndex.values.GetEntry(bodyModeIndex));
                }
            }
        }
    }
}

using RainMeadow.Generics;
using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RainMeadow
{
    // main-ish component of PhysicalObjectEntityState
    public class RealizedPhysicalObjectState : OnlineState, IDelta<RealizedPhysicalObjectState>
    {
        private ChunkState[] chunkStates;
        private byte collisionLayer;

        bool hasPhysicsValue;

        public virtual RealizedPhysicalObjectState EmptyDelta() => new();
        public RealizedPhysicalObjectState() { }
        public RealizedPhysicalObjectState(OnlinePhysicalObject onlineEntity)
        {
            chunkStates = onlineEntity.apo.realizedObject.bodyChunks.Select(c => new ChunkState(c)).ToArray();
            collisionLayer = (byte)onlineEntity.apo.realizedObject.collisionLayer;
        }

        public override StateType stateType => StateType.RealizedPhysicalObjectState;

        public virtual void ReadTo(OnlineEntity onlineEntity)
        {
            if (onlineEntity.owner.isMe || onlineEntity.isPending) return; // Don't sync if pending, reduces visibility and effect of lag
            var po = (onlineEntity as OnlinePhysicalObject).apo.realizedObject;
            if (chunkStates.Length == po.bodyChunks.Length)
            {
                float diffAverage = 0;
                for (int i = 0; i < chunkStates.Length; i++)
                {
                    var couldReasonablyReach = chunkStates[i].vel.magnitude;
                    diffAverage += Math.Max(0, (chunkStates[i].pos - po.bodyChunks[i].pos).magnitude - couldReasonablyReach);
                }
                diffAverage /= chunkStates.Length; //a rating of how different the two states are, more forgiving the
                if (diffAverage > 3)               //higher the object's velocity
                {
                    for (int i = 0; i < chunkStates.Length; i++) //sync bodychunk positions
                    {
                        chunkStates[i].ReadTo(po.bodyChunks[i]);
                    }
                }
            }
            po.collisionLayer = collisionLayer;
        }

        public bool IsDelta { get => _isDelta; set => _isDelta = value; }
        protected bool _isDelta;
        public bool IsEmptyDelta { get; set; }

        public override void CustomSerialize(Serializer serializer)
        {
            if (serializer.IsDelta) // In a non-delta context, can only be non-delta
            {
                serializer.Serialize(ref _isDelta);
                serializer.IsDelta = IsDelta;
            }
            if (IsDelta) serializer.Serialize(ref hasPhysicsValue);
            if (!IsDelta || hasPhysicsValue)
            {
                serializer.Serialize(ref chunkStates);
                serializer.Serialize(ref collisionLayer);
            }
        }

        public override long EstimatedSize(bool inDeltaContext)
        {
            long val = 1;
            if (inDeltaContext) val += 1; // In a non-delta context, can only be non-delta
            if (!IsDelta || hasPhysicsValue)
            {
                val += 2 + chunkStates.Length * 16;
            }
            return val;
        }

        public virtual RealizedPhysicalObjectState Delta(RealizedPhysicalObjectState _other)
        {
            if (_other == null) throw new InvalidProgrammerException("null");
            if (_other.IsDelta) throw new InvalidProgrammerException("other is delta");
            var delta = EmptyDelta();
            delta.IsDelta = true;
            delta.chunkStates = chunkStates;
            delta.collisionLayer = collisionLayer;
            delta.hasPhysicsValue = !chunkStates.SequenceEqual(_other.chunkStates) || collisionLayer != _other.collisionLayer;
            delta.IsEmptyDelta = !delta.hasPhysicsValue;
            return delta;
        }

        public virtual RealizedPhysicalObjectState ApplyDelta(RealizedPhysicalObjectState _other)
        {
            if (_other == null) throw new InvalidProgrammerException("null");
            if (!_other.IsDelta) throw new InvalidProgrammerException("other not delta");
            var result = EmptyDelta();
            result.chunkStates = _other.hasPhysicsValue ? _other.chunkStates : chunkStates;
            result.collisionLayer = _other.hasPhysicsValue ? _other.collisionLayer : collisionLayer;
            return result;
        }

        public override string DebugPrint(int ident)
        {
            var sb = new StringBuilder(new string(' ', ident) + GetType().Name + " " + (IsDelta ? "(delta)" : "(full)") + "\n");
            if (!IsDelta || hasPhysicsValue)
            {
                sb.Append(new string(' ', ident + 1) + "PhysicsValue\n");
            }
            return sb.ToString();
        }
    }

    // Todo: a lot can be optmized here. A custom list of these with member-wise delta/omit (see generics)
    // and then in each entry have a "delta mode" where it encodes a HALF relative to last pos
    public class ChunkState : Serializer.ICustomSerializable, IEquatable<ChunkState> 
    {
        public Vector2 pos;
        public Vector2 vel;

        public ChunkState() { }
        public ChunkState(BodyChunk c)
        {
            pos = c.pos;
            vel = c.vel;
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

        public override bool Equals(object obj)
        {
            return obj is ChunkState other && Equals(other);
        }

        public bool Equals(ChunkState other)
        {
            //return other != null && pos == other.pos && vel == other.vel;
            return other != null && pos.CloseEnough(other.pos, 1f) && vel.CloseEnoughZeroSnap(other.vel, 1f);
        }

        public override int GetHashCode()
        {
            return pos.GetHashCode() + vel.GetHashCode();
        }
    }
}

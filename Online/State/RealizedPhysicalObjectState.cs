using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    // main-ish component of PhysicalObjectEntityState
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class RealizedPhysicalObjectState : OnlineState
    {
        [OnlineField] // todo field that does sequenceequals or type that handles these better
        private ChunkState[] chunkStates; // pretty sure these get sent every time because the array comparison doesn't do sequenceequal but does referenceequal instead
        [OnlineField]
        private byte collisionLayer;

        public RealizedPhysicalObjectState() { }
        public RealizedPhysicalObjectState(OnlinePhysicalObject onlineEntity)
        {
            chunkStates = onlineEntity.apo.realizedObject.bodyChunks.Select(c => new ChunkState(c)).ToArray();
            collisionLayer = (byte)onlineEntity.apo.realizedObject.collisionLayer;
        }

        public virtual void ReadTo(OnlineEntity onlineEntity)
        {
            if (onlineEntity.isPending) { RainMeadow.Trace($"not syncing {onlineEntity} because pending"); return; };
            var po = (onlineEntity as OnlinePhysicalObject).apo.realizedObject;
            for (int i = 0; i < chunkStates.Length; i++) //sync bodychunk positions
            {
                chunkStates[i].ReadTo(po.bodyChunks[i]);
            }
            po.collisionLayer = collisionLayer;
        }
    }

    // Todo: a lot can be optmized here. A custom list of these with member-wise delta/omit (see serializer/generics)
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
            return other as object != null && pos.CloseEnough(other.pos, 1f) && vel.CloseEnoughZeroSnap(other.vel, 1f);
        }

        public static bool operator ==(ChunkState lhs, ChunkState rhs)
        {
            return lhs as object != null && lhs.Equals(rhs);
        }

        public static bool operator !=(ChunkState lhs, ChunkState rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return pos.GetHashCode() + vel.GetHashCode();
        }
    }
}

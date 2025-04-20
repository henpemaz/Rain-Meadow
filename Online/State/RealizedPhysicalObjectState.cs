using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    // main-ish component of AbstractPhysicalObjectState
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class RealizedPhysicalObjectState : OnlineState, Serializer.ICustomSerializable
    {
        private ChunkState[] chunkStates;
        private DeltaChunkState[] offsets;
        private byte collisionLayer;
        // error acceptable between deltas
        private const float AcceptableError = 0.25F;

        public RealizedPhysicalObjectState() { }
        public RealizedPhysicalObjectState(OnlinePhysicalObject onlineEntity)
        {
            var opo = onlineEntity as OnlinePhysicalObject;
            var po = opo.apo.realizedObject;
            if (!opo.lenientPos)
            {
                chunkStates = po.bodyChunks.Select(c => new ChunkState(c)).ToArray();
            }
            collisionLayer = (byte)po.collisionLayer;
        }

        private static bool CanDelta(ChunkState chunk)
        {
            float dx = Mathf.Abs(Mathf.HalfToFloat(Mathf.FloatToHalf(chunk.pos.x)) - Mathf.HalfToFloat(Mathf.FloatToHalf(chunk.lastPos.x)));
            float dy = Mathf.Abs(Mathf.HalfToFloat(Mathf.FloatToHalf(chunk.pos.y)) - Mathf.HalfToFloat(Mathf.FloatToHalf(chunk.lastPos.y)));
            return dx < AcceptableError && dy < AcceptableError;
        }
        private static bool CanDelta(ChunkState[] array) => array.All(e => CanDelta(e));

        private static DeltaChunkState[] GenerateOffsets(ChunkState[] array)
        {
            return array.Select(e => new DeltaChunkState(e.pos + e.vel)).ToArray();
        }

        public override void CustomSerialize(Serializer serializer)
        {
            bool isDelta = false;
            if (serializer.IsWriting)
            {
                if (CanDelta(chunkStates))
                {
                    isDelta = true;
                    offsets = GenerateOffsets(chunkStates);
                }
            }

            serializer.Serialize(ref collisionLayer);
            serializer.Serialize(ref isDelta);
            if (isDelta)
            {
                if (serializer.IsWriting)
                {
                    serializer.writer.Write((byte)offsets.Length);
                }
                if (serializer.IsReading)
                {
                    var count = serializer.reader.ReadByte();
                    offsets = new DeltaChunkState[count];
                }
                for (int i = 0; i < offsets.Length; i++)
                {
                    serializer.Serialize(ref offsets[i]);
                }
            }
            else
            {
                if (serializer.IsWriting)
                {
                    serializer.writer.Write((byte)chunkStates.Length);
                }
                if (serializer.IsReading)
                {
                    var count = serializer.reader.ReadByte();
                    chunkStates = new ChunkState[count];
                }
                for (int i = 0; i < chunkStates.Length; i++)
                {
                    serializer.Serialize(ref chunkStates[i]);
                }
            }
        }

        public virtual bool ShouldPosBeLenient(PhysicalObject po) {
            if (po.room?.world?.name == "SS" && (po is Oracle || po is PebblesPearl || po is Rock || po is OracleSwarmer || po is SSOracleSwarmer))
                return true; // 5 pebbles leniency due to 0G
            return po.grabbedBy.Any((x) => {
                if (x.grabber == null)
                    return false;
                var oe = x.grabber.abstractCreature.GetOnlineCreature();
                return oe != null && oe.lenientPos;
            });
        }

        public virtual void ReadTo(OnlineEntity onlineEntity)
        {
            if (onlineEntity.isPending) { RainMeadow.Trace($"not syncing {onlineEntity} because pending"); return; };
            if (onlineEntity is OnlinePhysicalObject opo)
            {
                var po = opo.apo.realizedObject;
                opo.lenientPos = ShouldPosBeLenient(po);
                if (!opo.lenientPos)
                {
                    for (int i = 0; i < chunkStates.Length; i++) //sync bodychunk positions
                    {
                        chunkStates[i].ReadTo(po.bodyChunks[i]);
                    }
                }
                if (po.collisionLayer != collisionLayer)
                {
                    po.ChangeCollisionLayer(collisionLayer);
                }
            }
        }
    }

    public class DeltaChunkState : Serializer.ICustomSerializable, IEquatable<DeltaChunkState>
    {
        public Vector2 change;

        public DeltaChunkState() { }
        public DeltaChunkState(Vector2 v)
        {
            change = v;
        }

        public void CustomSerialize(Serializer serializer)
        {
            serializer.SerializeHalf(ref change);
        }

        public void ReadTo(ref Vector2 v)
        {
            v = change;
        }

        public override bool Equals(object obj) => obj is DeltaChunkState other && Equals(other);
        public bool Equals(DeltaChunkState other) => other as object != null && change.CloseEnough(other.change, 1 / 4f);
        public static bool operator ==(DeltaChunkState lhs, DeltaChunkState rhs) => lhs as object != null && lhs.Equals(rhs);
        public static bool operator !=(DeltaChunkState lhs, DeltaChunkState rhs) => !(lhs == rhs);
        public override int GetHashCode() => change.GetHashCode() + change.GetHashCode();
    }

    // Todo: a lot can be optmized here. A custom list of these with member-wise delta/omit (see serializer/generics)
    // and then in each entry have a "delta mode" where it encodes a HALF relative to last pos
    public class ChunkState : Serializer.ICustomSerializable, IEquatable<ChunkState>
    {
        public Vector2 pos;
        public Vector2 vel;
        public Vector2 lastPos;

        public ChunkState() { }
        public ChunkState(BodyChunk c)
        {
            pos = c.pos;
            vel = c.vel;
            lastPos = c.pos;
        }

        public void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref pos);
            serializer.SerializeHalf(ref vel);
        }

        public void ReadTo(BodyChunk c)
        {
            lastPos = c.pos;
            c.pos = pos;
            c.vel = vel;
        }

        public override bool Equals(object obj) => obj is ChunkState other && Equals(other);
        public bool Equals(ChunkState other)
        {
            //return other != null && pos == other.pos && vel == other.vel;
            return other as object != null && pos.CloseEnough(other.pos, 1 / 4f) && vel.CloseEnoughZeroSnap(other.vel, 1 / 256f);
        }
        public static bool operator ==(ChunkState lhs, ChunkState rhs) => lhs as object != null && lhs.Equals(rhs);
        public static bool operator !=(ChunkState lhs, ChunkState rhs) => !(lhs == rhs);
        public override int GetHashCode() => pos.GetHashCode() + vel.GetHashCode();
    }
}

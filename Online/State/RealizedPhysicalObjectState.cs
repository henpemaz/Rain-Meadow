using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    // main-ish component of AbstractPhysicalObjectState
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class RealizedPhysicalObjectState : OnlineState
    {
        [OnlineField]
        private byte collisionLayer;
        [OnlineField]
        private ChunkStates chunkStates;

        public RealizedPhysicalObjectState() { }
        public RealizedPhysicalObjectState(OnlinePhysicalObject onlineEntity)
        {
            var opo = onlineEntity as OnlinePhysicalObject;
            var po = opo.apo.realizedObject;
            chunkStates = ShouldPosBeLenient(onlineEntity.apo.realizedObject) || !ShouldSyncChunks(onlineEntity.apo.realizedObject) ? new ChunkStates() : new ChunkStates(onlineEntity);
            collisionLayer = (byte)onlineEntity.apo.realizedObject.collisionLayer;
        }
        
        virtual public bool ShouldSyncChunks(PhysicalObject po) {
            return true;
        }
        
        virtual public bool ShouldPosBeLenient(PhysicalObject po) {
            if (po.room?.world?.name == "SS" && (po is Oracle || po is PebblesPearl || po is Rock || po is OracleSwarmer || po is SSOracleSwarmer))
                return true; // 5 pebbles leniency due to 0G
            if (po.grabbedBy.Any((x) => {
                if (x.grabber == null) return false;
                var onlinegrabber = x.grabber.abstractCreature.GetOnlineCreature();
                if (onlinegrabber == null) return false;
                return onlinegrabber.lenientPos;
            })) return true;
            return false;
        }

        public virtual void ReadTo(OnlineEntity onlineEntity)
        {
            if (onlineEntity.isPending) { RainMeadow.Trace($"not syncing {onlineEntity} because pending"); return; };
            if (onlineEntity is OnlinePhysicalObject opo)
            {
                var po = opo.apo.realizedObject;
                opo.lenientPos = ShouldPosBeLenient(po);
                chunkStates?.ReadTo(onlineEntity);
                if (po.collisionLayer != collisionLayer)
                {
                    po.ChangeCollisionLayer(collisionLayer);
                }
            }
        }
    }

    public class ChunkStates : Generics.IPrimaryDelta<ChunkStates>, Serializer.ICustomSerializable
    {
        private const float AcceptableError = 0.25F; // error acceptable between deltas
        private float AcceptableAccumError => 2.0F * AcceptableError * chunks.Length; // error acceptable on series of deltas
        private ChunkState[] chunks = [];
        private DeltaChunkState[] offsets = [];
        private float accumError = 0.0F;
        public bool IsEmptyDelta => offsets.Length > 0;

        public ChunkStates() { }
        public ChunkStates(OnlinePhysicalObject onlineEntity)
        {
            var opo = onlineEntity as OnlinePhysicalObject;
            var po = opo.apo.realizedObject;
            if (!opo.lenientPos) chunks = po.bodyChunks.Select(c => new ChunkState(c)).ToArray();
        }

        private static bool CanDelta(float a, float b) => Mathf.Abs(Mathf.HalfToFloat(Mathf.FloatToHalf(a)) - Mathf.HalfToFloat(Mathf.FloatToHalf(b))) < AcceptableError;
        private static bool CanDelta(ChunkState chunk) => CanDelta(chunk.pos.x, chunk.lastPos.x) && CanDelta(chunk.pos.y, chunk.lastPos.y);
        private static DeltaChunkState[] GenerateOffsets(ChunkState[] arr) => arr.Select(e => new DeltaChunkState(e.pos - e.lastPos)).ToArray();

        /// <summary>
        /// Calculates the error from converting to a half float
        /// </summary>
        private static float GetHalfError(float v) => Mathf.Abs(Mathf.HalfToFloat(Mathf.FloatToHalf(v)) - v);

        public ChunkStates Delta(ChunkStates other)
        {
            ChunkStates result = new()
            {
                chunks = this.chunks,
                offsets = GenerateOffsets(other != null ? other.chunks : this.chunks),
                accumError = other != null && other.accumError > 0.0F ? other.accumError : this.accumError
            };
            if (!chunks.All(CanDelta) || result.accumError >= AcceptableAccumError)
                result.offsets = []; //do not delta, please fix pos NOW
            return result;
        }

        public ChunkStates ApplyDelta(ChunkStates other)
        {
            ChunkStates result = new()
            {
                chunks = this.chunks,
                offsets = other != null ? other.offsets : [],
                accumError = other != null && other.accumError > 0.0F ? other.accumError : this.accumError
            };
            if (result.offsets.Length > 0)
            {
                if (result.chunks.Length != result.offsets.Length)
                { //force state resend
                    result.offsets = [];
                }
                else
                { //sync bodychunk positions
                    for (int i = 0; i < result.offsets.Length; i++)
                    {
                        result.accumError += GetHalfError(Mathf.Abs(result.chunks[i].lastPos.x - result.chunks[i].pos.x));
                        result.accumError += GetHalfError(Mathf.Abs(result.chunks[i].lastPos.y - result.chunks[i].pos.y));
                        result.chunks[i].lastPos = result.chunks[i].pos;
                        result.chunks[i].pos += result.offsets[i].change;
                    }
                }
            }
            return result;
        }

        public void CustomSerialize(Serializer serializer)
        {
            if (serializer.IsDelta)
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
                    serializer.writer.Write((byte)chunks.Length);
                }
                if (serializer.IsReading)
                {
                    var count = serializer.reader.ReadByte();
                    chunks = new ChunkState[count];
                }
                for (int i = 0; i < chunks.Length; i++)
                {
                    serializer.Serialize(ref chunks[i]);
                }
            }
        }

        public virtual void ReadTo(OnlineEntity oe)
        {
            if (!oe.isPending && oe is OnlinePhysicalObject opo)
            {
                var po = opo.apo.realizedObject;
                if (!opo.lenientPos)
                {
                    for (int i = 0; i < chunks.Length; i++)
                    {
                        chunks[i].ReadTo(po.bodyChunks[i]);
                    }
                }
            }
        }
    }

    public class DeltaChunkState : Serializer.ICustomSerializable, IEquatable<DeltaChunkState>
    {
        public Vector2 change;
        public DeltaChunkState() { }
        public DeltaChunkState(Vector2 v) { change = v; }
        public void CustomSerialize(Serializer serializer) => serializer.SerializeHalf(ref change);
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

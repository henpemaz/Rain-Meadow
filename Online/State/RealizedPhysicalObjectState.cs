using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    // main-ish component of AbstractPhysicalObjectState
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class RealizedPhysicalObjectState : OnlineState
    {
        [OnlineField(group = "chunkstate")]
        private ChunkState[] chunkStates;
        [OnlineField(group = "chunkstate")]
        private byte collisionLayer;

        public RealizedPhysicalObjectState() { }
        public RealizedPhysicalObjectState(OnlinePhysicalObject onlineEntity)
        {
            // LenientPos indictates that a physical object's position may cause graphical or errors due to latency.
            // Sometimes, this isn't enough. 

            // Here ShouldSyncChunks being false indicates that the creature's chunk state would not be valid, even if it arrived on time
            if (ShouldSyncChunks(onlineEntity.apo.realizedObject)) {
                chunkStates = onlineEntity.apo.realizedObject.bodyChunks.Select(c => new ChunkState(c)).ToArray();
            } else {
                chunkStates = Array.Empty<ChunkState>();
            }
            
            collisionLayer = (byte)onlineEntity.apo.realizedObject.collisionLayer;
        }
        virtual public bool ShouldSyncChunks(PhysicalObject po) {
            return true;
        }
        
        virtual public bool ShouldPosBeLenient(PhysicalObject po) {
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
            var opo = onlineEntity as OnlinePhysicalObject;
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

            // NET 4.8 doesn't have the functions :(
            // https://stackoverflow.com/questions/27237776/convert-int-bits-to-float-bits
            unsafe uint SingleToInt32Bits(float value)
            {
                return *(uint*)(&value);
            }
            unsafe float Int32BitsToSingle(uint value)
            {
                return *(float*)(&value);
            }
            void EncodeFloat(ref float f)
            {
                const float prec = 0.01f;
                // Must send full float data
                if (Mathf.Abs(Mathf.HalfToFloat(Mathf.FloatToHalf(f)) - f) >= prec)
                {
                    // Shifts away the LSB, sign bit preserved as second-MSB
                    uint bit = SingleToInt32Bits(f);
                    bit >>= 1;
                    // Force Big-endian
                    byte[] data = [
                        (byte)((bit >> 24) | 0x80),
                        (byte)(bit >> 16),
                        (byte)(bit >> 8),
                        (byte)bit
                    ];
                    serializer.Serialize(ref data[0]);
                    serializer.Serialize(ref data[1]);
                    serializer.Serialize(ref data[2]);
                    serializer.Serialize(ref data[3]);
                }
                // Below precision threshold, send as u16
                else
                {
                    ushort bit = Mathf.FloatToHalf(f);
                    bit >>= 1;
                    // Force Big-endian
                    byte[] data = [
                        (byte)(bit >> 8),
                        (byte)bit
                    ];
                    serializer.Serialize(ref data[0]);
                    serializer.Serialize(ref data[1]);
                }
            }
            void DecodeFloat(ref float f)
            {
                byte b0 = 0;
                serializer.Serialize(ref b0);
                // 32-bits
                if ((b0 & 0x80) == 1)
                {
                    byte b1 = 0, b2 = 0, b3 = 0;
                    serializer.Serialize(ref b1);
                    serializer.Serialize(ref b2);
                    serializer.Serialize(ref b3);
                    uint bit = (uint)(((int)b0 << 24) | ((int)b1 << 16) | ((int)b2 << 8) | ((int)b3 << 0));
                    bit <<= 1; //Shift back (LSB lost)
                    f = Int32BitsToSingle(bit);
                }
                // 16-bits
                else
                {
                    byte b1 = 0;
                    serializer.Serialize(ref b1);
                    ushort bit = (ushort)(((int)b0 << 8) | ((int)b1 << 0));
                    bit <<= 1; //Shift back (LSB lost)
                    f = Mathf.HalfToFloat(bit);
                }
            }

            if (serializer.IsWriting)
            {
                EncodeFloat(ref pos.x);
                EncodeFloat(ref pos.y);
            }
            else
            {
                DecodeFloat(ref pos.x);
                DecodeFloat(ref pos.y);
            }
            //serializer.SerializeHalf(ref vel);
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
            return other as object != null && pos.CloseEnough(other.pos, 1 / 4f) && vel.CloseEnoughZeroSnap(other.vel, 1 / 256f);
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

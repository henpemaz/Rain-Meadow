﻿using System;
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
        
        public virtual bool ShouldPosBeLenient(PhysicalObject po) {
            if (po is Oracle _)
                return true;
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
            serializer.SerializeHalf(ref vel);
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

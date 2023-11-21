﻿using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    // main-ish component of PhysicalObjectEntityState
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class RealizedPhysicalObjectState : OnlineState
    {
        [OnlineField]
        private ChunkState[] chunkStates;
        [OnlineField]
        private byte collisionLayer;

        public RealizedPhysicalObjectState() { }
        public RealizedPhysicalObjectState(OnlinePhysicalObject onlineEntity)
        {
            chunkStates = onlineEntity.apo.realizedObject.bodyChunks.Select(c => new ChunkState(c)).ToArray();
            collisionLayer = (byte)onlineEntity.apo.realizedObject.collisionLayer;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            if (chunkStates == null)
                RainMeadow.Debug($"Null reading ?{serializer.IsReading} isDelta?{isDelta} ");
        }

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
            return other != null && pos.CloseEnough(other.pos, 1f) && vel.CloseEnoughZeroSnap(other.vel, 1f);
        }

        public override int GetHashCode()
        {
            return pos.GetHashCode() + vel.GetHashCode();
        }
    }
}

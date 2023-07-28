﻿using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class RealizedPhysicalObjectState : OnlineState
    {
        ChunkState[] chunkStates;
        private int collisionLayer;

        public RealizedPhysicalObjectState() { }
        public RealizedPhysicalObjectState(OnlinePhysicalObject onlineEntity)
        {
            chunkStates = onlineEntity.apo.realizedObject.bodyChunks.Select(c => new ChunkState(c)).ToArray();
            collisionLayer = onlineEntity.apo.realizedObject.collisionLayer;
        }

        public override StateType stateType => StateType.RealizedPhysicalObjectState;

        public virtual void ReadTo(OnlineEntity onlineEntity)
        {
            if (onlineEntity.owner.isMe || onlineEntity.isPending) return; // Don't sync if pending, reduces visibility and effect of lag
            var po = (onlineEntity as OnlinePhysicalObject).apo.realizedObject;
            if (chunkStates.Length == po.bodyChunks.Length)
            {
                RainMeadow.Debug(onlineEntity.owner.lastAckdTick - PlayersManager.mePlayer.tick); //??????
                float diffAverage = 0;
                for (int i = 0; i < chunkStates.Length; i++)
                {
                    float physicsLoopDeltaTime = Math.Max(26, UnityEngine.Time.deltaTime);
                    var couldReasonablyReach = po.bodyChunks[i].vel.magnitude * (UdpPeer.simulatedLatency.Milliseconds / physicsLoopDeltaTime);
                    diffAverage += Math.Max(0, (chunkStates[i].pos - po.bodyChunks[i].pos).magnitude - couldReasonablyReach);
                }
                diffAverage /= chunkStates.Length; //a rating of how different the two states are, more
                if (diffAverage > 4)               //forgiving the higher the object's velocity and the
                {                                  //update's latency
                    for (int i = 0; i < chunkStates.Length; i++) //sync bodychunk positions
                    {
                        chunkStates[i].ReadTo(po.bodyChunks[i]);
                    }

                    foreach (BodyPart bodyPart in po.graphicsModule.bodyParts) //prevent tail from stretching accross the screen
                    {
                        var tailSegment = bodyPart as TailSegment;
                        if (tailSegment != null) tailSegment.affectPrevious = 0f;
                    }
                }
            }
            po.collisionLayer = collisionLayer;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref chunkStates);
            serializer.Serialize(ref collisionLayer);
        }
    }

    public class ChunkState : Serializer.ICustomSerializable // : OnlineState // no need for serializing its type, its just always the same data
    {
        public Vector2 pos;
        public Vector2 vel;
        private Vector2 lastpos;
        private Vector2 lastlastpos;

        public ChunkState() { }
        public ChunkState(BodyChunk c)
        {
            pos = c.pos;
            vel = c.vel;
            lastpos = c.lastPos;
            lastlastpos = c.lastLastPos;
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
            c.lastPos = lastpos;
            c.lastLastPos = lastlastpos;
        }
    }
}

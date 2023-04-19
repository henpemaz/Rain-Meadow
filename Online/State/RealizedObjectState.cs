using System.Linq;
using UnityEngine;

namespace RainMeadow
{
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

    public class ChunkState // : OnlineState // no need for serializing its type, its just always the same data
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
}

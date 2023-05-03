using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class PhysicalObjectState : OnlineState
    {
        ChunkState[] chunkStates;
        private int collisionLayer;

        public PhysicalObjectState() { }
        public PhysicalObjectState(OnlineEntity onlineEntity)
        {
            chunkStates = onlineEntity.entity.realizedObject.bodyChunks.Select(c => new ChunkState(c)).ToArray();
            collisionLayer = onlineEntity.entity.realizedObject.collisionLayer;
        }

        public override StateType stateType => StateType.PhysicalObjectState;

        public virtual void ReadTo(OnlineEntity onlineEntity)
        {
            if (!onlineEntity.owner.isMe && onlineEntity.isPending) return; // Don't sync pos if pending, reduces visibility and effect of lag
            
            var po = onlineEntity.entity.realizedObject;
            
            if (chunkStates.Length == po.bodyChunks.Length)
            {
                for (int i = 0; i < chunkStates.Length; i++)
                {
                    chunkStates[i].ReadTo(po.bodyChunks[i]);
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
        private Vector2 pos;
        private Vector2 vel;

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
    }
}

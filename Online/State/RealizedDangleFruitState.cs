using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    // 
    public class RealizedDangleFruitState : RealizedPhysicalObjectState
    {
        bool hasStalk = false;
        bool isEaten = false;
        public override RealizedPhysicalObjectState EmptyDelta() => new RealizedDangleFruitState();

        public RealizedDangleFruitState() { }

        public RealizedDangleFruitState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var fruit = (DangleFruit)onlineEntity.apo.realizedObject;
            if (fruit.stalk != null)
            {
                this.hasStalk = true;
            }
            if (fruit.BitesLeft < 2) {
                this.isEaten = true;
            }

        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            var fruit = (DangleFruit)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            if (isEaten)
            {
                fruit.Destroy();
            }
            if (hasStalk && fruit.stalk == null)
            {
                fruit.stalk = new DangleFruit.Stalk(fruit, fruit.room, fruit.firstChunk.pos);
                fruit.room.AddObject(fruit.stalk);
            }
            base.ReadTo(onlineEntity);
        }

        public override StateType stateType => StateType.RealizedDangleFruitState;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref hasStalk);
            serializer.Serialize(ref isEaten);
        }

        public override long EstimatedSize(bool inDeltaContext)
        {
            var size = base.EstimatedSize(inDeltaContext);
            size += 1;
            size += 1;
            return size;
        }

        public override RealizedPhysicalObjectState Delta(RealizedPhysicalObjectState _other)
        {
            var other = (RealizedDangleFruitState)_other;
            var delta = (RealizedDangleFruitState)base.Delta(_other);
            delta.hasStalk = this.hasStalk;
            delta.isEaten = this.isEaten;
            return delta;
        }

        public override RealizedPhysicalObjectState ApplyDelta(RealizedPhysicalObjectState _other) {
            var other = (RealizedDangleFruitState)_other;
            var result = (RealizedDangleFruitState)base.ApplyDelta(_other);
            result.hasStalk = other.hasStalk;
            result.isEaten = other.isEaten;
            return result;
        }
    }
}

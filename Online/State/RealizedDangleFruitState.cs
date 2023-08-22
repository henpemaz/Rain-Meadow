using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    // 
    public class RealizedDangleFruitState : RealizedPhysicalObjectState
    {
        bool hasStalk = false;
        byte bites = 3; //for some reason converting this to a bool does not work
        Vector2 pos; //helps with physics simulation. Stops massive jumps
        public override RealizedPhysicalObjectState EmptyDelta() => new RealizedDangleFruitState();

        public RealizedDangleFruitState() { }

        public RealizedDangleFruitState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var fruit = (DangleFruit)onlineEntity.apo.realizedObject;
            this.pos = fruit.firstChunk.pos;
            this.bites = (byte)fruit.bites;
            if (fruit.stalk.ropeLength > 0f)
            {
                this.hasStalk = true;
            }

        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            var fruit = (DangleFruit)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            if (bites < 1)
            {
                fruit.Destroy();
            }
            if (hasStalk && fruit.stalk == null)
            {
                fruit.stalk = new DangleFruit.Stalk(fruit, fruit.room, pos);
                fruit.room.AddObject(fruit.stalk);
            }
            base.ReadTo(onlineEntity);
        }

        public override StateType stateType => StateType.RealizedDangleFruitState;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref hasStalk);
            serializer.Serialize(ref bites);
            serializer.Serialize(ref pos);
        }

        public override RealizedPhysicalObjectState Delta(RealizedPhysicalObjectState _other)
        {
            var other = (RealizedDangleFruitState)_other;
            var delta = (RealizedDangleFruitState)base.Delta(_other);
            delta.hasStalk = this.hasStalk;
            delta.bites = this.bites;

            return delta;
        }

        public override RealizedPhysicalObjectState ApplyDelta(RealizedPhysicalObjectState _other) {
            var other = (RealizedDangleFruitState)_other;
            var result = (RealizedDangleFruitState)base.ApplyDelta(_other);
            result.hasStalk = other.hasStalk;
            result.bites = other.bites;

            return result;
        }
    }
}

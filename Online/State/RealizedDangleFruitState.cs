using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{

    public class RealizedDangleFruitState : RealizedPhysicalObjectState
    {
        public override RealizedPhysicalObjectState EmptyDelta() => new RealizedDangleFruitState();

        public RealizedDangleFruitState() { }

        public RealizedDangleFruitState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var fruit = (DangleFruit)onlineEntity.apo.realizedObject;
            if (fruit.stalk != null)
            {
                var stalk = fruit.stalk;
                //stalk.stuckPos;
                //stalk.ropeLength;
                //this.stalk = new DangleFruit.Stalk(this, placeRoom, base.firstChunk.pos);
                RainMeadow.Debug("we found a stalk!");
            }

        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            if (!onlineEntity.owner.isMe && onlineEntity.isPending) return;
            base.ReadTo(onlineEntity);

        }

        public override StateType stateType => StateType.RealizedDangleFruitState;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
        }

        public override long EstimatedSize(bool inDeltaContext)
        {
            return base.EstimatedSize(inDeltaContext);
        }

        public override RealizedPhysicalObjectState Delta(RealizedPhysicalObjectState _other)
        {
            var other = (RealizedDangleFruitState)_other;
            var delta = (RealizedDangleFruitState)base.Delta(_other);

            return delta;
        }

        public override RealizedPhysicalObjectState ApplyDelta(RealizedPhysicalObjectState _other) {
            var other = (RealizedDangleFruitState)_other;
            var result = (RealizedDangleFruitState)base.ApplyDelta(_other);

            return result;
        }
    }
}

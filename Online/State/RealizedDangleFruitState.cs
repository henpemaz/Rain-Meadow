using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    // 
    public class RealizedDangleFruitState : RealizedPhysicalObjectState
    {
        [OnlineField]
        bool hasStalk = false;
        [OnlineField]
        byte bites = 3;
        [OnlineField]
        Vector2 pos; //helps with physics simulation. Stops massive jumps

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
            fruit.bites = bites;
            if (hasStalk && fruit.stalk == null)
            {
                fruit.stalk = new DangleFruit.Stalk(fruit, fruit.room, pos);
                fruit.room.AddObject(fruit.stalk);
            }
            base.ReadTo(onlineEntity);
        }
    }
}

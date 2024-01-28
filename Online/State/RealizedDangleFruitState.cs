using System;
using System.Collections.Generic;
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
        public RealizedDangleFruitState() { }

        public RealizedDangleFruitState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var fruit = (DangleFruit)onlineEntity.apo.realizedObject;

            this.bites = (byte)fruit.bites;
            if (fruit.stalk?.ropeLength > 0f)
            {
                this.hasStalk = true;
            }

        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var fruit = (DangleFruit)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            fruit.bites = bites;
            if (hasStalk && fruit.stalk == null)
            {
                fruit.stalk = new DangleFruit.Stalk(fruit, fruit.room, fruit.bodyChunks[0].pos);
                fruit.room.AddObject(fruit.stalk);
            }
        }
    }
}

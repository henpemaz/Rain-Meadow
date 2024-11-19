using UnityEngine;
using System;

namespace RainMeadow
{
    public class RealizedGarbageWormState : RealizedPhysicalObjectState
    {
        [OnlineField]
        public bool extended;
        [OnlineField]
        public byte hole;
        [OnlineField]
        public bool showAsAngry;
        [OnlineField]
        public Vector2 lookPoint;
        
        public RealizedGarbageWormState() { }

        public RealizedGarbageWormState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var garbageWorm = (GarbageWorm)onlineEntity.apo.realizedObject;

            this.extended = garbageWorm.extended > 0 && garbageWorm.retractSpeed >= 0;
            this.hole = (byte)garbageWorm.hole;
            this.showAsAngry = garbageWorm.AI.showAsAngry;
            this.lookPoint = garbageWorm.lookPoint;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var garbageWorm = (GarbageWorm)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            garbageWorm.AI.retractCounter = 0;
            garbageWorm.AI.comeBackOutCounter = 0;
            garbageWorm.chargePos = null;
            if (garbageWorm.hole != this.hole)
            {
                garbageWorm.hole = this.hole;
                garbageWorm.NewHole(true);
            }
            if (this.extended != garbageWorm.extended > 0)
            {
                if (this.extended)
                    garbageWorm.Extend();
                else
                    garbageWorm.Retract();
            }
            garbageWorm.AI.showAsAngry = this.showAsAngry;
            garbageWorm.lookPoint = this.lookPoint;
        }
    }
}

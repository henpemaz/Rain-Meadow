using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    // 
    public class RealizedSlimeMoldState : RealizedPhysicalObjectState
    {
        [OnlineField]
        byte bites = 3;
        public RealizedSlimeMoldState() { }

        public RealizedSlimeMoldState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var slime = (SlimeMold)onlineEntity.apo.realizedObject;

            this.bites = (byte)slime.bites;

        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var slime = (SlimeMold)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            slime.bites = bites;
        }
    }
}

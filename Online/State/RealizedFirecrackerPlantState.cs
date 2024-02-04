using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    // 
    public class RealizedFirecrackerPlantState : RealizedPhysicalObjectState
    {
        public RealizedFirecrackerPlantState() { }

        public RealizedFirecrackerPlantState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var plant = (FirecrackerPlant)onlineEntity.apo.realizedObject;
            //TODO we need to sync lumps
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var plant = (FirecrackerPlant)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;

        }
    }
}

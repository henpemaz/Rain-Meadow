using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    // 
    public class RealizedFirecrackerPlantState : RealizedPhysicalObjectState
    {
        [OnlineField(nullable = true, always =true)]
        Vector2? growPos;
        public RealizedFirecrackerPlantState() { }

        public RealizedFirecrackerPlantState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var plant = (FirecrackerPlant)onlineEntity.apo.realizedObject;

            growPos = plant.growPos;

            RainMeadow.Debug("READ FIRECRACKER");
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var plant = (FirecrackerPlant)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            RainMeadow.Debug("WRITE FIRECRACKER");
            if (growPos != null)
            {
                //plant.growPos = growPos;
                plant.PlaceInRoom(plant.room);
            }
        }
    }
}

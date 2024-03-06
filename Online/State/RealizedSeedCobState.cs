using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    // 
    public class RealizedSeedCobState : RealizedPhysicalObjectState
    {
        [OnlineField]
        public List<Vector2> seedPositions;
        public RealizedSeedCobState() { }

        public RealizedSeedCobState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var seedCob = (SeedCob)onlineEntity.apo.realizedObject;
            seedPositions = seedCob.seedPositions.ToList();
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var seedCob = (SeedCob)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            seedCob.seedPositions = seedPositions.ToArray();
        }
    }
}

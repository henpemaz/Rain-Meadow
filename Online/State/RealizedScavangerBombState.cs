using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class RealizedScavengerBombState : RealizedPhysicalObjectState
    {
        [OnlineField]
        bool ignited;
        public RealizedScavengerBombState() { }

        public RealizedScavengerBombState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var scavBomb = (ScavengerBomb)onlineEntity.apo.realizedObject;

            ignited = scavBomb.ignited;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var scavBomb = (ScavengerBomb)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;

            if (scavBomb.ignited != ignited) 
            {
                scavBomb.ignited = ignited;
                scavBomb.room.PlaySound(SoundID.Slugcat_Throw_Bomb, scavBomb.firstChunk);
            }
        }

    }
}
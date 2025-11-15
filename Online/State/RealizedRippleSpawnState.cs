using System.Drawing;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class RealizedRippleSpawnState : RealizedPhysicalObjectState
    {
        [OnlineFieldHalf]
        private float fade;
        public RealizedRippleSpawnState() { }
        public RealizedRippleSpawnState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var rip = (VoidSpawn)onlineEntity.apo.realizedObject;
            fade = rip.fade;

        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var rip = (VoidSpawn)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            rip.fade = fade;
        }
    }

  
}
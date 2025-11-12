using System.Drawing;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class RealizedRippleSpawnState : RealizedCreatureState
    {
        [OnlineField]
        private VoidSpawn.SpawnType rippleType;
        [OnlineFieldHalf]
        private float sizeFac;
        public RealizedRippleSpawnState() { }
        public RealizedRippleSpawnState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            var rip = (VoidSpawn)onlineEntity.apo.realizedObject;
            rippleType = rip.variant;
            sizeFac = rip.sizeFac;

        }

        public void ReadTo(OnlineCreature onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var rip = (VoidSpawn)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            rip.variant = rippleType;
            rip.sizeFac = sizeFac;
        }
    }

  
}
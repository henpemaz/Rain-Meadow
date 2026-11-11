using System.Drawing;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class RealizedRippleSpawnState : RealizedPhysicalObjectState
    {
        [OnlineField]
        private int timer;
        [OnlineField]
        private bool startFadeOut;
        [OnlineField]
        private bool rippleLayer;
        public RealizedRippleSpawnState() { }
        public RealizedRippleSpawnState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var rip = (VoidSpawn)onlineEntity.apo.realizedObject;
            timer = rip.timeUntilFadeout;
            startFadeOut = rip.startFadeOut;
            rippleLayer = rip.abstractPhysicalObject.rippleLayer == 0;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var rip = (VoidSpawn)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            rip.timeUntilFadeout = timer;
            rip.startFadeOut = startFadeOut;
            int layer = rippleLayer ? 0 : 1;
            if (layer != rip.abstractPhysicalObject.rippleLayer)
            {
                rip.ChangeRippleLayer(layer, true);
            }
        }
    }

  
}
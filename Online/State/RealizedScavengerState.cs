using UnityEngine;

namespace RainMeadow
{
    internal class RealizedScavengerState : RealizedCreatureState
    {
        [OnlineField(group = "swing")]
        private Vector2? swingPos;
        [OnlineFieldHalf(group = "swing")]
        private float swingRadius;
        [OnlineFieldHalf]
        private float flip;
        [OnlineField(group = "swing")]
        private byte swingClimbCounter;
        [OnlineField(group = "swing")]
        private byte swingArm;

        public RealizedScavengerState() { }
        public RealizedScavengerState(OnlineCreature onlineCreature) : base(onlineCreature)
        {
            var scav = onlineCreature.apo.realizedObject as Scavenger;
            this.swingPos = scav.swingPos;
            this.swingRadius = scav.swingRadius;
            this.flip = scav.flip;
            this.swingClimbCounter = (byte)scav.swingClimbCounter;
            this.swingArm = (byte)scav.swingArm;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var scav = (onlineEntity as OnlineCreature).realizedCreature as Scavenger;
            if (scav != null)
            {
                scav.swingPos = this.swingPos;
                scav.swingRadius = this.swingRadius;
                scav.flip = this.flip;
                scav.swingClimbCounter = this.swingClimbCounter;
                scav.swingArm = this.swingArm;
            }
        }
    }
}
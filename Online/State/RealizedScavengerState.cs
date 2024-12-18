using UnityEngine;

namespace RainMeadow
{
    internal class RealizedScavengerState : RealizedPhysicalObjectState
    {
        [OnlineField(nullable = true)]
        private Vector2? swingPos;
        [OnlineFieldHalf]
        private float swingRadius;
        [OnlineFieldHalf]
        private float flip;
        [OnlineField]
        private byte swingClimbCounter;

        public RealizedScavengerState() { }
        public RealizedScavengerState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var scav = onlineEntity.apo.realizedObject as Scavenger;
            this.swingPos = scav.swingPos;
            this.swingRadius = scav.swingRadius;
            this.flip = scav.flip;
            this.swingClimbCounter = (byte)scav.swingClimbCounter;
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
            }
        }
    }
}
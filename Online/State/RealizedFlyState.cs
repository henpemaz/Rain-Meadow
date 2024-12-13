using UnityEngine;

namespace RainMeadow
{
    public class RealizedFlyState : RealizedCreatureState
    {
        [OnlineField]
        byte bites;
        [OnlineField]
        byte eaten;
        [OnlineFieldHalf]
        private Vector2 dir;
        [OnlineField(nullable: true)]
        private Vector2? burrowOrHangSpot;
        public RealizedFlyState() { }
        public RealizedFlyState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            var fly = (Fly)onlineEntity.apo.realizedObject;

            this.bites = (byte)fly.bites;
            this.eaten = (byte)fly.eaten;
            this.dir = fly.dir;
            if (fly.burrowOrHangSpot.HasValue)
            {
                this.burrowOrHangSpot = fly.burrowOrHangSpot.Value;
            }
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var fly = (Fly)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            fly.bites = bites;
            fly.eaten = eaten;
            fly.dir = dir;
            if (burrowOrHangSpot != null)
            {
                fly.burrowOrHangSpot = this.burrowOrHangSpot;
            }
        }
    }
}
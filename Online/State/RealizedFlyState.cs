using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using UnityEngine;

namespace RainMeadow
{
    // 
    public class RealizedFlyState : RealizedPhysicalObjectState
    {
        [OnlineField]
        byte bites;
        [OnlineField]
        byte eaten;
        [OnlineField]
        byte movMode;
        [OnlineField]
        private Vector2 dir;
        [OnlineField(nullable:true)]
        private Vector2? burrowOrHangSpot;
        public RealizedFlyState() { }
        public RealizedFlyState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var fly = (Fly)onlineEntity.apo.realizedObject;

            this.bites = (byte)fly.bites;
            this.eaten = (byte)fly.eaten;
            this.dir = fly.dir;
            this.movMode = (byte)fly.movMode.index;
            if (fly.burrowOrHangSpot.HasValue) {
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
            fly.movMode = new Fly.MovementMode(Fly.MovementMode.values.GetEntry(movMode));
            if (burrowOrHangSpot != null) {
                fly.burrowOrHangSpot = this.burrowOrHangSpot;
            }
        }
    }
}
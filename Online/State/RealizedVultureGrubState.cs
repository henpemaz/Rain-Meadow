using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    // 
    public class RealizedVultureGrubState : RealizedPhysicalObjectState
    {
        [OnlineField]
        byte bites = 3;
        public RealizedVultureGrubState() { }

        public RealizedVultureGrubState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var grub = (VultureGrub)onlineEntity.apo.realizedObject;

            this.bites = (byte)grub.bites;

        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var grub = (VultureGrub)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            grub.bites = bites;
        }
    }
}

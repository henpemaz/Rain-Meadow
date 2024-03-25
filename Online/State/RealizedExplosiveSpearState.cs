using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.Online.State
{
    public class RealizedExplosiveSpearState : RealizedSpearState
    {

        // Most of these don't seem to require syncing?
        [OnlineField]
        int explodeAt;
        [OnlineField]
        List<int> miniExplosions;
        [OnlineFieldColorRgb]
        Color redColor;

        [OnlineFieldColorRgb] // maybe?
        Color explodeColor;

        public RealizedExplosiveSpearState() { }

        public RealizedExplosiveSpearState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var explosiveSpear = (ExplosiveSpear)onlineEntity.apo.realizedObject;
            explodeAt = explosiveSpear.explodeAt;
            redColor = explosiveSpear.redColor;
            explodeColor = explosiveSpear.explodeColor;
            miniExplosions = explosiveSpear.miniExplosions;

        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            
            var explosiveSpear = (ExplosiveSpear)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            explosiveSpear.explodeAt = explodeAt;
            explosiveSpear.miniExplosions = miniExplosions;
            explosiveSpear.redColor = redColor;
            explosiveSpear.explodeColor = explodeColor;
        }

    }
}
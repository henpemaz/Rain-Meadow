using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.Online.State
{
    public class RealizedExplosiveSpearState : RealizedSpearState
    {
        [OnlineField]
        int explodeAt;
/*        [OnlineField]
        Vector2[,] rag;*/
        [OnlineField]
        List<int> miniExplosions; 

        public RealizedExplosiveSpearState() { }

        public RealizedExplosiveSpearState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var explosiveSpear = (ExplosiveSpear)onlineEntity.apo.realizedObject;
            explodeAt = explosiveSpear.explodeAt;
            //rag = explosiveSpear.rag;
            miniExplosions = explosiveSpear.miniExplosions;

        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var explosiveSpear = (ExplosiveSpear)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            explosiveSpear.explodeAt = explodeAt;
            //explosiveSpear.rag = rag;
            explosiveSpear.miniExplosions = miniExplosions;
        }

    }
}
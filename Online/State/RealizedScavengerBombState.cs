using IL.RWCustom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;
namespace RainMeadow
{
    public class RealizedScavengerBombState : RealizedPhysicalObjectState
    {

        [OnlineFieldColorRgb]
        Color explosionColor;

        [OnlineFieldColorRgb]
        Color color;

        [OnlineField]
        bool explosionIsForShow;

        [OnlineField]
        int floorBounceFrames;

        public RealizedScavengerBombState() { }
        public RealizedScavengerBombState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var scavBomb = (ScavengerBomb)onlineEntity.apo.realizedObject;
            explosionColor = scavBomb.explodeColor;
            color = scavBomb.color;
            explosionIsForShow = scavBomb.explosionIsForShow;
            floorBounceFrames = scavBomb.floorBounceFrames;

        }


        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var scavBomb = (ScavengerBomb)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            scavBomb.explodeColor = explosionColor;
            scavBomb.color = color;
            scavBomb.explosionIsForShow= explosionIsForShow;
            scavBomb.floorBounceFrames = floorBounceFrames;
        }
    }
}
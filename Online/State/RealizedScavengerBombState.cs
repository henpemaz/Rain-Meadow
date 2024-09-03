using UnityEngine;
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

        [OnlineField]
        bool ignited;

        [OnlineField]
        float burn;

        [OnlineField]
        float[] spikes;



        public RealizedScavengerBombState() { }
        public RealizedScavengerBombState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var scavBomb = (ScavengerBomb)onlineEntity.apo.realizedObject;
            explosionColor = scavBomb.explodeColor;
            color = scavBomb.color;
            explosionIsForShow = scavBomb.explosionIsForShow;
            floorBounceFrames = scavBomb.floorBounceFrames;
            ignited = scavBomb.ignited;
            burn = scavBomb.burn;
            spikes = scavBomb.spikes;

        }


        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var scavBomb = (ScavengerBomb)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            scavBomb.explodeColor = explosionColor;
            scavBomb.color = color;
            scavBomb.explosionIsForShow = explosionIsForShow;
            scavBomb.floorBounceFrames = floorBounceFrames;
            scavBomb.ignited = ignited;
            scavBomb.burn = burn;
            scavBomb.spikes = spikes;
        }
    }
}
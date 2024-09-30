
using MoreSlugcats;
using UnityEngine;
namespace RainMeadow
{
    public class RealizedSingularityBombState : RealizedPhysicalObjectState
    {

        [OnlineFieldColorRgb]
        Color explosionColor;

        [OnlineFieldColorRgb]
        Color color;

        [OnlineField]
        int floorBounceFrames;

        [OnlineField]
        bool ignited;

        [OnlineField]
        float burn;

        [OnlineField]
        float[] spikes;



        public RealizedSingularityBombState() { }
        public RealizedSingularityBombState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var singBomb = (SingularityBomb)onlineEntity.apo.realizedObject;
            explosionColor = singBomb.explodeColor;
            color = singBomb.color;
            floorBounceFrames = singBomb.floorBounceFrames;
            ignited = singBomb.ignited;

        }


        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var singBomb = (SingularityBomb)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            singBomb.explodeColor = explosionColor;
            singBomb.color = color;
            singBomb.floorBounceFrames = floorBounceFrames;
            singBomb.ignited = ignited;
        }
    }
}
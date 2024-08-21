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

        [OnlineField]
        Vector2 pos;

        [OnlineField]
        Vector2 lastPos;

        public RealizedScavengerBombState() { }

        public RealizedScavengerBombState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var scavBomb = (ScavengerBomb)onlineEntity.apo.realizedObject;
            scavBomb.firstChunk.pos = pos;
            scavBomb.firstChunk.lastPos = lastPos;


        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var scavBomb = (ScavengerBomb)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            scavBomb.firstChunk.pos = pos;
            scavBomb.firstChunk.lastPos = lastPos;


        }

    }
}
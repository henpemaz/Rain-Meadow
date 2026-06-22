using UnityEngine;
using Watcher;

namespace RainMeadow
{
    public class RealizedPrinceBulbState : RealizedPhysicalObjectState
    {
        [OnlineField(nullable = true)]
        PrinceState prince;
        [OnlineField(nullable = true)]
        ChunkState body;

        public RealizedPrinceBulbState() { }
        public RealizedPrinceBulbState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            PrinceBulb bulb = onlineEntity.apo.realizedObject as PrinceBulb;
            body = new ChunkState(bulb.bodyChunks[0]);
            prince = new(bulb.prince);            
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            PrinceBulb bulb = (PrinceBulb)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            if (bulb == null) return;
            body.ReadTo(bulb.bodyChunks[0]);
            prince.ReadTo(bulb.prince);            
        }
        [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
        public class PrinceState : OnlineState
        {
            [OnlineField]
            Vector2 lookPoint;            
            [OnlineField]
            Vector2 getToDir;
            [OnlineField]
            Vector2 getToPos;

            public PrinceState() { }
            public PrinceState(Prince prince)
            {                
                lookPoint = prince.behavior.lookPoint;
                getToDir = prince.behavior.GetToDir;
                getToPos = prince.behavior.GetToPos;
            }

            public void ReadTo(Prince prince)
            {
                prince.behavior.lookPoint = lookPoint;                
                prince.behavior.GetToDir = getToDir;
                prince.behavior.GetToPos = getToPos;                
            }

        }        
    }
}

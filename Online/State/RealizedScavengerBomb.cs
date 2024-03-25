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
        /*        [OnlineField]
                BodyChunk[] bodyChunks;*/
        /*
                [OnlineField]
                PhysicalObject.BodyChunkConnection[] bodyChunkConnections;
        */
        [OnlineField]
        float airFriction;

        [OnlineField]
        float gravity;

        [OnlineField]
        float bounce;

        [OnlineField]
        float surfaceFriction;
        [OnlineField]
        int collisionLayer;
        [OnlineField]
        float waterFriction;

        [OnlineField]
        int loudness;

        [OnlineField]
        Vector2 tailPos;
        [OnlineField]
        float[] spikes;

        [OnlineFieldColorRgb]
        Color explosionColor;

        [OnlineFieldColorRgb]
        Color color;

        public RealizedScavengerBombState() { }

        public RealizedScavengerBombState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var scavBomb = (ScavengerBomb)onlineEntity.apo.realizedObject;

            /*            bodyChunks = scavBomb.bodyChunks;
                        bodyChunkConnections = scavBomb.bodyChunkConnections;*/
            surfaceFriction = scavBomb.surfaceFriction;
            gravity = scavBomb.gravity;
            airFriction = scavBomb.airFriction;
            bounce = scavBomb.bounce;
            surfaceFriction = scavBomb.surfaceFriction;
            collisionLayer = scavBomb.collisionLayer;
            waterFriction = scavBomb.waterFriction;
            tailPos = scavBomb.tailPos;
            spikes = scavBomb.spikes;
            explosionColor = scavBomb.explodeColor;
            color = scavBomb.color;
            
            
            //loudness = scavBomb.firstChunk.loudness;



        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);


            var scavBomb = (ScavengerBomb)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            scavBomb.explodeColor = explosionColor;
/*            scavBomb.bodyChunks = bodyChunks;
            scavBomb.bodyChunkConnections = bodyChunkConnections;*/
            scavBomb.airFriction = airFriction;
            scavBomb.gravity = gravity;
            scavBomb.surfaceFriction = surfaceFriction;
            scavBomb.collisionLayer = collisionLayer;
            scavBomb.waterFriction = waterFriction;
            scavBomb.tailPos = tailPos;
            scavBomb.spikes = spikes;
            scavBomb.bounce = bounce;
            scavBomb.color = color;
            //scavBomb.lou = loudness;
        }

    }
}
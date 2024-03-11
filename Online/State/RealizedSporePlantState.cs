using UnityEngine;

namespace RainMeadow
{
    public class RealizedSporePlantState : RealizedWeaponState
    {
        [OnlineField]
        float angry;
        [OnlineField]
        int releaseBeesCounter;
        [OnlineField]
        int releaseBeesDelay;
        [OnlineField]
        bool hasStalk;
        [OnlineField]
        Vector2 stalkDirVec;
        [OnlineField]
        Vector2 baseDirVec;
        [OnlineField]
        Vector2 stalkStuckPos;
        [OnlineField]
        float coil;
        //[OnlineField]
        //bool deployOnCollision;
        public RealizedSporePlantState() { }

        public RealizedSporePlantState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var sporePlant = (SporePlant)onlineEntity.apo.realizedObject;

            this.angry = sporePlant.angry;
            this.releaseBeesCounter = sporePlant.releaseBeesCounter;
            this.releaseBeesDelay = sporePlant.releaseBeesDelay;
            if (sporePlant.stalk != null)
            {
                this.hasStalk = true;
                this.stalkDirVec = sporePlant.stalk.stalkDirVec;
                this.baseDirVec = sporePlant.stalk.baseDirVec;
                this.stalkStuckPos = sporePlant.stalk.stuckPos;
                this.coil = sporePlant.stalk.coil;
            }
            else 
            {
                this.hasStalk = false;
            }
            //this.deployOnCollision = sporePlant.deployOnCollision;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var sporePlant = (SporePlant)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            sporePlant.angry = this.angry;
            sporePlant.releaseBeesCounter = this.releaseBeesCounter;
            sporePlant.releaseBeesDelay = this.releaseBeesDelay;
            //sporePlant.deployOnCollision = this.deployOnCollision;
            if (hasStalk && sporePlant.stalk != null) {
                sporePlant.stalk.baseDirVec = this.baseDirVec;
                sporePlant.stalk.stalkDirVec = this.stalkDirVec;
                sporePlant.stalk.stuckPos = this.stalkStuckPos;
                sporePlant.stalk.fruitPos = sporePlant.firstChunk.pos;
                sporePlant.stalk.coil = this.coil;
            }
        }
    }
}

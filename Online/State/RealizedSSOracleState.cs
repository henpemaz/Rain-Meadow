using System.Linq;
using UnityEngine;
using static SLOracleWakeUpProcedure;

namespace RainMeadow
{
    public class RealizedSSOracleState : RealizedOracleState
    {
        public static bool PebblesActionAllow = false;
        [OnlineFieldHalf]
        public Vector2 currentGetTo;
        [OnlineFieldHalf]
        public Vector2 nextPos;
        [OnlineFieldHalf]
        public float investigateAngle;
        [OnlineField]
        public SSOracleBehavior.MovementBehavior movementBehavior;

        public RealizedSSOracleState() { }

        public RealizedSSOracleState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var oracle = (Oracle)onlineEntity.apo.realizedObject;
            var behavior = (SSOracleBehavior)oracle.oracleBehavior;
            currentGetTo = behavior.currentGetTo;
            nextPos = behavior.nextPos;
            investigateAngle = behavior.investigateAngle;
            movementBehavior = behavior.movementBehavior;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            var oracle = (Oracle)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            var behavior = (SSOracleBehavior)oracle.oracleBehavior;
            behavior.currentGetTo = currentGetTo;
            behavior.nextPos = nextPos;
            behavior.investigateAngle = investigateAngle;
            behavior.movementBehavior = movementBehavior;
            base.ReadTo(onlineEntity);
        }
    }
}

using System.Linq;
using UnityEngine;
using static SLOracleWakeUpProcedure;

namespace RainMeadow
{
    public class RealizedSSOracleState : RealizedOracleState
    {
        public static bool allowActionChange = false;
        [OnlineFieldHalf]
        public Vector2 currentGetTo;
        [OnlineFieldHalf]
        public Vector2 nextPos;
        [OnlineFieldHalf]
        public float investigateAngle;
        [OnlineFieldHalf]
        public float working;
        [OnlineField]
        public SSOracleBehavior.MovementBehavior movementBehavior;
        [OnlineField]
        public SSOracleBehavior.Action action;

        public RealizedSSOracleState() { }

        public RealizedSSOracleState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var oracle = (Oracle)onlineEntity.apo.realizedObject;
            var behavior = (SSOracleBehavior)oracle.oracleBehavior;
            currentGetTo = behavior.currentGetTo;
            nextPos = behavior.nextPos;
            investigateAngle = behavior.investigateAngle;
            movementBehavior = behavior.movementBehavior;
            working = behavior.working;
            action = behavior.action;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var oracle = (Oracle)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            var behavior = (SSOracleBehavior)oracle.oracleBehavior;
            behavior.currentGetTo = currentGetTo;
            behavior.nextPos = nextPos;
            behavior.investigateAngle = investigateAngle;
            behavior.movementBehavior = movementBehavior;
            behavior.working = working;
            if (behavior.action != action)
            {
                allowActionChange = true;
                behavior.NewAction(action);
                allowActionChange = false;
            }
        }
    }
}

using System.Linq;
using UnityEngine;
using static SLOracleWakeUpProcedure;

namespace RainMeadow
{
    public class RealizedSLOracleState : RealizedOracleState
    {
        public static bool allowConversationChange = false;
        [OnlineField(nullable = true)]
        public OnlinePhysicalObject? holdingObject;
        [OnlineField]
        public Phase revivePhase = Phase.LookingForSwarmer;
        [OnlineField]
        public int inPhaseCounter;
        [OnlineField(nullable = true)]
        public Conversation.ID convoId;
        [OnlineField(nullable = true)]
        public SLOracleBehaviorHasMark.MiscItemType convoItemType;
        // TODO: genericize with 5P?
        [OnlineFieldHalf]
        public Vector2 currentGetTo;
        [OnlineFieldHalf]
        public Vector2 nextPos;
        [OnlineFieldHalf]
        public float investigateAngle;
        [OnlineField]
        public SLOracleBehavior.MovementBehavior movementBehavior;

        public RealizedSLOracleState() { }

        public RealizedSLOracleState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var oracle = (Oracle)onlineEntity.apo.realizedObject;
            var behavior = (SLOracleBehavior)oracle.oracleBehavior;
            this.holdingObject = behavior.holdingObject?.abstractPhysicalObject?.GetOnlineObject();
            if (behavior.initWakeUpProcedure)
            {
                var wakeUpProcedure = oracle.room.updateList.OfType<SLOracleWakeUpProcedure>().FirstOrDefault();
                if (wakeUpProcedure is not null)
                {
                    revivePhase = wakeUpProcedure.phase;
                    inPhaseCounter = wakeUpProcedure.inPhaseCounter;
                }
            }
            if (oracle.oracleBehavior is SLOracleBehaviorHasMark markBehavior && markBehavior.currentConversation != null)
            {
                convoId = markBehavior.currentConversation.id;
                convoItemType = ((SLOracleBehaviorHasMark.MoonConversation)markBehavior.currentConversation).describeItem;
            }
            // TODO: genericize with 5P??
            currentGetTo = behavior.currentGetTo;
            nextPos = behavior.nextPos;
            investigateAngle = (float)behavior.investigateAngle;
            movementBehavior = behavior.movementBehavior;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var oracle = (Oracle)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            var behavior = (SLOracleBehavior)oracle.oracleBehavior;
            var po = this.holdingObject?.apo?.realizedObject;
            if (po != behavior.holdingObject)
            {
                if (po is null)
                {
                    behavior.holdingObject = null;
                }
                else
                {
                    behavior.GrabObject(po);
                }
            }
            var wakeUpProcedure = oracle.room.updateList.OfType<SLOracleWakeUpProcedure>().FirstOrDefault();
            if (wakeUpProcedure is not null)
            {
                if ((int)wakeUpProcedure.phase < (int)Phase.Rumble && (int)Phase.Rumble < (int)this.revivePhase)
                {
                    wakeUpProcedure.inPhaseCounter = 0;
                    wakeUpProcedure.phase = Phase.Rumble;  // skip GoToOracle because we might not have NSHSwarmer
                }
                if ((int)wakeUpProcedure.phase < (int)Phase.Booting && (int)Phase.Booting <= (int)this.revivePhase)
                {
                    wakeUpProcedure.inPhaseCounter = 0;
                    wakeUpProcedure.phase = Phase.Booting;  // skip Rumble because laggy
                }
                for (var timeout = 500; timeout > 0 && ((int)wakeUpProcedure.phase < (int)this.revivePhase || ((int)wakeUpProcedure.phase == (int)this.revivePhase && wakeUpProcedure.inPhaseCounter < this.inPhaseCounter)); timeout--)
                {
                    wakeUpProcedure.Update(!wakeUpProcedure.evenUpdate);
                }
            }
            // Somewhat synchs the stuff - some dialogue is decided randomly through
            if (oracle.oracleBehavior is SLOracleBehaviorHasMark markBehavior)
            {
                allowConversationChange = true;
                if (convoId != null && convoItemType != null && (markBehavior.currentConversation == null || markBehavior.currentConversation.id != convoId))
                    markBehavior.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(convoId, markBehavior, convoItemType);
                allowConversationChange = false;
            }
            // TODO: genericize with 5P?
            behavior.currentGetTo = currentGetTo;
            behavior.nextPos = nextPos;
            behavior.investigateAngle = investigateAngle;
            behavior.movementBehavior = movementBehavior;
        }
    }
}

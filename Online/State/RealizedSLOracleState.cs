using System.Linq;
using UnityEngine;
using static SLOracleWakeUpProcedure;

namespace RainMeadow
{
    public class RealizedSLOracleState : RealizedOracleState
    {
        [OnlineField]
        public OnlinePhysicalObject? holdingObject;
        [OnlineField]
        public Phase revivePhase = Phase.LookingForSwarmer;
        [OnlineField]
        public int inPhaseCounter;

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
        }
    }
}

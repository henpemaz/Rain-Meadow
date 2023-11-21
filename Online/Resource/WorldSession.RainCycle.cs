﻿namespace RainMeadow
{
    public partial class WorldSession
    {
        [DeltaSupport(level = StateHandler.DeltaSupport.FollowsContainer)]
        public class RainCycleData : OnlineState
        {
            [OnlineField]
            public int cycleLength = 0;
            [OnlineField(group: "timer")]
            public int timer = 0;
            [OnlineField]
            public int preTimer = 0;

            public RainCycleData()
            {

            }
            public RainCycleData(RainCycle rainCycle)
            {
                this.cycleLength = rainCycle.cycleLength;
                this.timer = rainCycle.timer;
                this.preTimer = rainCycle.preTimer;
            }

            public override bool Equals(object obj)
            {
                if (obj is RainCycleData)
                {
                    var rainCycle = (RainCycleData)obj;
                    return (this.cycleLength == rainCycle.cycleLength && this.timer == rainCycle.timer && this.preTimer == rainCycle.preTimer);
                }
                return false;
            }
        }
    }
}

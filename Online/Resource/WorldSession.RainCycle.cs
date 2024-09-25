namespace RainMeadow
{
    public partial class WorldSession
    {
        // todo make this entitydata and only bind it in storymode

        [DeltaSupport(level = StateHandler.DeltaSupport.FollowsContainer)]
        public class RainCycleData : OnlineState
        {
            [OnlineField]
            public int cycleLength = 0;
            [OnlineField(group:"timer")]
            public int timer = 0;
            [OnlineField]
            public int preTimer = 0;
            [OnlineField]
            public bool antiGravity = false;

            public RainCycleData() { 
            
            }
            public RainCycleData(RainCycle rainCycle) {
                this.cycleLength = rainCycle.cycleLength;
                this.timer = rainCycle.timer;
                this.preTimer = rainCycle.preTimer;
                this.antiGravity = rainCycle.brokenAntiGrav?.on ?? false;
            }

            public override bool Equals(object obj)
            {
                if (obj is RainCycleData) {
                    var rainCycle = (RainCycleData)obj;
                    return (this.cycleLength == rainCycle.cycleLength && this.timer == rainCycle.timer && this.preTimer == rainCycle.preTimer && this.antiGravity == rainCycle.antiGravity);
                }
                return false;
            }
        }
    }
}

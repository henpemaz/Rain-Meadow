namespace RainMeadow
{
    public partial class WorldSession
    {
        // todo make this entitydata and only bind it in storymode

        [DeltaSupport(level = StateHandler.DeltaSupport.FollowsContainer)]
        public class RainCycleData : OnlineState
        {
            // RainCycle
            [OnlineField]
            public int cycleLength = 0;
            [OnlineField(group: "timer")]
            public int timer = 0;
            [OnlineField]
            public int preTimer = 0;
            [OnlineField]
            public bool antiGravity = false;

            // WaterLevelCycle
            [OnlineFieldHalf]
            public float stageDuration;
            [OnlineFieldHalf]
            public float timeInStage;
            [OnlineField]
            public byte stage;

            //GlobalRain
            [OnlineField(group = "counter")]
            public int waterFluxTicker;

            public RainCycleData()
            {

            }
            public RainCycleData(RainCycle rainCycle)
            {
                // RainCycle
                this.cycleLength = rainCycle.cycleLength;
                this.timer = rainCycle.timer;
                this.preTimer = rainCycle.preTimer;
                this.antiGravity = rainCycle.brokenAntiGrav?.on ?? false;

                // WaterLevelCycle
                this.stageDuration = rainCycle.waterCycle.stageDuration;
                this.timeInStage = rainCycle.waterCycle.timeInStage;
                this.stage = (byte)rainCycle.waterCycle.stage;

                //GlobalRain
                this.waterFluxTicker = rainCycle.world.game.globalRain.waterFluxTicker;
            }

            public override bool Equals(object obj)
            {
                if (obj is RainCycleData)
                {
                    var rainCycle = (RainCycleData)obj;
                    return (
                        this.cycleLength == rainCycle.cycleLength &&
                        this.timer == rainCycle.timer &&
                        this.preTimer == rainCycle.preTimer &&
                        this.antiGravity == rainCycle.antiGravity &&

                        this.stageDuration == rainCycle.stageDuration &&
                        this.timeInStage == rainCycle.timeInStage &&
                        this.stage == rainCycle.stage &&

                        this.waterFluxTicker == rainCycle.waterFluxTicker
                        );
                }
                return false;
            }
        }
    }
}

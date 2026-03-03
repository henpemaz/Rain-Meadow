using UnityEngine;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class TentacleState : OnlineState
    {
        [OnlineField]
        bool limp;
        [OnlineFieldHalf]
        Vector2 tipPos;
        [OnlineField]
        int backtrackFrom;

        public TentacleState() { }
        public TentacleState(Tentacle tentacle)
        {
            limp = tentacle.limp;
            tipPos = tentacle.Tip.pos;
            backtrackFrom = tentacle.backtrackFrom;
        }

        public void ReadTo(Tentacle tentacle)
        {
            tentacle.limp = limp;
            tentacle.Tip.pos = tipPos;
            tentacle.backtrackFrom = backtrackFrom;
        }
    }
}

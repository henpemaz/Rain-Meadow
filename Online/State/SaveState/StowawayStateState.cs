using MoreSlugcats;
using UnityEngine;

namespace RainMeadow
{
    public class StowawayStateState : CreatureStateState
    {
        [OnlineFieldHalf]
        Vector2 HomePos;
        [OnlineFieldHalf]
        Vector2 AimPos;

        public StowawayStateState() { }

        public StowawayStateState(OnlineCreature onlineCreature) : base(onlineCreature)
        {
            var abstractCreature = (AbstractCreature)onlineCreature.apo;
            var stowawayState = (StowawayBugState)abstractCreature.state;

            HomePos = stowawayState.HomePos;
            AimPos = stowawayState.HomePos;
        }

        public override void ReadTo(AbstractCreature abstractCreature)
        {
            base.ReadTo(abstractCreature);
            var stowawayState = (StowawayBugState)abstractCreature.state;

            stowawayState.HomePos = HomePos;
            stowawayState.aimPos = AimPos;
        }
    }
}

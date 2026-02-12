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
        [OnlineField]
        bool debugForceAwake;
        [OnlineField]
        int digestionTimeStart;
        [OnlineField]
        int digestionLength;

        public StowawayStateState() { }

        public StowawayStateState(OnlineCreature onlineCreature) : base(onlineCreature)
        {
            var abstractCreature = (AbstractCreature)onlineCreature.apo;
            var stowawayState = (StowawayBugState)abstractCreature.state;

            HomePos = stowawayState.HomePos;
            AimPos = stowawayState.aimPos;

            debugForceAwake = stowawayState.debugForceAwake;

            digestionTimeStart = stowawayState.digestionTimeStart;
            digestionLength = stowawayState.digestionLength;
        }

        public override void ReadTo(AbstractCreature abstractCreature)
        {
            base.ReadTo(abstractCreature);
            var stowawayState = (StowawayBugState)abstractCreature.state;

            stowawayState.HomePos = HomePos;
            stowawayState.aimPos = AimPos;

            stowawayState.debugForceAwake = debugForceAwake;

            stowawayState.digestionTimeStart = digestionTimeStart;
            stowawayState.digestionLength = digestionLength;
        }
    }
}

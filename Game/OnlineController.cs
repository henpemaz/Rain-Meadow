namespace RainMeadow
{
    public class OnlineController : Player.PlayerController
    {
        private OnlineEntity ent;
        private Player self;

        public OnlineController(OnlineEntity ent, Player self)
        {
            this.ent = ent;
            this.self = self;
        }

        public override Player.InputPackage GetInput()
        {
            var latestState = ent.lastStates[ent.currentlyJoinedResource];
            if (latestState is AbstractCreatureState ces && ces.realizedObjectState is RealizedPlayerState rps)
            {
                return rps.GetInput();
            }
            RainMeadow.Error($"no state for player {ent}");
            RainMeadow.Error($"reasons: {latestState is AbstractCreatureState} {latestState is AbstractCreatureState ces2 && ces2.realizedObjectState is RealizedPlayerState}");
            return base.GetInput();
        }
    }
}
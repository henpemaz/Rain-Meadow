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
            if(ent.latestState is AbstractCreatureState ces && ces.realizedObjectState is RealizedPlayerState rps)
            {
                return rps.GetInput();
            }
            RainMeadow.Error("no state for player");
            RainMeadow.Error($"reasons: {ent.latestState is AbstractCreatureState} {ent.latestState is AbstractCreatureState ces2 && ces2.realizedObjectState is RealizedPlayerState}");
            return base.GetInput();
        }
    }
}
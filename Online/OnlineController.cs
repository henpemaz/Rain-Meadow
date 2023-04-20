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
            if(ent.latestState is CreatureEntityState ces && ces.realizedObjectState is RealizedPlayerState rps)
            {
                return rps.GetInput();
            }
            RainMeadow.Error("no state for player");
            RainMeadow.Error($"reasons: {ent.latestState is CreatureEntityState} {ent.latestState is CreatureEntityState ces2 && ces2.realizedObjectState is RealizedPlayerState}");
            return base.GetInput();
        }
    }
}
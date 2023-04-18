namespace RainMeadow
{
    internal class OnlineController : Player.PlayerController
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
            if(ent.latestState is OnlineEntity.CreatureEntityState ces && ces.realizedObjectState is OnlineEntity.RealizedPlayerState rps)
            {
                return rps.GetInput();
            }
            RainMeadow.Error("no state for player");
            RainMeadow.Error($"reasons: {ent.latestState is OnlineEntity.CreatureEntityState} {ent.latestState is OnlineEntity.CreatureEntityState ces2 && ces2.realizedObjectState is OnlineEntity.RealizedPlayerState}");
            return base.GetInput();
        }
    }
}
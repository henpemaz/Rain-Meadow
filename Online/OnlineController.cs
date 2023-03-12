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
            if(ent.latestState is OnlineEntity.CreatureEntityState ces && ces.realizedState is OnlineEntity.RealizedPlayerState rps)
            {
                return rps.GetInput();
            }
            RainMeadow.Error("no state for player");
            return base.GetInput();
        }
    }
}
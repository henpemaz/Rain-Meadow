namespace RainMeadow
{
    partial class RainMeadow
    {
        public void GameplayHooks()
        {
            On.Player.ctor += Player_ctor;
        }

        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if(world.game.session is OnlineGameSession)
            {
                // remote player
                if (OnlineEntity.map.TryGetValue(self.abstractPhysicalObject, out var ent) && self.playerState.slugcatCharacter == Ext_SlugcatStatsName.OnlineSessionRemotePlayer)
                {
                    self.controller = new OnlineController(ent, self);
                }
            }
        }
    }
}

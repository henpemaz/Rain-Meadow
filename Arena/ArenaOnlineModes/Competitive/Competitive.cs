
namespace RainMeadow
{
    public class Competitive : InternalArenaGameMode
    {
        private int _timerDuration;  // Backing field for TimerDuration

        public override bool IsExitsOpen(On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ArenaBehaviors.ExitManager self)
        {
            var deadCount = 0;
            foreach (var player in self.gameSession.Players)
            {
                if (player.realizedCreature != null && (player.realizedCreature.State.dead || player.state.dead))
                {

                    deadCount++;
                }
            }

            if (deadCount != 0 && deadCount == self.gameSession.Players.Count - 1)
            {

                return true;
            }

            if (self.world.rainCycle.TimeUntilRain <= 100)
            {
                return true;
            }

            orig(self);
            return orig(self);
        }

        public override bool SpawnBatflies(FliesWorldAI self, int spawnRoom)
        {
            return false;
        }

        public override void InitAsGameType(On.ArenaSetup.GameTypeSetup.orig_InitAsGameType orig, ArenaSetup.GameTypeSetup self, ArenaSetup.GameTypeID gameType)
        {
           base.InitAsGameType(orig, self, gameType);
        }
        public override string TimerText()
        {
            return $"Prepare for combat, {SlugcatStats.getSlugcatName((OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>()).playingAs)}";
        }


        public override int TimerDuration
        {
            get { return _timerDuration; }
            set { _timerDuration = value; }
        }
    }
}

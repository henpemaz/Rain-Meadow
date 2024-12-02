using System.Linq;
namespace RainMeadow
{
    public class Competitive : ExternalArenaGameMode
    {

        public static ArenaSetup.GameTypeID CompetitiveMode = new ArenaSetup.GameTypeID("Free For All", register: true);

        private int _timerDuration;  // Backing field for TimerDuration

    

        public override bool IsExitsOpen(ArenaOnlineGameMode arena, On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ArenaBehaviors.ExitManager self)
        {
            int playersStillStanding = self.gameSession.Players?.Count(player =>
                player.realizedCreature != null &&
                (player.realizedCreature.State.alive)) ?? 0;

            if (playersStillStanding == 1 && arena.arenaSittingOnlineOrder.Count > 1)
            {
                return true;
            }

            return orig(self);
        }

        public override bool SpawnBatflies(FliesWorldAI self, int spawnRoom)
        {
            return false;
        }
        public override string TimerText()
        {
            return $"Prepare for combat, {SlugcatStats.getSlugcatName((OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>()).playingAs)}";
        }
        public override void InitAsCustomGameType(ArenaSetup.GameTypeSetup self)
        {
            self.foodScore = 1;
            self.survivalScore = 0;
            self.spearHitScore = 0;
            self.repeatSingleLevelForever = false;
            self.savingAndLoadingSession = true;
            self.denEntryRule = ArenaSetup.GameTypeSetup.DenEntryRule.Standard;
            self.rainWhenOnePlayerLeft = true;
            self.levelItems = true;
            self.fliesSpawn = true;
            self.saveCreatures = false;
        }

        public override int SetTimer(ArenaOnlineGameMode arena)
        {
            return arena.setupTime =  RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
        }
        public override int TimerDuration
        {
            get { return _timerDuration; }
            set { _timerDuration = value; }
        }
        public override int TimerDirection(int timer)
        {
            return --timer;
        }
        public override bool HoldFireWhileTimerIsActive(ArenaOnlineGameMode arena)
        {
            return arena.countdownInitiatedHoldFire = true;
        }
    }
}
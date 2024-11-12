
using System;

namespace RainMeadow
{
    public class Onslaught : InternalArenaGameMode
    {

        public static ArenaSetup.GameTypeID OnslaughtMode = new ArenaSetup.GameTypeID("Onslaught", register: true);

        public int onslaughtTimer = 3000;
        public int currentPoints;
        public int scoreToWin;
        private int _timerDuration;


        public override bool IsExitsOpen(On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ArenaBehaviors.ExitManager self)
        {
            if (currentPoints >= scoreToWin)
            {
                return true;
            }
            return false;
        }

        public override bool SpawnBatflies(FliesWorldAI self, int spawnRoom)
        {
            return true;
        }

        public void SetupRulesForOnslaught(ArenaOnlineGameMode arena)
        {
            arena.countdownInitiatedHoldFire = false;
        }

        public override void ArenaSessionCtor(ArenaOnlineGameMode arena, On.ArenaGameSession.orig_ctor orig, ArenaGameSession self, RainWorldGame game)
        {
            arena.countdownInitiatedHoldFire = false;
            scoreToWin = 1;
            currentPoints = 0;


        }

        public override void InitAsGameType(ArenaSetup.GameTypeSetup self)
        {
            self.foodScore = 1;
            self.survivalScore = 0;
            self.spearHitScore = 1;
            self.repeatSingleLevelForever = false;
            self.denEntryRule = ArenaSetup.GameTypeSetup.DenEntryRule.Standard;
            self.rainWhenOnePlayerLeft = false;
            self.levelItems = true;
            self.fliesSpawn = true;
        }

        public override string TimerText()
        {
            return $": Time until rain. Current points: {currentPoints}";
        }

        public override int SetTimer()
        {
            return TimerDuration = 3000;
        }

        public override int TimerDuration
        {
            get { return _timerDuration; }
            set { _timerDuration = value; }
        }
    }
}

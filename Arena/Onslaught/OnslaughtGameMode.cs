
namespace RainMeadow
{
    public class Onslaught : InternalArenaGameMode
    {

        public static ArenaSetup.GameTypeID OnslaughtMode = new ArenaSetup.GameTypeID("Onslaught", register: true);

        public int onslaughtTimer = 3000;
        public int currentPoints;
        public int scoreToWin;


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

}
}

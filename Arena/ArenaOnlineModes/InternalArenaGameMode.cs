

using System;

namespace RainMeadow
{
    public abstract class InternalArenaGameMode
    {

        public abstract bool IsExitsOpen(On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ArenaBehaviors.ExitManager self);
        public abstract bool SpawnBatflies(FliesWorldAI self, int spawnRoom);

        public abstract int TimerDuration { get; set; }

        public virtual void ArenaSessionCtor(ArenaOnlineGameMode arena, On.ArenaGameSession.orig_ctor orig, ArenaGameSession self, RainWorldGame game)
        {
            orig(self, game);
        }

        public virtual void InitAsGameType(On.ArenaSetup.GameTypeSetup.orig_InitAsGameType orig, ArenaSetup.GameTypeSetup self, ArenaSetup.GameTypeID gameType)
        {
            orig(self, gameType);
        }

        public virtual string TimerText()
        {
            return "";
        }

        public virtual int SetTimer()
        {
            return RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
        }

    }
}

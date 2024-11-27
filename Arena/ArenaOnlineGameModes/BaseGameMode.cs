using System;
using System.Collections.Generic;
using JollyCoop;
using Menu;
using RWCustom;
using UnityEngine;
namespace RainMeadow
{
    public abstract class InternalArenaGameMode
    {
        private int _timerDuration;

        public abstract bool IsExitsOpen(ArenaOnlineGameMode arena, On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ArenaBehaviors.ExitManager self);
        public abstract bool SpawnBatflies(FliesWorldAI self, int spawnRoom);

        public abstract int TimerDuration { get; set; }

        public virtual void ArenaSessionCtor(ArenaOnlineGameMode arena, On.ArenaGameSession.orig_ctor orig, ArenaGameSession self, RainWorldGame game)
        {
            orig(self, game);
        }

        public abstract void InitAsCustomGameType(ArenaSetup.GameTypeSetup self);

        public virtual string TimerText()
        {
            return "";
        }

        public virtual int SetTimer()
        {
            return RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
        }

        public virtual int TimerDirection(int timer)
        {
            return --timer;
        }
        public virtual void Killing(ArenaOnlineGameMode arena, On.ArenaGameSession.orig_Killing orig, ArenaGameSession self, Player player, Creature killedCrit, int playerIndex)
        {
            
        }

        public virtual void HUD_InitMultiplayerHud(ArenaOnlineGameMode arena, On.HUD.HUD.orig_InitMultiplayerHud orig, HUD.HUD self, ArenaGameSession session)
        {
            self.AddPart(new HUD.TextPrompt(self));
            self.AddPart(new ChatHud(self, session.game.cameras[0]));
            self.AddPart(new SpectatorHud(self, session.game.cameras[0]));
            self.AddPart(new ArenaPrepTimer(self, self.fContainers[0], arena, session));
            self.AddPart(new OnlineHUD(self, session.game.cameras[0], arena));
        }
    }
}
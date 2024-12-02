using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Menu;
namespace RainMeadow
{
    public abstract class ExternalArenaGameMode
    {
        private int _timerDuration;

        public abstract bool IsExitsOpen(ArenaOnlineGameMode arena, On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ArenaBehaviors.ExitManager self);
        public abstract bool SpawnBatflies(FliesWorldAI self, int spawnRoom);

        public abstract int TimerDuration { get; set; }

        public virtual void ArenaSessionCtor(ArenaOnlineGameMode arena, On.ArenaGameSession.orig_ctor orig, ArenaGameSession self, RainWorldGame game)
        {
           
        }

        public abstract void InitAsCustomGameType(ArenaSetup.GameTypeSetup self);

        public virtual string TimerText()
        {
            return "";
        }

        public virtual int SetTimer(ArenaOnlineGameMode arena)
        {
            return arena.setupTime = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
        }

        public virtual void ResetGameTimer()
        {
            _timerDuration = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
            
        }

        public virtual int TimerDirection(int timer)
        {
            return timer--;
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
        public virtual void ArenaCreatureSpawner_SpawnCreatures(ArenaOnlineGameMode arena, On.ArenaCreatureSpawner.orig_SpawnArenaCreatures orig, RainWorldGame game, ArenaSetup.GameTypeSetup.WildLifeSetting wildLifeSetting, ref List<AbstractCreature> availableCreatures, ref MultiplayerUnlocks unlocks)
        {

        }

        public virtual void ArenaImage(ArenaOnlineGameMode arena, On.Menu.MultiplayerMenu.orig_ArenaImage orig, Menu.MultiplayerMenu self, SlugcatStats.Name classID, int color)
        {

        }

        public virtual bool HoldFireWhileTimerIsActive(ArenaOnlineGameMode arena)
        {
            return arena.countdownInitiatedHoldFire;
        }

    }
}
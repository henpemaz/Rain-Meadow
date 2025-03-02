using RainMeadow;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RainMeadow
{
    public class Competitive : ExternalArenaGameMode
    {

        public static ArenaSetup.GameTypeID CompetitiveMode = new ArenaSetup.GameTypeID("Free For All", register: false);

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

            if (self.world.rainCycle.TimeUntilRain <= 100)
            {
                return true;
            }

            orig(self);

            return orig(self);
        }

        public override bool SpawnBatflies(FliesWorldAI self, int spawnRoom)
        {
            return true;
        }
        public override string TimerText()
        {
            if (ModManager.MSC && (OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>()).playingAs == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
            {
                return $"Prepare for combat, {(OnlineManager.lobby.gameMode as ArenaOnlineGameMode).paincatName}";
            }
            return $"Prepare for combat, {SlugcatStats.getSlugcatName((OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>()).playingAs)}";
        }

        public override string FormatTimer(float time)
        {
            int seconds = Mathf.FloorToInt(time / 60);
            return $"{seconds:D1}";
        }

        public override int SetTimer(ArenaOnlineGameMode arena)
        {
            return arena.setupTime = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
        }
        public override int TimerDuration
        {
            get { return _timerDuration; }
            set { _timerDuration = value; }
        }
        public override int TimerDirection(ArenaOnlineGameMode arena, int timer)
        {            
            return arena.setupTime - 60;
        }
        public override bool HoldFireWhileTimerIsActive(ArenaOnlineGameMode arena)
        {
            if (arena.setupTime > 0)
            {
                return arena.countdownInitiatedHoldFire = true;
            }
            else
            {
                return arena.countdownInitiatedHoldFire = false;
            }
        }

        public override void LandSpear(ArenaOnlineGameMode arena, ArenaGameSession self, Player player, Creature target, ArenaSitting.ArenaPlayer aPlayer)
        {
            aPlayer.AddSandboxScore(self.GameTypeSetup.spearHitScore);

        }

        public override void ArenaSessionCtor(ArenaOnlineGameMode arena, On.ArenaGameSession.orig_ctor orig, ArenaGameSession self, RainWorldGame game)
        {
            base.ArenaSessionCtor(arena, orig, self, game);
            arena.ResetInvDetails();
        }
    }
}
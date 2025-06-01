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
            return false;
        }
        public override string TimerText()
        {
            var client_settings = OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>();
            
            SlugcatStats.Name playingAs;
            if (client_settings.playingAs != RainMeadow.Ext_SlugcatStatsName.OnlineRandomSlugcat) {
                playingAs = client_settings.playingAs;
            } else {
                playingAs = client_settings.randomPlayingAs ?? SlugcatStats.Name.White;
            }

            if (ModManager.MSC && playingAs == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
            {
                return Utils.Translate($"Prepare for combat,") + " " + Utils.Translate((OnlineManager.lobby.gameMode as ArenaOnlineGameMode)?.paincatName ?? "");
            }
            
            return Utils.Translate("Prepare for combat,") + " " + Utils.Translate(SlugcatStats.getSlugcatName(playingAs));
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
            return --arena.setupTime;
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
        }
    }
}

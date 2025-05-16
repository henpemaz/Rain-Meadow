using RainMeadow;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
namespace RainMeadow
{
    public class TeamBattleMode : ExternalArenaGameMode
    {

        public static ArenaSetup.GameTypeID TeamBattle = new ArenaSetup.GameTypeID("Team Battle", register: false);

        private int _timerDuration;  // Backing field for TimerDuration

        public List<OnlinePlayer> ChieftainPlayers = new();
        public List<OnlinePlayer> Dragonslayers = new();
        public enum TeamMappings
        {
            Martyrs,
            Outlaws,
            Dragonslayers,
            Chieftains
        }
        public static Dictionary<TeamMappings, string> TeamMappingsDictionary = new Dictionary<TeamMappings, string>
        {
            { TeamMappings.Martyrs, "yes" },
            { TeamMappings.Outlaws, "no" },
            { TeamMappings.Dragonslayers, "Kill_Slugcat" },
            { TeamMappings.Chieftains, "ChieftainA" }
    };

        public static Dictionary<TeamMappings, Color> TeamColors = new Dictionary<TeamMappings, Color>
        {
            { TeamMappings.Martyrs, Color.red },
            { TeamMappings.Outlaws, Color.yellow },
            { TeamMappings.Dragonslayers, Color.magenta },
            { TeamMappings.Chieftains, Color.blue }
    };

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
            if (ModManager.MSC && (OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>()).playingAs == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
            {
                return Utils.Translate($"Prepare for combat,") + " " + Utils.Translate((OnlineManager.lobby.gameMode as ArenaOnlineGameMode)?.paincatName ?? "");
            }
            return Utils.Translate("Prepare for combat,") + " " + Utils.Translate(SlugcatStats.getSlugcatName((OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>()).playingAs));
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
            if (this.ChieftainPlayers.Contains(OnlineManager.mePlayer))
            {
                this.ChieftainPlayers.Add(OnlineManager.mePlayer);
            }

        }


        public override string AddCustomIcon(ArenaOnlineGameMode arena, PlayerSpecificOnlineHud onlineHud)
        {
            if ((arena.onlineArenaGameMode as TeamBattleMode).ChieftainPlayers.Contains(OnlineManager.mePlayer))
            {
                return TeamMappingsDictionary[TeamMappings.Chieftains];
            }
            if ((arena.onlineArenaGameMode as TeamBattleMode).Dragonslayers.Contains(OnlineManager.mePlayer))
            {
                return TeamMappingsDictionary[TeamMappings.Dragonslayers];
            }
            return "";
        }
    }
}

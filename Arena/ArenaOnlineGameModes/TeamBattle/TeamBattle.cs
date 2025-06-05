using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
namespace RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle
{

    public class Team
    {
        public string teamName = "";
        public Color teamColor;

    }
    public class TeamBattleMode : ExternalArenaGameMode
    {

        public static ArenaSetup.GameTypeID TeamBattle = new ArenaSetup.GameTypeID("Team Battle", register: false);

        public override ArenaSetup.GameTypeID GameModeSetups
        {
            get
            {
                return TeamBattle;
            }
        }

        public static bool isTeamBattleMode(ArenaOnlineGameMode arena, out TeamBattleMode tb)
        {
            tb = null;
            if (arena.currentGameMode == TeamBattle.value)
            {
                tb = (arena.registeredGameModes.FirstOrDefault(x => x.Key == TeamBattle.value).Value as TeamBattleMode);
                return true;
            }
            return false;
        }



        private int _timerDuration;

        public Generics.DynamicOrderedPlayerIDs martyrs = new Generics.DynamicOrderedPlayerIDs();
        public Generics.DynamicOrderedPlayerIDs outlaws = new Generics.DynamicOrderedPlayerIDs();
        public Generics.DynamicOrderedPlayerIDs dragonslayers = new Generics.DynamicOrderedPlayerIDs();
        public Generics.DynamicOrderedPlayerIDs chieftains = new Generics.DynamicOrderedPlayerIDs();

        public int winningTeam = -1;

        public enum TeamMappings
        {
            Martyrs,
            Outlaws,
            Dragonslayers,
            Chieftains
        }

        public static List<TeamMappings> teamMappingsList = new List<TeamMappings>
    {
        TeamMappings.Martyrs,
        TeamMappings.Outlaws,
        TeamMappings.Dragonslayers,
        TeamMappings.Chieftains
    };
        public static Dictionary<TeamMappings, string> TeamMappingsDictionary = new Dictionary<TeamMappings, string>
        {
            { TeamMappings.Martyrs, "SaintA" },
            { TeamMappings.Outlaws, "OutlawA" },
            { TeamMappings.Dragonslayers, "Kill_Yellow_Lizard" },
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
                player.realizedCreature.State.alive) ?? 0;

            if (playersStillStanding == 1 && arena.arenaSittingOnlineOrder.Count > 1)
            {
                return true;
            }

            if (self.world.rainCycle.TimeUntilRain <= 100)
            {
                return true;
            }

            if (playersStillStanding > 1 && arena.setupTime == 0)
            {
                if (self.gameSession.Players != null)
                {
                    foreach (var acPlayer in self.gameSession.Players)
                    {
                        if (acPlayer != null)
                        {
                            if (acPlayer.state.alive)
                            {
                                var player = acPlayer?.GetOnlineObject().owner;
                                if (player != null)
                                {
                                    if (OnlineManager.lobby.clientSettings[player].TryGetData<ArenaTeamClientSettings>(out var tb2) && OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].TryGetData<ArenaTeamClientSettings>(out var tb1))
                                    {
                                        if (tb1.team != tb2.team)
                                        {
                                            return false;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return true;

                }
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

            if (ModManager.MSC && OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>().playingAs == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
            {

                return Utils.Translate($"Prepare for war, {Utils.Translate((OnlineManager.lobby.gameMode as ArenaOnlineGameMode)?.paincatName ?? "")}");
            }
            return Utils.Translate($"Prepare for war, {Utils.Translate(SlugcatStats.getSlugcatName(OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>().playingAs))}");
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

            // TODO: Remove this testing

            if (OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].TryGetData<ArenaTeamClientSettings>(out var tb))
            {
                int teamNumber = UnityEngine.Random.Range(0, 4);
                tb.team = (int)(TeamMappings)teamNumber;

            }

        }


        public override string AddCustomIcon(ArenaOnlineGameMode arena, PlayerSpecificOnlineHud onlineHud)
        {

            if (OnlineManager.lobby.clientSettings[onlineHud.clientSettings.owner].TryGetData<ArenaTeamClientSettings>(out var tb2))
            {
                return TeamMappingsDictionary[(TeamMappings)tb2.team];
            }
            return "";
        }
    }
}

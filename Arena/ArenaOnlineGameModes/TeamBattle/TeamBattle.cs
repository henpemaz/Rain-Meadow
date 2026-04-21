using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ArenaMode = RainMeadow.ArenaOnlineGameMode;

namespace RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle
{
    public partial class TeamBattleMode : ExternalArenaGameMode
    {
        public static ArenaSetup.GameTypeID TeamBattle = new ArenaSetup.GameTypeID(
            "Team Battle",
            register: false
        );

        public override ArenaSetup.GameTypeID GetGameModeId => TeamBattle;

        public static bool isTeamBattleMode(ArenaOnlineGameMode arena, out TeamBattleMode tb)
        {
            tb = null;
            if (arena.currentGameMode == TeamBattle.value)
            {
                tb = (
                    arena.registeredGameModes.FirstOrDefault(x => x.Key == TeamBattle.value).Value
                    as TeamBattleMode
                );
                return true;
            }
            return false;
        }

        private int _timerDuration;

        // used for finalresult organization
        public static Dictionary<int, int> teamScores = new();
        public static Dictionary<int, int> playerToTeam = new(); // Cache for sorting

        public static void ClearSortingDictionaries()
        {
            teamScores.Clear();
            playerToTeam.Clear();
        }

        public override void ResetOnSessionEnd()
        {
            winningTeam = -1;
            martyrsSpawn = 0;
            outlawsSpawn = 0;
            dragonslayersSpawn = 0;
            chieftainsSpawn = 0;
            roundSpawnPointCycler = 0;

            TeamBattleMode.ClearSortingDictionaries();

        }

        public override bool IsExitsOpen(
            ArenaOnlineGameMode arena,
            On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig,
            ArenaBehaviors.ExitManager self
        )
        {
            if (self.gameSession.GameTypeSetup.denEntryRule == ArenaSetup.GameTypeSetup.DenEntryRule.Always)
            {
                // idk why orig ignores this when 2 player exists
                return true;
            }

            if (self.gameSession.GameTypeSetup.denEntryRule == ArenaSetup.GameTypeSetup.DenEntryRule.Score)
            {
                return orig(self) || (self.gameSession?.arenaSitting?.players?.Any(p => p?.score >= arena.denScore) ?? false);
            }

            int playersStillStanding =
                self.gameSession.Players?.Count(player =>
                    player.realizedCreature != null && player.realizedCreature.State.alive
                ) ?? 0;

            if (
                playersStillStanding == 1
                && arena.arenaSittingOnlineOrder.Count > 1
                && !arena.countdownInitiatedHoldFire
            )
            {
                return true;
            }

            if (self.world.rainCycle.TimeUntilRain <= 100)
            {
                return true;
            }

            if (playersStillStanding > 1 && arena.setupTime == 0)
            {
                HashSet<int> aliveTeams = new HashSet<int>();
                if (self.gameSession.Players != null)
                {
                    foreach (var acPlayer in self.gameSession.Players)
                    {
                        if (acPlayer != null)
                        {
                            OnlinePhysicalObject? onlineP = acPlayer.GetOnlineObject();
                            if (onlineP != null)
                            {
                                bool gotPlayerTeam = OnlineManager.lobby.clientSettings.TryGetValue(
                                    onlineP.owner,
                                    out var onlineClientP
                                );
                                if (gotPlayerTeam)
                                {
                                    onlineClientP.TryGetData<ArenaTeamClientSettings>(
                                        out var playerTeam
                                    );
                                    if (gotPlayerTeam)
                                    {
                                        if (acPlayer.realizedCreature != null)
                                        {
                                            if (acPlayer.realizedCreature.State.alive)
                                            {
                                                aliveTeams.Add(playerTeam.team);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (aliveTeams.Count == 1)
                    {
                        if (self.gameSession.game.world.rainCycle.speedUpToRain == false)
                        {
                            RainMeadow.Debug("Team Battle: Adding rain");
                            self.gameSession.game.world.rainCycle.ArenaEndSessionRain();
                        }
                        return true;
                    }
                }
            }
            return orig(self);
        }

        public override bool SpawnBatflies(FliesWorldAI self, int spawnRoom)
        {
            return false;
        }

        public override string TimerText()
        {
            return Utils.Translate("Prepare for war,") + " " + Utils.Translate(PlayingAsText());
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

        public override void LandSpear(
            ArenaOnlineGameMode arena,
            ArenaGameSession self,
            Player player,
            Creature target,
            ArenaSitting.ArenaPlayer aPlayer
        )
        {
            aPlayer.AddSandboxScore(self.GameTypeSetup.spearHitScore);
        }

        public override void ArenaSessionCtor(
            ArenaOnlineGameMode arena,
            On.ArenaGameSession.orig_ctor orig,
            ArenaGameSession self,
            RainWorldGame game
        )
        {
            base.ArenaSessionCtor(arena, orig, self, game);
            if (TeamBattleMode.isTeamBattleMode(arena, out var tb))
            {
                if (
                    OnlineManager
                        .lobby.clientSettings[OnlineManager.mePlayer]
                        .TryGetData<ArenaTeamClientSettings>(out var t)
                )
                {
                    arena.avatarSettings.bodyColor = Color.Lerp(
                        arena.avatarSettings.bodyColor,
                        teamColors[t.team],
                        tb.lerp
                    );
                }
            }
        }

        public static int CalculateTeamScoresAndWinner(
    IEnumerable<ArenaSitting.ArenaPlayer> players,
    ArenaMode arena,
    bool winByScore, bool winByRoundScore, bool finalOverlay)
        {
            HashSet<int> teamsRemaining = new HashSet<int>();

            int finalOverlayWinner = -1;

            foreach (var player in players)
            {
                OnlinePlayer pl = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, player.playerNumber);
                if (pl == null) continue;

                if (OnlineManager.lobby.clientSettings.TryGetValue(pl, out var clientSettings) &&
                    clientSettings.TryGetData<ArenaTeamClientSettings>(out var teamSettings))
                {
                    int team = teamSettings.team;

                    if (player.alive)
                    {
                        teamsRemaining.Add(team);
                    }

                    arena.ReadFromStats(player, pl);
                    playerToTeam[player.playerNumber] = team; // Cache team assignment

                    if (winByScore)
                    {
                        if (!teamScores.ContainsKey(team))
                        {
                            teamScores[team] = 0;
                        }

                        // Sum scores
                        teamScores[team] += winByRoundScore ? player.score : player.totScore;
                    }
                    else if (finalOverlay)
                    {
                        var topPlayer = players.OrderByDescending(p => p.wins).FirstOrDefault();
                        if (topPlayer != null && playerToTeam.TryGetValue(topPlayer.playerNumber, out int teamId))
                        {
                            finalOverlayWinner = teamId;
                        }
                    }
                }
            }

            if (!winByScore)
            {
                if (finalOverlay)
                {
                    return finalOverlayWinner;
                }
                // If exactly one team is left, they win. 
                if (teamsRemaining.Count == 1)
                {
                    return teamsRemaining.First();
                }
                return -1;
            }

            if (teamScores.Count == 0) return -1;

            var sortedTeams = teamScores.Keys.ToList();
            sortedTeams.Sort((t1, t2) => teamScores[t2].CompareTo(teamScores[t1]));

            int topTeam = sortedTeams[0];
            int topScore = teamScores[topTeam];

            if (sortedTeams.Count > 1)
            {
                int secondTeam = sortedTeams[1];
                if (topScore == teamScores[secondTeam])
                {
                    return -1; // Draw
                }
            }

            if (topScore == 0)
            {
                return -1; // Draw
            }

            return topTeam;
        }

        public override bool PlayerSittingResultSort(
            ArenaMode arena,
            On.ArenaSitting.orig_PlayerSittingResultSort orig,
            ArenaSitting self,
            ArenaSitting.ArenaPlayer A,
            ArenaSitting.ArenaPlayer B
        )
        {
            if (isTeamBattleMode(arena, out var tb))
            {
                OnlinePlayer? playerA = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(
                    arena,
                    A.playerNumber
                );
                OnlinePlayer? playerB = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(
                    arena,
                    B.playerNumber
                );

                if (playerA != null && playerB != null)
                {
                    OnlineManager
                        .lobby.clientSettings[playerA]
                        .TryGetData<ArenaTeamClientSettings>(out var teamA);
                    OnlineManager
                        .lobby.clientSettings[playerB]
                        .TryGetData<ArenaTeamClientSettings>(out var teamB);

                    if (teamA != null && teamB != null)
                    {
                        bool aIsWinningTeam = teamA.team == tb.winningTeam;
                        bool bIsWinningTeam = teamB.team == tb.winningTeam;

                        // Prioritize winning team
                        if (aIsWinningTeam != bIsWinningTeam)
                        {
                            return aIsWinningTeam; // If A is on winning team and B is not, A comes first
                        }

                        if (aIsWinningTeam && bIsWinningTeam)
                        {
                            return A.score > B.score; // If both are on winning team, sort by kill value
                        }
                    }
                }
            }

            return orig(self, A, B);
        }

        public override List<ArenaSitting.ArenaPlayer> FinalSittingResult(ArenaMode arena, On.ArenaSitting.orig_FinalSittingResult orig, ArenaSitting self)
        {
            var resultList = orig(self);

            if (TeamBattleMode.isTeamBattleMode(arena, out var tb))
            {
                tb.winningTeam = CalculateTeamScoresAndWinner(resultList, arena, arena.winByScore, false, true);

                resultList.Sort((a, b) =>
                {
                    OnlinePlayer? playerA = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, a.playerNumber);
                    OnlinePlayer? playerB = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, b.playerNumber);


                    if (playerA != null && playerB != null)
                    {
                        OnlineManager.lobby.clientSettings[playerA].TryGetData<ArenaTeamClientSettings>(out var teamA);
                        OnlineManager.lobby.clientSettings[playerB].TryGetData<ArenaTeamClientSettings>(out var teamB);


                        // --- Tier 1: Winner Status ---
                        // If there is a winning team, anyone on that team goes to the top.
                        if (tb.winningTeam != -1)
                        {
                            bool aIsWinner = teamA.team == tb.winningTeam;
                            bool bIsWinner = teamB.team == tb.winningTeam;

                            if (aIsWinner != bIsWinner)
                            {
                                return aIsWinner ? -1 : 1;
                            }
                        }
                    }

                    // --- Tier 2: Individual Performance ---
                    // This sorts teammates against each other, AND sorts all losers against each other.
                    int indStatA = arena.winByScore ? a.totScore : a.wins;
                    int indStatB = arena.winByScore ? b.totScore : b.wins;

                    if (indStatA != indStatB)
                        return indStatB.CompareTo(indStatA);

                    return a.deaths.CompareTo(b.deaths); // Fewer deaths first
                });
            }

            return resultList;
        }

        public override bool PlayerSessionResultSort(
            ArenaMode arena,
            On.ArenaSitting.orig_PlayerSessionResultSort orig,
            ArenaSitting self,
            ArenaSitting.ArenaPlayer A,
            ArenaSitting.ArenaPlayer B
        )
        {
            if (isTeamBattleMode(arena, out var tb))
            {
                OnlinePlayer? playerA = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, A.playerNumber);
                OnlinePlayer? playerB = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, B.playerNumber);

                if (playerA != null && playerB != null)
                {
                    OnlineManager.lobby.clientSettings[playerA].TryGetData<ArenaTeamClientSettings>(out var teamA);
                    OnlineManager.lobby.clientSettings[playerB].TryGetData<ArenaTeamClientSettings>(out var teamB);

                    if (teamA != null && teamB != null)
                    {
                        // Only consider them on the winning team if a winning team was actually decided (!= -1)
                        bool aIsWinningTeam = (tb.winningTeam != -1) && (teamA.team == tb.winningTeam);
                        bool bIsWinningTeam = (tb.winningTeam != -1) && (teamB.team == tb.winningTeam);

                        // Prioritize winning team
                        if (aIsWinningTeam != bIsWinningTeam)
                        {
                            return aIsWinningTeam; // If A is on winning team and B is not, A comes first
                        }

                        // If both are on the winning team (or both lost), sort by performance
                        if (A.alive != B.alive)
                        {
                            return A.alive;
                        }
                        if (A.score != B.score)
                        {
                            return A.score > B.score; // Sort by score
                        }

                        return A.deaths < B.deaths; // Sort by fewest deaths
                    }
                }
            }

            return orig(self, A, B);
        }

        public override void ArenaSessionNextLevel(ArenaMode arena, On.ArenaSitting.orig_NextLevel orig, ArenaSitting self, ProcessManager process)
        {
            base.ArenaSessionNextLevel(arena, orig, self, process);
            ClearSortingDictionaries();
        }

        public override void ArenaSessionEnded(
            ArenaOnlineGameMode arena,
            On.ArenaSitting.orig_SessionEnded orig,
            ArenaSitting self,
            ArenaGameSession session
        )
        {
            base.ArenaSessionEnded(arena, orig, self, session);
            if (TeamBattleMode.isTeamBattleMode(arena, out var tb) && OnlineManager.lobby.isOwner)
            {
                tb.roundSpawnPointCycler = tb.roundSpawnPointCycler + 1;
            }
        }

        public override void SpawnPlayer(
            ArenaOnlineGameMode arena,
            ArenaGameSession self,
            Room room,
            List<int> suggestedDens
        )
        {
            // Shameful copy-paste
            if (isTeamBattleMode(arena, out var teamBattleMode))
            {
                List<OnlinePlayer> list = new List<OnlinePlayer>();

                List<OnlinePlayer> list2 = new List<OnlinePlayer>();

                for (int j = 0; j < OnlineManager.players.Count; j++)
                {
                    if (arena.arenaSittingOnlineOrder.Contains(OnlineManager.players[j].inLobbyId))
                    {
                        list2.Add(OnlineManager.players[j]);
                    }
                }

                while (list2.Count > 0)
                {
                    int index = UnityEngine.Random.Range(0, list2.Count);
                    list.Add(list2[index]);
                    list2.RemoveAt(index);
                }
                int randomExitIndex = 0;
                int totalExits = self.game.world.GetAbstractRoom(0).exits;
                teamBattleMode.roundSpawnPointCycler = (
                    teamBattleMode.roundSpawnPointCycler % totalExits
                );

                if (
                    OnlineManager
                        .lobby.clientSettings[OnlineManager.mePlayer]
                        .TryGetData<ArenaTeamClientSettings>(out var teamSettings)
                )
                {
                    teamBattleMode.martyrsSpawn =
                        (
                            (int)TeamSpawnPoints.martyrsTeamName
                            + teamBattleMode.roundSpawnPointCycler
                        ) % totalExits;
                    teamBattleMode.outlawsSpawn =
                        ((int)TeamSpawnPoints.outlawTeamName + teamBattleMode.roundSpawnPointCycler)
                        % totalExits;
                    teamBattleMode.dragonslayersSpawn =
                        (
                            (int)TeamSpawnPoints.dragonslayersTeamName
                            + teamBattleMode.roundSpawnPointCycler
                        ) % totalExits;
                    teamBattleMode.chieftainsSpawn =
                        (
                            (int)TeamSpawnPoints.chieftainsTeamName
                            + teamBattleMode.roundSpawnPointCycler
                        ) % totalExits;

                    switch ((TeamSpawnPoints)teamSettings.team)
                    {
                        case TeamSpawnPoints.martyrsTeamName:
                            randomExitIndex = teamBattleMode.martyrsSpawn;
                            break;
                        case TeamSpawnPoints.outlawTeamName:
                            randomExitIndex = teamBattleMode.outlawsSpawn;
                            break;
                        case TeamSpawnPoints.dragonslayersTeamName:
                            randomExitIndex = teamBattleMode.dragonslayersSpawn;
                            break;
                        case TeamSpawnPoints.chieftainsTeamName:
                            randomExitIndex = teamBattleMode.chieftainsSpawn;
                            break;
                        default:
                            Debug.LogWarning(
                                "Current player's team is not recognized for spawn point assignment."
                            );
                            randomExitIndex = 0;
                            break;
                    }
                    if (OnlineManager.lobby.isOwner)
                    {
                        foreach (var player in OnlineManager.players)
                        {
                            if (player.isMe)
                            {
                                continue; //
                            }
                            player.InvokeOnceRPC(
                                ArenaRPCs.Arena_NotifySpawnPoint,
                                teamBattleMode.martyrsSpawn,
                                teamBattleMode.outlawsSpawn,
                                teamBattleMode.dragonslayersSpawn,
                                teamBattleMode.chieftainsSpawn
                            );
                        }
                    }
                }

                if (
                    ArenaHelpers.GetArenaClientSettings(OnlineManager.mePlayer)!.playingAs
                    == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator
                )
                {
                    RainMeadow.Debug("Player spawned as Overseer");
                    if (arena.enableOverseer)
                    {
                        SpawnTransferableCreature(
                            arena,
                            self,
                            room,
                            randomExitIndex,
                            CreatureTemplate.Type.Overseer
                        );
                    }
                }
                else
                {
                    SpawnNonTransferableCreature(
                        arena,
                        self,
                        room,
                        randomExitIndex,
                        CreatureTemplate.Type.Slugcat
                    );
                }

                self.playersSpawned = true;
                if (OnlineManager.lobby.isOwner)
                {
                    arena.isInGame = true; // used for readied players at the beginning
                    arena.leaveForNextLevel = false;

                    foreach (var onlineArenaPlayer in arena.arenaSittingOnlineOrder)
                    {
                        OnlinePlayer? getPlayer = ArenaHelpers.FindOnlinePlayerByLobbyId(
                            onlineArenaPlayer
                        );
                        if (getPlayer != null)
                        {
                            arena.CheckToAddPlayerStatsToDicts(getPlayer);
                        }
                    }
                    arena.playersLateWaitingInLobbyForNextRound.Clear();
                    arena.hasPermissionToRejoin = false;
                }
            }
        }

        public override string AddIcon(
            ArenaOnlineGameMode arena,
            OnlinePlayerDisplay display,
            PlayerSpecificOnlineHud owner,
            SlugcatCustomization customization,
            OnlinePlayer player
        )
        {

            if (base.AddIcon(arena, display, owner, customization, player) != "")
            {
                return base.AddIcon(arena, display, owner, customization, player);
            }

            if (OnlineManager.lobby.clientSettings.TryGetValue(key: player, out _) == false)
            {
                return "";
            }

            if (
                OnlineManager
                    .lobby.clientSettings[player]
                    .TryGetData<ArenaTeamClientSettings>(out var tb2)
            )
            {
                return teamIcons[tb2.team];
            }
            return "";
        }

        public override Color IconColor(
            ArenaOnlineGameMode arena,
            OnlinePlayerDisplay display,
            PlayerSpecificOnlineHud owner,
            SlugcatCustomization customization,
            OnlinePlayer player
        )
        {
            if (OnlineManager.lobby.clientSettings.TryGetValue(key: player, out _) == false)
            {
                return customization.bodyColor;
            }

            if (owner.PlayerConsideredDead)
            {
                return Color.grey;
            }

            if (
                OnlineManager
                    .lobby.clientSettings[player]
                    .TryGetData<ArenaTeamClientSettings>(out var tb2)
            )
            {
                return teamColors[tb2.team];
            }

            return customization.bodyColor;
        }
    }
}

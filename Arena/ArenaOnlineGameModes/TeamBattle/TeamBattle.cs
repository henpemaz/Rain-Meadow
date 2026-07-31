using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle
{
    public partial class TeamBattleMode : ExternalArenaGameMode
    {
        public static ArenaSetup.GameTypeID TeamBattle = new("Team Battle");

        public override ArenaSetup.GameTypeID GetGameModeId => TeamBattle;
        private int _timerDuration;
        public override int TimerDuration
        {
            get { return _timerDuration; }
            set { _timerDuration = value; }
        }
        // used for finalresult organization
        public Dictionary<int, int> teamScores = [];
        public Dictionary<int, int> playerToTeam = []; // Cache for sorting

        public static bool IsTeamBattleMode(out TeamBattleMode teamBattle)
        {
            teamBattle = null!;

            if (!RainMeadow.isArenaMode(out ArenaOnlineGameMode arenaOnline))
                return false;

            if (arenaOnline.registeredGameModes.TryGetValue(TeamBattle.value, out ExternalArenaGameMode externalArena)
                && arenaOnline.currentGameMode == TeamBattle.value)
            {
                teamBattle = (TeamBattleMode)externalArena;
                return true;
            }

            return false;
        }

        public void ClearSortingDictionaries()
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

            ClearSortingDictionaries();

        }

        public override bool IsExitsOpen(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig,
            ArenaBehaviors.ExitManager self)
        {
            if (self.gameSession.GameTypeSetup.denEntryRule == ArenaSetup.GameTypeSetup.DenEntryRule.Always)
            {
                // idk why orig ignores this when 2 player exists
                return true;
            }

            if (self.gameSession.GameTypeSetup.denEntryRule == ArenaSetup.GameTypeSetup.DenEntryRule.Score)
            {
                return orig(self) || (self.gameSession?.arenaSitting?.players?.Any(p => p?.score >= arenaOnline.denScore) ?? false);
            }

            int playersStillStanding =
                self.gameSession.Players?.Count(player =>
                    player.realizedCreature != null && player.realizedCreature.State.alive
                ) ?? 0;

            if (
                playersStillStanding == 1
                && arenaOnline.arenaSittingOnlineOrder.Count > 1
                && !arenaOnline.countdownInitiatedHoldFire
            )
            {
                return true;
            }

            if (self.world.rainCycle.TimeUntilRain <= 100)
            {
                return true;
            }

            if (playersStillStanding > 1 && arenaOnline.setupTime == 0)
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

        public override int SetTimer(ArenaOnlineGameMode arenaOnline)
        {
            return arenaOnline.setupTime = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
        }

        public override int TimerDirection(ArenaOnlineGameMode arenaOnline, int timer)
        {
            return --arenaOnline.setupTime;
        }

        public override bool HoldFireWhileTimerIsActive(ArenaOnlineGameMode arenaOnline)
        {
            if (arenaOnline.setupTime > 0)
            {
                return arenaOnline.countdownInitiatedHoldFire = true;
            }
            else
            {
                return arenaOnline.countdownInitiatedHoldFire = false;
            }
        }

        public override void ArenaSessionCtor(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaGameSession.orig_ctor orig,
            ArenaGameSession self,
            RainWorldGame game)
        {
            base.ArenaSessionCtor(arenaOnline, orig, self, game);
            if (IsTeamBattleMode(out TeamBattleMode teamBattle))
            {
                if (
                    OnlineManager
                        .lobby.clientSettings[OnlineManager.mePlayer]
                        .TryGetData<ArenaTeamClientSettings>(out var t)
                )
                {
                    arenaOnline.avatarSettings.bodyColor = Color.Lerp(
                        arenaOnline.avatarSettings.bodyColor,
                        teamColors[t.team],
                        teamBattle.lerp
                    );
                }
            }
            ClearSortingDictionaries();
        }

        public int CalculateTeamScoresAndWinner(
            IEnumerable<ArenaSitting.ArenaPlayer> players,
            ArenaOnlineGameMode arenaOnline,
            bool WinByScore,
            bool winByRoundScore,
            bool finalOverlay)
        {
            HashSet<int> teamsRemaining = new HashSet<int>();
            int finalOverlayWinner = -1;

            foreach (var player in players)
            {
                OnlinePlayer pl = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arenaOnline, player.playerNumber);
                if (pl == null) continue;

                if (OnlineManager.lobby.clientSettings.TryGetValue(pl, out var clientSettings) &&
                    clientSettings.TryGetData<ArenaTeamClientSettings>(out var teamSettings))
                {
                    int team = teamSettings.team;

                    if (player.alive)
                    {
                        teamsRemaining.Add(team);
                    }

                    arenaOnline.ReadFromStats(player, pl);
                    playerToTeam[player.playerNumber] = team; // Cache team assignment

                    if (WinByScore)
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
                        int maxWins = players.Max(p => p.wins);

                        var teamsWithMaxWins = players
                            .Where(p => p.wins == maxWins)
                            .Select(p => playerToTeam.TryGetValue(p.playerNumber, out int t) ? t : -1)
                            .Where(t => t != -1) // Filter out players not assigned to a team
                            .Distinct()
                            .ToList();

                        // 3. Return winner or tie in finalResult
                        finalOverlayWinner = teamsWithMaxWins.Count == 1 ? teamsWithMaxWins[0] : -1;
                    }
                }
            }

            if (!WinByScore)
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
            ArenaOnlineGameMode arenaOnline,
            On.ArenaSitting.orig_PlayerSittingResultSort orig,
            ArenaSitting self,
            ArenaSitting.ArenaPlayer A,
            ArenaSitting.ArenaPlayer B)
        {
            if (IsTeamBattleMode(out TeamBattleMode teamBattle))
            {
                OnlinePlayer? playerA = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(
                    arenaOnline,
                    A.playerNumber
                );
                OnlinePlayer? playerB = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(
                    arenaOnline,
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
                        bool aIsWinningTeam = teamA.team == teamBattle.winningTeam;
                        bool bIsWinningTeam = teamB.team == teamBattle.winningTeam;

                        // Prioritize winning team
                        if (aIsWinningTeam != bIsWinningTeam)
                        {
                            return aIsWinningTeam; // If A is on winning team and B is not, A comes first
                        }

                        if (teamA.team == teamB.team)
                        {
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
            }

            return orig(self, A, B);
        }

        public override List<ArenaSitting.ArenaPlayer> FinalSittingResult(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaSitting.orig_FinalSittingResult orig,
            ArenaSitting self)
        {
            var resultList = orig(self);

            if (IsTeamBattleMode(out TeamBattleMode teamBattle))
            {
                teamBattle.winningTeam = CalculateTeamScoresAndWinner(resultList, arenaOnline, arenaOnline.WinByScore, false, true);

                resultList.Sort((a, b) =>
                {
                    OnlinePlayer? playerA = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arenaOnline, a.playerNumber);
                    OnlinePlayer? playerB = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arenaOnline, b.playerNumber);


                    if (playerA != null && playerB != null)
                    {
                        OnlineManager.lobby.clientSettings[playerA].TryGetData<ArenaTeamClientSettings>(out var teamA);
                        OnlineManager.lobby.clientSettings[playerB].TryGetData<ArenaTeamClientSettings>(out var teamB);


                        // --- Tier 1: Winner Status ---
                        // If there is a winning team, anyone on that team goes to the top.
                        if (teamBattle.winningTeam != -1)
                        {
                            bool aIsWinner = teamA.team == teamBattle.winningTeam;
                            bool bIsWinner = teamB.team == teamBattle.winningTeam;

                            if (aIsWinner != bIsWinner)
                            {
                                return aIsWinner ? -1 : 1;
                            }
                        }
                    }

                    // --- Tier 2: Individual Performance ---
                    // This sorts teammates against each other, AND sorts all losers against each other.
                    int indStatA = arenaOnline.WinByScore ? a.totScore : a.wins;
                    int indStatB = arenaOnline.WinByScore ? b.totScore : b.wins;

                    if (indStatA != indStatB)
                        return indStatB.CompareTo(indStatA);

                    return a.deaths.CompareTo(b.deaths); // Fewer deaths first
                });
            }

            return resultList;
        }

        public override bool PlayerSessionResultSort(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaSitting.orig_PlayerSessionResultSort orig,
            ArenaSitting self,
            ArenaSitting.ArenaPlayer A,
            ArenaSitting.ArenaPlayer B)
        {
            if (IsTeamBattleMode(out TeamBattleMode teamBattle))
            {
                OnlinePlayer? playerA = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arenaOnline, A.playerNumber);
                OnlinePlayer? playerB = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arenaOnline, B.playerNumber);

                if (playerA != null && playerB != null)
                {
                    OnlineManager.lobby.clientSettings[playerA].TryGetData<ArenaTeamClientSettings>(out var teamA);
                    OnlineManager.lobby.clientSettings[playerB].TryGetData<ArenaTeamClientSettings>(out var teamB);

                    if (teamA != null && teamB != null)
                    {
                        // Only consider them on the winning team if a winning team was actually decided (!= -1)
                        bool aIsWinningTeam = (teamBattle.winningTeam != -1) && (teamA.team == teamBattle.winningTeam);
                        bool bIsWinningTeam = (teamBattle.winningTeam != -1) && (teamB.team == teamBattle.winningTeam);

                        // Prioritize winning team
                        if (aIsWinningTeam != bIsWinningTeam)
                        {
                            return aIsWinningTeam; // If A is on winning team and B is not, A comes first
                        }

                        if (teamA.team == teamB.team)
                        {
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
            }

            return orig(self, A, B);
        }

        public override void ArenaSessionNextLevel(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaSitting.orig_NextLevel orig,
            ArenaSitting self,
            ProcessManager process)
        {
            base.ArenaSessionNextLevel(arenaOnline, orig, self, process);
            ClearSortingDictionaries();
        }

        public override void ArenaSessionEnded(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaSitting.orig_SessionEnded orig,
            ArenaSitting self,
            ArenaGameSession session)
        {
            base.ArenaSessionEnded(arenaOnline, orig, self, session);

            if (IsTeamBattleMode(out TeamBattleMode teamBattle) && OnlineManager.lobby.isOwner)
                teamBattle.roundSpawnPointCycler++;
        }

        public override void SpawnPlayer(
            ArenaOnlineGameMode arenaOnline,
            ArenaGameSession self,
            Room room,
            List<int> suggestedDens)
        {
            // Shameful copy-paste
            if (IsTeamBattleMode(out TeamBattleMode teamBattle))
            {
                List<OnlinePlayer> list = new List<OnlinePlayer>();

                List<OnlinePlayer> list2 = new List<OnlinePlayer>();

                for (int j = 0; j < OnlineManager.players.Count; j++)
                {
                    if (arenaOnline.arenaSittingOnlineOrder.Contains(OnlineManager.players[j].inLobbyId))
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
                teamBattle.roundSpawnPointCycler = (
                    teamBattle.roundSpawnPointCycler % totalExits
                );

                if (
                    OnlineManager
                        .lobby.clientSettings[OnlineManager.mePlayer]
                        .TryGetData<ArenaTeamClientSettings>(out var teamSettings)
                )
                {
                    teamBattle.martyrsSpawn =
                        (
                            (int)TeamSpawnPoints.martyrsTeamName
                            + teamBattle.roundSpawnPointCycler
                        ) % totalExits;
                    teamBattle.outlawsSpawn =
                        ((int)TeamSpawnPoints.outlawTeamName + teamBattle.roundSpawnPointCycler)
                        % totalExits;
                    teamBattle.dragonslayersSpawn =
                        (
                            (int)TeamSpawnPoints.dragonslayersTeamName
                            + teamBattle.roundSpawnPointCycler
                        ) % totalExits;
                    teamBattle.chieftainsSpawn =
                        (
                            (int)TeamSpawnPoints.chieftainsTeamName
                            + teamBattle.roundSpawnPointCycler
                        ) % totalExits;

                    switch ((TeamSpawnPoints)teamSettings.team)
                    {
                        case TeamSpawnPoints.martyrsTeamName:
                            randomExitIndex = teamBattle.martyrsSpawn;
                            break;
                        case TeamSpawnPoints.outlawTeamName:
                            randomExitIndex = teamBattle.outlawsSpawn;
                            break;
                        case TeamSpawnPoints.dragonslayersTeamName:
                            randomExitIndex = teamBattle.dragonslayersSpawn;
                            break;
                        case TeamSpawnPoints.chieftainsTeamName:
                            randomExitIndex = teamBattle.chieftainsSpawn;
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
                                teamBattle.martyrsSpawn,
                                teamBattle.outlawsSpawn,
                                teamBattle.dragonslayersSpawn,
                                teamBattle.chieftainsSpawn
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
                    if (arenaOnline.enableOverseer)
                    {
                        SpawnPlayerOverseer(
                            arenaOnline,
                            self,
                            room,
                            randomExitIndex
                        );
                    }
                }
                else
                {
                    SpawnNonTransferableCreature(
                        arenaOnline,
                        self,
                        room,
                        randomExitIndex,
                        CreatureTemplate.Type.Slugcat
                    );
                }

                self.playersSpawned = true;
                if (OnlineManager.lobby.isOwner)
                {
                    arenaOnline.isInGame = true; // used for readied players at the beginning
                    arenaOnline.leaveForNextLevel = false;
                    arenaOnline.playersLateWaitingInLobbyForNextRound.Clear();
                    arenaOnline.hasPermissionToRejoin = false;
                }
                for (int x = 0; x < arenaOnline.arenaSittingOnlineOrder.Count; x++)
                {
                    OnlinePlayer? getPlayer = ArenaHelpers.FindOnlinePlayerByLobbyId(arenaOnline.arenaSittingOnlineOrder[x]);
                    if (getPlayer != null)
                    {
                        if (OnlineManager.lobby.isOwner)
                        {
                            arenaOnline.CheckToAddPlayerStatsToDicts(getPlayer);
                        }
                        RainMeadow.Info($"RMEL;{getPlayer.id.DisplayName};CLASS;${ArenaHelpers.GetArenaClientSettings(getPlayer)?.playingAs}");
                        RainMeadow.Info($"RMEL;{getPlayer.id.DisplayName};TEAM;{teamNames[ArenaHelpers.GetDataSettings<ArenaTeamClientSettings>(getPlayer).team]}");

                    }
                }
            }
        }

        public override string AddIcon(
            ArenaOnlineGameMode arenaOnline,
            OnlinePlayerDisplay display,
            PlayerSpecificOnlineHud owner,
            SlugcatCustomization customization,
            OnlinePlayer player)
        {

            if (base.AddIcon(arenaOnline, display, owner, customization, player) != "")
            {
                return base.AddIcon(arenaOnline, display, owner, customization, player);
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
            ArenaOnlineGameMode arenaOnline,
            OnlinePlayerDisplay display,
            PlayerSpecificOnlineHud owner,
            SlugcatCustomization customization,
            OnlinePlayer player)
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
                if (player.isMe // Custom color only for me in team battles
                    && OnlineManager.lobby.clientSettings.TryGetValue(player, out var cs) 
                    && cs.chatUsernameColor is Color color)
                {
                    return color;
                }
                return teamColors[tb2.team];
            }

            return customization.bodyColor;
        }

        public override string ExportLocalSettings(ArenaOnlineGameMode arenaOnline)
        {
            string baseExport = base.ExportLocalSettings(arenaOnline);
            string decodedBase = string.IsNullOrEmpty(baseExport) ? "" : Encoding.UTF8.GetString(Convert.FromBase64String(baseExport));

            var pairs = new List<string>
            {
                $"chieftainsTeamNames={chieftainsTeamNames}",
                $"dragonSlayersTeamNames={dragonSlayersTeamNames}",
                $"lerp={lerp}",
                $"martyrsTeamName={martyrsTeamName}",
                $"outlawTeamNames={outlawTeamNames}",
            };

            string combined = string.Join("|", pairs);

            if (!string.IsNullOrEmpty(decodedBase))
            {
                combined = decodedBase + "|" + combined;
            }

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
        }

        public override bool ImportLocalSettings(ArenaOnlineGameMode arenaOnline, string base64Data)
        {
            bool success = base.ImportLocalSettings(arenaOnline, base64Data);
            if (string.IsNullOrEmpty(base64Data)) return false;
            if (!success) return false;

            try
            {
                string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64Data));
                string[] pairs = decoded.Split('|');

                foreach (string pair in pairs)
                {
                    string[] kvp = pair.Split('=');
                    if (kvp.Length != 2) continue;

                    string key = kvp[0];
                    string val = kvp[1];

                    switch (key)
                    {
                        case "chieftainsTeamNames":
                            chieftainsTeamNames = val;
                            teamNames[3] = val;
                            break;
                        case "dragonSlayersTeamNames":
                            dragonSlayersTeamNames = val;
                            teamNames[2] = val;
                            break;
                        case "lerp":
                            if (float.TryParse(val, out float f1)) lerp = f1;
                            break;
                        case "martyrsTeamName":
                            martyrsTeamName = val;
                            teamNames[0] = val;
                            break;
                        case "outlawTeamNames":
                            outlawTeamNames = val;
                            teamNames[1] = val;
                            break;
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                return false;
            }
        }
    }
}

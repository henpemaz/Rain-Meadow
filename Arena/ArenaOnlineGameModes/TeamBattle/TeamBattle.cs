using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle
{
    // TODO: Add some custom type that represents teams or team data. It is harder than it needs to be to handle teams right now.
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

        /// <summary>
        /// Stores the indexes of the team that won, or the teams that tied. If no teams
        /// are applicable, this is empty (such as when everyone is a spectator).
        /// </summary>
        /// <remarks>
        /// This only has meaning at the end of <see cref="ArenaGameSession"/>s
        /// and <see cref="ArenaSitting"/>s.
        /// </remarks>
        public List<int> BestTeamIndexes { get; set; } = [];

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

        public List<List<ArenaSitting.ArenaPlayer>> GetTeamGroupedArenaPlayers(
            ArenaOnlineGameMode arenaOnline,
            List<ArenaSitting.ArenaPlayer> arenaPlayers,
            bool shouldIncludeSpectators = true)
        {
            List<List<ArenaSitting.ArenaPlayer>> teamGroupedArenaPlayers = [ [], [], [], [] ];

            foreach (ArenaSitting.ArenaPlayer arenaPlayer in arenaPlayers)
            {
                if (!shouldIncludeSpectators
                    && arenaPlayer.playerClass == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                {
                    continue;
                }
                if (ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arenaOnline, arenaPlayer.playerNumber)
                    is not OnlinePlayer onlinePlayer)
                {
                    RainMeadow.Warn($"Unable to find arena player's online player. Player number: {arenaPlayer.playerNumber}");
                    continue;
                }
                if (!OnlineManager.lobby.clientSettings[onlinePlayer].TryGetData(out ArenaTeamClientSettings clientData))
                {
                    RainMeadow.Error($"Unable to find {onlinePlayer}'s team client data.");
                    continue;
                }

                teamGroupedArenaPlayers[clientData.team].Add(arenaPlayer);
            }

            return teamGroupedArenaPlayers;
        }

        public override void ResetOnSessionEnd()
        {
            martyrsSpawn = 0;
            outlawsSpawn = 0;
            dragonslayersSpawn = 0;
            chieftainsSpawn = 0;
            roundSpawnPointCycler = 0;
        }

        public override bool On_ArenaBehaviors_ExitManager_ExitsOpen(
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
                return orig(self) || (self.gameSession?.arenaSitting?.players?.Any(p => p?.score >= self.gameSession.GameTypeSetup.ScoreToEnterDen) ?? false);
            }

            int playersStillStanding =
                self.gameSession.Players?.Count(player =>
                    player.realizedCreature != null && player.realizedCreature.State.alive
                ) ?? 0;

            if (
                playersStillStanding == 1
                && arenaOnline.arenaSittingOnlineOrder.Count >= 1
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

        public override void On_ArenaGameSession_ctor(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaGameSession.orig_ctor orig,
            ArenaGameSession self,
            RainWorldGame game)
        {
            ArenaTeamClientSettings teamClientData = OnlineManager.lobby
                .clientSettings[OnlineManager.mePlayer]
                .GetData<ArenaTeamClientSettings>();

            arenaOnline.avatarSettings.bodyColor = Color.Lerp(
                arenaOnline.avatarSettings.bodyColor,
                teamColors[teamClientData.team],
                lerp
            );

            base.On_ArenaGameSession_ctor(arenaOnline, orig, self, game);
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

        // This is run on the victim's end, not the killer's!
        public override void On_ArenaGameSession_Killing(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaGameSession.orig_Killing orig,
            ArenaGameSession self,
            Player attacker,
            Creature target)
        {
            // Copy ArenaGameSession.Killing's guard clause
            if (self.sessionEnded || ModManager.MSC && attacker.AI is not null)
                return;

            if (attacker.abstractCreature.GetOnlineCreature() is not OnlineCreature attackerOCreature)
            {
                RainMeadow.Error("Unable to find attacker's online creature.");
                return;
            }
            if (target.abstractCreature.GetOnlineCreature() is not OnlineCreature targetOCreature)
            {
                RainMeadow.Error("Unable to find target's online creature.");
                return;
            }
            if (ArenaHelpers.FindArenaPlayerByOnlinePlayer(arenaOnline, self.arenaSitting, attackerOCreature.owner)
                is not ArenaSitting.ArenaPlayer attackerArenaPlayer)
            {
                RainMeadow.Error($"Unable to find {attackerOCreature.owner}'s arena player.");
                return;
            }


            bool isTeamKill = attackerOCreature.isAvatar && targetOCreature.isAvatar &&
                ArenaHelpers.CheckSameTeam(attackerOCreature.owner, targetOCreature.owner);

            if (isTeamKill)
            {
                int scoreChange = -self.GameTypeSetup.KillScore;

                if (scoreChange != 0)
                {
                    ArenaRPCs.ModifyArenaPlayerScore(
                        attackerArenaPlayer.playerNumber,
                        scoreChange
                    );

                    attackerOCreature.BroadcastRPCInRoom(
                        ArenaRPCs.ModifyArenaPlayerScore,
                        attackerArenaPlayer.playerNumber,
                        scoreChange
                    );
                }
            }
            else
                base.On_ArenaGameSession_Killing(arenaOnline, orig, self, attacker, target);
        }

        public override void On_ArenaSitting_SessionEnded(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaSitting.orig_SessionEnded orig,
            ArenaSitting self,
            ArenaGameSession session)
        {
            base.On_ArenaSitting_SessionEnded(arenaOnline, orig, self, session);

            if (IsTeamBattleMode(out TeamBattleMode teamBattle) && OnlineManager.lobby.isOwner)
                teamBattle.roundSpawnPointCycler++;
        }

        public override void On_ArenaGameSession_SpawnPlayers(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaGameSession.orig_SpawnPlayers orig,
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

                foreach (ArenaSitting.ArenaPlayer arenaPlayer in self.arenaSitting.players)
                {
                    OnlinePlayer? onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(
                        arenaOnline,
                        arenaPlayer.playerNumber
                    );
                    if (onlinePlayer is null) continue;

                    RainMeadow.Info(
                        $"RMEL;{onlinePlayer.id.DisplayName};CLASS;"
                        + $"{ArenaHelpers.GetArenaClientSettings(onlinePlayer)?.playingAs}"
                    );

                    if (ArenaHelpers.GetDataSettings<ArenaTeamClientSettings>(onlinePlayer) is ArenaTeamClientSettings playerTeamSettings
                        && teamNames.TryGetValue(playerTeamSettings.team, out string? teamName))
                    {
                        RainMeadow.Info($"RMEL;{onlinePlayer.id.DisplayName};TEAM;{teamName}");
                    }
                }
            }
        }

        public override bool On_ArenaSitting_PlayerSessionResultSort(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaSitting.orig_PlayerSessionResultSort orig,
            ArenaSitting self,
            ArenaSitting.ArenaPlayer a,
            ArenaSitting.ArenaPlayer b)
        {
            if (ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arenaOnline, a.playerNumber) is not OnlinePlayer onlinePlayerA)
            {
                RainMeadow.Warn($"Unable to find arena player A's online player. Player number: {a.playerNumber}");
                return false;
            }
            if (ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arenaOnline, b.playerNumber) is not OnlinePlayer onlinePlayerB)
            {
                RainMeadow.Warn($"Unable to find arena player B's online player. Player number: {b.playerNumber}");
                return false;
            }
            if (!OnlineManager.lobby.clientSettings[onlinePlayerA].TryGetData(out ArenaTeamClientSettings clientDataA))
            {
                RainMeadow.Error($"Unable to find {onlinePlayerA}'s team client data.");
                return false;
            }
            if (!OnlineManager.lobby.clientSettings[onlinePlayerB].TryGetData(out ArenaTeamClientSettings clientDataB))
            {
                RainMeadow.Error($"Unable to find {onlinePlayerB}'s team client data.");
                return false;
            }


            int teamIndexA = clientDataA.team;
            int teamIndexB = clientDataB.team;

            // We want players of the same team to be grouped together even if individuals
            // in a team scored better/worse than some individuals on other teams.
            if (teamIndexA != teamIndexB)
            {
                List<List<ArenaSitting.ArenaPlayer>> teamGroupedPlayers = GetTeamGroupedArenaPlayers(
                    arenaOnline,
                    self.players,
                    false
                );

                List<int> bestTeamIndexes = GetTeamIndexesWithHighestValue(
                    teamGroupedPlayers,
                    ShouldWinByScore(self.gameTypeSetup)
                        ? players => players.Sum(plr => plr.score)
                        : players => players.Sum(plr => plr.alive ? 1 : 0)
                );

                if (bestTeamIndexes.Contains(teamIndexA) != bestTeamIndexes.Contains(teamIndexB))
                    return bestTeamIndexes.Contains(teamIndexA);

                RainMeadow.Warn(
                    $"Team {teamIndexA} and team {teamIndexB} tied but team {teamIndexA} will be displayed "
                    + $"above team {teamIndexB} to keep players grouped into their respective team."
                );
                return teamIndexA > teamIndexB;
            }

            return base.On_ArenaSitting_PlayerSessionResultSort(
                arenaOnline,
                orig,
                self,
                a,
                b
            );
        }

        public override bool On_ArenaSitting_PlayerSittingResultSort(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaSitting.orig_PlayerSittingResultSort orig,
            ArenaSitting self,
            ArenaSitting.ArenaPlayer a,
            ArenaSitting.ArenaPlayer b)
        {
            if (ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arenaOnline, a.playerNumber) is not OnlinePlayer onlinePlayerA)
            {
                RainMeadow.Warn($"Unable to find arena player A's online player. Player number: {a.playerNumber}");
                return false;
            }
            if (ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arenaOnline, b.playerNumber) is not OnlinePlayer onlinePlayerB)
            {
                RainMeadow.Warn($"Unable to find arena player B's online player. Player number: {b.playerNumber}");
                return false;
            }
            if (!OnlineManager.lobby.clientSettings[onlinePlayerA].TryGetData(out ArenaTeamClientSettings clientDataA))
            {
                RainMeadow.Error($"Unable to find {onlinePlayerA}'s team client data.");
                return false;
            }
            if (!OnlineManager.lobby.clientSettings[onlinePlayerB].TryGetData(out ArenaTeamClientSettings clientDataB))
            {
                RainMeadow.Error($"Unable to find {onlinePlayerB}'s team client data.");
                return false;
            }


            int teamIndexA = clientDataA.team;
            int teamIndexB = clientDataB.team;

            // We want players of the same team to be grouped together even if individuals
            // in the team scored better/worse than some individuals on other teams.
            if (teamIndexA != teamIndexB)
            {
                List<List<ArenaSitting.ArenaPlayer>> teamGroupedPlayers = GetTeamGroupedArenaPlayers(
                    arenaOnline,
                    self.players,
                    false
                );

                List<int> bestTeamIndexes = GetTeamIndexesWithHighestValue(
                    teamGroupedPlayers,
                    ShouldWinByScore(self.gameTypeSetup)
                        ? players => players.Sum(plr => plr.totScore)
                        : players => players.Sum(plr => plr.wins)
                );

                if (bestTeamIndexes.Contains(teamIndexA) != bestTeamIndexes.Contains(teamIndexB))
                    return bestTeamIndexes.Contains(teamIndexA);

                RainMeadow.Warn(
                    $"Team {teamIndexA} and team {teamIndexB} tied but team {teamIndexA} will be displayed "
                    + $"above team {teamIndexB} to keep players grouped into their respective team."
                );
                return teamIndexA > teamIndexB;
            }

            return base.On_ArenaSitting_PlayerSittingResultSort(
                arenaOnline,
                orig,
                self,
                a,
                b
            );
        }

        /// <inheritdoc/>
        public override List<ArenaSitting.ArenaPlayer> DetermineArenaSessionWinners(
            ArenaOnlineGameMode arenaOnline,
            ArenaGameSession arenaSession)
        {
            ArenaSitting arenaSitting = arenaSession.arenaSitting;

            List<List<ArenaSitting.ArenaPlayer>> teamGroupedPlayers = GetTeamGroupedArenaPlayers(
                arenaOnline,
                arenaSession.arenaSitting.players,
                false
            );

            // There needs at least 2 teams for someone to win. If there is only 1, they can't logically win or lose.
            if (teamGroupedPlayers.Count(players => players.Count > 0) < 2)
            {
                BestTeamIndexes.Clear();
                return [];
            }

            BestTeamIndexes = GetTeamIndexesWithHighestValue(
                teamGroupedPlayers,
                ShouldWinByScore(arenaSitting.gameTypeSetup)
                    ? players => players.Sum(plr => plr.score)
                    : players => players.Any(plr => plr.alive) ? 1 : 0
            );


            return BestTeamIndexes.Count == 1
                ? teamGroupedPlayers[BestTeamIndexes[0]]
                : [];
        }

        /// <inheritdoc/>
        public override List<ArenaSitting.ArenaPlayer> DetermineArenaSittingWinners(
            ArenaOnlineGameMode arenaOnline,
            ArenaSitting arenaSitting)
        {
            List<List<ArenaSitting.ArenaPlayer>> teamGroupedPlayers = GetTeamGroupedArenaPlayers(
                arenaOnline,
                arenaSitting.players,
                false
            );

            // There needs at least 2 teams for someone to win. If there is only 1, they can't logically win or lose.
            if (teamGroupedPlayers.Count(players => players.Count > 0) < 2)
            {
                BestTeamIndexes.Clear();
                return [];
            }

            BestTeamIndexes = GetTeamIndexesWithHighestValue(
                teamGroupedPlayers,
                ShouldWinByScore(arenaSitting.gameTypeSetup)
                    ? players => players.Sum(plr => plr.totScore)
                    : players => players.Sum(plr => plr.wins)
            );

            return BestTeamIndexes.Count == 1
                ? teamGroupedPlayers[BestTeamIndexes[0]]
                : [];
        }

        /// <summary>
        /// Gets the indexes of all non-empty teams with the highest value returned by
        /// <paramref name="valueSelector"/>.
        /// </summary>
        /// <param name="teamGroupedPlayers">The teams to compare.</param>
        /// <param name="valueSelector">The delegate used to determine the team's value.</param>
        /// <returns>
        /// A list of ordered (ascending) team indexes whose value is equal to the highest
        /// value among all non-empty teams. Returns an empty list if all teams are empty.
        /// </returns>
        /// <remarks>
        /// <paramref name="valueSelector"/> is not run on empty teams (empty lists).
        /// </remarks>
        public List<int> GetTeamIndexesWithHighestValue(
            List<List<ArenaSitting.ArenaPlayer>> teamGroupedPlayers,
            Func<List<ArenaSitting.ArenaPlayer>, int> valueSelector)
        {
            if (teamGroupedPlayers.Count == 0)
                return [];

            List<int?> values = teamGroupedPlayers
                .Select(players => players.Count > 0
                    ? (int?)valueSelector(players)
                    : null)
                .ToList();

            int? highestValue = values.Max();

            if (highestValue is null)
                return [];

            return values
                .Select((value, index) => (value, index))
                .Where(pair => pair.value == highestValue)
                .Select(pair => pair.index)
                .ToList();
        }

        public override string AddIcon(
            ArenaOnlineGameMode arenaOnline,
            OnlinePlayerDisplay display,
            PlayerSpecificOnlineHud owner,
            SlugcatCustomization customization,
            OnlinePlayer player)
        {
            string arenaIcon = base.AddIcon(arenaOnline, display, owner, customization, player);
            if (arenaIcon != "")
                return arenaIcon;

            if (OnlineManager.lobby.clientSettings.TryGetValue(key: player, out _) == false)
                return "";

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

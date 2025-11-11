using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ArenaMode = RainMeadow.ArenaOnlineGameMode;

namespace RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle
{
    public partial class TeamBattleMode : ExternalArenaGameMode
    {

        public static ArenaSetup.GameTypeID TeamBattle = new ArenaSetup.GameTypeID("Team Battle", register: false);

        public override ArenaSetup.GameTypeID GetGameModeId
        {
            get
            {
                return TeamBattle;
            }
            set { GetGameModeId = value; }

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

        public List<string> teamNameList;

        public override void ResetOnSessionEnd()
        {
            winningTeam = -1;
            martyrsSpawn = 0;
            outlawsSpawn = 0;
            dragonslayersSpawn = 0;
            chieftainsSpawn = 0;
            roundSpawnPointCycler = 0;

        }

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
                                bool gotPlayerTeam = OnlineManager.lobby.clientSettings.TryGetValue(onlineP.owner, out var onlineClientP);
                                if (gotPlayerTeam)
                                {
                                    onlineClientP.TryGetData<ArenaTeamClientSettings>(out var playerTeam);
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

        public override void LandSpear(ArenaOnlineGameMode arena, ArenaGameSession self, Player player, Creature target, ArenaSitting.ArenaPlayer aPlayer)
        {
            aPlayer.AddSandboxScore(self.GameTypeSetup.spearHitScore);

        }

        public override void ArenaSessionCtor(ArenaOnlineGameMode arena, On.ArenaGameSession.orig_ctor orig, ArenaGameSession self, RainWorldGame game)
        {
            base.ArenaSessionCtor(arena, orig, self, game);
            if (TeamBattleMode.isTeamBattleMode(arena, out var tb))
            {
                if (OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].TryGetData<ArenaTeamClientSettings>(out var t))
                {
                    arena.avatarSettings.bodyColor = Color.Lerp(arena.avatarSettings.bodyColor, teamColors[t.team], tb.lerp);
                }
            }

        }

        public override bool PlayerSittingResultSort(ArenaMode arena, On.ArenaSitting.orig_PlayerSittingResultSort orig, ArenaSitting self, ArenaSitting.ArenaPlayer A, ArenaSitting.ArenaPlayer B)
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

        public override bool PlayerSessionResultSort(ArenaMode arena, On.ArenaSitting.orig_PlayerSessionResultSort orig, ArenaSitting self, ArenaSitting.ArenaPlayer A, ArenaSitting.ArenaPlayer B)
        {
            if (isTeamBattleMode(arena, out var tb))
            {
                tb.winningTeam = -1;
                HashSet<int> teamsRemaining = new HashSet<int>();
                foreach (var player in self.players)
                {
                    if (player.alive)
                    {
                        OnlinePlayer? onlineP = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, player.playerNumber);
                        if (onlineP != null)
                        {
                            bool getPlayerTeam = OnlineManager.lobby.clientSettings[onlineP].TryGetData<ArenaTeamClientSettings>(out var playerTeam);
                            if (getPlayerTeam)
                            {
                                teamsRemaining.Add(playerTeam.team);
                            }

                        }
                    }
                }

                foreach (var player in self.players)
                {
                    if (teamsRemaining.Count == 1)
                    {
                        tb.winningTeam = teamsRemaining.First();

                        OnlinePlayer? onlineP = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, player.playerNumber);
                        if (onlineP != null)
                        {
                            bool gotPlayerTeam = OnlineManager.lobby.clientSettings[onlineP].TryGetData<ArenaTeamClientSettings>(out var playerTeam);
                            if (gotPlayerTeam)
                            {
                                player.winner = teamsRemaining.TryGetValue(playerTeam.team, out _);
                            }
                        }
                    }
                    else
                    {
                        player.winner = false; // everyone's a loser. Kill your enemies!
                    }
                }

                OnlinePlayer? playerA = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, A.playerNumber);
                OnlinePlayer? playerB = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, B.playerNumber);

                if (playerA != null && playerB != null)
                {
                    OnlineManager.lobby.clientSettings[playerA].TryGetData<ArenaTeamClientSettings>(out var teamA);
                    OnlineManager.lobby.clientSettings[playerB].TryGetData<ArenaTeamClientSettings>(out var teamB);

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
                            if (A.allKills.Count != B.allKills.Count)
                            {
                                return A.allKills.Count > B.allKills.Count;
                            }
                            else if (A.deaths != B.deaths)
                            {
                                return A.deaths > B.deaths;
                            }
                            return A.score > B.score; // If both are on winning team, sort by kill value
                        }
                    }
                }
            }

            return orig(self, A, B);
        }

        public override void ArenaSessionEnded(ArenaOnlineGameMode arena, On.ArenaSitting.orig_SessionEnded orig, ArenaSitting self, ArenaGameSession session, List<ArenaSitting.ArenaPlayer> list)
        {
            if (TeamBattleMode.isTeamBattleMode(arena, out var tb) && OnlineManager.lobby.isOwner)
            {
                tb.roundSpawnPointCycler = tb.roundSpawnPointCycler + 1;
            }
        }

        public override void SpawnPlayer(ArenaOnlineGameMode arena, ArenaGameSession self, Room room, List<int> suggestedDens)
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
                teamBattleMode.roundSpawnPointCycler = (teamBattleMode.roundSpawnPointCycler % totalExits);

                if (OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].TryGetData<ArenaTeamClientSettings>(out var teamSettings))
                {
                    teamBattleMode.martyrsSpawn = ((int)TeamSpawnPoints.martyrsTeamName + teamBattleMode.roundSpawnPointCycler) % totalExits;
                    teamBattleMode.outlawsSpawn = ((int)TeamSpawnPoints.outlawTeamName + teamBattleMode.roundSpawnPointCycler) % totalExits;
                    teamBattleMode.dragonslayersSpawn = ((int)TeamSpawnPoints.dragonslayersTeamName + teamBattleMode.roundSpawnPointCycler) % totalExits;
                    teamBattleMode.chieftainsSpawn = ((int)TeamSpawnPoints.chieftainsTeamName + teamBattleMode.roundSpawnPointCycler) % totalExits;

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
                            Debug.LogWarning("Current player's team is not recognized for spawn point assignment.");
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
                            player.InvokeOnceRPC(ArenaRPCs.Arena_NotifySpawnPoint,
                                                teamBattleMode.martyrsSpawn,
                                                teamBattleMode.outlawsSpawn,
                                                teamBattleMode.dragonslayersSpawn,
                                                teamBattleMode.chieftainsSpawn);
                        }
                    }
                }



                RainMeadow.Debug("Trying to create an abstract creature");
                RainMeadow.Debug($"RANDOM EXIT INDEX: {randomExitIndex}");
                RainMeadow.Debug($"RANDOM START TILE INDEX: {room.ShortcutLeadingToNode(randomExitIndex).StartTile}");
                RainMeadow.sSpawningAvatar = true;
                AbstractCreature abstractCreature = new AbstractCreature(self.game.world, StaticWorld.GetCreatureTemplate("Slugcat"), null, new WorldCoordinate(0, -1, -1, -1), new EntityID(-1, 0));
                abstractCreature.pos.room = self.game.world.GetAbstractRoom(0).index;
                abstractCreature.pos.abstractNode = room.ShortcutLeadingToNode(randomExitIndex).destNode;
                abstractCreature.Room.AddEntity(abstractCreature);


                RainMeadow.Debug("assigned ac, registering");

                self.game.world.GetResource().ApoEnteringWorld(abstractCreature);
                RainMeadow.sSpawningAvatar = false;

                self.game.cameras[0].followAbstractCreature = abstractCreature;

                if (abstractCreature.GetOnlineObject(out var oe) && oe.TryGetData<SlugcatCustomization>(out var customization))
                {
                    abstractCreature.state = new PlayerState(abstractCreature, 0, customization.playingAs, isGhost: false);

                }
                else
                {
                    RainMeadow.Error("Could not get online owner for spawned player!");
                    abstractCreature.state = new PlayerState(abstractCreature, 0, self.arenaSitting.players[ArenaHelpers.FindOnlinePlayerNumber(arena, OnlineManager.mePlayer)].playerClass, isGhost: false);
                }

                RainMeadow.Debug("Arena: Realize Creature!");
                abstractCreature.Realize();
                var shortCutVessel = new ShortcutHandler.ShortCutVessel(room.ShortcutLeadingToNode(randomExitIndex).DestTile, abstractCreature.realizedCreature, self.game.world.GetAbstractRoom(0), 0);

                shortCutVessel.entranceNode = abstractCreature.pos.abstractNode;
                shortCutVessel.room = self.game.world.GetAbstractRoom(abstractCreature.Room.name);

                self.game.shortcuts.betweenRoomsWaitingLobby.Add(shortCutVessel);
                self.AddPlayer(abstractCreature);
                if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Night)
                {
                    (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;
                }
                if (ModManager.MSC)
                {
                    if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Red)
                    {
                        self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, -0.75f);
                        self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, 0.5f);
                    }

                    if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Yellow)
                    {
                        self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, 0.75f);
                        self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, 0.3f);
                    }

                    if ((abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer)
                    {
                        self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, -0.5f);
                        self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, -1f);
                    }

                    if ((abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup)
                    {
                        (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;
                    }

                    if ((abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
                    {
                        (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = arena.painCatThrowingSkill;
                        RainMeadow.Debug("ENOT THROWING SKILL " + (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill);
                        if ((abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill == 0 && arena.painCatEgg)
                        {
                            AbstractPhysicalObject bringThePain = new AbstractPhysicalObject(room.world, DLCSharedEnums.AbstractObjectType.SingularityBomb, null, abstractCreature.pos, shortCutVessel.room.world.game.GetNewID());
                            room.abstractRoom.AddEntity(bringThePain);
                            bringThePain.RealizeInRoom();

                            self.room.world.GetResource().ApoEnteringWorld(bringThePain);
                            self.room.abstractRoom.GetResource()?.ApoEnteringRoom(bringThePain, bringThePain.pos);
                        }

                        if (arena.lizardEvent == 99 && arena.painCatLizard)
                        {
                            self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Lizards, -1, 0, 1f);
                            AbstractCreature bringTheTrain = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate("Red Lizard"), null, room.GetWorldCoordinate(shortCutVessel.pos), shortCutVessel.room.world.game.GetNewID()); // Train too big :( 
                            room.abstractRoom.AddEntity(bringTheTrain);
                            bringTheTrain.Realize();
                            bringTheTrain.realizedCreature.PlaceInRoom(room);


                            self.room.world.GetResource().ApoEnteringWorld(bringTheTrain);
                            self.room.abstractRoom.GetResource()?.ApoEnteringRoom(bringTheTrain, bringTheTrain.pos);
                        }
                    }

                    if ((abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint)
                    {
                        if (!arena.sainot) // ascendance saint
                        {
                            (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 0;
                        }
                        else
                        {
                            (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;

                        }
                    }
                }

                if (ModManager.Watcher && (abstractCreature.realizedCreature as Player).SlugCatClass == Watcher.WatcherEnums.SlugcatStatsName.Watcher)
                {
                    (abstractCreature.realizedCreature as Player).enterIntoCamoDuration = 40;
                }



                self.playersSpawned = true;
                if (OnlineManager.lobby.isOwner)
                {
                    arena.isInGame = true; // used for readied players at the beginning
                    arena.leaveForNextLevel = false;

                    foreach (var onlineArenaPlayer in arena.arenaSittingOnlineOrder)
                    {
                        OnlinePlayer? getPlayer = ArenaHelpers.FindOnlinePlayerByLobbyId(onlineArenaPlayer);
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


        public override string AddIcon(ArenaOnlineGameMode arena, PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player)
        {
            if (OnlineManager.lobby.clientSettings.TryGetValue(key: player, out _) == false)
            {
                return "";
            }

            if (OnlineManager.lobby.clientSettings[player].TryGetData<ArenaTeamClientSettings>(out var tb2))
            {
                return teamIcons[tb2.team];
            }
            return "";
        }

        public override Color IconColor(ArenaOnlineGameMode arena, OnlinePlayerDisplay display, PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player)
        {
            if (OnlineManager.lobby.clientSettings.TryGetValue(key: player, out _) == false)
            {
                return customization.bodyColor;
            }

            if (owner.PlayerConsideredDead)
            {
                return Color.grey;
            }


            if (OnlineManager.lobby.clientSettings[player].TryGetData<ArenaTeamClientSettings>(out var tb2))
            {
                return teamColors[tb2.team];
            }

            return customization.bodyColor;
        }



    }
}

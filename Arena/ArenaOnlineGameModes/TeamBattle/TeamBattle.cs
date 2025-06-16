using Menu;
using Menu.Remix;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using System.Collections.Generic;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;
using System.Linq;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using RainMeadow.UI.Components;
using System.Runtime.CompilerServices;

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

        public override ArenaSetup.GameTypeID GetGameModeId
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

        public int winningTeam = -1;
        public int martyrsSpawn = 0;
        public int outlawsSpawn = 0;
        public int dragonslayersSpawn = 0;
        public int chieftainsSpawn = 0;
        public int roundSpawnPointCycler = 0;

        public string martyrsTeamName = RainMeadow.rainMeadowOptions.MartyrTeamName.Value;
        public string outlawTeamNames = RainMeadow.rainMeadowOptions.OutlawsTeamName.Value;
        public string dragonSlayersTeamNames = RainMeadow.rainMeadowOptions.DragonSlayersTeamName.Value;
        public string chieftainsTeamNames = RainMeadow.rainMeadowOptions.ChieftainTeamName.Value;

        public UIelementWrapper externalModeWrapper;

        public OpComboBox? arenaTeamComboBox;
        public OpTextBox? martyrsTeamNameUpdate;
        public OpTextBox? outlawsTeamNameUpdate;
        public OpTextBox? dragonsSlayersTeamNameUpdate;
        public OpTextBox? chieftainsTeamNameUpdate;

        public bool teamComboBoxLastHeld;

        public List<string> teamNameList;

        public Dictionary<int, string> teamNameDictionary = new Dictionary<int, string>
        {
            { 0, RainMeadow.rainMeadowOptions.MartyrTeamName.Value },
            { 1, RainMeadow.rainMeadowOptions.OutlawsTeamName.Value },
            { 2, RainMeadow.rainMeadowOptions.DragonSlayersTeamName.Value },
            { 3, RainMeadow.rainMeadowOptions.ChieftainTeamName.Value }
        };
        public enum TeamMappings
        {
            martyrsTeamName,
            outlawTeamName,
            dragonslayersTeamName,
            chieftainsTeamName
        }

        public  Dictionary<int, string> TeamMappingsDictionary = new Dictionary<int, string>
        {
            { 0, "SaintA" },
            { 1, "OutlawA" },
            { 2, "DragonSlayerA" },
            { 3, "ChieftainA" }
    };

        public  Dictionary<int, Color> TeamColors = new Dictionary<int, Color>
        {
            { 0, Color.red },
            { 1, Color.yellow },
            { 2, Color.magenta },
            { 3, Color.blue }
    };

        public override void ResetOnSessionEnd()
        {
            winningTeam = -1;
            martyrsSpawn = 0;
            outlawsSpawn = 0;
            dragonslayersSpawn = 0;
            chieftainsSpawn = 0;
            roundSpawnPointCycler = 0;

        }

        public override List<ListItem> ArenaOnlineInterfaceListItems(ArenaMode arena)
        {
            return this.TeamMappingsDictionary.Select(v => new ListItem(v.Value.ToString())).ToList();
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
                                bool gotPlayerTeam = OnlineManager.lobby.clientSettings[onlineP.owner].TryGetData<ArenaTeamClientSettings>(out var playerTeam);
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
                    if (aliveTeams.Count == 1)
                        return true;
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
            if (TeamBattleMode.isTeamBattleMode(arena, out var tb))
            {
                if (OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].TryGetData<ArenaTeamClientSettings>(out var t))
                {
                    arena.avatarSettings.bodyColor = Color.Lerp(arena.avatarSettings.bodyColor, tb.TeamColors[t.team], 0.7f);
                }
            }

        }

        public override void ArenaSessionEnded(ArenaOnlineGameMode arena, On.ArenaSitting.orig_SessionEnded orig, ArenaSitting self, ArenaGameSession session, List<ArenaSitting.ArenaPlayer> list)
        {
            if (TeamBattleMode.isTeamBattleMode(arena, out var tb))
            {
                if (list.Count == 1)
                {
                    list[0].winner = list[0].alive;
                }
                else if (list.Count > 1)
                {
                    HashSet<int> teamsRemaining = new HashSet<int>();

                    foreach (var player in list)
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

                    foreach (var player in list)
                    {
                        if (teamsRemaining.Count == 1)
                        {
                            OnlinePlayer? onlineP = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, player.playerNumber);
                            if (onlineP != null)
                            {
                                bool gotPlayerTeam = OnlineManager.lobby.clientSettings[onlineP].TryGetData<ArenaTeamClientSettings>(out var playerTeam);
                                if (gotPlayerTeam)
                                {
                                    player.winner = teamsRemaining.TryGetValue(playerTeam.team, out var winningTeam);
                                    tb.winningTeam = winningTeam;
                                }
                            }
                        }
                        else
                        {
                            player.winner = false; // everyone's a loser. Kill your enemies!
                        }
                    }
                }
            }

            if (OnlineManager.lobby.isOwner)
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
                    teamBattleMode.martyrsSpawn = ((int)TeamMappings.martyrsTeamName + teamBattleMode.roundSpawnPointCycler) % totalExits;
                    teamBattleMode.outlawsSpawn = ((int)TeamMappings.outlawTeamName + teamBattleMode.roundSpawnPointCycler) % totalExits;
                    teamBattleMode.dragonslayersSpawn = ((int)TeamMappings.dragonslayersTeamName + teamBattleMode.roundSpawnPointCycler) % totalExits;
                    teamBattleMode.chieftainsSpawn = ((int)TeamMappings.chieftainsTeamName + teamBattleMode.roundSpawnPointCycler) % totalExits;

                    switch ((TeamMappings)teamSettings.team)
                    {
                        case TeamMappings.martyrsTeamName:
                            randomExitIndex = teamBattleMode.martyrsSpawn;
                            break;
                        case TeamMappings.outlawTeamName:
                            randomExitIndex = teamBattleMode.outlawsSpawn;
                            break;
                        case TeamMappings.dragonslayersTeamName:
                            randomExitIndex = teamBattleMode.dragonslayersSpawn;
                            break;
                        case TeamMappings.chieftainsTeamName:
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
                            bringTheTrain.RealizeInRoom();

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
                arena.playerEnteredGame++;
                foreach (var player in arena.arenaSittingOnlineOrder)
                {
                    OnlinePlayer? getPlayer = ArenaHelpers.FindOnlinePlayerByLobbyId(player);
                    if (getPlayer != null)
                    {
                        if (!getPlayer.isMe)
                        {
                            getPlayer.InvokeOnceRPC(ArenaRPCs.Arena_IncrementPlayersJoined);
                        }
                    }
                }
                if (OnlineManager.lobby.isOwner)
                {
                    arena.isInGame = true; // used for readied players at the beginning
                    arena.leaveForNextLevel = false;
                    if (arena.playersLateWaitingInLobbyForNextRound.Count > 0)
                    {
                        foreach (var p in arena.playersLateWaitingInLobbyForNextRound)
                        {
                            OnlinePlayer? onlineP = ArenaHelpers.FindOnlinePlayerByLobbyId(p);
                            if (onlineP != null)
                            {
                                onlineP.InvokeOnceRPC(ArenaRPCs.Arena_NotifyRejoinAllowed, true);
                            }
                        }
                    }
                    foreach (var arenaPlayer in self.arenaSitting.players)
                    {
                        if (!arena.playerNumberWithKills.ContainsKey(arenaPlayer.playerNumber))
                        {
                            arena.playerNumberWithKills.Add(arenaPlayer.playerNumber, 0);
                        }
                        if (!arena.playerNumberWithDeaths.ContainsKey(arenaPlayer.playerNumber))
                        {
                            arena.playerNumberWithDeaths.Add(arenaPlayer.playerNumber, 0);
                        }
                        if (!arena.playerNumberWithWins.ContainsKey(arenaPlayer.playerNumber))
                        {
                            arena.playerNumberWithWins.Add(arenaPlayer.playerNumber, 0);
                        }
                    }
                    arena.playersLateWaitingInLobbyForNextRound.Clear();


                }
                arena.hasPermissionToRejoin = false;
            }


        }


        public override string AddCustomIcon(ArenaOnlineGameMode arena, PlayerSpecificOnlineHud onlineHud)
        {

            if (OnlineManager.lobby.clientSettings[onlineHud.clientSettings.owner].TryGetData<ArenaTeamClientSettings>(out var tb2))
            {
                return TeamMappingsDictionary[tb2.team];
            }
            return "";
        }
        public override void ArenaExternalGameModeSettingsInterface_ctor(ArenaOnlineGameMode arena, OnlineArenaExternalGameModeSettingsInterface extComp, Menu.Menu menu, MenuObject owner, MenuTabWrapper tabWrapper, Vector2 pos, float settingsWidth = 300)
        {
            if (isTeamBattleMode(arena, out var tb))
            {

                ListItem martyrListItem = new ListItem(RainMeadow.rainMeadowOptions.MartyrTeamName.Value);
                ListItem outlawsListItem = new ListItem(RainMeadow.rainMeadowOptions.OutlawsTeamName.Value);
                ListItem dragonSlayersListItem = new ListItem(RainMeadow.rainMeadowOptions.DragonSlayersTeamName.Value);
                ListItem chieftainsListItem = new ListItem(RainMeadow.rainMeadowOptions.ChieftainTeamName.Value);


                List<ListItem> teamNameListItems = new List<ListItem>();
                teamNameListItems.Add(martyrListItem);
                teamNameListItems.Add(outlawsListItem);
                teamNameListItems.Add(dragonSlayersListItem);
                teamNameListItems.Add(chieftainsListItem);

                var arenaGameModeLabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("Team:"), new Vector2(50, 380f), new Vector2(0, 20), false);
                arenaTeamComboBox = new OpComboBox2(new Configurable<string>(""), new Vector2(arenaGameModeLabel.pos.x + 50, arenaGameModeLabel.pos.y), 175f, teamNameListItems);
                arenaTeamComboBox.OnValueChanged += (config, value, lastValue) =>
                {
                    if (OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].TryGetData<ArenaTeamClientSettings>(out var tb))
                    {
                        var alListItems = arenaTeamComboBox.GetItemList();
                        for (int i = 0; i < alListItems.Length; i++)
                        {
                            if (alListItems[i].name == value)
                            {
                                tb.team = i;
                            }
                        }

                    }
                };

                var martyrTeamLabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("Team 1:"), new Vector2(arenaGameModeLabel.pos.x, arenaTeamComboBox.pos.y - 45), new Vector2(0, 20), false);

                martyrsTeamNameUpdate = new(new Configurable<string>(RainMeadow.rainMeadowOptions.MartyrTeamName.Value), new(martyrTeamLabel.pos.x + 50, martyrTeamLabel.pos.y), 150);
                if (!OnlineManager.lobby.isOwner)
                {
                    martyrsTeamNameUpdate.Deactivate();
                }

                martyrsTeamNameUpdate.allowSpace = true;
                martyrsTeamNameUpdate.OnValueUpdate += (config, value, lastValue) =>
                {
                    var alListItems = arenaTeamComboBox.GetItemList();
                    RainMeadow.rainMeadowOptions.MartyrTeamName.Value = value;
                    alListItems[0].name = value;
                    alListItems[0].desc = value;
                    alListItems[0].displayName = value;
                    tb.martyrsTeamName = value;
                };

                var outlawTeamlabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("Team 2:"), new Vector2(arenaGameModeLabel.pos.x, martyrsTeamNameUpdate.pos.y - 45), new Vector2(0, 20), false);

                outlawsTeamNameUpdate = new(new Configurable<string>(RainMeadow.rainMeadowOptions.OutlawsTeamName.Value), new(outlawTeamlabel.pos.x + 50, outlawTeamlabel.pos.y), 150);
                if (!OnlineManager.lobby.isOwner)
                {
                    outlawsTeamNameUpdate.Deactivate();
                }

                outlawsTeamNameUpdate.allowSpace = true;
                outlawsTeamNameUpdate.OnValueUpdate += (config, value, lastValue) =>
                {
                    var alListItems = arenaTeamComboBox.GetItemList();
                    RainMeadow.rainMeadowOptions.OutlawsTeamName.Value = value;
                    alListItems[1].name = value;
                    alListItems[1].desc = value;
                    alListItems[1].displayName = value;
                    tb.outlawTeamNames = value;
                };
                ///
                var dragonSlayersLabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("Team 3:"), new Vector2(arenaGameModeLabel.pos.x, outlawsTeamNameUpdate.pos.y - 45), new Vector2(0, 20), false);

                dragonsSlayersTeamNameUpdate = new(new Configurable<string>(RainMeadow.rainMeadowOptions.DragonSlayersTeamName.Value), new(dragonSlayersLabel.pos.x + 50, dragonSlayersLabel.pos.y), 150);
                if (!OnlineManager.lobby.isOwner)
                {
                    dragonsSlayersTeamNameUpdate.Deactivate();
                }

                dragonsSlayersTeamNameUpdate.allowSpace = true;
                dragonsSlayersTeamNameUpdate.OnValueUpdate += (config, value, lastValue) =>
                {
                    var alListItems = arenaTeamComboBox.GetItemList();
                    RainMeadow.rainMeadowOptions.DragonSlayersTeamName.Value = value;
                    alListItems[2].name = value;
                    alListItems[2].desc = value;
                    alListItems[2].displayName = value;
                    tb.dragonSlayersTeamNames = value;
                };


                ///
                var chifetainTeamLabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("Team 4:"), new Vector2(arenaGameModeLabel.pos.x, dragonsSlayersTeamNameUpdate.pos.y - 45), new Vector2(0, 20), false);

                chieftainsTeamNameUpdate = new(new Configurable<string>(RainMeadow.rainMeadowOptions.ChieftainTeamName.Value), new(chifetainTeamLabel.pos.x + 50, chifetainTeamLabel.pos.y), 150);
                if (!OnlineManager.lobby.isOwner)
                {
                    chieftainsTeamNameUpdate.Deactivate();
                }

                chieftainsTeamNameUpdate.allowSpace = true;
                chieftainsTeamNameUpdate.OnValueUpdate += (config, value, lastValue) =>
                {
                    var alListItems = arenaTeamComboBox.GetItemList();
                    RainMeadow.rainMeadowOptions.ChieftainTeamName.Value = value;
                    alListItems[3].name = value;
                    alListItems[3].desc = value;
                    alListItems[3].displayName = value;
                    tb.chieftainsTeamNames = value;
                };

                externalModeWrapper = new UIelementWrapper(tabWrapper, arenaTeamComboBox);

                martyrColor = new OpTinyColorPicker(menu, new Vector2(martyrsTeamNameUpdate.pos.x + martyrsTeamNameUpdate.rect.size.x + 50, martyrsTeamNameUpdate.pos.y), TeamColors[0]);
                UIelementWrapper martyrColorsWrapper = new UIelementWrapper(tabWrapper, martyrColor);

                dragonSlayerColor = new OpTinyColorPicker(menu, new Vector2(dragonsSlayersTeamNameUpdate.pos.x + 50, dragonsSlayersTeamNameUpdate.pos.y), TeamColors[1]);
                UIelementWrapper dragonSlayerColorsWrapper = new UIelementWrapper(tabWrapper, dragonSlayerColor);



                UIelementWrapper martyrWrapper = new UIelementWrapper(tabWrapper, martyrsTeamNameUpdate);
                UIelementWrapper outlawWrapper = new UIelementWrapper(tabWrapper, outlawsTeamNameUpdate);
                UIelementWrapper dragonSlayerWrapper = new UIelementWrapper(tabWrapper, dragonsSlayersTeamNameUpdate);
                UIelementWrapper chiefTainWrapper = new UIelementWrapper(tabWrapper, chieftainsTeamNameUpdate);

                martyrColor.OnValueChangedEvent += ColorSelector_OnValueChangedEvent;


                extComp.SafeAddSubobjects(tabWrapper, martyrColorsWrapper, dragonSlayerColorsWrapper, externalModeWrapper, arenaGameModeLabel, martyrWrapper, martyrTeamLabel, outlawWrapper, outlawTeamlabel, dragonSlayerWrapper, dragonSlayersLabel, chiefTainWrapper, chifetainTeamLabel);
            }
        }
        OpTinyColorPicker martyrColor;
        OpTinyColorPicker dragonSlayerColor;

        private void ColorSelector_OnValueChangedEvent()
        {
            TeamColors[0] = Extensions.SafeColorRange(martyrColor.valuecolor);
        }

        public override void ArenaExternalGameModeSettingsInterface_Update(ArenaMode arena, OnlineArenaExternalGameModeSettingsInterface extComp, Menu.Menu menu, Menu.MenuObject owner, MenuTabWrapper tabWrapper, Vector2 pos, float settingsWidth = 300)
        {
            if (arenaTeamComboBox != null)
            {
                if (arenaTeamComboBox.greyedOut = arena.currentGameMode != TeamBattleMode.TeamBattle.value)
                    if (!arenaTeamComboBox.held && !teamComboBoxLastHeld) arenaTeamComboBox.value = OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaTeamClientSettings>().team.ToString();
            }

        }


        public override void ArenaPlayerBox_GrafUpdate(ArenaMode arena, float timestacker, bool showRainbow, Color rainbow, FLabel pingLabel, FSprite[] sprites, List<UiLineConnector> lines, MenuLabel selectingStatusLabel, ProperlyAlignedMenuLabel nameLabel, OnlinePlayer profileIdentifier, SlugcatColorableButton slugcatButton)
        {

            if (TeamBattleMode.isTeamBattleMode(arena, out var tb))
            {
                if (OnlineManager.lobby.clientSettings.TryGetValue(profileIdentifier, out var clientSettings))
                {

                    if (clientSettings.TryGetData<ArenaTeamClientSettings>(out var team))
                    {
                        if (team.team == tb.winningTeam && tb.winningTeam != -1)
                        {
                            slugcatButton.secondaryColor = rainbow;
                        }
                        else
                        {
                            slugcatButton.secondaryColor = tb.TeamColors[team.team];
                        }
                    }

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
                    OnlineManager.lobby.clientSettings[playerA].TryGetData<ArenaTeamClientSettings>(out var team);
                    OnlineManager.lobby.clientSettings[playerB].TryGetData<ArenaTeamClientSettings>(out var team2);

                    if (team != null && team2 != null)
                    {
                        if (team.team != team2.team)
                        {
                            return team.team == tb.winningTeam;
                        }
                        //if (team.team == tb.winningTeam && team2.team == tb.winningTeam)
                        //{
                        //    if (A.totScore != B.totScore)
                        //    {
                        //        return A.totScore > B.totScore;
                        //    }
                        //    else
                        //    {
                        //        return playerA.isMe;
                        //    }
                        //}

                    }

                }

            }

            return orig(self, A, B);

        }

        public override bool PlayerSessionResultSort(ArenaMode arena, On.ArenaSitting.orig_PlayerSessionResultSort orig, ArenaSitting self, ArenaSitting.ArenaPlayer A, ArenaSitting.ArenaPlayer B)
        {

            if (isTeamBattleMode(arena, out var tb))
            {
                OnlinePlayer? playerA = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, A.playerNumber);
                OnlinePlayer? playerB = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, B.playerNumber);

                if (playerA != null && playerB != null)
                {
                    OnlineManager.lobby.clientSettings[playerA].TryGetData<ArenaTeamClientSettings>(out var team);
                    OnlineManager.lobby.clientSettings[playerB].TryGetData<ArenaTeamClientSettings>(out var team2);

                    if (team != null && team2 != null)
                    {
                        // TODO: Doesn't show winning team on top
                        if (team.team != team2.team)
                        {
                            return team.team == tb.winningTeam;
                        }
                        //if (team.team == tb.winningTeam && team2.team == tb.winningTeam)
                        //{
                        //    if (A.alive != B.alive)
                        //    {
                        //        return A.alive;
                        //    }

                        //}

                    }

                }

            }

            return orig(self, A, B);


        }

        public override string AddGameSettingsTab()
        {
            return "Team Settings";
        }
    }
}

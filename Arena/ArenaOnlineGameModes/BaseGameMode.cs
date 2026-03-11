using System.Collections.Generic;
using System.Linq;
using Menu;
using Menu.Remix.MixedUI;
using RainMeadow.Arena.ArenaOnlineGameModes.ArenaChallengeModeNS;
using RainMeadow.UI;
using RainMeadow.UI.Components;
using UnityEngine;

namespace RainMeadow
{
    public abstract class ExternalArenaGameMode
    {
        private int _timerDuration;
        public OnlineArenaBaseGameModeTab? arenaBaseGameModeTab;
        public TabContainer.Tab? myTab;

        public abstract ArenaSetup.GameTypeID GetGameModeId { get; set; }

        public virtual void ResetOnSessionEnd() { }

        public abstract bool IsExitsOpen(
            ArenaOnlineGameMode arena,
            On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig,
            ArenaBehaviors.ExitManager self
        );
        public abstract bool SpawnBatflies(FliesWorldAI self, int spawnRoom);

        public abstract int TimerDuration { get; set; }

        public virtual void ArenaSessionCtor(
            ArenaOnlineGameMode arena,
            On.ArenaGameSession.orig_ctor orig,
            ArenaGameSession self,
            RainWorldGame game
        )
        {
            arena.session = self;
            arena.ResetAtSession_ctor();
        }

        public virtual void ArenaSessionNextLevel(
            ArenaOnlineGameMode arena,
            On.ArenaSitting.orig_NextLevel orig,
            ArenaSitting self,
            ProcessManager process
        )
        {
            arena.ResetAtNextLevel();
        }

        /// <summary> Used for managing winner conditions, after the list is originally sorted but before the overlay is initialized </summary>


        public virtual void InitAsCustomGameType(ArenaOnlineGameMode arena, ArenaSetup.GameTypeSetup self)
        {
            self.foodScore = 0;
            self.survivalScore = arena.aliveScore;
            self.spearHitScore = arena.spearScore;
            self.repeatSingleLevelForever = false;
            self.savingAndLoadingSession = true;
            self.denEntryRule = arena.denEntryRule;
            self.rainWhenOnePlayerLeft = true;
            self.levelItems = true;
            self.fliesSpawn = false;
            self.saveCreatures = false;
            self.gameType = ArenaSetup.GameTypeID.Competitive;

        }

        public string PlayingAsText()
        {
            var clientSettings = OnlineManager
                .lobby.clientSettings[OnlineManager.mePlayer]
                .GetData<ArenaClientSettings>();
            if (
                ModManager.MSC
                && clientSettings.playingAs
                    == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel
            )
            {
                return (OnlineManager.lobby.gameMode as ArenaOnlineGameMode)?.paincatName
                    ?? SlugcatStats.getSlugcatName(clientSettings.playingAs);
            }
            else if (
                clientSettings.playingAs == RainMeadow.Ext_SlugcatStatsName.OnlineRandomSlugcat
            )
            {
                return SlugcatStats.getSlugcatName(clientSettings.randomPlayingAs);
            }
            else
            {
                return SlugcatStats.getSlugcatName(clientSettings.playingAs);
            }
        }

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

        public virtual int TimerDirection(ArenaOnlineGameMode arena, int timer)
        {
            return --timer;
        }

        /// <summary> This is ran on the victim's end, not the killer's! </summary>
        public virtual void Killing(
            ArenaOnlineGameMode arena,
            On.ArenaGameSession.orig_Killing orig,
            ArenaGameSession self,
            Player player,
            Creature killedCrit
        )
        {
            RainMeadow.Debug(this);

            if (!OnlineCreature.map.TryGetValue(player.abstractCreature, out var absPlayerCreature))
            {
                RainMeadow.Error("Error getting abs Player Creature");
                return;
            }

            if (!OnlineCreature.map.TryGetValue(killedCrit.abstractCreature, out var onlineKilledCreature))
            {
                RainMeadow.Error("Error getting targetAbsCreature");
                return;
            }

            if (self.sessionEnded || (ModManager.MSC && player.AI != null))
            {
                return;
            }

            IconSymbol.IconSymbolData iconSymbolData = CreatureSymbol.SymbolDataFromCreature(killedCrit.abstractCreature);
            bool earnsTrophy = CreatureSymbol.DoesCreatureEarnATrophy(killedCrit.Template.type);

            for (int i = 0; i < self.arenaSitting.players.Count; i++)
            {
                OnlinePlayer? onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, self.arenaSitting.players[i].playerNumber);
                if (onlinePlayer == null)
                {
                    continue;
                }
                if (absPlayerCreature.owner != onlinePlayer)
                {
                    continue;
                }

                if (earnsTrophy)
                {

                    int unlockIndex = MultiplayerUnlocks.SandboxUnlockForSymbolData(iconSymbolData).Index;
                    int scoreToAdd = arena.spearScore;
                    self.arenaSitting.players[i].roundKills.Add(iconSymbolData);
                    self.arenaSitting.players[i].allKills.Add(iconSymbolData);
                    self.arenaSitting.players[i].score += scoreToAdd;

                    RainMeadow.Debug($"==== {self.arenaSitting.players[i].playerNumber} EARNED {scoreToAdd}");

                    if (OnlineManager.lobby.isOwner)
                    {
                        string trophyString = iconSymbolData.ToString();
                        ushort lobbyId = absPlayerCreature.owner.inLobbyId;

                        arena.playerNumberWithTrophies[lobbyId].Add(trophyString);
                        arena.playerNumberWithTrophiesPerRound[lobbyId].Add(trophyString);

                    }

                    // notify the killer about the trophy
                    if (!player.IsLocal())
                    {
                        player.abstractCreature.GetOnlineCreature().owner.InvokeOnceRPC(ArenaRPCs.AddKilledCreatureToHUD, onlineKilledCreature);
                    }
                    // player is local AND killed creature is local
                    else
                    {
                        for (int j = 0; j < self.game.cameras[0].hud.parts.Count; j++)
                        {
                            if (self.game.cameras[0].hud.parts[j] is HUD.PlayerSpecificMultiplayerHud multiHud)
                            {
                                multiHud.killsList.Killing(CreatureSymbol.SymbolDataFromCreature(onlineKilledCreature.apo as AbstractCreature));
                                break;
                            }
                        }
                    }
                }

                break; // We found the player and processed everything, exit the loop
            }
        }

        public virtual void LandSpear(
            ArenaOnlineGameMode arena,
            ArenaGameSession self,
            Player player,
            Creature target,
            ArenaSitting.ArenaPlayer aPlayer
        )
        { }

        public virtual void HUD_InitMultiplayerHud(
            ArenaOnlineGameMode arena,
            HUD.HUD self,
            ArenaGameSession session
        )
        {
            self.AddPart(new HUD.TextPrompt(self));

            if (MatchmakingManager.currentInstance.canSendChatMessages)
                self.AddPart(new ChatHud(self, session.game.cameras[0]));

            self.AddPart(new SpectatorHud(self, session.game.cameras[0]));
            self.AddPart(new ArenaPrepTimer(self, self.fContainers[0], arena, session));
            self.AddPart(new OnlineHUD(self, session.game.cameras[0], arena));
            self.AddPart(new Pointing(self));
            self.AddPart(new ArenaSpawnLocationIndicator(self, session.game.cameras[0]));
            self.AddPart(new Watcher.CamoMeter(self, null, self.fContainers[1]));
            if (
                ModManager.Watcher
                && OnlineManager
                    .lobby.clientSettings[OnlineManager.mePlayer]
                    .GetData<ArenaClientSettings>()
                    .playingAs == Watcher.WatcherEnums.SlugcatStatsName.Watcher
            )
            {
                RainMeadow.Debug("Adding Watcher Camo Meter");
                self.AddPart(new Watcher.CamoMeter(self, null, self.fContainers[1]));
            }
            var psmh = new HUD.PlayerSpecificMultiplayerHud(self, session, session.Players.FirstOrDefault(x => x != null && x.IsLocal()));
            psmh.cornerPos = new Vector2(self.rainWorld.options.ScreenSize.x - self.rainWorld.options.SafeScreenOffset.x, 20f + self.rainWorld.options.SafeScreenOffset.y);
            psmh.flip = -1;
            psmh.parts.RemoveAll(x => x is HUD.PlayerSpecificMultiplayerHud.PlayerArrow);
            var killsList = new HUD.PlayerSpecificMultiplayerHud.KillList(psmh);
            var scoreCounter = new HUD.PlayerSpecificMultiplayerHud.ScoreCounter(psmh);
            scoreCounter.scoreText.color = Color.white; // can't see crap
            scoreCounter.lightGradient.color = Color.white;
            psmh.parts.Add(killsList);
            psmh.parts.Add(scoreCounter);
            self.AddPart(psmh);
        }

        public virtual void ArenaCreatureSpawner_SpawnCreatures(
            ArenaOnlineGameMode arena,
            On.ArenaCreatureSpawner.orig_SpawnArenaCreatures orig,
            RainWorldGame game,
            ArenaSetup.GameTypeSetup.WildLifeSetting wildLifeSetting,
            ref List<AbstractCreature> availableCreatures,
            ref MultiplayerUnlocks unlocks
        )
        { }

        public virtual bool HoldFireWhileTimerIsActive(ArenaOnlineGameMode arena)
        {
            return arena.countdownInitiatedHoldFire = false;
        }

        public virtual string AddIcon(
            ArenaOnlineGameMode arena,
            PlayerSpecificOnlineHud owner,
            SlugcatCustomization customization,
            OnlinePlayer player
        )
        {
            return "";
        }

        public virtual Color IconColor(
            ArenaOnlineGameMode arena,
            OnlinePlayerDisplay display,
            PlayerSpecificOnlineHud owner,
            SlugcatCustomization customization,
            OnlinePlayer player
        )
        {
            Color.RGBToHSV(customization.SlugcatColor(), out var H, out var S, out var V);
            if (V < 0.8)
            {
                return Color.HSVToRGB(H, S, 0.8f);
            }
            return customization.SlugcatColor();
        }

        public virtual List<ListItem> ArenaOnlineInterfaceListItems(ArenaOnlineGameMode arena)
        {
            return null;
        }

        /// <summary>
        /// Spawns a creature in an online space
        /// </summary>
        /// <param name="arena"></param>
        /// <param name="self"></param>
        /// <param name="room"></param>
        /// <param name="randomExitIndex"></param>
        /// <param name="templateType"></param>
        public void SpawnTransferableCreature(
            ArenaOnlineGameMode arena,
            ArenaGameSession self,
            Room room,
            int randomExitIndex,
            CreatureTemplate.Type templateType
        )
        {
            AbstractCreature abstractCreature = new AbstractCreature(
                self.game.world,
                StaticWorld.GetCreatureTemplate(templateType),
                null,
                new WorldCoordinate(0, -1, -1, -1),
                new EntityID(-1, 0)
            );
            abstractCreature.pos.room = self.game.world.GetAbstractRoom(0).index;
            abstractCreature.pos.abstractNode = room.ShortcutLeadingToNode(
                randomExitIndex
            ).destNode;
            abstractCreature.Room.AddEntity(abstractCreature);
            abstractCreature.RealizeInRoom();
            self.game.world.GetResource().ApoEnteringWorld(abstractCreature);
        }

        /// <summary>
        /// Spawns a player-controlled avatar in an online space
        /// </summary>
        /// <param name="arena"></param>
        /// <param name="self"></param>
        /// <param name="room"></param>
        /// <param name="randomExitIndex"></param>
        /// <param name="templateType"></param>
        public void SpawnNonTransferableCreature(
            ArenaOnlineGameMode arena,
            ArenaGameSession self,
            Room room,
            int randomExitIndex,
            CreatureTemplate.Type templateType
        )
        {
            RainMeadow.Debug("Trying to create an abstract creature");
            RainMeadow.Debug($"RANDOM EXIT INDEX: {randomExitIndex}");
            RainMeadow.Debug(
                $"RANDOM START TILE INDEX: {room.ShortcutLeadingToNode(randomExitIndex).StartTile}"
            );
            RainMeadow.sSpawningAvatar = true;
            AbstractCreature abstractCreature = new AbstractCreature(
                self.game.world,
                StaticWorld.GetCreatureTemplate(templateType),
                null,
                new WorldCoordinate(0, -1, -1, -1),
                new EntityID(-1, 0)
            );
            abstractCreature.pos.room = self.game.world.GetAbstractRoom(0).index;
            abstractCreature.pos.abstractNode = room.ShortcutLeadingToNode(
                randomExitIndex
            ).destNode;
            abstractCreature.Room.AddEntity(abstractCreature);
            RainMeadow.Debug("assigned ac, registering");
            self.game.world.GetResource().ApoEnteringWorld(abstractCreature);
            RainMeadow.sSpawningAvatar = false;
            self.game.cameras[0].followAbstractCreature = abstractCreature;

            if (
                abstractCreature.GetOnlineObject(out var oe)
                && oe.TryGetData<SlugcatCustomization>(out var customization)
            )

            {
                abstractCreature.state = new PlayerState(
                    abstractCreature,
                    0,
                    customization.playingAs,
                    isGhost: false
                );
            }
            else
            {
                RainMeadow.Error("Could not get online owner for spawned player!");
                abstractCreature.state = new PlayerState(
                    abstractCreature,
                    0,
                    self.arenaSitting
                        .players[ArenaHelpers.FindOnlinePlayerNumber(arena, OnlineManager.mePlayer)]
                        .playerClass,
                    isGhost: false
                );
            }

            RainMeadow.Debug("Arena: Realize Creature!");
            abstractCreature.Realize();
            var shortCutVessel = new ShortcutHandler.ShortCutVessel(
                room.ShortcutLeadingToNode(randomExitIndex).DestTile,
                abstractCreature.realizedCreature,
                self.game.world.GetAbstractRoom(0),
                0
            );

            shortCutVessel.entranceNode = abstractCreature.pos.abstractNode;
            shortCutVessel.room = self.game.world.GetAbstractRoom(abstractCreature.Room.name);

            self.game.shortcuts.betweenRoomsWaitingLobby.Add(shortCutVessel);
            self.AddPlayer(abstractCreature);
            if (SpecialEvents.IsSpecialEventInLobby && SpecialEvents.GetActiveEvent() is SpecialEvents.AprilFools a)
            {
                a.SpawnSnails(shortCutVessel.room.realizedRoom, shortCutVessel);
            }
            if (abstractCreature.realizedCreature is not Player)
            {
                return;
            }
            if (
                (abstractCreature.realizedCreature as Player).SlugCatClass
                == SlugcatStats.Name.Night
            )
            {
                (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;
            }
            if (ModManager.MSC)
            {
                if (
                    (abstractCreature.realizedCreature as Player).SlugCatClass
                    == SlugcatStats.Name.Red
                )
                {
                    self.creatureCommunities.SetLikeOfPlayer(
                        CreatureCommunities.CommunityID.All,
                        -1,
                        0,
                        -0.75f
                    );
                    self.creatureCommunities.SetLikeOfPlayer(
                        CreatureCommunities.CommunityID.Scavengers,
                        -1,
                        0,
                        0.5f
                    );
                }

                if (
                    (abstractCreature.realizedCreature as Player).SlugCatClass
                    == SlugcatStats.Name.Yellow
                )
                {
                    self.creatureCommunities.SetLikeOfPlayer(
                        CreatureCommunities.CommunityID.All,
                        -1,
                        0,
                        0.75f
                    );
                    self.creatureCommunities.SetLikeOfPlayer(
                        CreatureCommunities.CommunityID.Scavengers,
                        -1,
                        0,
                        0.3f
                    );
                }

                if (
                    (abstractCreature.realizedCreature as Player).SlugCatClass
                    == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer
                )
                {
                    self.creatureCommunities.SetLikeOfPlayer(
                        CreatureCommunities.CommunityID.All,
                        -1,
                        0,
                        -0.5f
                    );
                    self.creatureCommunities.SetLikeOfPlayer(
                        CreatureCommunities.CommunityID.Scavengers,
                        -1,
                        0,
                        -1f
                    );
                }

                if (
                    (abstractCreature.realizedCreature as Player).SlugCatClass
                    == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup
                )
                {
                    (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;
                }

                if (
                    (abstractCreature.realizedCreature as Player).SlugCatClass
                    == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel
                )
                {
                    (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill =
                        arena.painCatThrowingSkill;
                    RainMeadow.Debug(
                        "ENOT THROWING SKILL "
                            + (abstractCreature.realizedCreature as Player)
                                .slugcatStats
                                .throwingSkill
                    );
                    if (
                        (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill
                            == 0
                        && arena.painCatEgg
                    )
                    {
                        AbstractPhysicalObject bringThePain = new AbstractPhysicalObject(
                            room.world,
                            DLCSharedEnums.AbstractObjectType.SingularityBomb,
                            null,
                            abstractCreature.pos,
                            shortCutVessel.room.world.game.GetNewID()
                        );
                        room.abstractRoom.AddEntity(bringThePain);
                        bringThePain.RealizeInRoom();

                        self.room.world.GetResource().ApoEnteringWorld(bringThePain);
                        self.room.abstractRoom.GetResource()
                            ?.ApoEnteringRoom(bringThePain, bringThePain.pos);
                    }

                    if (arena.lizardEvent == 99 && arena.painCatLizard)
                    {
                        self.creatureCommunities.SetLikeOfPlayer(
                            CreatureCommunities.CommunityID.Lizards,
                            -1,
                            0,
                            1f
                        );
                        AbstractCreature bringTheTrain = new AbstractCreature(
                            room.world,
                            StaticWorld.GetCreatureTemplate("Red Lizard"),
                            null,
                            room.GetWorldCoordinate(shortCutVessel.pos),
                            shortCutVessel.room.world.game.GetNewID()
                        ); // Train too big :(
                        room.abstractRoom.AddEntity(bringTheTrain);
                        bringTheTrain.Realize();
                        bringTheTrain.realizedCreature.PlaceInRoom(room);

                        self.room.world.GetResource().ApoEnteringWorld(bringTheTrain);
                        self.room.abstractRoom.GetResource()
                            ?.ApoEnteringRoom(bringTheTrain, bringTheTrain.pos);
                    }
                }

                if (
                    (abstractCreature.realizedCreature as Player).SlugCatClass
                    == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint
                )
                {
                    if (!arena.sainot) // ascendance saint
                    {
                        (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill =
                            0;
                    }
                    else
                    {
                        (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill =
                            1;
                    }
                }
            }

            if (
                ModManager.Watcher
                && (abstractCreature.realizedCreature as Player).SlugCatClass
                    == Watcher.WatcherEnums.SlugcatStatsName.Watcher
            )
            {
                (abstractCreature.realizedCreature as Player).enterIntoCamoDuration = 40;
            }
        }

        public virtual void SpawnPlayer(
            ArenaOnlineGameMode arena,
            ArenaGameSession self,
            Room room,
            List<int> suggestedDens
        )
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

            int totalExits = self.game.world.GetAbstractRoom(0).exits;
            int[] exitScores = new int[totalExits];
            if (suggestedDens != null)
            {
                for (int k = 0; k < suggestedDens.Count; k++)
                {
                    if (suggestedDens[k] >= 0 && suggestedDens[k] < exitScores.Length)
                    {
                        exitScores[suggestedDens[k]] -= 1000;
                    }
                }
            }

            int randomExitIndex = UnityEngine.Random.Range(0, totalExits);
            float highestScore = float.MinValue;

            for (int currentExitIndex = 0; currentExitIndex < totalExits; currentExitIndex++)
            {
                float score =
                    UnityEngine.Random.value - (float)exitScores[currentExitIndex] * 1000f;
                RWCustom.IntVector2 startTilePosition = room.ShortcutLeadingToNode(
                    currentExitIndex
                ).StartTile;

                for (int otherExitIndex = 0; otherExitIndex < totalExits; otherExitIndex++)
                {
                    if (otherExitIndex != currentExitIndex && exitScores[otherExitIndex] > 0)
                    {
                        float distanceAdjustment =
                            Mathf.Clamp(
                                startTilePosition.FloatDist(
                                    room.ShortcutLeadingToNode(otherExitIndex).StartTile
                                ),
                                8f,
                                17f
                            ) * UnityEngine.Random.value;
                        score += distanceAdjustment;
                    }
                }

                if (score > highestScore)
                {
                    randomExitIndex = currentExitIndex;
                    highestScore = score;
                }
            }

            if (
                ArenaHelpers.GetArenaClientSettings(OnlineManager.mePlayer)!.playingAs
                == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator
            )
            {
                RainMeadow.Debug("Player spawned as overseer");
                // maybr add toggle later
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

            if (
                OnlineManager.lobby.isOwner
                && ModManager.MSC
                && room.abstractRoom.name == "Chal_AI"
                && self.GameTypeSetup.gameType == DLCSharedEnums.GameTypeID.Challenge
            )
            {
                Oracle obj = new Oracle(
                    new AbstractPhysicalObject(
                        self.game.world,
                        AbstractPhysicalObject.AbstractObjectType.Oracle,
                        null,
                        new WorldCoordinate(room.abstractRoom.index, 15, 15, -1),
                        self.game.GetNewID()
                    ),
                    room
                );
                room.AddObject(obj);
            }
        }

        public virtual void ArenaSessionUpdate(
            On.ArenaGameSession.orig_Update orig,
            ArenaGameSession self,
            ArenaOnlineGameMode arena
        )
        {
            bool isOwnerOverseer =
                ArenaHelpers.GetArenaClientSettings(OnlineManager.lobby.owner)?.playingAs
                == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator;
            if (arena.countdownInitiatedHoldFire && isOwnerOverseer)
            {
                self.endSessionCounter = 30;
            }
            orig(self);

            if (arena.currentLobbyOwner != OnlineManager.lobby.owner)
            {
                self.game.manager.RequestMainProcessSwitch(
                    ProcessManager.ProcessID.MultiplayerResults
                );
                arena.currentLobbyOwner = OnlineManager.lobby.owner;
            }

            int activePlayerCountWithOverseers = arena
                .arenaSittingOnlineOrder.Select(id => ArenaHelpers.FindOnlinePlayerByLobbyId(id)) // Get the player
                .Where(player => player != null) // Ensure player exists
                .Select(player => ArenaHelpers.GetArenaClientSettings(player)) // Get settings
                .Where(settings => settings != null) // Ensure settings exist
                .Count(settings =>
                    settings.playingAs == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator
                );
            if (
                self.Players.Count + activePlayerCountWithOverseers
                != arena.arenaSittingOnlineOrder.Count
            )
            {
                RainMeadow.Trace(
                    $"Arena: Abstract Creature count does not equal registered players in the online Sitting! AC Count: {self.Players.Count} | ArenaSittingOnline Count: {arena.arenaSittingOnlineOrder.Count}"
                );

                var extraPlayers = self.Players.Skip(arena.arenaSittingOnlineOrder.Count).ToList();

                self.Players.RemoveAll(p => extraPlayers.Contains(p));

                foreach (
                    var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value)
                )
                {
                    if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none)
                        continue; // not in game
                    if (
                        playerAvatar.FindEntity(true) is OnlinePhysicalObject opo
                        && opo.apo is AbstractCreature ac
                        && !self.Players.Contains(ac)
                    ) //&& ac.state.alive
                    {
                        self.Players.Add(ac);
                    }
                }
            }
            if (OnlineManager.lobby.isOwner)
            {
                arena.playersEqualToOnlineSitting =
                    self.Players.Count + activePlayerCountWithOverseers
                    == arena.arenaSittingOnlineOrder.Count;
            }

            if (!self.sessionEnded)
            {
                foreach (var s in self.arenaSitting.players)
                {
                    var os = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, s.playerNumber); // current player
                    {
                        for (int i = 0; i < self.Players.Count; i++)
                        {
                            if (
                                OnlinePhysicalObject.map.TryGetValue(
                                    self.Players[i],
                                    out var onlineC
                                )
                            )
                            {
                                if (
                                    onlineC.owner == os
                                    && self.Players[i].realizedCreature != null
                                    && !self.Players[i].realizedCreature.State.dead
                                )
                                {
                                    s.timeAlive++;
                                }
                            }
                            else
                            {
                                if (self.Players[i].state.alive) // alive and without an owner? Die
                                {
                                    self.Players[i].Die();
                                }
                            }
                        }
                    }
                }
            }
        }
        public virtual void ArenaSessionEnded(
            ArenaOnlineGameMode arena,
            On.ArenaSitting.orig_SessionEnded orig,
            ArenaSitting self,
            ArenaGameSession session,
            List<ArenaSitting.ArenaPlayer> list
        )
        {
            int foodScore = self.gameTypeSetup.foodScore;
            bool countFood = foodScore != 0 && System.Math.Abs(foodScore) < 100;

            for (int i = 0; i < self.players.Count; i++)
            {
                var arenaPlayer = self.players[i];

                if (session.Players != null && i < session.Players.Count)
                {
                    var sessionPlayer = session.Players[i];

                    if (countFood)
                    {
                        if (sessionPlayer.state is PlayerState playerState)
                        {
                            arenaPlayer.score += playerState.foodInStomach * foodScore;
                        }

                        var creature = sessionPlayer.realizedCreature;
                        if (creature != null)
                        {
                            foreach (var grasp in creature.grasps)
                            {
                                if (grasp?.grabbed is IPlayerEdible edible)
                                {
                                    arenaPlayer.score += edible.FoodPoints * foodScore;
                                }
                            }
                        }
                    }
                }
                arenaPlayer.alive = session.EndOfSessionLogPlayerAsAlive(arenaPlayer.playerNumber);
                if (arenaPlayer.alive)
                {
                    arenaPlayer.AddSandboxScore(arena.aliveScore);
                }

                arenaPlayer.score += 100 * arenaPlayer.sandboxWin;
            }

            for (int m = 0; m < self.players.Count; m++)
            {
                ArenaSitting.ArenaPlayer arenaPlayer = self.players[m];
                bool inserted = false;
                for (int n = 0; n < list.Count; n++)
                {
                    if (self.PlayerSessionResultSort(arenaPlayer, list[n]))
                    {
                        list.Insert(n, arenaPlayer);
                        inserted = true;
                        break;
                    }
                }

                if (!inserted)
                {
                    list.Add(arenaPlayer);
                }
            }

            if (list.Count == 1)
            {
                list[0].winner = list[0].alive;
            }
            else if (list.Count > 1)
            {

                // if survivalScore && killScore are 0, then this should skip 
                if (list[0].score > list[1].score)
                {
                    list[0].winner = true;
                }
                else if (list[0].alive && !list[1].alive)
                {
                    list[0].winner = true;
                }

            }

            for (int x = 0; x < list.Count; x++)
            {
                var sortedPlayer = list[x];
                if (sortedPlayer.winner)
                {
                    sortedPlayer.wins++;
                }

                if (!sortedPlayer.alive)
                {
                    sortedPlayer.deaths++;
                }

                sortedPlayer.totScore += sortedPlayer.score;

                OnlinePlayer? pl = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(
                    arena,
                    sortedPlayer.playerNumber
                );

                if (pl != null)
                {
                    if (OnlineManager.lobby.isOwner)
                    {
                        arena.AddOrInsertPlayerStats(arena, sortedPlayer, pl);
                    }
                    else
                    {
                        arena.ReadFromStats(sortedPlayer, pl);
                    }
                }
            }

            session.game.arenaOverlay = new Menu.ArenaOverlay(session.game.manager, self, list);
            session.game.manager.sideProcesses.Add(session.game.arenaOverlay);
        }
        public virtual bool PlayerSessionResultSort(
            ArenaOnlineGameMode arena,
            On.ArenaSitting.orig_PlayerSessionResultSort orig,
            ArenaSitting self,
            ArenaSitting.ArenaPlayer A,
            ArenaSitting.ArenaPlayer B
        )
        {
            return orig(self, A, B);
        }

        public virtual bool PlayerSittingResultSort(
            ArenaOnlineGameMode arena,
            On.ArenaSitting.orig_PlayerSittingResultSort orig,
            ArenaSitting self,
            ArenaSitting.ArenaPlayer A,
            ArenaSitting.ArenaPlayer B
        )
        {
            RainMeadow.Debug(
                $"PlayerSittingResultSort Player A: Score: {A.score} - Wins: {A.wins} - All Kills: {A.allKills.Count} - Deaths: {A.deaths}"
            );
            RainMeadow.Debug(
                $"PlayerSittingResultSort Player B: Score: {B.score} - Wins: {B.wins} - All Kills: {B.allKills.Count} - Deaths: {B.deaths}"
            );

            return orig(self, A, B);
        }

        public virtual bool DidPlayerWinRainbow(ArenaOnlineGameMode arena, OnlinePlayer player) =>
            arena.reigningChamps.list.Contains(player.id);

        public virtual void OnUIEnabled(ArenaOnlineLobbyMenu menu)
        {
            myTab = new(menu, menu.arenaMainLobbyPage.tabContainer);
            myTab.AddObjects(
                arenaBaseGameModeTab = new OnlineArenaBaseGameModeTab(
                    myTab.menu,
                    myTab,
                    new(0, 0),
                    menu.arenaMainLobbyPage.tabContainer.size
                )
            );
            menu.arenaMainLobbyPage.tabContainer.AddTab(
                myTab,
                menu.Translate("Arena Settings")
            );
        }

        public virtual void OnUIDisabled(ArenaOnlineLobbyMenu menu)
        {
            arenaBaseGameModeTab?.OnShutdown();
            if (myTab != null)
                menu.arenaMainLobbyPage.tabContainer.RemoveTab(myTab);
            myTab = null;
        }

        public virtual void OnUIUpdate(ArenaOnlineLobbyMenu menu)
        {
            arenaBaseGameModeTab?.Update();
        }

        public virtual void OnUIShutDown(ArenaOnlineLobbyMenu menu)
        {
            arenaBaseGameModeTab?.OnShutdown();
        }

        public virtual Color GetPortraitColor(
            ArenaOnlineGameMode arena,
            OnlinePlayer? player,
            Color origPortraitColor
        ) => origPortraitColor;

        public virtual Dialog AddGameModeInfo(ArenaOnlineGameMode arena, Menu.Menu menu)
        {
            return new DialogNotify(
                menu.LongTranslate("This game mode doesnt have any info to give"),
                new Vector2(500f, 400f),
                menu.manager,
                () =>
                {
                    menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                }
            );
        }

        public virtual Dialog AddPostGameStatsFeed(ArenaOnlineGameMode arena, Menu.Menu menu)
        {
            return new ArenaPostGameStatsDialog(menu.manager, arena);
        }
    }
}

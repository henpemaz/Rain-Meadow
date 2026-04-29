using System.Collections.Generic;
using System.Linq;
using Menu;
using Menu.Remix.MixedUI;
using RainMeadow.Arena.ArenaOnlineGameModes.ArenaChallengeModeNS;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using RainMeadow.UI;
using RainMeadow.UI.Components;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
namespace RainMeadow
{
    public abstract class ExternalArenaGameMode
    {
        private int _timerDuration;
        public OnlineArenaBaseGameModeTab? arenaBaseGameModeTab;
        public TabContainer.Tab? myTab;

        public abstract ArenaSetup.GameTypeID GetGameModeId { get; }

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
            self.foodScore = arena.foodScore;
            self.survivalScore = arena.aliveScore;
            self.spearHitScore = arena.spearHitScore;
            self.repeatSingleLevelForever = false;
            self.savingAndLoadingSession = true;
            self.denEntryRule = arena.denEntryRule;
            self.rainWhenOnePlayerLeft = true;
            self.levelItems = true;
            self.fliesSpawn = false;
            self.saveCreatures = false;
            self.gameType = ArenaSetup.GameTypeID.Competitive;
            self.spearsHitPlayers = arena.onlineArenaSettingsInterfaceeBool["SPEARSHIT"];

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

            // 1. Find the target player first 
            int targetPlayerNumber = -1;
            bool playerFound = false;
            foreach (var sittingPlayer in self.arenaSitting.players)
            {
                OnlinePlayer? onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, sittingPlayer.playerNumber);
                if (onlinePlayer != null && onlinePlayer == absPlayerCreature.owner)
                {
                    targetPlayerNumber = sittingPlayer.playerNumber;
                    playerFound = true;
                    break;
                }
            }

            // 2. Early Exit: If the player isn't relevant to this execution, stop here.
            // 3. Early Exit: Stop processing if the killed creature isn't local.

            if (!playerFound || !RoomSession.map.TryGetValue(self.room.abstractRoom, out var rs)) return;
            if (!killedCrit.abstractCreature.IsLocal()) return;


            ushort lobbyId = absPlayerCreature.owner.inLobbyId;
            bool isLobbyOwner = OnlineManager.lobby.isOwner;

            // 4. Handle Trophies
            // TOOD: Client can sometimes get wrong amount of trophies from one kill
            if (earnsTrophy)
            {
                if (isLobbyOwner)
                {
                    string trophyString = iconSymbolData.ToString();
                    arena.playerNumberWithTrophies[lobbyId].Add(trophyString);
                    arena.playerNumberWithTrophiesPerRound[lobbyId].Add(trophyString);
                }
                else
                {

                    OnlineManager.lobby.owner.InvokeRPC(
                        ArenaRPCs.Arena_AddTrophy,
                        onlineKilledCreature,
                        self.arenaSitting.players[targetPlayerNumber].playerNumber
                    );

                }
                // 5. Handle HUD Updates
                if (player.IsLocal()) // if the player is local then we are seeing this method from a locally killed creature
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
                else
                {
                    player.abstractCreature.GetOnlineCreature()?.owner.InvokeOnceRPC(ArenaRPCs.AddKilledCreatureToHUD, onlineKilledCreature);
                }

            }

            // 6. Handle Scoring 
            int scoreToAdd = arena.killScore;
            if (killedCrit.Template.type != CreatureTemplate.Type.Slugcat)
            {
                if (self.arenaSitting.gameTypeSetup.wildLifeSetting == ArenaSetup.GameTypeSetup.WildLifeSetting.Off)
                {
                    scoreToAdd = 0; // creature got in somehow
                }
            }
            if (arena.externalArenaGameMode is ArenaChallengeMode)
            {
                int index = MultiplayerUnlocks.SandboxUnlockForSymbolData(iconSymbolData).Index;
                scoreToAdd = (index >= 0) ? self.arenaSitting.gameTypeSetup.killScores[index] : 0;
            }

            // this is set locally because we return if the victim is not ours, so we need to notify everyone of this update
            self.arenaSitting.players[targetPlayerNumber].score += scoreToAdd;
            if (isLobbyOwner) // host creature was killed
            {
                arena.playerNumberWithScore[lobbyId] += scoreToAdd;
                onlineKilledCreature.BroadcastRPCInRoomExceptOwners(ArenaRPCs.UpdatePlayerScore, targetPlayerNumber, arena.playerNumberWithScore[lobbyId]);
            }
            else // my creature, not host - tell the room
            {
                onlineKilledCreature.BroadcastRPCInRoom(ArenaRPCs.UpdatePlayerScore, targetPlayerNumber, self.arenaSitting.players[targetPlayerNumber].score);
            }

            // 7.
            if (killedCrit.Template.type == CreatureTemplate.Type.Slugcat)
            {
                RainMeadow.Debug($"RMEL;{absPlayerCreature.owner.id.DisplayName};KILLED;{onlineKilledCreature.owner.id.DisplayName};SCORE;{self.arenaSitting.players[targetPlayerNumber].score}");
                // Cash Money Slugs
                ArenaClientSettings? playerClient = ArenaHelpers.GetArenaClientSettings(absPlayerCreature.owner);
                if ((playerClient != null && playerClient.gotSlugcat) || SpecialEvents.EventActiveInLobby<SpecialEvents.AprilFools>())
                {
                    absPlayerCreature.BroadcastRPCInRoom(ArenaRPCs.ShowMeTheMoney, absPlayerCreature, onlineKilledCreature);
                    if (killedCrit != null)
                    {
                        SpecialEvents.PlayMeadowCoinSound(room: self.room);
                        if (absPlayerCreature.isMine)
                        {
                            SpecialEvents.GainedMeadowCoin(1);
                        }
                        for (int x = 0; x < 20; x++)
                        {
                            self.room.AddObject(new MeadowTokenCoin.MeadowCoin(killedCrit.bodyChunks.OfType<BodyChunk>().First().pos + RWCustom.Custom.RNV() * 2f, RWCustom.Custom.RNV() * 16f * UnityEngine.Random.value, Color.Lerp(Color.yellow, new Color(1f, 1f, 1f), 0.5f + 0.5f * UnityEngine.Random.value), false));
                        }
                    }

                }
            }



        }


        public virtual void LandSpear(
            ArenaOnlineGameMode arena,
            ArenaGameSession self,
            Player player,
            Creature target,
            ArenaSitting.ArenaPlayer aPlayer
        )
        {
            if (!ModManager.MSC)
            {
                RainMeadow.Warn("Player_LandSpear: MSC is not active, returning...");
                return;
            }

            if (player.gourmandExhausted)
            {
                RainMeadow.Warn("Player_LandSpear: Player is exhausted. Spamming hits for score is not allowed, returning...");
                return;
            }

            if (target is Player pl && pl.State is PlayerState st && st.permanentDamageTracking >= 1)
            {
                RainMeadow.Warn("Player_LandSpear: Player is going to die and this will corrupt killing score, returning...");
                return;
            }

            if (TeamBattleMode.isTeamBattleMode(arena, out _) && ArenaHelpers.CheckSameTeam(player.abstractCreature.GetOnlineCreature()?.owner, target.abstractCreature.GetOnlineCreature()?.owner))
            {
                RainMeadow.Warn("Player_LandSpear: Players on same team, returning...");
                return;
            }

            aPlayer.AddSandboxScore(arena.spearHitScore);

            if (OnlineManager.lobby.isOwner)
            {

                OnlinePlayer? onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, aPlayer.playerNumber);
                if (onlinePlayer == null)
                {
                    return;
                }

                if (arena.playerNumberWithScore[onlinePlayer.inLobbyId] < aPlayer.score)
                {
                    arena.playerNumberWithScore[onlinePlayer.inLobbyId] = aPlayer.score;
                }
                player.abstractCreature.GetOnlineCreature()?.BroadcastRPCInRoomExceptOwners(ArenaRPCs.UpdatePlayerScore, aPlayer.playerNumber, arena.playerNumberWithScore[onlinePlayer.inLobbyId]);
            }
            else
            {
                player.abstractCreature.GetOnlineCreature()?.BroadcastRPCInRoom(ArenaRPCs.UpdatePlayerScore, aPlayer.playerNumber, aPlayer.score);
            }
        }

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
            if (OnlineManager
                    .lobby.clientSettings[OnlineManager.mePlayer]
                    .GetData<ArenaClientSettings>()
                    .playingAs == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator
            )
            {
                return;
            }
            var psmh = new HUD.PlayerSpecificMultiplayerHud(self, session, session.Players.FirstOrDefault(x => x != null && x.IsLocal()));
            psmh.cornerPos = new Vector2(self.rainWorld.options.ScreenSize.x - self.rainWorld.options.SafeScreenOffset.x, 20f + self.rainWorld.options.SafeScreenOffset.y);
            psmh.flip = -1;
            psmh.parts.RemoveAll(x => x is HUD.PlayerSpecificMultiplayerHud.PlayerArrow || x is HUD.PlayerSpecificMultiplayerHud.PlayerDeathBump);
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
            OnlinePlayerDisplay display,
            PlayerSpecificOnlineHud owner,
            SlugcatCustomization customization,
            OnlinePlayer player
        )
        {
            bool playerGotSlots = ArenaHelpers.GetArenaClientSettings(player) != null && ArenaHelpers.GetArenaClientSettings(player).gotSlugcat;
            if (SpecialEvents.EventActiveInLobby<SpecialEvents.AprilFools>() || playerGotSlots)
            {
                SpecialEvents.LoadElement("meadowcoin");
                if (display.slugIcon is not null)
                {
                    display.slugIcon.scale = 0.08f;
                }
                return "meadowcoin";
            }

            if (customization.globalMute)
            {
                return "Meadow_Menu_MutePlayerChat00";
            }
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
            //if (SpecialEvents.EventActiveInLobby<SpecialEvents.AprilFools>(out var a))
            //{
            //a.SpawnSnails(shortCutVessel.room.realizedRoom, shortCutVessel);
            //}
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
            if (!OnlineManager.lobby.isOwner && !arena.hostLoadedOverlay)
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
                    if (os != null)
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
    ArenaGameSession session)
        {
            List<ArenaSitting.ArenaPlayer> list = new List<ArenaSitting.ArenaPlayer>();
            int foodScore = self.gameTypeSetup.foodScore;
            bool countFood = foodScore != 0 && System.Math.Abs(foodScore) < 100;
            bool isTeamMode = TeamBattleMode.isTeamBattleMode(arena, out var tb);

            // 1. TALLY SCORES & SURVIVAL STATUS
            for (int i = 0; i < self.players.Count; i++)
            {
                var arenaPlayer = self.players[i];
                var onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, arenaPlayer.playerNumber);
                if (onlinePlayer == null) continue;

                if (arenaPlayer.playerClass == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                {
                    // overseer does not get any love
                    arena.ResetPlayerStats(arenaPlayer);
                    if (OnlineManager.lobby.isOwner)
                    {
                        arena.SetPlayerStatsFromLocalPlayer(arenaPlayer, onlinePlayer);
                    }
                    arena.ReadFromStats(arenaPlayer, onlinePlayer);
                    continue;

                }

                if (session.Players != null && i < session.Players.Count)
                {
                    var sessionPlayer = session.Players[i];

                    if (sessionPlayer?.GetOnlineCreature()?.owner == onlinePlayer)
                    {
                        if (countFood)
                        {
                            if (sessionPlayer.state is PlayerState playerState)
                            {
                                arenaPlayer.score += playerState.foodInStomach * foodScore;
                            }

                            if (sessionPlayer.realizedCreature != null)
                            {
                                foreach (var grasp in sessionPlayer.realizedCreature.grasps)
                                {
                                    if (grasp?.grabbed is IPlayerEdible edible)
                                    {
                                        arenaPlayer.score += edible.FoodPoints * foodScore;
                                    }
                                }
                            }
                        }
                    }
                }

                arenaPlayer.alive = session.EndOfSessionLogPlayerAsAlive(arenaPlayer.playerNumber);

                if (arenaPlayer.alive)
                {
                    arenaPlayer.AddSandboxScore(self.gameTypeSetup.survivalScore);
                }

                arenaPlayer.score += 100 * arenaPlayer.sandboxWin;
                arenaPlayer.winner = false; // Reset winner flag for everyone initially


                if (OnlineManager.lobby.isOwner)
                {
                    arena.SetPlayerStatsFromLocalPlayer(arenaPlayer, onlinePlayer);
                }
                arena.ReadFromStats(arenaPlayer, onlinePlayer);
            }

            // 2. DETERMINE WINNING TEAM (IF IN TEAM MODE) BEFORE SORTING

            if (isTeamMode)
            {
                tb.winningTeam = tb.CalculateTeamScoresAndWinner(self.players, arena, arena.winByScore, true, false);
            }

            // 3. SORT PLAYERS (Using the newly cleaned, pure sort method)
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
            // 4. ASSIGN WINNERS BASED ON GAME MODE
            if (isTeamMode)
            {
                // Everyone on the winning team wins, everyone else loses
                if (tb.winningTeam != -1)
                {
                    for (int x = 0; x < list.Count; x++)
                    {
                        var onlineP = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, list[x].playerNumber);
                        if (onlineP == null) continue;

                        if (OnlineManager.lobby.clientSettings[onlineP].TryGetData<ArenaTeamClientSettings>(out var teamInfo))
                        {
                            list[x].winner = teamInfo.team == tb.winningTeam;
                        }
                    }
                }
            }
            else
            {
                // Standard Free-For-All Logic
                if (list.Count == 1)
                {
                    list[0].winner = list[0].alive;
                }
                else if (list.Count > 1)
                {
                    // if survivalScore && killScore are 0, then this should skip 
                    if (list[0].score > list[1].score && arena.winByScore)
                    {
                        list[0].winner = true;
                    }
                    else if (list[0].alive && !list[1].alive)
                    {
                        list[0].winner = true;
                    }
                }
            }

            // 5. UPDATE TOTALS AND UI
            for (int x = 0; x < list.Count; x++)
            {
                var sortedPlayer = list[x];
                if (sortedPlayer.winner)
                {
                    sortedPlayer.wins++;
                }

                if (!sortedPlayer.alive && sortedPlayer.playerClass != RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                {
                    sortedPlayer.deaths++;
                }

                sortedPlayer.totScore += sortedPlayer.score;

                OnlinePlayer? pl = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, sortedPlayer.playerNumber);
                if (pl == null) continue;

                if (OnlineManager.lobby.isOwner)
                {
                    arena.SetPlayerStatsFromLocalPlayer(sortedPlayer, pl);
                }
            }

            session.game.arenaOverlay = new Menu.ArenaOverlay(session.game.manager, self, list);
            session.game.manager.sideProcesses.Add(session.game.arenaOverlay);
        }

        public virtual List<ArenaSitting.ArenaPlayer> FinalSittingResult(ArenaOnlineGameMode arena,
            On.ArenaSitting.orig_FinalSittingResult orig,
            ArenaSitting self)
        {

            var resultList = orig(self);
            if (resultList.Count > 1)
            {
                foreach (var player in resultList)
                {
                    OnlinePlayer pl = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, player.playerNumber);
                    if (pl == null)
                    {
                        continue;
                    }
                    arena.ReadFromStats(player, pl);
                    player.winner = false;
                }
                // Sort by score if spear score > 0
                resultList.Sort((a, b) =>
                {
                    if (arena.winByScore && a.totScore != b.totScore)
                    {
                        return b.totScore.CompareTo(a.totScore); // Higher score first
                    }

                    return b.wins.CompareTo(a.wins); // Higher wins second
                });

                // Determine the winner 
                var p1 = resultList[0];
                var p2 = resultList[1];
                RainMeadow.Info($"Checking sc:{p1.totScore}, {p2.totScore} ");


                bool winsStrictlyHigher = p1.wins > p2.wins && arena.winByScore == false;
                bool scoreStrictlyHigher = p1.totScore > p2.totScore && arena.winByScore;
                RainMeadow.Info($"Checking wins:{winsStrictlyHigher}, {scoreStrictlyHigher}");

                if (winsStrictlyHigher || scoreStrictlyHigher)
                {
                    p1.winner = true;
                }
            }
            return resultList;
        }
        public virtual bool PlayerSessionResultSort(
            ArenaOnlineGameMode arena,
            On.ArenaSitting.orig_PlayerSessionResultSort orig,
            ArenaSitting self,
            ArenaSitting.ArenaPlayer A,
            ArenaSitting.ArenaPlayer B
        )
        {
            if (A.score != B.score && arena.winByScore)
            {
                return A.score > B.score;
            }
            if (A.alive != B.alive)
            {
                return A.alive;
            }

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

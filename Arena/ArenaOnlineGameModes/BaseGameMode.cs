using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Menu;
using MoreSlugcats;
using RainMeadow.UI;
using RainMeadow.UI.Components;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public abstract class ExternalArenaGameMode
    {
        public abstract ArenaSetup.GameTypeID GetGameModeId { get; }
        private int _timerDuration;
        public abstract int TimerDuration { get; set; }
        public virtual bool ShowAddedScoreBetweenRoundsInOnlinePlayerUI { get; set; } = true;
        public OnlineArenaBaseGameModeTab? arenaBaseGameModeTab;
        public TabContainer.Tab? myTab;

        /// <summary>
        /// Stores me player's previous value of <see cref="PlayerState.foodInStomach"/>
        /// from the last <see cref="ArenaGameSession"/> update.
        /// Reset to 0 in <see cref="On_ArenaGameSession_ctor"/>.
        /// </summary>
        public int PreviousFoodInStomach { get; set; }

        public List<ExternalArenaGameModeSetting> savedSettings =
        [
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.survivalScore)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.allowJoiningMidRound)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.amoebaControl)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.amoebaDuration)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.arenaSaintAscendanceTimer)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.artiExplosionCount)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.artiStunDistanceMult)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.artiParryDistanceMult)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.artiParryLeniency)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.challengeDenEjection)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.denScore)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.disableMaul)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.emptyDeathScore)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.enableMeadowCosmetics)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.enableBees)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.enableBombs)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.enableCorpseGrab)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.enableOverseer)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.foodScore)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.friendlyFire)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.itemSteal)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.killScore)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.painCatEgg)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.painCatLizard)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.painCatThrows)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.piggyBack)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.sainot)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.setupTime)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.shufflePlayList)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.spearHitScore)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.voidMasterEnabled)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.voidSpawnLethalityFactor)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.watcherCamoTimer)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.watcherRippleLevel)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.weaponCollisionFix)),
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.bannedSlugs)),
            new ExternalArenaGameModeInterfaceMultiChoiceSetting(OnlineArenaSettingsInferface.ROOMREPEAT),
            new ExternalArenaGameModeInterfaceMultiChoiceSetting(OnlineArenaSettingsInferface.SESSIONLENGTH),
            new ExternalArenaGameModeInterfaceMultiChoiceSetting(OnlineArenaSettingsInferface.WILDLIFE),
        ];

        public virtual bool ShouldWinByScore(ArenaSetup.GameTypeSetup gameTypeSetup)
        {
            return gameTypeSetup.survivalScore > 0
                || gameTypeSetup.KillScore > 0
                || gameTypeSetup.EmptyDeathScore > 0
                || gameTypeSetup.spearHitScore > 0
                || gameTypeSetup.foodScore > 0;
        }

        public virtual void InitAsCustomGameType(
            ArenaOnlineGameMode arenaOnline,
            ArenaSetup.GameTypeSetup self)
        {
            self.survivalScore = arenaOnline.survivalScore;
            self.KillScore = arenaOnline.killScore;
            self.EmptyDeathScore = arenaOnline.emptyDeathScore;
            self.spearHitScore = arenaOnline.spearHitScore;
            self.foodScore = arenaOnline.foodScore;

            self.repeatSingleLevelForever = false;
            self.savingAndLoadingSession = true;
            self.denEntryRule = arenaOnline.denEntryRule;
            self.rainWhenOnePlayerLeft = true;
            self.levelItems = true;
            self.fliesSpawn = false;
            self.saveCreatures = false;
            self.gameType = ArenaSetup.GameTypeID.Competitive;
            self.spearsHitPlayers = arenaOnline.onlineArenaSettingsInterfaceeBool["SPEARSHIT"];

            SandboxSettingsInterface.DefaultKillScores(ref self.killScores);
        }

        public virtual void ResetOnSessionEnd() { }

        public abstract bool On_ArenaBehaviors_ExitManager_ExitsOpen(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig,
            ArenaBehaviors.ExitManager self);

        public virtual void On_ArenaGameSession_ctor(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaGameSession.orig_ctor orig,
            ArenaGameSession self,
            RainWorldGame game)
        {
            PreviousFoodInStomach = 0;
            arenaOnline.ResetAtSession_ctor();

            foreach (ArenaSitting.ArenaPlayer arenaPlayer in self.arenaSitting.players)
            {
                arenaOnline.ResetArenaPlayerPerSessionStats(arenaPlayer);

                OnlinePlayer? onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(
                    arenaOnline,
                    arenaPlayer.playerNumber
                );
                if (onlinePlayer is null)
                {
                    RainMeadow.Error(
                        $"Unable to find arena player's online player. Player number: {arenaPlayer.playerNumber}."
                    );
                    continue;
                }

                if (OnlineManager.lobby.isOwner)
                    arenaOnline.CopyStatsToLobbyData(arenaPlayer, onlinePlayer);
            }
        }

        public virtual void On_ArenaSitting_NextLevel(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaSitting.orig_NextLevel orig,
            ArenaSitting self,
            ProcessManager processManager)
        {
            if (OnlineManager.lobby.isOwner)
            {
                arenaOnline.leaveForNextLevel = true;
            }

            arenaOnline.ResetAtNextLevel();

            ArenaGameSession arenaSession = ((RainWorldGame)processManager.currentMainLoop).GetArenaGameSession;
            AbstractRoom abstractRoom = arenaSession.game.world.abstractRooms[0];
            Room room = abstractRoom.realizedRoom;

            WorldSession worldSession =
                WorldSession.map.TryGetValue(abstractRoom.world, out WorldSession ws)
                    ? ws
                    : OnlineManager.lobby.overworld.worldSessions.TryGetValue("arena", out WorldSession ws2)
                        ? ws2
                        : null;

            if (worldSession.transitionInProgress)
                return;

            for (int i = arenaOnline.arenaSittingOnlineOrder.Count - 1; i >= 0; i--)
            {
                OnlinePlayer? missingPlayer = ArenaHelpers.FindOnlinePlayerByLobbyId(
                    arenaOnline.arenaSittingOnlineOrder[i]
                );
                if (missingPlayer == null)
                {
                    arenaOnline.arenaSittingOnlineOrder.RemoveAt(i);
                }
            }

            if (RoomSession.map.TryGetValue(abstractRoom, out var roomSession))
            {
                // we go over all APOs in the room
                RainMeadow.Debug("Next level switching");
                RainMeadow.Debug("Unsubscribing from old world");
                if (roomSession.worldSession.isActive)
                {
                    roomSession.worldSession.Deactivate();
                    roomSession.worldSession.NotNeeded();
                }

                if (roomSession.worldSession.participants.Count > 0)
                {
                    if (OnlineManager.lobby.isOwner)
                    {
                        RainMeadow.Debug(
                            $"Waiting for {roomSession.worldSession.participants.Count} players to leave..."
                        );
                    }
                    else
                    {
                        RainMeadow.Debug($"Waiting for host  players to join new world...");
                    }
                    processManager.rainWorld.StartCoroutine(
                        NextLevelWaitLoop(orig, self, processManager, roomSession.worldSession)
                    );
                    roomSession.worldSession.transitionInProgress = true;
                    return;
                }

                if (processManager.currentMainLoop is RainWorldGame)
                {
                    self.creatures.Clear();
                    self.savCommunities = null;

                    self.firstGameAfterMenu = false;

                    if (ModManager.MSC && arenaSession.challengeCompleted)
                    {
                        processManager.RequestMainProcessSwitch(
                            ProcessManager.ProcessID.MultiplayerMenu
                        );
                        self.players.Clear();
                        return;
                    }
                }

                RainMeadow.Debug("Arena: Moving to next level");
                self.currentLevel++;
                if (OnlineManager.lobby.isOwner)
                {
                    arenaOnline.currentLevel = self.currentLevel;
                }

                if (
                    self.currentLevel >= arenaOnline.playList.Count
                    && !self.gameTypeSetup.repeatSingleLevelForever
                )
                {
                    processManager.RequestMainProcessSwitch(
                        ProcessManager.ProcessID.MultiplayerResults
                    );
                    return;
                }

                List<OnlinePlayer> waitingPlayers =
                [
                    .. OnlineManager.players.Where(x =>
                        ArenaHelpers.GetArenaClientSettings(x)?.ready == true && !x.isMe
                    ),
                ];

                self.players.Clear();
                for (int i = 0; i < arenaOnline.arenaSittingOnlineOrder.Count; i++)
                {
                    OnlinePlayer? pl = ArenaHelpers.FindOnlinePlayerByLobbyId(
                        arenaOnline.arenaSittingOnlineOrder[i]
                    );
                    if (pl != null)
                    {
                        ArenaSitting.ArenaPlayer newArenaPlayer = new(i)
                        {
                            playerClass = ArenaHelpers.GetArenaClientSettings(pl)!.playingAs,
                            hasEnteredGameArea = true,
                        };

                        RainMeadow.Debug(
                            $"Arena: Local Sitting Data: {newArenaPlayer.playerNumber}: {newArenaPlayer.playerClass}"
                        );

                        arenaOnline.CopyStatsFromLobbyData(newArenaPlayer, pl);

                        self.players.Add(newArenaPlayer);
                    }
                }

                // Add waiting players
                if (arenaOnline.allowJoiningMidRound)
                {
                    foreach (OnlinePlayer player in waitingPlayers)
                    {
                        if (player != null) // always gotta check in case something happened to them
                        {
                            if (
                                !arenaOnline.arenaSittingOnlineOrder.Contains(player.inLobbyId)
                                && OnlineManager.lobby.isOwner
                            )
                            {
                                arenaOnline.arenaSittingOnlineOrder.Add(player.inLobbyId);
                            }
                            ArenaSitting.ArenaPlayer newArenaPlayer = new(
                                arenaOnline.arenaSittingOnlineOrder.Count - 1
                            )
                            {
                                playerClass = ArenaHelpers
                                    .GetArenaClientSettings(player)!
                                    .playingAs,
                                hasEnteredGameArea = true,
                            };
                            RainMeadow.Debug(
                                $"Arena: Local Sitting Data: {newArenaPlayer.playerNumber}: {newArenaPlayer.playerClass}"
                            );

                            arenaOnline.CopyStatsFromLobbyData(newArenaPlayer, player);

                            self.players.Add(newArenaPlayer);
                        }
                    }
                }

                processManager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
            }
        }

        private IEnumerator NextLevelWaitLoop(
            On.ArenaSitting.orig_NextLevel orig,
            ArenaSitting self,
            ProcessManager manager,
            WorldSession oldWorldSession)
        {
            return WorldSession.WaitAndExecuteSession(
                oldWorldSession,
                null,
                () => self.NextLevel(manager)
            );
        }

        public virtual int On_ArenaSetup_GameTypeSetup_get_ScoreToEnterDen(
            Func<ArenaSetup.GameTypeSetup, int> orig,
            ArenaSetup.GameTypeSetup self)
        {
            ArenaOnlineGameMode arenaOnline = (ArenaOnlineGameMode)OnlineManager.lobby.gameMode;

            return arenaOnline.denScore;
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

        public virtual int SetTimer(ArenaOnlineGameMode arenaOnline)
        {
            return arenaOnline.setupTime = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
        }

        public virtual void ResetGameTimer()
        {
            _timerDuration = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
        }

        public virtual int TimerDirection(ArenaOnlineGameMode arenaOnline, int timer)
        {
            return --timer;
        }

        // This is run on the victim's end, not the killer's!
        public virtual void On_ArenaGameSession_Killing(
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
            if (self.arenaSitting.gameTypeSetup.wildLifeSetting == ArenaSetup.GameTypeSetup.WildLifeSetting.Off
                && !targetOCreature.isAvatar)
            {
                RainMeadow.Warn(
                    $"A non-avatar creature ({target}) was killed by "
                    + $"{attackerOCreature.owner} despite wildlife being off."
                );
                return;
            }
            if (!attackerOCreature.isAvatar)
            {
                RainMeadow.Debug("Attacker is not an avatar. Returning early.");
                return;
            }
            if (!targetOCreature.isMine)
            {
                RainMeadow.Debug("Target is not mine. Returning early.");
                return;
            }


            IconSymbol.IconSymbolData trophy = CreatureSymbol.SymbolDataFromCreature(target.abstractCreature);

            // Handle Score
            int scoreChange = 0;

            if (targetOCreature.isAvatar)
            {
                scoreChange = self.GameTypeSetup.KillScore;
            }
            else
            {
                int index = MultiplayerUnlocks.SandboxUnlockForSymbolData(trophy).Index;

                if (index == -1)
                    RainMeadow.Warn($"No sandbox unlock for {trophy.critType}. No score change will occur.");
                else
                    scoreChange = self.arenaSitting.gameTypeSetup.killScores[index];
            }


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


            // Handle Trophies
            if (CreatureSymbol.DoesCreatureEarnATrophy(target.Template.type))
            {
                ArenaRPCs.AddArenaPlayerRoundKills(attackerArenaPlayer.playerNumber, [trophy.ToString()]);

                attackerOCreature.BroadcastRPCInRoom(
                    ArenaRPCs.AddArenaPlayerRoundKills,
                    attackerArenaPlayer.playerNumber,
                    new List<string> { trophy.ToString() }
                );
            }


            // Handle Meadow Coins
            if (target.Template.type == CreatureTemplate.Type.Slugcat)
            {
                RainMeadow.Info(
                    $"RMEL;{attackerOCreature.owner.id.DisplayName};KILLED;"
                    + $"{targetOCreature.owner.id.DisplayName};SCORE;{attackerArenaPlayer.score}"
                );

                // Cash Money Slugs
                ArenaClientSettings? attackerClientData = ArenaHelpers.GetArenaClientSettings(attackerOCreature.owner);
                if (attackerClientData?.gotSlugcat == true
                    || SpecialEvents.EventActiveInLobby<SpecialEvents.AprilFools>())
                {
                    attackerOCreature.BroadcastRPCInRoom(ArenaRPCs.ShowMeTheMoney, attackerOCreature, targetOCreature);

                    SpecialEvents.PlayMeadowCoinSound(room: self.room);
                    if (attackerOCreature.isMine)
                        SpecialEvents.GainedMeadowCoin(1);

                    for (int x = 0; x < 20; x++)
                    {
                        float posMagnitude = 2f;
                        float velocityMagnitude = 16f * UnityEngine.Random.value;
                        float lerpMagnitude = 0.5f + (0.5f * UnityEngine.Random.value);

                        self.room.AddObject(
                            new MeadowTokenCoin.MeadowCoin(
                                target.bodyChunks.First().pos + RWCustom.Custom.RNV() * posMagnitude,
                                RWCustom.Custom.RNV() * velocityMagnitude,
                                Color.Lerp(Color.yellow, Color.white, lerpMagnitude),
                                false
                            )
                        );
                    }
                }
            }
        }

        public virtual void On_ArenaGameSession_PlayerLandSpear(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaGameSession.orig_PlayerLandSpear orig,
            ArenaGameSession self,
            Player attacker,
            Creature target)
        {
            // Copy ArenaGameSession.PlayerLandSpear's guard clause
            if (self.sessionEnded
                || self.GameTypeSetup.spearHitScore == 0
                || !CreatureSymbol.DoesCreatureEarnATrophy(target.Template.type))
            {
                return;
            }

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

            if (self.arenaSitting.gameTypeSetup.wildLifeSetting == ArenaSetup.GameTypeSetup.WildLifeSetting.Off
                && !targetOCreature.isAvatar)
            {
                RainMeadow.Warn(
                    $"A non-avatar creature ({target}) was killed by "
                    + $"{attackerOCreature.owner} despite wildlife being off."
                );
                return;
            }
            if (attackerOCreature is not { isAvatar: true, isMine: true })
            {
                RainMeadow.Debug("Attacker is not my avatar. Returning early.");
                return;
            }
            if (!targetOCreature.isAvatar)
            {
                RainMeadow.Debug("A non-avatar creature was stabbed. Returning early.");
                return;
            }
            if (target.State is PlayerState { permanentDamageTracking: >= 1 })
            {
                RainMeadow.Debug(
                    $"Target ({targetOCreature.owner}) is going to die or is already "
                    + $"dead. Kill scoring is handled elsewhere. Returning early."
                );
                return;
            }
            if (attacker.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
            {
                RainMeadow.Debug(
                    "Gourmand stabbed someone. Logic needs to be added to give the spear hit "
                    + "score if the gourmand was not exhausted before throwing. Returning early."
                );
                return;
            }


            ArenaRPCs.ModifyArenaPlayerScore(attackerArenaPlayer.playerNumber, self.GameTypeSetup.spearHitScore);

            targetOCreature.BroadcastRPCInRoom(
                ArenaRPCs.ModifyArenaPlayerScore,
                attackerArenaPlayer.playerNumber,
                self.GameTypeSetup.spearHitScore
            );
        }

        public virtual void On_HUD_HUD_InitMultiplayerHud(
            ArenaOnlineGameMode arenaOnline,
            On.HUD.HUD.orig_InitMultiplayerHud orig,
            HUD.HUD self,
            ArenaGameSession session)
        {
            self.AddPart(new HUD.TextPrompt(self));

            if (MatchmakingManager.currentInstance.canSendChatMessages
                && RMOverlayHUDMenu.TryGetOverlay(out var overlayHUD))
            {
                if (overlayHUD.chatHud is null) overlayHUD.AddChatHUD(session.game.cameras[0]);
                else overlayHUD.SetNewChatHUDCamera(session.game.cameras[0]);
            }


            self.AddPart(new SpectatorHud(self, session.game.cameras[0]));
            self.AddPart(new ArenaPrepTimer(self, self.fContainers[0], arenaOnline, session));
            self.AddPart(new OnlineHUD(self, session.game.cameras[0], arenaOnline));

            self.AddPart(new ArenaSpawnLocationIndicator(self, session.game.cameras[0]));

            if (OnlineManager
                    .lobby.clientSettings[OnlineManager.mePlayer]
                    .GetData<ArenaClientSettings>()
                    .playingAs == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator && arenaOnline.enableOverseer)
            {

                self.AddPart(new MeadowEmoteHud(self, session.game.cameras[0],
                    arenaOnline.avatars.First(x => x.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Overseer).realizedCreature));
            }
            else
            {
                self.AddPart(new Pointing(self));
                foreach (AbstractCreature localPlayer in session.Players.Where(x => x != null && x.IsLocal()).ToArray())
                {
                    var psmh = new HUD.PlayerSpecificMultiplayerHud(self, session, localPlayer)
                    {
                        cornerPos = new Vector2(self.rainWorld.options.ScreenSize.x - self.rainWorld.options.SafeScreenOffset.x,
                                    20f + self.rainWorld.options.SafeScreenOffset.y),
                        flip = -1
                    };

                    psmh.parts.RemoveAll(x => x is HUD.PlayerSpecificMultiplayerHud.PlayerArrow || x is HUD.PlayerSpecificMultiplayerHud.PlayerDeathBump);
                    psmh.parts.Add(new HUD.PlayerSpecificMultiplayerHud.KillList(psmh));
                    var scoreCounter = new HUD.PlayerSpecificMultiplayerHud.ScoreCounter(psmh);
                    scoreCounter.scoreText.color = Color.white; // can't see crap
                    scoreCounter.lightGradient.color = Color.white;
                    psmh.parts.Add(scoreCounter);
                    self.AddPart(psmh);

                    if (ModManager.Watcher && OnlineManager
                        .lobby.clientSettings[OnlineManager.mePlayer]
                        .GetData<ArenaClientSettings>()
                        .playingAs == Watcher.WatcherEnums.SlugcatStatsName.Watcher)
                    {
                        self.AddPart(new Watcher.CamoMeter(self, psmh, self.fContainers[1]));
                    }
                }

            }
        }

        public virtual bool HoldFireWhileTimerIsActive(ArenaOnlineGameMode arenaOnline)
        {
            return arenaOnline.countdownInitiatedHoldFire = false;
        }

        public virtual string AddIcon(
            ArenaOnlineGameMode arenaOnline,
            OnlinePlayerDisplay display,
            PlayerSpecificOnlineHud owner,
            SlugcatCustomization customization,
            OnlinePlayer player)
        {
            if (ModManager.MSC && owner.abstractPlayer != null && owner.abstractPlayer.realizedCreature != null && owner.abstractPlayer.realizedCreature is Player p && p.rippleDeathIntensity > 0.4f)
            {
                return "warpIconSealed";
            }
            if (customization.globalMute)
            {
                return "Meadow_Menu_MutePlayerChat00";
            }


            bool playerGotSlots = ArenaHelpers.GetArenaClientSettings(player) != null && ArenaHelpers.GetArenaClientSettings(player).gotSlugcat;
            if (SpecialEvents.EventActiveInLobby<SpecialEvents.AprilFools>() || playerGotSlots)
            {
                SpecialEvents.LoadElement("meadowcoin");
                return "meadowcoin";
            }

            if (arenaOnline.reigningChamps != null && arenaOnline.reigningChamps.list != null && arenaOnline.reigningChamps.list.Contains(player.id))
            {
                return "Multiplayer_Star";
            }

            return "";
        }

        public virtual Color IconColor(
            ArenaOnlineGameMode arenaOnline,
            OnlinePlayerDisplay display,
            PlayerSpecificOnlineHud owner,
            SlugcatCustomization customization,
            OnlinePlayer player)
        {
            if (OnlineManager.lobby.clientSettings.TryGetValue(player, out var cs)
                && cs.chatUsernameColor is Color color)
            {
                return color;
            }
            Color.RGBToHSV(customization.SlugcatColor(), out var H, out var S, out var V);
            if (V < 0.8)
            {
                return Color.HSVToRGB(H, S, 0.8f);
            }
            return customization.SlugcatColor();
        }

        /// <summary>
        /// Spawns a creature in an online space
        /// </summary>
        public void SpawnTransferableCreature(
            ArenaOnlineGameMode arenaOnline,
            ArenaGameSession self,
            Room room,
            int randomExitIndex,
            CreatureTemplate.Type templateType)
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
        public void SpawnNonTransferableCreature(
            ArenaOnlineGameMode arenaOnline,
            ArenaGameSession self,
            Room room,
            int randomExitIndex,
            CreatureTemplate.Type templateType)
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
                        .players[ArenaHelpers.FindOnlinePlayerNumber(arenaOnline, OnlineManager.mePlayer)]
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
                        arenaOnline.painCatThrowingSkill;
                    RainMeadow.Debug(
                        "ENOT THROWING SKILL "
                            + (abstractCreature.realizedCreature as Player)
                                .slugcatStats
                                .throwingSkill
                    );
                    if (
                        (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill
                            == 0
                        && arenaOnline.painCatEgg
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

                    if (arenaOnline.lizardEvent == 99 && arenaOnline.painCatLizard)
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
                    if (!arenaOnline.sainot) // ascendance saint
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
                if ((abstractCreature.realizedCreature as Player).rippleLevel >= 3f)
                {
                    (abstractCreature.realizedCreature as Player).enterIntoCamoDuration = 80;
                }
                else
                {
                    (abstractCreature.realizedCreature as Player).enterIntoCamoDuration = 40;
                }
            }
        }

        public virtual void On_ArenaGameSession_SpawnPlayers(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaGameSession.orig_SpawnPlayers orig,
            ArenaGameSession self,
            Room room,
            List<int> suggestedDens)
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

        public void SpawnPlayerOverseer(
            ArenaOnlineGameMode arenaOnline,
            ArenaGameSession self,
            Room room,
            int randomExitIndex)
        {
            bool spawningAvatars = RainMeadow.sSpawningAvatar;
            RainMeadow.sSpawningAvatar = true;
            AbstractCreature abstractCreature = new AbstractCreature(self.game.world,
                StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Overseer),
                null,
                new WorldCoordinate(0, -1, -1, -1),
                new EntityID(-1, 0)
            );

            Vector2 pos = room.cameraPositions[room.CameraViewingNode(room.ShortcutLeadingToNode(randomExitIndex).destNode)];
            abstractCreature.pos = room.GetWorldCoordinate(pos);
            abstractCreature.Room.AddEntity(abstractCreature);
            abstractCreature.RealizeInRoom();
            RainMeadow.sSpawningAvatar = spawningAvatars;
        }

        public virtual void On_Player_Die(ArenaOnlineGameMode arenaOnline, On.Player.orig_Die orig, Player self)
        {
            // There isn't a specific reason self.room.game.GetArenaGameSession isn't used, I just don't trust room to be non-null.
            ArenaSitting arenaSitting = Custom.rainWorld.processManager.arenaSitting;

            // We can always find an online creature because before forwarding this hook, Rain Meadow requires one to exist.
            OnlineCreature onlineCreature = self.abstractCreature.GetOnlineCreature()!;

            bool wasAlreadyDead = self.dead;
            orig(self);

            if (wasAlreadyDead)
                return;

            if (onlineCreature is not { isAvatar: true, isMine: true })
            {
                RainMeadow.Debug("Player is not my avatar. Returning early.");
                return;
            }
            if (ArenaHelpers.FindArenaPlayerByOnlinePlayer(arenaOnline, arenaSitting, onlineCreature.owner)
                is not ArenaSitting.ArenaPlayer arenaPlayer)
            {
                RainMeadow.Error($"Unable to find {onlineCreature.owner}'s arena player.");
                return;
            }

            if (self.killTag is null)
            {
                int scoreChange = -arenaSitting.gameTypeSetup.EmptyDeathScore;

                if (scoreChange != 0)
                {
                    ArenaRPCs.ModifyArenaPlayerScore(
                        arenaPlayer.playerNumber,
                        scoreChange
                    );

                    onlineCreature.BroadcastRPCInRoom(
                        ArenaRPCs.ModifyArenaPlayerScore,
                        arenaPlayer.playerNumber,
                        scoreChange
                    );
                }
            }

            ArenaRPCs.ModifyArenaPlayerRoundDeaths(
                arenaPlayer.playerNumber,
                1
            );

            onlineCreature.BroadcastRPCInRoom(
                ArenaRPCs.ModifyArenaPlayerRoundDeaths,
                arenaPlayer.playerNumber,
                1
            );
        }

        public virtual void On_ArenaGameSession_Update(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaGameSession.orig_Update orig,
            ArenaGameSession self)
        {
            bool isOwnerOverseer =
                ArenaHelpers.GetArenaClientSettings(OnlineManager.lobby.owner)?.playingAs
                == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator;
            if (arenaOnline.countdownInitiatedHoldFire && isOwnerOverseer)
            {
                self.endSessionCounter = 30;
            }
            if (!OnlineManager.lobby.isOwner && !arenaOnline.hostLoadedOverlay)
            {
                self.endSessionCounter = 30;
            }
            orig(self);

            if (arenaOnline.currentLobbyOwner != OnlineManager.lobby.owner)
            {
                self.game.manager.RequestMainProcessSwitch(
                    ProcessManager.ProcessID.MultiplayerResults
                );
                arenaOnline.currentLobbyOwner = OnlineManager.lobby.owner;
            }

            int activePlayerCountWithOverseers = arenaOnline
                .arenaSittingOnlineOrder.Select(id => ArenaHelpers.FindOnlinePlayerByLobbyId(id)) // Get the player
                .Where(player => player != null) // Ensure player exists
                .Select(player => ArenaHelpers.GetArenaClientSettings(player)) // Get settings
                .Where(settings => settings != null) // Ensure settings exist
                .Count(settings =>
                    settings.playingAs == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator
                );
            if (
                self.Players.Count + activePlayerCountWithOverseers
                != arenaOnline.arenaSittingOnlineOrder.Count
            )
            {
                RainMeadow.Trace(
                    $"Arena: Abstract Creature count does not equal registered players in the online Sitting! AC Count: {self.Players.Count} | ArenaSittingOnline Count: {arenaOnline.arenaSittingOnlineOrder.Count}"
                );

                var extraPlayers = self.Players.Skip(arenaOnline.arenaSittingOnlineOrder.Count).ToList();

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
                        && ac.creatureTemplate.type != CreatureTemplate.Type.Overseer
                    ) //&& ac.state.alive
                    {
                        self.Players.Add(ac);
                    }
                }
            }
            if (OnlineManager.lobby.isOwner)
            {
                arenaOnline.playersEqualToOnlineSitting =
                    self.Players.Count + activePlayerCountWithOverseers
                    == arenaOnline.arenaSittingOnlineOrder.Count;
            }

            if (!self.sessionEnded)
            {
                foreach (ArenaSitting.ArenaPlayer arenaPlayer in self.arenaSitting.players)
                {
                    AbstractCreature? playerAC = ArenaHelpers.FindPlayerACByArenaPlayer(arenaOnline, self, arenaPlayer);
                    OnlineCreature? onlineCreature = playerAC?.GetOnlineCreature();
                    if (playerAC is null || onlineCreature is null)
                        continue;

                    PlayerState playerState = (PlayerState)playerAC.state;

                    if (!playerState.dead)
                        arenaPlayer.timeAlive++;

                    int newFoodInStomach = playerState.foodInStomach - PreviousFoodInStomach;
                    int scoreChange = newFoodInStomach * self.GameTypeSetup.foodScore;

                    if (onlineCreature.isMine && scoreChange != 0)
                    {
                        ArenaRPCs.ModifyArenaPlayerScore(
                            arenaPlayer.playerNumber,
                            scoreChange
                        );

                        onlineCreature.BroadcastRPCInRoom(
                            ArenaRPCs.ModifyArenaPlayerScore,
                            arenaPlayer.playerNumber,
                            scoreChange
                        );

                        PreviousFoodInStomach = playerState.foodInStomach;
                    }
                }
            }
        }

        public virtual void On_ArenaSitting_SessionEnded(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaSitting.orig_SessionEnded orig,
            ArenaSitting self,
            ArenaGameSession arenaSession)
        {
            UpdateArenaSessionFinalStats(arenaOnline, arenaSession);

            List<ArenaSitting.ArenaPlayer> sortedArenaPlayers = [];
            foreach (ArenaSitting.ArenaPlayer arenaPlayer in self.players)
            {
                bool isInserted = false;
                for (int i = 0; i < sortedArenaPlayers.Count; i++)
                {
                    if (self.PlayerSessionResultSort(arenaPlayer, sortedArenaPlayers[i]))
                    {
                        sortedArenaPlayers.Insert(i, arenaPlayer);
                        isInserted = true;
                        break;
                    }
                }

                if (!isInserted)
                    sortedArenaPlayers.Add(arenaPlayer);
            }

            arenaSession.game.arenaOverlay = new ArenaOverlay(
                arenaSession.game.manager,
                self,
                sortedArenaPlayers
            );
            arenaSession.game.manager.sideProcesses.Add(
                arenaSession.game.arenaOverlay
            );
        }

        public virtual List<ArenaSitting.ArenaPlayer> On_ArenaSitting_FinalSittingResult(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaSitting.orig_FinalSittingResult orig,
            ArenaSitting self)
        {
            UpdateArenaSittingFinalStats(arenaOnline, self);

            List<ArenaSitting.ArenaPlayer> sortedArenaPlayers = [];
            foreach (ArenaSitting.ArenaPlayer arenaPlayer in self.players)
            {
                bool isInserted = false;
                for (int i = 0; i < sortedArenaPlayers.Count; i++)
                {
                    if (self.PlayerSittingResultSort(arenaPlayer, sortedArenaPlayers[i]))
                    {
                        sortedArenaPlayers.Insert(i, arenaPlayer);
                        isInserted = true;
                        break;
                    }
                }

                if (!isInserted)
                    sortedArenaPlayers.Add(arenaPlayer);
            }

            return sortedArenaPlayers;
        }

        public virtual void UpdateArenaSessionFinalStats(
            ArenaOnlineGameMode arenaOnline,
            ArenaGameSession arenaSession)
        {
            ArenaSitting arenaSitting = arenaSession.arenaSitting;

            foreach (ArenaSitting.ArenaPlayer arenaPlayer in arenaSitting.players)
            {
                OnlinePlayer? onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(
                    arenaOnline,
                    arenaPlayer.playerNumber
                );

                if (onlinePlayer is null)
                {
                    arenaOnline.ResetArenaPlayerStats(arenaPlayer);
                    continue;
                }
                if (arenaPlayer.playerClass == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                {
                    arenaOnline.ResetArenaPlayerStats(arenaPlayer);

                    if (OnlineManager.lobby.isOwner)
                        arenaOnline.CopyStatsToLobbyData(arenaPlayer, onlinePlayer);

                    continue;
                }

                // Winner and Alive are not part of lobby data.
                arenaPlayer.winner = false;
                arenaPlayer.alive = arenaSession.EndOfSessionLogPlayerAsAlive(arenaPlayer.playerNumber);

                if (OnlineManager.lobby.isOwner)
                {
                    if (arenaPlayer.alive)
                    {
                        AbstractCreature? playerAC = ArenaHelpers.FindPlayerACByArenaPlayer(
                            arenaOnline,
                            arenaSession,
                            arenaPlayer
                        );
                        if (playerAC is not null)
                        {
                            arenaPlayer.score += arenaOnline.externalArenaGameMode.CalculateGraspsFoodScore(
                                arenaOnline,
                                arenaSession.GameTypeSetup,
                                playerAC
                            );
                        }
                        else
                        {
                            RainMeadow.Error(
                                $"Unable to find arena player's player AC. Player number: {arenaPlayer.playerNumber}."
                            );
                        }

                        arenaPlayer.score += arenaSitting.gameTypeSetup.survivalScore;
                    }

                    arenaPlayer.totScore += arenaPlayer.score;
                    arenaPlayer.deaths += arenaPlayer.RoundDeaths;
                    arenaPlayer.allKills.AddRange(arenaPlayer.roundKills);

                    arenaOnline.CopyStatsToLobbyData(arenaPlayer, onlinePlayer);
                }
                else
                    arenaOnline.CopyStatsFromLobbyData(arenaPlayer, onlinePlayer);
            }

            // Winners must be handled here to ensure that every other player's stats have been updated.
            List<ArenaSitting.ArenaPlayer> winners = DetermineArenaSessionWinners(arenaOnline, arenaSession);

            foreach (ArenaSitting.ArenaPlayer arenaPlayer in winners)
            {
                OnlinePlayer? onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(
                    arenaOnline,
                    arenaPlayer.playerNumber
                );
                if (onlinePlayer is null) continue;

                arenaPlayer.winner = true;

                if (OnlineManager.lobby.isOwner)
                {
                    arenaPlayer.wins++;
                    arenaOnline.CopyStatsToLobbyData(arenaPlayer, onlinePlayer);
                }
            }

            // For the Arena tournament
            foreach (ArenaSitting.ArenaPlayer arenaPlayer in arenaSitting.players)
            {
                OnlinePlayer? onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(
                    arenaOnline,
                    arenaPlayer.playerNumber
                );
                if (onlinePlayer is null) continue;

                RainMeadow.Info(
                    $"RMEL;{onlinePlayer.id.DisplayName};{arenaPlayer.wins};"
                    + $"{arenaPlayer.allKills.Count};{arenaPlayer.deaths};{arenaPlayer.totScore}"
                );
            }
        }

        public virtual void UpdateArenaSittingFinalStats(
            ArenaOnlineGameMode arenaOnline,
            ArenaSitting arenaSitting)
        {
            arenaSitting.players
                .ForEach(arenaPlayer => arenaPlayer.winner = false);

            DetermineArenaSittingWinners(arenaOnline, arenaSitting)
                .ForEach(arenaPlayer => arenaPlayer.winner = true);
        }

        public virtual bool On_ArenaSitting_PlayerSessionResultSort(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaSitting.orig_PlayerSessionResultSort orig,
            ArenaSitting self,
            ArenaSitting.ArenaPlayer a,
            ArenaSitting.ArenaPlayer b)
        {
            if (a.playerClass == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                return false;
            if (b.playerClass == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                return true;

            bool shouldWinByScore = ShouldWinByScore(self.gameTypeSetup);

            if (a.winner != b.winner)
                return a.winner;
            if (shouldWinByScore)
            {
                if (a.score != b.score)
                    return a.score > b.score;
                if (a.roundKills.Count != b.roundKills.Count)
                    return a.roundKills.Count > b.roundKills.Count;
                if (a.alive != b.alive)
                    return a.alive;
            }
            else
            {
                if (a.alive != b.alive)
                    return a.alive;
                if (a.score != b.score)
                    return a.score > b.score;
                if (a.roundKills.Count != b.roundKills.Count)
                    return a.roundKills.Count > b.roundKills.Count;
            }

            return a.timeAlive > b.timeAlive;
        }

        public virtual bool On_ArenaSitting_PlayerSittingResultSort(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaSitting.orig_PlayerSittingResultSort orig,
            ArenaSitting self,
            ArenaSitting.ArenaPlayer a,
            ArenaSitting.ArenaPlayer b)
        {
            if (a.playerClass == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                return false;
            if (b.playerClass == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                return true;

            bool shouldWinByScore = ShouldWinByScore(self.gameTypeSetup);

            if (a.winner != b.winner)
                return a.winner;
            if (shouldWinByScore)
            {
                if (a.totScore != b.totScore)
                    return a.totScore > b.totScore;
                if (a.wins != b.wins)
                    return a.wins > b.wins;
            }
            else
            {
                if (a.wins != b.wins)
                    return a.wins > b.wins;
                if (a.totScore != b.totScore)
                    return a.totScore > b.totScore;
            }
            if (a.deaths != b.deaths)
                return a.deaths > b.deaths;

            return a.allKills.Count > b.allKills.Count;
        }

        /// <remarks>
        /// Expects all <see cref="ArenaSitting.ArenaPlayer"/>
        /// stats to be fully updated.
        /// </remarks>
        public virtual List<ArenaSitting.ArenaPlayer> DetermineArenaSessionWinners(
            ArenaOnlineGameMode arenaOnline,
            ArenaGameSession arenaSession)
        {
            ArenaSitting arenaSitting = arenaSession.arenaSitting;

            List<ArenaSitting.ArenaPlayer> bestArenaPlayers = arenaSitting.players
                .Where(arenaPlayer => arenaPlayer.playerClass != RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                .ToList();

            // There needs at least 2 players for someone to win. If there is only 1, they can't logically win or lose.
            if (bestArenaPlayers.Count < 2)
                return [];

            if (ShouldWinByScore(arenaSession.GameTypeSetup))
            {
                int highestScore = bestArenaPlayers.Max(arenaPlayer => arenaPlayer.score);
                bestArenaPlayers.RemoveAll(arenaPlayer => arenaPlayer.score != highestScore);
            }
            else
            {
                bestArenaPlayers.RemoveAll(arenaPlayer => !arenaPlayer.alive);
            }

            // If there are multiple best arena players, they tied.
            return bestArenaPlayers.Count == 1
                ? bestArenaPlayers
                : [];
        }

        /// <remarks>
        /// Expects all <see cref="ArenaSitting.ArenaPlayer"/>
        /// stats to be fully updated.
        /// </remarks>
        public virtual List<ArenaSitting.ArenaPlayer> DetermineArenaSittingWinners(
            ArenaOnlineGameMode arenaOnline,
            ArenaSitting arenaSitting)
        {
            List<ArenaSitting.ArenaPlayer> bestArenaPlayers = arenaSitting.players
                .Where(arenaPlayer => arenaPlayer.playerClass != RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                .ToList();

            // There needs at least 2 players for someone to win. If there is only 1, they can't logically win or lose.
            if (bestArenaPlayers.Count < 2)
                return [];

            if (ShouldWinByScore(arenaSitting.gameTypeSetup))
            {
                int highestTotalScore = bestArenaPlayers.Max(arenaPlayer => arenaPlayer.totScore);
                bestArenaPlayers.RemoveAll(arenaPlayer => arenaPlayer.totScore != highestTotalScore);
            }
            else
            {
                int highestWins = bestArenaPlayers.Max(arenaPlayer => arenaPlayer.wins);
                bestArenaPlayers.RemoveAll(arenaPlayer => arenaPlayer.wins != highestWins);
            }

            return bestArenaPlayers.Count == 1
                ? bestArenaPlayers
                : [];
        }

        public int CalculateGraspsFoodScore(
            ArenaOnlineGameMode arenaOnline,
            ArenaSetup.GameTypeSetup gameTypeSetup,
            AbstractCreature playerAC)
        {
            if (playerAC.realizedCreature is not Player player)
                return 0;

            int score = 0;
            foreach (Creature.Grasp? grasp in player.grasps)
            {
                if (grasp?.grabbed is IPlayerEdible playerEdible)
                {
                    if (ModManager.MSC
                        && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint
                        && playerEdible is JellyFish or Centipede or Fly or VultureGrub or SmallNeedleWorm or Hazer)
                    {
                        continue;
                    }

                    score += playerEdible.FoodPoints * gameTypeSetup.foodScore;
                }
            }

            return score;
        }

        public virtual bool DidPlayerWinRainbow(ArenaOnlineGameMode arenaOnline, OnlinePlayer player)
        {
            return arenaOnline.reigningChamps.list.Contains(player.id);
        }

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
            ArenaOnlineGameMode arenaOnline,
            OnlinePlayer? player,
            Color origPortraitColor)
        {
            return origPortraitColor;
        }

        /// <summary>
        /// Gets a translated, user-displayable message describing the results of the game.
        /// </summary>
        /// <param name="arenaOnline"></param>
        /// <param name="resultMenu"></param>
        /// <param name="isSpecific">
        /// Indicates if the returned text describes the result of
        /// the game. (rather than simply stating that the game ended)
        /// </param>
        public virtual string GetResultText(
            ArenaOnlineGameMode arenaOnline,
            PlayerResultMenu resultMenu,
            out bool isSpecific)
        {
            isSpecific = true;
            string nonSpecificText = resultMenu is MultiplayerResults
                ? resultMenu.Translate("SESSION ENDED!")
                : resultMenu.Translate("GAME OVER");

            List<ArenaSitting.ArenaPlayer> activeArenaPlayers = resultMenu.result
                .Where(arenaPlayer => arenaPlayer.playerClass != RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                .ToList();

            if (activeArenaPlayers.Count < 2)
            {
                isSpecific = false;
                return nonSpecificText;
            }

            List<ArenaSitting.ArenaPlayer> winners = activeArenaPlayers
                .Where(arenaPlayer => arenaPlayer.winner)
                .ToList();

            // Multiple winners are technically supported. Zero or multiple winners is a draw.
            if (winners.Count != 1)
                return resultMenu.Translate("IT'S A DRAW!");

            ArenaSitting.ArenaPlayer winner = winners[0];
            OnlinePlayer? onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(
                arenaOnline,
                winner.playerNumber
            );

            if (onlinePlayer is null)
            {
                RainMeadow.Error(
                    $"Unable to find the online player corresponding to the winner. "
                    + $"Player number: {winner.playerNumber}."
                );

                isSpecific = false;
                return nonSpecificText;
            }

            string displayName = MatchmakingManager.currentInstance.FilterTeamName(onlinePlayer.id.DisplayName);

            return resultMenu.Translate("<USERNAME> WINS!")
                .Replace("<USERNAME>", displayName);
        }

        public virtual Dialog AddGameModeInfo(ArenaOnlineGameMode arenaOnline, Menu.Menu menu)
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

        public virtual Dialog AddPostGameStatsFeed(ArenaOnlineGameMode arenaOnline, Menu.Menu menu)
        {
            return new ArenaPostGameStatsDialog(menu.manager, arenaOnline);
        }

        public virtual string ExportLocalSettings(ArenaOnlineGameMode arenaOnline)
        {
            List<string> pairs = new();
            for (int i = 0; i < savedSettings.Count; i++)
            {
                string val = savedSettings[i].GetSaveString(arenaOnline);
                pairs.Add($"{savedSettings[i].settingNickname}={val}");
                RainMeadow.Debug($"Copy setting {savedSettings[i].settingNickname} at {val}");
            }

            string combined = string.Join("|", pairs);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
        }

        public virtual bool ImportLocalSettings(ArenaOnlineGameMode arenaOnline, string base64Data)
        {
            if (string.IsNullOrEmpty(base64Data)) return false;

            try
            {
                string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64Data));
                if (decoded.Contains(";"))
                {
                    return false; // NO MAPS
                }
                string[] pairs = decoded.Split('|');

                foreach (string pair in pairs)
                {
                    string[] kvp = pair.Split('=');
                    if (kvp.Length != 2) continue;

                    string key = kvp[0];
                    string val = kvp[1];

                    int index = savedSettings.FindIndex(x => x.settingNickname == key);
                    RainMeadow.Debug($"Reading setting {key}, found index {index}, read value is {val}");
                    if (index >= 0)
                    {
                        savedSettings[index].SetValueFromString(val, arenaOnline);
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

    public abstract class ExternalArenaGameModeSetting(string settingID, string settingNickname = "")
    {
        public string settingID { get; } = settingID;
        public string settingNickname { get; } = settingNickname == "" ? settingID : settingNickname;

        public abstract object GetValueFromString(string value);

        public abstract void SetValueFromString(string value, ArenaOnlineGameMode arenaOnline);

        public abstract object GetValueFromArenaMode(ArenaOnlineGameMode arenaOnline);

        public abstract string GetSaveString(ArenaOnlineGameMode arenaOnline);
    }

    public class ExternalArenaGameModeFieldSetting(string settingID, string settingNickname = "")
        : ExternalArenaGameModeSetting(settingID, settingNickname)
    {
        protected const char SEPARATOR = ',';

        public FieldInfo settingField { get; } = typeof(ArenaOnlineGameMode).GetField(settingID);
        public Type settingType { get; } = typeof(ArenaOnlineGameMode).GetField(settingID).FieldType;

        // For now, suppose simple values (with IConvertible) and list of simple values.
        protected static bool TryParseSimpleType(object value, Type type, out object? result)
        {
            try
            {
                result = Convert.ChangeType(value, type);
                return true;
            }
            catch
            {
                RainMeadow.Debug($"Value {value} couldn't be converted in {type}");
                result = null;
                return false;
            }
        }

        protected static object ParseOrDefaultSimpleType(object value, Type type)
        {
            return TryParseSimpleType(value, type, out var result) && result is not null
                ? result
                : Activator.CreateInstance(type);
        }

        public override object GetValueFromString(string value)
        {
            if (settingType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(settingType))
            {
                Type ListingType = settingType.GetGenericArguments()[0];
                IEnumerable<object> elements = string.IsNullOrWhiteSpace(value)
                    ? []
                    : value.Split(SEPARATOR).Select(s => ParseOrDefaultSimpleType(s, ListingType));

                RainMeadow.Debug($"Found enumerable {settingType}:{ListingType}, converted values are {string.Join(",", elements)}");
                if (settingType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(ListingType));
                    foreach (object item in elements)
                    {
                        list.Add(ParseOrDefaultSimpleType(item, ListingType));
                    }
                    return list;
                }
                return elements;
            }
            else if (TryParseSimpleType(value, settingType, out var result) && result is not null)
            {
                RainMeadow.Debug($"Found simple type {settingType}, converted value to {result}");
                return result;
            }
            throw new ArgumentException($"Couldn't find a solution for type {settingType}");
        }

        public override void SetValueFromString(string value, ArenaOnlineGameMode arenaOnline)
        {
            try
            {
                settingField.SetValue(arenaOnline, GetValueFromString(value));
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
            }
        }

        public override object GetValueFromArenaMode(ArenaOnlineGameMode arenaOnline)
        {
            return settingField.GetValue(arenaOnline);
        }

        public override string GetSaveString(ArenaOnlineGameMode arenaOnline)
        {
            var value = GetValueFromArenaMode(arenaOnline);

            if (value is IEnumerable enumerable && value is not string)
            {
                return string.Join(SEPARATOR.ToString(), enumerable.Cast<object>());
            }
            return settingField.GetValue(arenaOnline).ToString();
        }
    }

    public class ExternalArenaGameModeInterfaceMultiChoiceSetting(string settingID, string settingNickname = "")
        : ExternalArenaGameModeSetting(settingID, settingNickname)
    {
        public override object GetValueFromArenaMode(ArenaOnlineGameMode arenaOnline)
        {
            return arenaOnline.onlineArenaSettingsInterfaceMultiChoice[settingID];
        }

        public override object GetValueFromString(string value)
        {
            return int.TryParse(value, out var result) ? result : 0;
        }

        public override void SetValueFromString(string value, ArenaOnlineGameMode arenaOnline)
        {
            try
            {
                arenaOnline.onlineArenaSettingsInterfaceMultiChoice[settingID] = (int)GetValueFromString(value);
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
            }
        }

        public override string GetSaveString(ArenaOnlineGameMode arenaOnline)
        {
            return arenaOnline.onlineArenaSettingsInterfaceMultiChoice[settingID].ToString();
        }
    }
}

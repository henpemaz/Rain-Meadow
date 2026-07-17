using System.Collections.Generic;
using System.Linq;
using Menu;
using Menu.Remix.MixedUI;
using RainMeadow.UI;
using RainMeadow.UI.Components;
using UnityEngine;
using System;
using System.Text;
using System.Reflection;
using System.Collections;
using DevInterface;

namespace RainMeadow
{
    public abstract class ExternalArenaGameMode
    {
        private int _timerDuration;
        public OnlineArenaBaseGameModeTab? arenaBaseGameModeTab;
        public TabContainer.Tab? myTab;
        /// <summary>
        /// Stores the previous value of <see cref="PlayerState.foodInStomach"/>
        /// for the local player from the last <see cref="ArenaGameSession"/> update.
        /// Reset to 0 in <see cref="On_ArenaGameSession_ctor"/>.
        /// </summary>
        private int previousFoodInStomach;

        public abstract ArenaSetup.GameTypeID GetGameModeId { get; }

        public virtual void ResetOnSessionEnd() { }

        public abstract bool On_ArenaBehaviors_ExitManager_ExitsOpen(
            ArenaOnlineGameMode arena,
            On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig,
            ArenaBehaviors.ExitManager self
        );

        public abstract bool SpawnBatflies(FliesWorldAI self, int spawnRoom);

        public abstract int TimerDuration { get; set; }

        public virtual bool ShowAddedScoreBetweenRoundsInOnlinePlayerUI { get; set; } = true;

        public virtual void On_ArenaGameSession_ctor(
            ArenaOnlineGameMode arena,
            On.ArenaGameSession.orig_ctor orig,
            ArenaGameSession self,
            RainWorldGame game)
        {
            arena.session = self;
            previousFoodInStomach = 0;

            arena.ResetAtSession_ctor();
        }

        public virtual void On_ArenaSitting_NextLevel(
            ArenaOnlineGameMode arena,
            On.ArenaSitting.orig_NextLevel orig,
            ArenaSitting self,
            ProcessManager process
        )
        {
            arena.ResetAtNextLevel();
        }

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

        public virtual void On_Player_Die(ArenaOnlineGameMode arenaOnline, On.Player.orig_Die orig, Player self)
        {
            if (self.dead)
            {
                orig(self);
                return;
            }

            orig(self);

            if (self.abstractCreature.GetOnlineCreature() is not OnlineCreature onlineCreature)
            {
                RainMeadow.Error("Unable to find the attacker online creature.");
                return;
            }
            if (!onlineCreature.isMine || !onlineCreature.isAvatar)
            {
                RainMeadow.Info("Player is not my avatar. Returning early.");
                return;
            }
            if (ArenaHelpers.FindArenaPlayerByOnlinePlayer(arenaOnline, onlineCreature.owner) is not ArenaSitting.ArenaPlayer arenaPlayer)
            {
                RainMeadow.Error($"Unable to find {onlineCreature.owner}'s arena player.");
                return;
            }

            if (self.killTag is null)
            {
                int scoreChange = -arenaOnline.emptyKillTagScore;

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
        }

        public virtual bool On_ArenaGameSession_EndOfSessionLogPlayerAsAlive(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaGameSession.orig_EndOfSessionLogPlayerAsAlive orig,
            ArenaGameSession self,
            int playerNumber)
        {
            // Copy ArenaGameSession.EndOfSessionLogPlayerAsAlive's guard clause
            if (self.exitManager is null)
                return true;

            OnlinePlayer? onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(
                arenaOnline,
                playerNumber
            );
            if (onlinePlayer is null)
            {
                RainMeadow.Warn($"Unable to find online player with player number {playerNumber}.");
                return false;
            }

            foreach (ShortcutHandler.ShortCutVessel playerShortcutVessel in self.exitManager.playersInDens)
            {
                AbstractCreature playerAC = playerShortcutVessel.creature.abstractCreature;
                OnlineCreature? onlineCreature = playerAC.GetOnlineCreature();
                if (onlineCreature is null)
                {
                    RainMeadow.Warn($"Unable to find player AC's online creature. Player AC: {playerAC}.");
                    continue;
                }

                if (onlineCreature.owner == onlinePlayer)
                    return true;
            }

            foreach (AbstractCreature playerAC in self.Players)
            {
                OnlineCreature? onlineCreature = playerAC.GetOnlineCreature();
                if (onlineCreature is null)
                {
                    RainMeadow.Warn($"Unable to find player AC's online creature. Player AC: {playerAC}.");
                    continue;
                }

                if (Input.GetKey(KeyCode.G))
                    RainMeadow.Info(onlineCreature.owner);

                if (onlineCreature.owner == onlinePlayer)
                    return playerAC.state.alive;
            }

            return false;
        }

        /// <remarks>This is only run from locally observed kills!</remarks>
        public virtual void On_ArenaGameSession_Killing(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaGameSession.orig_Killing orig,
            ArenaGameSession self,
            Player attacker,
            Creature target)
        {
            RainMeadow.Info($"{attacker} killed {target}");
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
            if (ArenaHelpers.FindArenaPlayerByOnlinePlayer(arenaOnline, attackerOCreature.owner) is not ArenaSitting.ArenaPlayer attackerArenaPlayer)
            {
                RainMeadow.Error($"Unable to find {attackerOCreature.owner}'s arena player.");
                return;
            }
            if (self.arenaSitting.gameTypeSetup.wildLifeSetting == ArenaSetup.GameTypeSetup.WildLifeSetting.Off &&
                !targetOCreature.isAvatar)
            {
                RainMeadow.Warn($"A non-avatar creature ({target}) was killed by {attackerOCreature.owner} despite wildlife being off.");
                return;
            }
            if (!targetOCreature.isMine || !targetOCreature.isAvatar)
            {
                RainMeadow.Info($"Target is not my avatar. Owner: {attackerOCreature.owner}. Returning early.");
                return;
            }


            IconSymbol.IconSymbolData trophy = CreatureSymbol.SymbolDataFromCreature(target.abstractCreature);

            // Handle Score
            int scoreChange = 0;

            if (targetOCreature.isAvatar)
                scoreChange = arenaOnline.killScore;
            else
            {
                int index = MultiplayerUnlocks.SandboxUnlockForSymbolData(trophy).Index;

                if (index == -1)
                    RainMeadow.Warn($"No sandbox unlock for {trophy.critType}. No score change will occur.");
                else
                {
                    scoreChange = self.arenaSitting.gameTypeSetup.killScores[index];
                }
            }

            if (scoreChange != 0) // No need to waste network.
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
                ArenaRPCs.AddArenaPlayerRoundKills(attackerArenaPlayer.playerNumber, [ trophy.ToString() ]);

                attackerOCreature.BroadcastRPCInRoom(
                    ArenaRPCs.AddArenaPlayerRoundKills,
                    attackerArenaPlayer.playerNumber,
                    new List<string> { trophy.ToString() }
                );
            }


            // Handle Meadow Coins
            if (target.Template.type == CreatureTemplate.Type.Slugcat)
            {
                // Cash Money Slugs
                ArenaClientSettings? attackerClientData = ArenaHelpers.GetArenaClientSettings(attackerOCreature.owner);
                if (attackerClientData?.gotSlugcat == true || SpecialEvents.EventActiveInLobby<SpecialEvents.AprilFools>())
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
            if (self.sessionEnded ||
                self.GameTypeSetup.spearHitScore == 0 ||
                !CreatureSymbol.DoesCreatureEarnATrophy(target.Template.type))
            {
                return;
            }

            if (attacker.abstractCreature.GetOnlineCreature() is not OnlineCreature attackerOCreature)
            {
                RainMeadow.Error("Unable to find the attacker online creature.");
                return;
            }
            if (target.abstractCreature.GetOnlineCreature() is not OnlineCreature targetOCreature)
            {
                RainMeadow.Error("Unable to find the target online creature.");
                return;
            }
            if (ArenaHelpers.FindArenaPlayerByOnlinePlayer(arenaOnline, attackerOCreature.owner) is not ArenaSitting.ArenaPlayer attackerArenaPlayer)
            {
                RainMeadow.Error($"Unable to find {attackerOCreature.owner}'s arena player.");
                return;
            }

            if (self.arenaSitting.gameTypeSetup.wildLifeSetting == ArenaSetup.GameTypeSetup.WildLifeSetting.Off &&
                !targetOCreature.isAvatar)
            {
                RainMeadow.Warn($"A non-avatar creature ({target}) was killed by {attackerOCreature.owner} despite wildlife being off.");
                return;
            }
            if (!attackerOCreature.isMine)
            {
                RainMeadow.Info($"Attacker ({attackerOCreature.owner}) is not me. Returning early.");
                return;
            }
            if (!targetOCreature.isAvatar)
            {
                RainMeadow.Info("A non-avatar creature was stabbed. Returning early.");
                return;
            }
            if (target.State is PlayerState { permanentDamageTracking: >= 1 })
            {
                RainMeadow.Info(
                    $"Target ({targetOCreature.owner}) is going to die or is already" +
                    $"dead, which kill scoring is handled elsewhere. Returning early."
                );
                return;
            }
            if (attacker.SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
            {
                RainMeadow.Info(
                    "Gourmand stabbed someone. Logic needs to be added to give the spear hit " +
                    "score if the gourmand was not exhausted before throwing. Returning early."
                );
                return;
            }


            // TODO: Theoretically, on high enough ping, you can get a ton of duplicate points.
            ArenaRPCs.ModifyArenaPlayerScore(attackerArenaPlayer.playerNumber, arenaOnline.spearHitScore);

            targetOCreature.BroadcastRPCInRoom(
                ArenaRPCs.ModifyArenaPlayerScore,
                attackerArenaPlayer.playerNumber,
                arenaOnline.spearHitScore
            );
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
            
            self.AddPart(new ArenaSpawnLocationIndicator(self, session.game.cameras[0]));

            if (OnlineManager
                    .lobby.clientSettings[OnlineManager.mePlayer]
                    .GetData<ArenaClientSettings>()
                    .playingAs == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator && arena.enableOverseer)
            {
                
                self.AddPart(new MeadowEmoteHud(self, session.game.cameras[0], 
                    arena.avatars.First(x => x.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Overseer).realizedCreature));
            }
            else
            {
                self.AddPart(new Pointing(self));
                foreach(AbstractCreature localPlayer in session.Players.Where(x => x != null && x.IsLocal()).ToArray())
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

        public virtual void On_ArenaCreatureSpawner_SpawnArenaCreatures(
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
                if (display.slugIcon != null) display.slugIcon.scale = 0.08f;
                return "meadowcoin";
            }

            if (arena.reigningChamps != null && arena.reigningChamps.list != null && arena.reigningChamps.list.Contains(player.id))
            {
                return "Multiplayer_Star";
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
                    0, // Must be 0 so the correct inputs are used. This breaks the vanilla way of finding the corresponding ArenaPlayer and Player.
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
                    SpawnPlayerOverseer(
                        arena,
                        self,
                        room,
                        randomExitIndex
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

        public void SpawnPlayerOverseer(ArenaOnlineGameMode arena,
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

        public virtual void On_ArenaGameSession_Update(
            On.ArenaGameSession.orig_Update orig,
            ArenaGameSession self,
            ArenaOnlineGameMode arena)
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
                        && ac.creatureTemplate.type != CreatureTemplate.Type.Overseer
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

            // RainMeadow.Warn($"{string.Join(", ", arena.session.arenaSitting.players.Select(arenaPlayer => ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, arenaPlayer.playerNumber)?.ToString() ?? "<unknown>"))}");

            if (!self.sessionEnded)
            {
                foreach (ArenaSitting.ArenaPlayer arenaPlayer in self.arenaSitting.players)
                {
                    AbstractCreature? playerAC = ArenaHelpers.FindPlayerACByArenaPlayer(arena, arenaPlayer);
                    OnlineCreature? onlineCreature = playerAC?.GetOnlineCreature();
                    if (playerAC is null || onlineCreature is null) // Included playerAC is null to satisfy the compiler.
                        continue;

                    PlayerState playerState = (PlayerState)playerAC.state;
                    int newFoodInStomach = playerState.foodInStomach - previousFoodInStomach;
                    int scoreChange = newFoodInStomach * arena.foodScore;

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

                        previousFoodInStomach = playerState.foodInStomach;
                    }

                    if (!playerState.dead)
                        arenaPlayer.timeAlive++;
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

        // TODO: Implement override in team battle.
        public virtual List<ArenaSitting.ArenaPlayer> On_ArenaSitting_FinalSittingResult(
            ArenaOnlineGameMode arena,
            On.ArenaSitting.orig_FinalSittingResult orig,
            ArenaSitting self)
        {
            UpdateArenaSittingFinalStats(arena, self);

            if (self.players.Count <= 1)
                return self.players;

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

        // TODO: Name this better.
        public virtual void UpdateArenaSessionFinalStats(
            ArenaOnlineGameMode arenaOnline,
            ArenaGameSession arenaSession)
        {
            ArenaSitting arenaSitting = arenaSession.arenaSitting;

            foreach (ArenaSitting.ArenaPlayer arenaPlayer in arenaSitting.players)
            {
                OnlinePlayer? onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arenaOnline, arenaPlayer.playerNumber);

                if (onlinePlayer is null)
                    continue;
                if (arenaPlayer.playerClass == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                {
                    arenaOnline.ResetArenaPlayerStats(arenaPlayer);

                    if (OnlineManager.lobby.isOwner)
                        arenaOnline.CopyStatsToLobbyData(arenaPlayer, onlinePlayer);

                    continue;
                }

                // Winner and Alive are not part of lobby data.
                arenaPlayer.winner = false; // Wins are handled later.
                arenaPlayer.alive = arenaSession.EndOfSessionLogPlayerAsAlive(arenaPlayer.playerNumber);

                if (OnlineManager.lobby.isOwner)
                {
                    if (arenaPlayer.alive)
                    {
                        arenaPlayer.score += CalculateGraspsFoodScore(arenaOnline, arenaPlayer);
                        arenaPlayer.score += arenaSitting.gameTypeSetup.survivalScore;
                    }
                    else
                        arenaPlayer.deaths++;

                    arenaPlayer.allKills.AddRange(arenaPlayer.roundKills);
                    arenaPlayer.totScore += arenaPlayer.score;

                    arenaOnline.CopyStatsToLobbyData(arenaPlayer, onlinePlayer);
                }
                else
                {
                    // This works for both loops because clients only change non-lobby data stats.
                    arenaOnline.CopyStatsFromLobbyData(arenaPlayer, onlinePlayer);
                }
            }

            // Winners must be handled here to ensure that every other player's stats have been updated.
            List<ArenaSitting.ArenaPlayer> winners = DetermineWinnersOfArenaSession(arenaOnline, arenaSession);

            foreach (ArenaSitting.ArenaPlayer arenaPlayer in winners)
            {
                OnlinePlayer? onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arenaOnline, arenaPlayer.playerNumber);
                if (onlinePlayer is null) continue;

                arenaPlayer.winner = true;

                if (OnlineManager.lobby.isOwner)
                {
                    arenaPlayer.wins++;
                    arenaOnline.CopyStatsToLobbyData(arenaPlayer, onlinePlayer);
                }
            }
        }

        // TODO: Name this better.
        // This is called from On_ArenaSitting_FinalSittingResult which is just a getter. This may be called multiple
        // times per session end and therefore stats changing should be the same regardless of times called. TODO: Make this inline documentation
        public virtual void UpdateArenaSittingFinalStats(
            ArenaOnlineGameMode arenaOnline,
            ArenaSitting arenaSitting)
        {
            arenaSitting.players
                .ForEach(arenaPlayer => arenaPlayer.winner = false);

            DetermineWinnersOfArenaSitting(arenaOnline, arenaSitting)
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

            if (a.winner != b.winner)
                return a.winner;
            if (a.score != b.score && arenaOnline.WinByScore)
                return a.score > b.score;
            if (a.alive != b.alive)
                return a.alive;

            return orig(self, a, b);
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

            if (a.winner != b.winner)
                return a.winner;
            if (a.score != b.score && arenaOnline.WinByScore)
                return a.score > b.score;
            if (a.wins != b.wins)
                return a.wins > b.wins;

            return orig(self, a, b);
        }

        public virtual List<ArenaSitting.ArenaPlayer> DetermineWinnersOfArenaSession(
            ArenaOnlineGameMode arenaOnline,
            ArenaGameSession arenaSession)
        {
            ArenaSitting arenaSitting = arenaSession.arenaSitting;

            List<ArenaSitting.ArenaPlayer> aliveArenaPlayers = arenaSitting.players
                .Where(aPlayer => aPlayer.alive)
                .ToList();

            return aliveArenaPlayers.Count == 1
                ? aliveArenaPlayers
                : [];
        }

        public virtual List<ArenaSitting.ArenaPlayer> DetermineWinnersOfArenaSitting(
            ArenaOnlineGameMode arenaOnline,
            ArenaSitting arenaSitting)
        {
            if (arenaSitting.players.Count == 0)
                return [];

            List<ArenaSitting.ArenaPlayer> bestArenaPlayers = arenaSitting.players
                .Where(arenaPlayer => arenaPlayer.playerClass != RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                .ToList();

            if (arenaOnline.WinByScore)
            {
                int highestTotalScore = bestArenaPlayers.Max(aPlayer => aPlayer.totScore);

                bestArenaPlayers.RemoveAll(arenaPlayer => arenaPlayer.totScore != highestTotalScore);
            }
            else
            {
                bestArenaPlayers.RemoveAll(arenaPlayer => !arenaPlayer.alive);
            }

            return bestArenaPlayers.Count == 1
                ? bestArenaPlayers
                : [];
        }

        public virtual int CalculateGraspsFoodScore(
            ArenaOnlineGameMode arenaOnline,
            ArenaSitting.ArenaPlayer arenaPlayer)
        {
            AbstractCreature? playerAC = ArenaHelpers.FindPlayerACByArenaPlayer(arenaOnline, arenaPlayer);
            if (playerAC is null)
            {
                RainMeadow.Error($"Unable to find arena player's player AC. Player number: {arenaPlayer.playerNumber}.");
                return 0;
            }

            if (playerAC.realizedCreature is not Player player)
                return 0;

            int score = 0;
            foreach (Creature.Grasp? grasp in player.grasps)
            {
                if (grasp?.grabbed is IPlayerEdible playerEdible)
                    score += playerEdible.FoodPoints * arenaOnline.foodScore;
            }

            return score;
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

        public List<ExternalArenaGameModeSetting> savedSettings =
        [
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.aliveScore)),
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
            new ExternalArenaGameModeFieldSetting(nameof(ArenaOnlineGameMode.emptyKillTagScore)),
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

        public virtual string ExportLocalSettings(ArenaOnlineGameMode arena)
        {
            List<string> pairs = new();
            for (int i = 0; i < savedSettings.Count; i++)
            {
                string val = savedSettings[i].GetSaveString(arena);
                pairs.Add($"{savedSettings[i].settingNickname}={val}");
                RainMeadow.Debug($"Copy setting {savedSettings[i].settingNickname} at {val}");
            }

            string combined = string.Join("|", pairs);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
        }

        public virtual bool ImportLocalSettings(ArenaOnlineGameMode arena, string base64Data)
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
                        savedSettings[index].SetValueFromString(val, arena);
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
        public abstract object GetValueFromString(string value);
        public abstract void SetValueFromString(string value, ArenaOnlineGameMode arenaMode);
        public abstract object GetValueFromArenaMode(ArenaOnlineGameMode arenaMode);
        public abstract string GetSaveString(ArenaOnlineGameMode arenaMode);
        public string settingID { get; } = settingID;
        public string settingNickname { get; } = settingNickname == "" ? settingID : settingNickname;
    }

    public class ExternalArenaGameModeFieldSetting(string settingID, string settingNickname = "")
        : ExternalArenaGameModeSetting(settingID, settingNickname)
    {
        // For now, suppost simple values (with IConvertible) and list of simple values.
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
        protected const char SEPARATOR = ',';
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
        public override void SetValueFromString(string value, ArenaOnlineGameMode arenaMode)
        {
            try
            {
                settingField.SetValue(arenaMode, GetValueFromString(value));
            }
            catch (System.Exception e)
            {
                RainMeadow.Error(e);
            }
        }
        public override object GetValueFromArenaMode(ArenaOnlineGameMode arenaMode)
        {
            return settingField.GetValue(arenaMode);
        }
        public override string GetSaveString(ArenaOnlineGameMode arenaMode)
        {
            var value = GetValueFromArenaMode(arenaMode);

            if (value is IEnumerable enumerable && value is not string)
            {
                return string.Join(SEPARATOR.ToString(), enumerable.Cast<object>());
            }
            return settingField.GetValue(arenaMode).ToString();
        }
        public FieldInfo settingField {get;} = typeof(ArenaOnlineGameMode).GetField(settingID);
        public Type settingType {get;} = typeof(ArenaOnlineGameMode).GetField(settingID).FieldType;
    }
    public class ExternalArenaGameModeInterfaceMultiChoiceSetting(string settingID, string settingNickname = "")
        : ExternalArenaGameModeSetting(settingID, settingNickname)
    {
        public override object GetValueFromArenaMode(ArenaOnlineGameMode arenaMode)
        {
            return arenaMode.onlineArenaSettingsInterfaceMultiChoice[settingID];
        }

        public override object GetValueFromString(string value)
        {
            return int.TryParse(value, out var result) ? result : 0;
        }
        public override void SetValueFromString(string value, ArenaOnlineGameMode arenaMode)
        {
            try
            {
                arenaMode.onlineArenaSettingsInterfaceMultiChoice[settingID] = (int)GetValueFromString(value);
            }
            catch (System.Exception e)
            {
                RainMeadow.Error(e);
            }
        }
        public override string GetSaveString(ArenaOnlineGameMode arenaMode)
        {
            return arenaMode.onlineArenaSettingsInterfaceMultiChoice[settingID].ToString();
        }
    }
}

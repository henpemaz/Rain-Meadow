using System.Collections.Generic;
using Menu;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using UnityEngine;
using System.Linq;
using HUD;

namespace RainMeadow
{
    public static class ArenaRPCs
    {
        [RPCMethod]
        public static void ShowMeTheMoney(OnlinePhysicalObject killer, OnlinePhysicalObject killedCreature)
        {
            ArenaClientSettings? playerClient = ArenaHelpers.GetArenaClientSettings(killer.owner);
            if ((playerClient != null && playerClient.gotSlugcat) || SpecialEvents.EventActiveInLobby<SpecialEvents.AprilFools>())
            {
                if (killer.owner.isMe)
                {
                    SpecialEvents.GainedMeadowCoin(1);
                }
                if (killedCreature.apo.realizedObject is Creature c && c != null)
                {
                    SpecialEvents.PlayMeadowCoinSound(room: c.room);
                    for (int x = 0; x < 20; x++)
                    {
                        c.room.AddObject(new MeadowTokenCoin.MeadowCoin(c.bodyChunks.OfType<BodyChunk>().First().pos + RWCustom.Custom.RNV() * 2f, RWCustom.Custom.RNV() * 16f * UnityEngine.Random.value, Color.Lerp(Color.yellow, new Color(1f, 1f, 1f), 0.5f + 0.5f * UnityEngine.Random.value), false));
                    }
                }

            }
        }

        [RPCMethod]
        public static void ModifyArenaPlayerScore(int playerNumber, int scoreChange)
        {
            if (scoreChange == 0)
                RainMeadow.Warn($"{scoreChange} is 0.");

            if (!RainMeadow.isArenaMode(out ArenaOnlineGameMode arenaOnline))
            {
                RainMeadow.Error("The online game mode is not Arena.");
                return;
            }
            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is not RainWorldGame game)
            {
                RainMeadow.Warn("Not currently in-game.");
                return;
            }
            if (game.session is not ArenaGameSession arenaSession)
            {
                RainMeadow.Warn("Not in an arena session.");
                return;
            }
            if (arenaSession.sessionEnded)
            {
                RainMeadow.Warn("Session ended.");
                return;
            }
            if (playerNumber < 0 || playerNumber >= arenaSession.arenaSitting.players.Count)
            {
                RainMeadow.Warn($"Player number is out of range. Player number: {playerNumber}. Player count: {arenaSession.arenaSitting.players.Count}");
                return;
            }
            if (ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arenaOnline, playerNumber) is not OnlinePlayer onlinePlayer)
            {
                RainMeadow.Warn($"Unable to find online player with player number {playerNumber}.");
                return;
            }


            ArenaSitting.ArenaPlayer arenaPlayer = arenaSession.arenaSitting.players[playerNumber];

            if (arenaPlayer.playerClass == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
            {
                RainMeadow.Error($"{onlinePlayer} is a spectator.");
                return;
            }

            RainMeadow.Info($"Adding {scoreChange} to {onlinePlayer}'s score.");
            arenaPlayer.score += scoreChange;

            if (OnlineManager.lobby.isOwner)
                arenaOnline.CopyStatsToLobbyData(arenaPlayer, onlinePlayer);
        }

        [RPCMethod]
        public static void AddArenaPlayerRoundKills(int playerNumber, List<string> trophiesAsStrings)
        {
            if (trophiesAsStrings.Count == 0)
                RainMeadow.Warn($"{nameof(trophiesAsStrings)} has 0 elements.");

            if (!RainMeadow.isArenaMode(out ArenaOnlineGameMode arenaOnline))
            {
                RainMeadow.Error("The online game mode is not Arena.");
                return;
            }
            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is not RainWorldGame game)
            {
                RainMeadow.Warn("Not currently in-game.");
                return;
            }
            if (game.session is not ArenaGameSession arenaSession)
            {
                RainMeadow.Warn("Not in an arena session.");
                return;
            }
            if (arenaSession.sessionEnded)
            {
                RainMeadow.Warn("Session ended.");
                return;
            }
            if (playerNumber < 0 || playerNumber >= arenaSession.arenaSitting.players.Count)
            {
                RainMeadow.Warn($"Player number is out of range. Player number: {playerNumber}. Player count: {arenaSession.arenaSitting.players.Count}");
                return;
            }
            if (ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arenaOnline, playerNumber) is not OnlinePlayer onlinePlayer)
            {
                RainMeadow.Warn($"Unable to find arena player's online player. Player number: {playerNumber}.");
                return;
            }

            ArenaSitting.ArenaPlayer arenaPlayer = arenaSession.arenaSitting.players[playerNumber];

            if (arenaPlayer.playerClass == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
            {
                RainMeadow.Error($"{onlinePlayer} is a spectator.");
                return;
            }

            List<IconSymbol.IconSymbolData> trophies = trophiesAsStrings
                .Select(IconSymbol.IconSymbolData.IconSymbolDataFromString)
                .ToList();

            RainMeadow.Info($"Adding [ {string.Join(", ", trophies.Select(trophy => trophy.critType))} ] to {onlinePlayer}'s round kills.");
            arenaPlayer.roundKills.AddRange(trophies);

            if (OnlineManager.lobby.isOwner)
                arenaOnline.CopyStatsToLobbyData(arenaPlayer, onlinePlayer);

            if (onlinePlayer.isMe)
            {
                arenaSession.game.cameras[0].hud.parts
                    .OfType<PlayerSpecificMultiplayerHud>()
                    .ToList()
                    .ForEach(hud => trophies.ForEach(hud.killsList.Killing));
                // There should only be one but might as well loop over it.
            }
        }

        [RPCMethod]
        public static void Arena_ForceReady()
        {
            if (!RainMeadow.isArenaMode(out var arena)) return;
            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is MultiplayerResults resl)
            {
                resl.manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.ArenaLobbyMenu);
                arena.ResetOnReturnMenu(resl.manager);
            }
            arena.arenaClientSettings.ready = true;
        }
        [RPCMethod]
        public static void Arena_NotifySpawnPoint(int martyrs, int outlaws, int dragonslayers, int chieftains)
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                if (TeamBattleMode.isTeamBattleMode(arena, out var tb))
                {

                    tb.martyrsSpawn = martyrs;
                    tb.outlawsSpawn = outlaws;
                    tb.dragonslayersSpawn = dragonslayers;
                    tb.chieftainsSpawn = chieftains;
                }
            }
        }
        [RPCMethod]
        public static void Arena_RemovePlayerWhoQuit(OnlinePlayer earlyQuitterOrLatecomer)
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                if (arena.arenaSittingOnlineOrder.Contains(earlyQuitterOrLatecomer.inLobbyId))
                {
                    arena.arenaSittingOnlineOrder.Remove(earlyQuitterOrLatecomer.inLobbyId); // you'll add them in NextLevel
                }
            }
        }

        [RPCMethod]
        public static void Arena_AddPlayerWaiting(OnlinePlayer earlyQuitterOrLatecomer)
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                if (!arena.playersLateWaitingInLobbyForNextRound.Contains(earlyQuitterOrLatecomer.inLobbyId))
                {
                    arena.playersLateWaitingInLobbyForNextRound.Add(earlyQuitterOrLatecomer.inLobbyId); // you'll add them in NextLevel
                }

            }
        }

        [RPCMethod]
        public static void Arena_NotifyRejoinAllowed(bool hasPermission)
        {
            var lobby = RWCustom.Custom.rainWorld.processManager.currentMainLoop as ArenaLobbyMenu;
            if (RainMeadow.isArenaMode(out var arena))
            {
                if (lobby == null)
                {
                    RainMeadow.Debug("Could not start player");
                    return;
                }
                RainMeadow.Debug("Starting game for player");
                arena.isInGame = true; // state might be too late
                arena.hasPermissionToRejoin = hasPermission;
                RainMeadow.Debug("Start game immediately");
                lobby.StartGame();
            }
        }
        [RPCMethod]
        public static void Arena_EndSessionEarly()
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
                if (game == null)
                {
                    RainMeadow.Error("Arena: RainWorldGame is null!");
                    return;
                }
                game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MultiplayerResults);



            }
        }

        [RPCMethod]
        public static void Arena_ForceReadyUp()
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                var lobby = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as ArenaLobbyMenu);
                var stillInGame = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as MultiplayerResults);

                if (stillInGame != null)
                {
                    arena.returnToLobby = true;
                    stillInGame.manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.ArenaLobbyMenu);
                    stillInGame.manager.rainWorld.options.DeleteArenaSitting();
                    stillInGame.ArenaSitting.players.Clear();
                    OnlineManager.lobby.owner.InvokeOnceRPC(ArenaRPCs.Arena_NotifyLobbyReadyUp, OnlineManager.mePlayer);
                    return;
                }
                if (lobby.manager.upcomingProcess != null)
                {
                    return;
                }

                if (lobby.playButton != null)
                {
                    lobby.playButton.Clicked();
                }
            }
        }
        [RPCMethod]
        public static void Arena_NotifyClassChange(OnlinePlayer userChangingClass, int currentColorIndex)
        {
            if (!RainMeadow.isArenaMode(out ArenaOnlineGameMode arena))
            {
                return;
            }
            if (OnlineManager.lobby.isOwner)
            {
                string id = userChangingClass.GetUniqueID();
                if (!arena.playersInLobbyChoosingSlugs.ContainsKey(id))
                {
                    arena.playersInLobbyChoosingSlugs.Add(id, currentColorIndex);
                    return;
                }
                arena.playersInLobbyChoosingSlugs[id] = currentColorIndex;

            }
        }

        [RPCMethod]
        public static void Arena_UpdateSelectedChoice(string stringID, int value)
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as ArenaLobbyMenu);
                if (game.manager.upcomingProcess != null)
                {
                    return;
                }

                foreach (var selectable in game.arenaSettingsInterface.menu.pages[0].selectables)
                {
                    if (selectable is MultipleChoiceArray.MultipleChoiceButton && (selectable as MultipleChoiceArray.MultipleChoiceButton).multipleChoiceArray.IDString == stringID)
                    {

                        game.arenaSettingsInterface.SetSelected((selectable as MultipleChoiceArray.MultipleChoiceButton).multipleChoiceArray, value); // why didn't this method take a freakin string
                    }

                }

            }
        }

        [RPCMethod]
        public static void Arena_UpdateSelectedCheckbox(string stringID, bool c)
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as ArenaLobbyMenu);
                if (game.manager.upcomingProcess != null)
                {
                    return;
                }

                foreach (var selectable in game.arenaSettingsInterface.menu.pages[0].selectables)
                {

                    if (selectable is Menu.CheckBox && (selectable as Menu.CheckBox).IDString == stringID)
                    {

                        game.arenaSettingsInterface.SetChecked((selectable as CheckBox), c);
                    }
                }

            }
        }

        [RPCMethod]
        public static void Arena_InitialSetupTimers(int setupTime, int saintMaxTime)
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                arena.setupTime = setupTime;
                // I don't think this is used so I'm not sure if the param here is 
                // retrieved from host's meadow remix settings or host's actual 
                // Player.maxGodTime. I'll leave it be but if this is causing issues
                // just slap on a * 40 or / 40
                arena.arenaSaintAscendanceTimer = saintMaxTime;
            }

        }


        [RPCMethod]
        public static void Arena_ReadyForNextLevel()
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                var lobby = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as ArenaLobbyMenu);
                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
                if (game.manager.upcomingProcess != null)
                {
                    return;
                }
                for (int i = 0; i < game.arenaOverlay.resultBoxes.Count; i++)
                {
                    game.arenaOverlay.result[i].readyForNextRound = true;
                }
                game.arenaOverlay.nextLevelCall = true;

            }

        }


        [RPCMethod(runDeferred = true)]
        public static void Arena_RestartGame(RPCEvent rpcEvent)
        {

            if (rpcEvent.from == OnlineManager.lobby.owner && RainMeadow.isArenaMode(out var arena))
            {
                arena.leaveToRestart = true;
            }
        }


        [RPCMethod]
        public static void Arena_NotifyLobbyReadyUp(OnlinePlayer userIsReady)
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                if (!arena.playersReadiedUp.list.Contains(userIsReady.id))
                {
                    arena.playersReadiedUp.list.Add(userIsReady.id);
                }

            }
        }


        [RPCMethod]
        public static void AddShortCutVessel(RWCustom.IntVector2 pos, OnlinePhysicalObject crit, RoomSession roomSess, int wait)
        {

            var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
            var lobby = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as ArenaLobbyMenu);

            if (lobby != null)
            {
                return;
            }
            if (game?.manager.upcomingProcess != null)
            {
                return;
            }
            var creature = (crit?.apo.realizedObject as Creature);
            var room = roomSess.absroom.world;
            var roomPos = room.GetAbstractRoom(0);
            var shortCutVessel = new ShortcutHandler.ShortCutVessel(pos, creature, roomPos, wait);
            game?.GetArenaGameSession.exitManager.playersInDens.Add(shortCutVessel);

        }


        [RPCMethod]
        public static void Arena_LevelToPlaylist(string chosenLevel)
        {

            var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as ArenaLobbyMenu);
            if (game.manager.upcomingProcess != null)
            {
                return;
            }

            (game).GetGameTypeSetup.playList.Add(chosenLevel);
            game.levelSelector.levelsPlaylist.AddLevelItem(new Menu.LevelSelector.LevelItem(game, game.levelSelector.levelsPlaylist, chosenLevel));
            game.levelSelector.levelsPlaylist.ScrollPos = game.levelSelector.levelsPlaylist.LastPossibleScroll;
            game.levelSelector.levelsPlaylist.ConstrainScroll();

        }

        [RPCMethod]
        public static void Arena_LevelFromPlaylist(int index, string chosenLevel)
        {

            var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as ArenaLobbyMenu);
            if (game.manager.upcomingProcess != null)
            {
                return;
            }

            if (game.levelSelector.levelsPlaylist.levelItems.Count > 0)
            {
                game.GetGameTypeSetup.playList.RemoveAt(index);
                game.levelSelector.levelsPlaylist.RemoveLevelItem(new Menu.LevelSelector.LevelItem(game, game.levelSelector.levelsPlaylist, chosenLevel));
                game.levelSelector.levelsPlaylist.ScrollPos = game.levelSelector.levelsPlaylist.LastPossibleScroll;
                game.levelSelector.levelsPlaylist.ConstrainScroll();
            }

        }
    }
}

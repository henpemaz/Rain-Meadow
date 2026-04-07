using Menu;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;

namespace RainMeadow
{
    public static class ArenaRPCs
    {

        [RPCMethod]
        public static void UpdatePlayerScore(int playerNumber, int newScore)
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                OnlinePlayer? onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, playerNumber);
                if (onlinePlayer == null)
                {
                    return;
                }
                var game = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;
                if (game == null)
                {
                    RainMeadow.Error("Arena: RainWorldGame is null!");
                    return;
                }
                if (game.session is ArenaGameSession a && a.arenaSitting.players.Contains(a.arenaSitting.players[playerNumber]) && a.arenaSitting.players[playerNumber].score < newScore)
                {
                    a.arenaSitting.players[playerNumber].score = newScore;
                    if (OnlineManager.lobby.isOwner)
                    {
                        arena.playerNumberWithScore[onlinePlayer.inLobbyId] = a.arenaSitting.players[playerNumber].score;
                    }
                }
            }
        }

        [RPCMethod]
        public static void AddKilledCreatureToHUD(OnlineCreature onlineKilledCreature)
        {

            var game = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;
            if (game == null)
            {
                RainMeadow.Error("Arena: RainWorldGame is null!");
                return;
            }
            for (int j = 0; j < game.cameras[0].hud.parts.Count; j++)
            {
                if (game.cameras[0].hud.parts[j] is HUD.PlayerSpecificMultiplayerHud multiHud)
                {
                    multiHud.killsList.Killing(CreatureSymbol.SymbolDataFromCreature(onlineKilledCreature.abstractCreature));
                    break;
                }
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

        // TODO: May be unused now since I updated _Killing
        public static void Arena_AddTrophy(OnlinePhysicalObject creatureKilled, int playerNum)
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
                if (game == null)
                {
                    return;
                }
                if (game.manager.upcomingProcess != null)
                {
                    return;
                }
                var crit = (creatureKilled.apo.realizedObject as Creature) ?? null;
                if (crit == null)
                {
                    return;
                }
                IconSymbol.IconSymbolData iconSymbolData = CreatureSymbol.SymbolDataFromCreature(crit.abstractCreature);
                for (int i = 0; i < game.GetArenaGameSession.arenaSitting.players.Count; i++)
                {
                    if (game.GetArenaGameSession.arenaSitting.players[i].playerNumber != playerNum)
                    {
                        continue;
                    }
                    OnlinePlayer? pl = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, playerNum);
                    if (CreatureSymbol.DoesCreatureEarnATrophy(crit.Template.type))
                    {
                        game.GetArenaGameSession.arenaSitting.players[i].roundKills.Add(iconSymbolData);
                        game.GetArenaGameSession.arenaSitting.players[i].allKills.Add(iconSymbolData);
                        if (pl != null)
                        {
                            arena.playerNumberWithTrophies[pl.inLobbyId].Add(iconSymbolData.ToString());
                            arena.playerNumberWithTrophiesPerRound[pl.inLobbyId].Add(iconSymbolData.ToString());
                            // 7
                            if (crit.abstractCreature.realizedCreature.Template.type == CreatureTemplate.Type.Slugcat)
                            {
                                RainMeadow.Debug($"RMEL;{pl.id.DisplayName};KILLED;{creatureKilled.owner.id.DisplayName};SCORE;{game.GetArenaGameSession.arenaSitting.players[i]}");
                            }
                        }

                    }
                }

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
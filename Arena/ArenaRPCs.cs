using Menu;
using UnityEngine;

namespace RainMeadow
{
    public static class ArenaRPCs
    {

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
                arena.arenaSaintAscendanceTimer = saintMaxTime;
            }

        }


        [RPCMethod]
        public static void Arena_IncrementPlayersLeftt()
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                arena.playerLeftGame = arena.playerLeftGame + 1;

            }

        }

        [RPCMethod]
        public static void Arena_IncrementPlayersJoined()
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                arena.playerEnteredGame = arena.playerEnteredGame + 1;

            }

        }

        [RPCMethod]
        public static void Arena_ResetPlayersLeft()
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                arena.playerLeftGame = 0;

            }

        }

        [RPCMethod]
        public static void Arena_ReadyForNextLevel(string userIsReady)
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
                if (game.manager.upcomingProcess != null)
                {
                    return;
                }
                for (int i = 0; i < game.arenaOverlay.resultBoxes.Count; i++)
                {
                    if (game.arenaOverlay.resultBoxes[i].playerNameLabel.text == userIsReady)
                    {
                        game.arenaOverlay.result[i].readyForNextRound = true;
                    }
                }
            }

        }

        [RPCMethod]
        public static void Arena_AddTrophy(OnlinePhysicalObject creatureKilled, int playerNum)
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
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
                    if (game.GetArenaGameSession.arenaSitting.players[i].playerNumber == playerNum)
                    {
                        if (CreatureSymbol.DoesCreatureEarnATrophy(crit.Template.type))
                        {
                            game.GetArenaGameSession.arenaSitting.players[i].roundKills.Add(iconSymbolData);
                            game.GetArenaGameSession.arenaSitting.players[i].allKills.Add(iconSymbolData);

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
        public static void Arena_NotifyStartGame()
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
                lobby.StartGame();
            }
        }




        [RPCMethod]
        public static void Arena_NextLevelCall()
        {
            var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
            var lobby = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as ArenaLobbyMenu);

            if (lobby != null)
            {
                return;
            }

            if (game.manager.upcomingProcess != null)
            {
                return;
            }
            game.GetArenaGameSession.arenaSitting.NextLevel(game.manager);
            game.arenaOverlay.nextLevelCall = true;
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
            if (game.manager.upcomingProcess != null)
            {
                return;
            }
            var creature = (crit?.apo.realizedObject as Creature);
            var room = roomSess.absroom.world;
            var roomPos = room.GetAbstractRoom(0);
            var shortCutVessel = new ShortcutHandler.ShortCutVessel(pos, creature, roomPos, wait);
            game.GetArenaGameSession.exitManager.playersInDens.Add(shortCutVessel);

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
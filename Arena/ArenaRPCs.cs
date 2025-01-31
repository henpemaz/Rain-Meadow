using Menu;
using Newtonsoft.Json.Linq;
using System;

namespace RainMeadow
{
    public static class ArenaRPCs
    {

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
        public static void Arena_NotifyClassChange(OnlinePlayer userChangingClass, int currentColorIndex)
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                arena.playersInLobbyChoosingSlugs[userChangingClass.id.name] = currentColorIndex;

                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as ArenaLobbyMenu);
                if (game.manager.upcomingProcess != null)
                {
                    return;
                }
                var Sluglist = ArenaHelpers.AllSlugcats();
                try
                {
                    for (int i = 1; i < game.usernameButtons.Length; i++)
                    {
                        if (MatchmakingManager.currentDomain == MatchmakingManager.MatchMakingDomain.LAN)
                        {
                            game.classButtons[i].portrait.fileName = game.ArenaImage(Sluglist[currentColorIndex], currentColorIndex);
                            game.classButtons[i].portrait.LoadFile();
                            game.classButtons[i].portrait.sprite.SetElementByName(game.classButtons[i].portrait.fileName); // trust the ordering
                        }
                        else
                        {
                            if (game.usernameButtons[i].menuLabel.text == userChangingClass.id.name) // TODO: Null referencing here
                            {
                                game.classButtons[i].portrait.fileName = game.ArenaImage(Sluglist[currentColorIndex], currentColorIndex);
                                game.classButtons[i].portrait.LoadFile();
                                game.classButtons[i].portrait.sprite.SetElementByName(game.classButtons[i].portrait.fileName);
                            }
                        }

                    }
                }
                catch
                {
                    RainMeadow.Debug("Could not find username button");
                }

            }

        }
        [RPCMethod]
        public static void Arena_NotifyLobbyReadyUp(OnlinePlayer userIsReady, int currentColorIndex)
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as ArenaLobbyMenu);
                if (game.manager.upcomingProcess != null)
                {
                    return;
                }
                arena.clientsAreReadiedUp++;
                if (!arena.playersReadiedUp.Contains(userIsReady.inLobbyId))
                {
                    arena.playersReadiedUp.Add(userIsReady.inLobbyId);
                }

                try
                {
                    for (int i = 1; i < game.usernameButtons.Length; i++)
                    {

                        if (MatchmakingManager.currentDomain == MatchmakingManager.MatchMakingDomain.LAN)
                        {
                            game.classButtons[i].readyForCombat = true; // trust the ordering
                        }
                        else
                        {
                            if (game.usernameButtons[i].menuLabel.text == userIsReady.id.name)
                            {

                                game.classButtons[i].readyForCombat = true;

                            }
                        }

                    }
                }
                catch
                {
                    RainMeadow.Debug("Could not find username button");
                }



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
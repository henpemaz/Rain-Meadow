﻿namespace RainMeadow
{
    public static class ArenaRPCs
    {

        [RPCMethod]
        public static void Arena_IncrementPlayersLeftt()
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                arena.clientWaiting = arena.clientWaiting + 1;

            }

        }

        [RPCMethod]
        public static void Arena_ResetPlayersLeft()
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                arena.clientWaiting = 0;

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
                for (int i = 0; i < arena.arenaSittingOnlineOrder.Count; i++)
                {
                    if (game.arenaOverlay.resultBoxes[i].playerNameLabel.text == userIsReady)
                    {
                        game.arenaOverlay.result[i].readyForNextRound = true;
                    }
                }
            }

        }

        [RPCMethod]
        public static void Arena_NotifyClassChange(string userIsReady, int currentColorIndex)
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as ArenaLobbyMenu);
                if (game.manager.upcomingProcess != null)
                {
                    return;
                }
                var Sluglist = ArenaHelpers.AllSlugcats();
                for (int i = 1; i < game.usernameButtons.Length; i++)
                {

                    if (game.usernameButtons[i].menuLabel.text == userIsReady)
                    {
                        if (currentColorIndex > 3 && ModManager.MSC)
                        {
                            game.classButtons[i].portrait.fileName = "MultiplayerPortrait" + "41-" + Sluglist[currentColorIndex];

                        }
                        else
                        {
                            game.classButtons[i].portrait.fileName = "MultiplayerPortrait" + currentColorIndex + "1";
                        }


                        game.classButtons[i].portrait.LoadFile();
                        game.classButtons[i].portrait.sprite.SetElementByName(game.classButtons[i].portrait.fileName);
                    }

                }

            }

        }
        [RPCMethod]
        public static void Arena_NotifyLobbyReadyUp(string userIsReady, int currentColorIndex)
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as ArenaLobbyMenu);
                if (game.manager.upcomingProcess != null)
                {
                    return;
                }

                for (int i = 1; i < game.usernameButtons.Length; i++)
                {

                    if (game.usernameButtons[i].menuLabel.text == userIsReady)
                    {
                        arena.clientsAreReadiedUp++;
                        game.classButtons[i].readyForCombat = true;
                    }

                }

            }

        }

        [RPCMethod]
        public static void Arena_Killing(OnlinePhysicalObject absCreaturePlayer, OnlinePhysicalObject target, string username)
        {
            if (RainMeadow.isArenaMode(out var arena))
            {

                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
                if (game.manager.upcomingProcess != null)
                {
                    return;
                }

                if (game.GetArenaGameSession.sessionEnded)
                {
                    return;
                }
                var killedCrit = (target.apo as AbstractCreature);

                IconSymbol.IconSymbolData iconSymbolData = CreatureSymbol.SymbolDataFromCreature(killedCrit);

                for (int i = 0; i < game.GetArenaGameSession.arenaSitting.players.Count; i++)
                {

                    if (absCreaturePlayer.owner.inLobbyId == arena.arenaSittingOnlineOrder[i])
                    {

                        if (CreatureSymbol.DoesCreatureEarnATrophy(killedCrit.realizedCreature.Template.type))
                        {
                            game.GetArenaGameSession.arenaSitting.players[i].roundKills.Add(iconSymbolData);
                            game.GetArenaGameSession.arenaSitting.players[i].allKills.Add(iconSymbolData);
                        }

                        int index = MultiplayerUnlocks.SandboxUnlockForSymbolData(iconSymbolData).Index;
                        if (index >= 0)
                        {
                            game.GetArenaGameSession.arenaSitting.players[i].AddSandboxScore(game.GetArenaGameSession.arenaSitting.gameTypeSetup.killScores[index]);
                        }
                        else
                        {
                            game.GetArenaGameSession.arenaSitting.players[i].AddSandboxScore(0);
                        }

                        break;
                    }

                }
            }

        }

        [RPCMethod]
        public static void Arena_NextLevelCall()
        {
            var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
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
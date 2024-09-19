using IL.RWCustom;
using System;
using System.Collections.Generic;
using Kittehface.Framework20;
using IL.Menu;
using System.Linq;

namespace RainMeadow
{
    public static class RPCs
    {
        [RPCMethod]
        public static void DeltaReset(RPCEvent rpcEvent, OnlineResource onlineResource, OnlineEntity.EntityId entity)
        {
            RainMeadow.Debug($"from {rpcEvent.from} resource {onlineResource} entity {entity}");
            if (entity != null)
            {
                foreach (var feed in OnlineManager.feeds)
                {
                    if (feed.player == rpcEvent.from && feed.entity.id == entity && feed.resource == onlineResource)
                    {
                        feed.ResetDeltas();
                        return;
                    }
                }
            }
            else
            {
                foreach (var subscription in OnlineManager.subscriptions)
                {
                    if (subscription.player == rpcEvent.from && subscription.resource == onlineResource)
                    {
                        subscription.ResetDeltas();
                        return;
                    }
                }
            }
        }

        [RPCMethod]
        public static void AddFood(short add)
        {
            ((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.Players[0].realizedCreature as Player).AddFood(add);
        }

        [RPCMethod]
        public static void AddQuarterFood()
        {
            ((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.Players[0].realizedCreature as Player).AddQuarterFood();
        }

        [RPCMethod]
        public static void AddMushroomCounter()
        {
            ((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.Players[0].realizedCreature as Player).mushroomCounter += 320;
        }

        [RPCMethod]
        public static void ReinforceKarma()
        {
            ((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.session as StoryGameSession).saveState.deathPersistentSaveData.reinforcedKarma = true;
        }

        [RPCMethod]
        public static void PlayReinforceKarmaAnimation()
        {
            (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame).cameras[0].hud.karmaMeter.reinforceAnimation = 0;
        }

        [RPCMethod]
        public static void InitGameOver()
        {
            var player = ((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.Players[0]);
            (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.cameras[0].hud.InitGameOverMode(null, 0, player.pos.room, new UnityEngine.Vector2(0f, 0f));
        }


        [RPCMethod]
        public static void IncrementPlayersLeftt()
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                arena.clientWaiting = arena.clientWaiting + 1;

            }

        }

        [RPCMethod]
        public static void ResetPlayersLeft()
        {
            if (RainMeadow.isArenaMode(out var arena))
            {
                arena.clientWaiting = 0;

            }

        }

        [RPCMethod]
        public static void MovePlayersToDeathScreen()
        {
            foreach (OnlinePlayer player in OnlineManager.players)
            {
                if (!player.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(RPCs.GoToDeathScreen)))
                {
                    player.InvokeRPC(RPCs.GoToDeathScreen);
                }
            }
        }

        [RPCMethod]
        public static void GoToDeathScreen()
        {
            var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
            if (game == null || game.manager.upcomingProcess != null)
            {
                return;
            }
            if (game.IsStorySession && game.GetStorySession.RedIsOutOfCycles && !game.rainWorld.ExpeditionMode)
            {
                game.GoToRedsGameOver();
                return;
            }
            game.GetStorySession.saveState.SessionEnded(game, false, false);
            RainMeadow.Debug("I am moving to the deathscreen");
            game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.DeathScreen);
        }

        [RPCMethod]
        public static void MovePlayersToWinScreen(bool malnourished, string denPos)
        {
            RainMeadow.Debug($"({malnourished}, {denPos})");

            var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
            if (game == null || game.manager.upcomingProcess != null) return;

            if (OnlineManager.lobby.playerAvatars.TryGetValue(OnlineManager.mePlayer, out var playerAvatar))
            {
                if (playerAvatar.type != (byte)OnlineEntity.EntityId.IdType.none
                    && playerAvatar.FindEntity(true) is OnlinePhysicalObject opo
                    && opo.apo is AbstractCreature ac
                    && ac.Room.shelter)
                {
                    denPos = ac.Room.name;
                }
            }

            if (denPos != null)
            {
                game.GetStorySession.saveState.denPosition = denPos;
                (OnlineManager.lobby.gameMode as StoryGameMode).defaultDenPos = denPos;
            }

            foreach (OnlinePlayer player in OnlineManager.players)
            {
                if (!player.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(RPCs.GoToWinScreen, malnourished, denPos)))
                {
                    if (player.isMe)
                    {
                        GoToWinScreen(malnourished, denPos);
                    }
                    else
                    {
                        player.InvokeRPC(RPCs.GoToWinScreen, malnourished, denPos);
                    }
                }
            }
        }

        //Assumed to be called for storymode only
        [RPCMethod]
        public static void GoToWinScreen(bool malnourished, string denPos)
        {
            RainMeadow.Debug($"({malnourished})");
            var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
            if (game == null || game.manager.upcomingProcess != null)
            {
                return;
            }

            if (OnlineManager.lobby.isOwner)
            {
                if (!malnourished && !game.rainWorld.saveBackedUp)
                {
                    game.rainWorld.saveBackedUp = true;
                    game.rainWorld.progression.BackUpSave("_Backup");
                }
            }
            else
            {
                var storyClientSettings = OnlineManager.lobby.gameMode.clientSettings as StoryClientSettings;
                if (storyClientSettings.isDead)
                {
                    storyClientSettings.myLastDenPos = denPos;
                }
            }

            // TODO: investigate client save desync (e.g. swallowed items)
            game.GetStorySession.saveState.SessionEnded(game, true, malnourished);

            //TODO: need to sync p5 and l2m deam events. Not doing it rn.
            DreamsState dreamsState = game.GetStorySession.saveState.dreamsState;

            if (dreamsState != null)
            {
                dreamsState.EndOfCycleProgress(game.GetStorySession.saveState, game.world.region.name, denPos);
                if (dreamsState.AnyDreamComingUp && !malnourished)
                {
                    game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Dream);
                    return;
                }
            }
            RainMeadow.Debug("I am moving to the sleepscreen");
            game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SleepScreen);
        }


        [RPCMethod]
        public static void MovePlayersToGhostScreen(string ghostID)
        {
            foreach (OnlinePlayer player in OnlineManager.players)
            {
                player.InvokeRPC(RPCs.GoToGhostScreen, ghostID);
            }
        }

        [RPCMethod]
        public static void GoToGhostScreen(string ghostID)
        {
            //For MSC support, we'll need to add a check for artificer campaign and send it to the VengeanceGhostScreen
            var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
            if (game.manager.upcomingProcess != null)
            {
                return;
            }
            ExtEnumBase.TryParse(typeof(GhostWorldPresence.GhostID), ghostID, false, out var rawEnumBase);
            game.sawAGhost = rawEnumBase as GhostWorldPresence.GhostID;
            game.GetStorySession.AppendTimeOnCycleEnd(true);
            if (game.GetStorySession.saveState.deathPersistentSaveData.karmaCap < 9)
            {
                game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.GhostScreen);
                return;
            }
            game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.KarmaToMaxScreen);
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

               for (int i = 1; i < game.usernameButtons.Length; i++) {

                    if (game.usernameButtons[i].menuLabel.text == userIsReady)
                    {
                        if (currentColorIndex > 3 && ModManager.MSC)
                        {
                            game.classButtons[i].portrait.fileName = "MultiplayerPortrait" + "41-" + game.mm.GetArenaSetup.playerClass[currentColorIndex];

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

    }
}

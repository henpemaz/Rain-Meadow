using System;
using System.Linq;

namespace RainMeadow
{
    public static class StoryRPCs
    {

        [RPCMethod]
        public static void ChangeFood(short amt)
        {
            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.Players[0]?.state is PlayerState state)
            {
                var newFood = Math.Max(0, Math.Min(state.foodInStomach * 4 + state.quarterFoodPoints + amt, game.session.characterStats.maxFood * 4));
                state.foodInStomach = newFood / 4;
                state.quarterFoodPoints = newFood % 4;
            }
        }

        [RPCMethod]
        public static void AddMushroomCounter()
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)) return;
            (game.FirstAnyPlayer.realizedCreature as Player).mushroomCounter += 320;
        }

        [RPCMethod]
        public static void ReinforceKarma()
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.session is StoryGameSession storyGameSession && game.manager.upcomingProcess is null)) return;
            storyGameSession.saveState.deathPersistentSaveData.reinforcedKarma = true;
        }

        [RPCMethod]
        public static void PlayReinforceKarmaAnimation()
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)) return;
            game.cameras[0].hud.karmaMeter.reinforceAnimation = 0;
        }

        [RPCMethod]
        public static void GoToWinScreen(bool malnourished, string? denPos, bool fromWarpPoint)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;

            if (RainMeadow.isStoryMode(out var storyGameMode) && !storyGameMode.hasSheltered)
            {
                storyGameMode.myLastDenPos = denPos;
            }

            game.Win(malnourished, fromWarpPoint);
        }

        [RPCMethod]
        public static void GoToStarveScreen(string? denPos)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;

            if (RainMeadow.isStoryMode(out var storyGameMode) && !storyGameMode.hasSheltered)
            {
                storyGameMode.myLastDenPos = denPos;
            }

            game.GoToStarveScreen();
        }

        [RPCMethod]
        public static void GoToGhostScreen(GhostWorldPresence.GhostID ghostID)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;

            game.GhostShutDown(ghostID);
        }

        [RPCMethod]
        public static void GoToDeathScreen()
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;

            game.GoToDeathScreen();
        }

        [RPCMethod]
        public static void GoToPassageScreen(WinState.EndgameID endGameID)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is Menu.SleepAndDeathScreen sleepAndDeathScreen && RWCustom.Custom.rainWorld.processManager.upcomingProcess is null)) return;
            sleepAndDeathScreen.proceedWithEndgameID = endGameID;
            RWCustom.Custom.rainWorld.processManager.RequestMainProcessSwitch(ProcessManager.ProcessID.CustomEndGameScreen);
        }

        [RPCMethod]
        public static void GoToRedsGameOver()
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;

            game.GoToRedsGameOver();
        }

        [RPCMethod]
        public static void GoToRivuletEnding(RPCEvent rpc)
        {
            if (rpc != null && OnlineManager.lobby.owner != rpc.from) return;
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;
            game.manager.pebblesHasHalcyon = true;
            game.manager.desiredCreditsSong = "NA_19 - Halcyon Memories";
            foreach (MoreSlugcats.PersistentObjectTracker persistentObjectTracker in game.GetStorySession.saveState.objectTrackers)
            {
                if (persistentObjectTracker.repType == MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType.HalcyonPearl && persistentObjectTracker.lastSeenRoom != "RM_AI")
                {
                    game.manager.pebblesHasHalcyon = false;
                    game.manager.desiredCreditsSong = "NA_43 - Isolation";
                    break;
                }
            }
            game.manager.nextSlideshow = MoreSlugcats.MoreSlugcatsEnums.SlideShowID.RivuletAltEnd;
            game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SlideShow);
        }
        
        [RPCMethod]
        public static void GoToSpearmasterEnding(RPCEvent rpc)
        {
            if (rpc != null && OnlineManager.lobby.owner != rpc.from) return;
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;
            game.manager.statsAfterCredits = true;
            game.manager.desiredCreditsSong = "NA_11 - Digital Sundown";
            game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Credits);
        }

        // Perform a warp (precast, host needs to "finish" to activate)
        [RPCMethod]
        public static void PerformWatcherRiftWarp(RPCEvent rpc, string? sourceRoomName, string warpData, bool useNormalWarpLoader)
        {
            if (rpc != null && OnlineManager.lobby.owner != rpc.from) return;
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;
            RainMeadow.Debug($"WARP.DATA? {warpData}, Loader={useNormalWarpLoader}");
            // generate "local" warp point
            Watcher.WarpPoint.WarpPointData newWarpData = new Watcher.WarpPoint.WarpPointData(null);
            newWarpData.FromString(warpData);
            PlacedObject placedObject = new PlacedObject(PlacedObject.Type.WarpPoint, newWarpData);
            Watcher.WarpPoint warpPoint = new Watcher.WarpPoint(null, placedObject);
            if (sourceRoomName is not null)
            {
                var abstractRoom2 = game.overWorld.activeWorld.GetAbstractRoom(sourceRoomName);
                if (abstractRoom2.realizedRoom == null)
                {
                    if (game.roomRealizer != null)
                    {
                        game.roomRealizer = new RoomRealizer(game.roomRealizer.followCreature, game.overWorld.activeWorld);
                    }
                    abstractRoom2.RealizeRoom(game.overWorld.activeWorld, game);
                }
                // do nat throw everyone into the same room?
                warpPoint.room = abstractRoom2.realizedRoom;
            }
            game.overWorld.InitiateSpecialWarp_WarpPoint(warpPoint, newWarpData, useNormalWarpLoader);
            if (RainMeadow.isStoryMode(out var storyGameMode))
            {
                storyGameMode.myLastWarp = newWarpData; // SAVE THE WARP POINT!
            }
            OnlineManager.forceWarp = true;
        }

        // Once host finishes animation and stuff, force client to perform warp
        [RPCMethod]
        public static void ForceWatcherWarpOnClient(RPCEvent rpc)
        {
            if (rpc != null && OnlineManager.lobby.owner != rpc.from) return;
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;
            if (game.overWorld.specialWarpCallback is Watcher.WarpPoint warpPoint)
            {
                RainMeadow.Debug($"Forcing client to warp");
                warpPoint.PerformWarp();
            }
            else
            {
                RainMeadow.Error($"warp does not exist? damn we are desynched i think");
            }
        }

        [RPCMethod]
        public static void PrinceSetHighestConversation(RPCEvent rpc, int newValue)
        {
            if (rpc != null && OnlineManager.lobby.owner != rpc.from) return;
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;
            game.GetStorySession.saveState.miscWorldSaveData.highestPrinceConversationSeen = newValue;
        }

        [RPCMethod]
        public static void TriggerGhostHunch(string ghostID)
        {
            var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
            ExtEnumBase.TryParse(typeof(GhostWorldPresence.GhostID), ghostID, false, out var rawEnumBase);
            if (rawEnumBase is not GhostWorldPresence.GhostID ghostNumber) return;
            var ghostsTalkedTo = (game.session as StoryGameSession).saveState.deathPersistentSaveData.ghostsTalkedTo;
            if (!ghostsTalkedTo.ContainsKey(ghostNumber) || ghostsTalkedTo[ghostNumber] < 1)
                ghostsTalkedTo[ghostNumber] = 1;
        }

        [RPCMethod]
        public static void LC_FINAL_TriggerFadeToEnding()
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;
            var script = game.FirstAnyPlayer.Room.realizedRoom.updateList.OfType<MoreSlugcats.MSCRoomSpecificScript.LC_FINAL>().FirstOrDefault();
            if (script is null) { RainMeadow.Error($"trigger not found in room {game.FirstAnyPlayer.Room}"); return; };

            script.TriggerFadeToEnding();
        }

        [RPCMethod]
        public static void RegionGateMeetRequirement()
        {
            if (RainMeadow.isStoryMode(out var storyGameMode) && storyGameMode.readyForGate == StoryGameMode.ReadyForGate.Closed)
            {
                if (OnlineManager.lobby.isOwner)
                {
                    storyGameMode.readyForGate = StoryGameMode.ReadyForGate.MeetRequirement;
                }
                else
                {
                    OnlineManager.lobby.owner.InvokeOnceRPC(RegionGateMeetRequirement);
                }
            }
        }
    }
}

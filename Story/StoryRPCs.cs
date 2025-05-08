using Rewired.Data.Mapping;
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
        public static void GoToWinScreen(bool malnourished, bool fromWarpPoint, string? denPos, string? warpPointTarget)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;

            if (RainMeadow.isStoryMode(out var storyGameMode) && !storyGameMode.hasSheltered)
            {
                storyGameMode.myLastDenPos = denPos;
                storyGameMode.myLastWarp = null;
                if (warpPointTarget != null)
                { //construct data
                    var warpPointData = new Watcher.WarpPoint.WarpPointData(null);
                    warpPointData.FromString(warpPointTarget);
                    storyGameMode.myLastWarp = warpPointData;
                    game.GetStorySession.saveState.warpPointTargetAfterWarpPointSave = warpPointData;
                }
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
                storyGameMode.myLastWarp = null;
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

        // Raises ripple level, usually client also tells
        [RPCMethod]
        public static void RaiseRippleLevel(UnityEngine.Vector2 vector)
        {
            if (RainMeadow.isStoryMode(out var story))
            {

                RainMeadow.Debug($"Raising Ripple Level from: {story.rippleLevel} to {vector.y}");

                if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.session is StoryGameSession storyGameSession && game.manager.upcomingProcess is null)) return;
                story.rippleLevel = vector.y;
                storyGameSession.saveState.deathPersistentSaveData.minimumRippleLevel = vector.x;
                storyGameSession.saveState.deathPersistentSaveData.maximumRippleLevel = vector.y;
                storyGameSession.saveState.deathPersistentSaveData.rippleLevel = vector.y;
            }
        }

        [RPCMethod]
        public static void PlayRaiseRippleLevelAnimation(UnityEngine.Vector2 vector)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.session is StoryGameSession storyGameSession && game.manager.upcomingProcess is null)) return;
            storyGameSession.saveState.deathPersistentSaveData.minimumRippleLevel = vector.x;
            storyGameSession.saveState.deathPersistentSaveData.maximumRippleLevel = vector.y;
            storyGameSession.saveState.deathPersistentSaveData.rippleLevel = vector.y;
            game.cameras[0].hud.karmaMeter.UpdateGraphic();
            game.cameras[0].hud.karmaMeter.forceVisibleCounter = 120; //it's max for a reason(?)
        }

        internal static Watcher.WarpPoint? PerformWarpHelper(string? sourceRoomName, string warpData, bool useNormalWarpLoader, bool hackFixRoom)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return null;
            RainMeadow.Debug($"Warp point? in {sourceRoomName}; data={warpData}, Loader={useNormalWarpLoader}");
            // generate "local" warp point
            Watcher.WarpPoint.WarpPointData newWarpData = new(null);
            newWarpData.FromString(warpData);
            PlacedObject placedObject = new(PlacedObject.Type.WarpPoint, newWarpData);
            Watcher.WarpPoint warpPoint = new(null, placedObject);

            if (hackFixRoom)
            {
                foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
                { //it will move places
                    if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                    if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo1 && opo1.isMine && opo1.apo is AbstractCreature ac)
                    {
                        if (ac.Room.realizedRoom == null)
                        {
                            RainMeadow.Error("something very evil happened, but let's try not to worry about it");
                            ac.Room.RealizeRoom(ac.Room.world, game);
                        }
                        warpPoint.room = ac.Room.realizedRoom;
                    }
                }
            }
            else
            {
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
            }
            if (RainMeadow.isStoryMode(out var storyGameMode))
            {
                storyGameMode.myLastWarp = newWarpData; // SAVE THE WARP POINT!
            }
            game.overWorld.InitiateSpecialWarp_WarpPoint(warpPoint, newWarpData, useNormalWarpLoader);
            // update camera position
            string destRoom = (warpPoint.overrideData != null) ? warpPoint.overrideData.destRoom : warpPoint.Data.destRoom;
            var destCam = (warpPoint.overrideData != null) ? warpPoint.overrideData.destCam : warpPoint.Data.destCam;
            // emulate as if we did actually warp
            game.cameras[0].WarpMoveCameraPrecast(destRoom, destCam);
            if (game.cameras[0].warpPointTimer == null)
            {
                game.cameras[0].warpPointTimer = new Watcher.WarpPoint.WarpPointTimer(warpPoint.activateAnimationTime * 2f, warpPoint);
                game.cameras[0].warpPointTimer.MoveToSecondHalf();
                game.cameras[0].BlankWarpPointHoldFrame();
            }
            RainMeadow.Debug($"switch camera to {destRoom}");
            warpPoint.activated = false;
            game.overWorld.readyForWarp = true;
            return warpPoint;
        }

        // Perform a warp (precast, host needs to "finish" to activate)
        [RPCMethod]
        public static void NormalExecuteWatcherRiftWarp(RPCEvent rpc, string? sourceRoomName, string warpData, bool useNormalWarpLoader)
        {
            if (rpc != null && OnlineManager.lobby.owner != rpc.from) return;
            PerformWarpHelper(sourceRoomName, warpData, useNormalWarpLoader, false);
        }

        // Performs a warp via an echo, can be triggered by anyone
        [RPCMethod]
        public static void EchoExecuteWatcherRiftWarp(RPCEvent rpc, string? sourceRoomName, string warpData)
        {
            Watcher.WarpPoint? warpPoint = PerformWarpHelper(sourceRoomName, warpData, true, true);
            if (warpPoint != null && RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
            {
                RainMeadow.Debug($"warp of kind echo executed; going to win screen warp={warpData}");
                var newWarpData = warpPoint.overrideData ?? warpPoint.Data;
                game.GetStorySession.saveState.warpPointTargetAfterWarpPointSave = newWarpData;
                if (RainMeadow.isStoryMode(out var storyGameMode))
                {
                    storyGameMode.myLastWarp = newWarpData;
                }
                game.Win(false, true);
            }
            else
            {
                RainMeadow.Error($"warp of kind echo FAILED because upcoming process exists");
            }
        }

        [RPCMethod]
        public static void InfectRegionRoomWithSentientRot(RPCEvent rpc, float amount, string roomName)
        {
            if (rpc != null && OnlineManager.lobby.owner != rpc.from) return;
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;
            RainMeadow.Debug($"setting infection of {roomName} to {amount}");
            // fill if does not exist - otherwise simply set :)
            int regionNumber = game.overWorld.activeWorld.region.regionNumber;
            if (!game.GetStorySession.saveState.regionStates[regionNumber].sentientRotProgression.ContainsKey(roomName))
            {
                RegionState.SentientRotState value = new RegionState.SentientRotState();
                game.GetStorySession.saveState.regionStates[regionNumber].sentientRotProgression[roomName] = value;
            }
            game.GetStorySession.saveState.regionStates[regionNumber].sentientRotProgression[roomName].rotIntensity = amount;
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
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;

            ExtEnumBase.TryParse(typeof(GhostWorldPresence.GhostID), ghostID, false, out var rawEnumBase);
            if (rawEnumBase is not GhostWorldPresence.GhostID ghostNumber) return;
            var ghostsTalkedTo = game.GetStorySession.saveState.deathPersistentSaveData.ghostsTalkedTo;
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
        public static void RegionGateOrWarpMeetRequirement()
        {
            if (RainMeadow.isStoryMode(out var storyGameMode) && storyGameMode.readyForTransition == StoryGameMode.ReadyForTransition.Closed)
            {
                if (OnlineManager.lobby.isOwner)
                {
                    storyGameMode.readyForTransition = StoryGameMode.ReadyForTransition.MeetRequirement;
                }
                else
                {
                    OnlineManager.lobby.owner.InvokeOnceRPC(RegionGateOrWarpMeetRequirement);
                }
            }
        }
    }
}

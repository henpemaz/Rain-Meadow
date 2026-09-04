using Rewired.Data.Mapping;
using System;
using System.Linq;

namespace RainMeadow
{
    public static class StoryRPCs
    {

        [RPCMethod]
        public static void ForceSaveNewDenLocation(string shelter, bool saveWorldStates)
        {
            if (!RainMeadow.isStoryMode(out var story)) return;

            // Adopt the den even with no game running. SaveStateHandler reads myLastDenPos on the next load.
            story.myLastDenPos = shelter;
            story.denForcedThisCycle = true;

            var sender = RPCEvent.currentRPCEvent?.from;
            if (OnlineManager.lobby.isOwner)
            {
                story.defaultDenPos = shelter;
                foreach (OnlinePlayer player in OnlineManager.players)
                {
                    if (!player.isMe && player != sender) player.InvokeOnceRPC(ForceSaveNewDenLocation, shelter, saveWorldStates);
                }
            }

            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)) return;

            bool wasApplying = story.applyingRemoteDenLocation;
            story.applyingRemoteDenLocation = true;
            try
            {
                RainWorldGame.ForceSaveNewDenLocation(game, shelter, saveWorldStates);
            }
            finally
            {
                story.applyingRemoteDenLocation = wasApplying;
            }
        }

        [RPCMethod]
        public static void ChangeFood(short amt)
        {
            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
            {
                for (int i = 0; i < game.StoryPlayerCount; i++)
                {
                    if (game.Players[i]?.state is PlayerState state)
                    {
                        var newFood = Math.Max(0, Math.Min(state.foodInStomach * 4 + state.quarterFoodPoints + amt, game.session.characterStats.maxFood * 4));
                        state.foodInStomach = newFood / 4;
                        state.quarterFoodPoints = newFood % 4;
                    }

                    if (game.Players[i].realizedCreature is Player p)
                    {
                        // refreshes malnourished and red's illness state.
                        p.AddFood(0);
                    }
                }
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

            // denForcedThisCycle: an echo already relocated us, and the sender's Win() ran before its own
            // relocation, so denPos here is stale. Keep the forced den.
            if (RainMeadow.isStoryMode(out var storyGameMode) && !storyGameMode.hasSheltered && !storyGameMode.denForcedThisCycle)
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

            if (RainMeadow.isStoryMode(out var storyGameMode) && !storyGameMode.hasSheltered && !storyGameMode.denForcedThisCycle)
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

        [RPCMethod]
        public static void GoToSaintEnding(RPCEvent rpc)
        {
            if (rpc != null && OnlineManager.lobby.owner != rpc.from) return;
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;
            game.manager.statsAfterCredits = true;
            game.manager.desiredCreditsSong = "BLIZZARD";
            game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Credits);
        }

        // Host accepts the first (min,max) report for a given echo id
        public static void DetermineRippleRaise(StoryGameMode story, int spinningTopID, UnityEngine.Vector2 vector)
        {
            if (spinningTopID != -1)
            {
                if (story.hostRippleRaiser.TryGetValue(spinningTopID, out var authoritative))
                {
                    RainMeadow.Debug($"discarding duplicate raise report for echo {spinningTopID} (reported {vector.y}, authoritative {authoritative.y})");
                    return;
                }
                story.hostRippleRaiser[spinningTopID] = vector;
                StoryHelpers.RecordSpinningTopEncounter(story, spinningTopID);
            }

            if (story.maximumRippleLevel >= vector.y) return; // max vs max

            RainMeadow.Debug($"Raising Ripple Level from: {story.maximumRippleLevel} to {vector.y} (echo {spinningTopID})");

            story.minimumRippleLevel = UnityEngine.Mathf.Max(story.minimumRippleLevel, vector.x);
            story.maximumRippleLevel = UnityEngine.Mathf.Max(story.maximumRippleLevel, vector.y);
            story.rippleLevel = UnityEngine.Mathf.Max(story.rippleLevel, vector.y); 

            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.session is StoryGameSession storyGameSession)
            {
                ApplyRippleLevelToSaveState(storyGameSession, vector);
            }
        }

        // Client -> host: Send to ripple raise request for determination
       // Helps prevent race condition
        [RPCMethod]
        public static void RaiseRippleLevelRequest(int spinningTopID, UnityEngine.Vector2 vector)
        {
            if (!RainMeadow.isStoryMode(out var story)) return;
            DetermineRippleRaise(story, spinningTopID, vector);
        }

        [RPCMethod]
        public static void PlayRaiseRippleLevelAnimation(int spinningTopID, UnityEngine.Vector2 vector)
        {
            if (RainMeadow.isStoryMode(out var story) && spinningTopID != -1)
                StoryHelpers.RecordSpinningTopEncounter(story, spinningTopID);

            // apply the data even mid-transition, only the HUD part needs a live game.
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.session is StoryGameSession storyGameSession)) return;
            ApplyRippleLevelToSaveState(storyGameSession, vector);

            if (RainMeadow.isStoryMode(out var story2) && OnlineManager.lobby.isOwner)
            {
                story2.rippleLevel = UnityEngine.Mathf.Max(story2.rippleLevel, vector.y);
                story2.minimumRippleLevel = UnityEngine.Mathf.Max(story2.minimumRippleLevel, vector.x);
                story2.maximumRippleLevel = UnityEngine.Mathf.Max(story2.maximumRippleLevel, vector.y);
            }

            var karmaMeter = game.cameras?.FirstOrDefault()?.hud?.karmaMeter;
            if (karmaMeter == null) return; // no hud yet (loading / mid warp), the data above still landed
            karmaMeter.UpdateGraphic();
            karmaMeter.forceVisibleCounter = 120; //it's max for a reason(?)
        }
        private static void ApplyRippleLevelToSaveState(StoryGameSession storyGameSession, UnityEngine.Vector2 vector)
        {
            var deathPersistentSaveData = storyGameSession.saveState.deathPersistentSaveData;
            deathPersistentSaveData.minimumRippleLevel = UnityEngine.Mathf.Max(deathPersistentSaveData.minimumRippleLevel, vector.x);
            deathPersistentSaveData.maximumRippleLevel = UnityEngine.Mathf.Max(deathPersistentSaveData.maximumRippleLevel, vector.y);
            deathPersistentSaveData.rippleLevel = UnityEngine.Mathf.Max(deathPersistentSaveData.rippleLevel, vector.y);
        }

        [RPCMethod]
        public static void AddSpinningTopEncounter(int spinningTopID)
        {
            if (!RainMeadow.isStoryMode(out var story)) return;
            StoryHelpers.RecordSpinningTopEncounter(story, spinningTopID);
        }

        // Perform a warp (precast, host needs to "finish" to activate)
        [RPCMethod]
        public static void NormalExecuteWatcherRiftWarp(RPCEvent rpc, string? sourceRoomName, string warpData, bool useNormalWarpLoader)
        {
            if (rpc != null && OnlineManager.lobby.owner != rpc.from) return;
            Watcher.WarpPoint? warpPoint = StoryHelpers.PerformWarpHelper(sourceRoomName, warpData, useNormalWarpLoader, false);
        }

        // Performs a warp via an echo, can be triggered by anyone
        [RPCMethod]
        public static void EchoExecuteWatcherRiftWarp(RPCEvent rpc, string? sourceRoomName, string warpData, int spinningTopID, UnityEngine.Vector2 pos)
        {
            // Record the encounter before attempting the warp. warp is allowed to fail, but losing
            // the encounter desyncs which echo everyone is on for the rest of the campaign.
            if (RainMeadow.isStoryMode(out var story)) StoryHelpers.RecordSpinningTopEncounter(story, spinningTopID);

            Watcher.WarpPoint? warpPoint = StoryHelpers.PerformWarpHelper(sourceRoomName, warpData, false, true);
            if (warpPoint != null && RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
            {
                RainMeadow.Debug($"warp of kind echo executed; going to win screen warp={warpData}");
                warpPoint.placedObject.pos = pos;
                StoryHelpers.SaveEchoWarp(game, warpPoint, true, true); //save string incase
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
            if (script is null) { RainMeadow.Error($"trigger not found in room {game.FirstAnyPlayer.Room}"); return; }
            ;

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

        static public bool RPCcloseShelter = false;
        [RPCMethod]
        public static void CloseAllShelters()
        {
            try
            {
                if (RPCEvent.currentRPCEvent?.from is OnlinePlayer op && op == OnlineManager.lobby.owner)
                {
                    if (RainMeadow.isStoryMode(out var storyGameMode))
                    {
                        int i = 0;
                        if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)) return;
                        foreach (Room shelter in game.world.activeRooms.Where(x => x.shelterDoor != null))
                        {
                            RPCcloseShelter = true;
                            shelter.shelterDoor.Close();
                            i++;
                        }
                        RainMeadow.Error($"Closed {i} Shelters.");
                    }
                }
                else
                {
                    RainMeadow.Error("Denied closing shelter because sender is not host");
                }
            }
            finally
            {
                RPCcloseShelter = false;
            }


        }
    }
}

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
        public static void GoToWinScreen(bool malnourished, string? denPos)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;

            if (RainMeadow.isStoryMode(out var storyGameMode) && !storyGameMode.hasSheltered)
            {
                storyGameMode.myLastDenPos = denPos;
            }

            game.Win(malnourished);
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

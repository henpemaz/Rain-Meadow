using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using static RainMeadow.MeadowCustomization;
using MonoMod.Cil;
using System;
using Mono.Cecil.Cil;
using HUD;
using System.Text.RegularExpressions;


namespace RainMeadow
{
    public partial class RainMeadow
    {
        private bool isPlayerReady = false;
        public static bool isStoryMode(out StoryGameMode? gameMode)
        {
            gameMode = null;
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is StoryGameMode) {
                gameMode = OnlineManager.lobby.gameMode as StoryGameMode;
                return true;
            }
            return false;
        }

        private void StoryHooks()
        {
            On.PlayerProgression.GetOrInitiateSaveState += PlayerProgression_GetOrInitiateSaveState;
            On.PlayerProgression.GetProgLinesFromMemory += PlayerProgression_GetProgLinesFromMemory;

            On.Menu.SleepAndDeathScreen.ctor += SleepAndDeathScreen_ctor;
            On.Menu.SleepAndDeathScreen.Update += SleepAndDeathScreen_Update;

            On.Menu.KarmaLadderScreen.Singal += KarmaLadderScreen_Singal;

            On.Player.Update += Player_Update;

            On.RegionGate.Update += RegionGate_Update;
            On.RegionGate.AllPlayersThroughToOtherSide += AllPlayersThroughtoOtherSide;
            On.RegionGate.PlayersStandingStill += PlayersStandingStill;
            On.RegionGate.PlayersInZone += PlayersInZone;

        }

        private string[] PlayerProgression_GetProgLinesFromMemory(On.PlayerProgression.orig_GetProgLinesFromMemory orig, PlayerProgression self)
        {
            var saveStateArr = orig(self);
            if (isStoryMode(out var gameMode) && OnlineManager.lobby.isOwner) {
                if (saveStateArr != null) {
                    for (int i = 0; i < saveStateArr.Length; i++) {
                        string[] progressStringArr = Regex.Split(saveStateArr[i], "<progDivB>");
                        if (progressStringArr.Length == 2 && progressStringArr[0] == "SAVE STATE") { 
                            gameMode.saveStateProgressString = progressStringArr[1];
                        }
                    }
                }
            }
            return saveStateArr;
        }

        private SaveState PlayerProgression_GetOrInitiateSaveState(On.PlayerProgression.orig_GetOrInitiateSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber, RainWorldGame game, ProcessManager.MenuSetup setup, bool saveAsDeathOrQuit)
        {
            var origSaveState = orig(self, saveStateNumber, game, setup, saveAsDeathOrQuit);
            if (isStoryMode(out var gameMode) && !OnlineManager.lobby.isOwner && gameMode.saveStateProgressString != null) {
                self.currentSaveState.LoadGame(gameMode.saveStateProgressString, game); //pretty sure we can just stuff the string here
                self.currentSaveState.denPosition = gameMode?.myDenPos;
                return self.currentSaveState;
            }
            return origSaveState;
        }

        private void KarmaLadderScreen_Singal(On.Menu.KarmaLadderScreen.orig_Singal orig, Menu.KarmaLadderScreen self, Menu.MenuObject sender, string message)
        {
            if (isStoryMode(out var gameMode)) {
                if (message == "CONTINUE") {
                    //place holder hook on the continue button
                }
            }
            orig(self, sender, message);
        }


        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (isStoryMode(out var gameMode)) {
                //fetch the online entity and check if it is mine. 
                //If it is mine run the below code
                //If not, update from the lobby state
                //self.readyForWin = OnlineMAnager.lobby.playerid === fetch if this is ours. 

                if (OnlinePhysicalObject.map.TryGetValue(self.abstractCreature, out var oe)) {
                    if (!oe.isMine) 
                    {
                        self.readyForWin = gameMode.readyForWinPlayers.Contains(oe.owner.inLobbyId);
                        return;
                    }
                }

                if (self.readyForWin
                    && self.touchedNoInputCounter > (ModManager.MMF ? 40 : 20)
                    && RWCustom.Custom.ManhattanDistance(self.abstractCreature.pos.Tile, self.room.shortcuts[0].StartTile) > 3)
                {
                    if (!gameMode.readyForWinPlayers.Contains(OnlineManager.mePlayer.inLobbyId))
                    {
                        if (!(OnlineManager.lobby.owner.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(RPCs.AddReadyToWinPlayer))))
                        {
                            OnlineManager.lobby.owner.InvokeRPC(RPCs.AddReadyToWinPlayer);
                        }
                    }
                }
                else
                {
                    if (gameMode.readyForWinPlayers.Contains(OnlineManager.mePlayer.inLobbyId))
                    {
                        if (!(OnlineManager.lobby.owner.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(RPCs.RemoveReadyToWinPlayer))))
                        {
                            OnlineManager.lobby.owner.InvokeRPC(RPCs.RemoveReadyToWinPlayer);
                        }
                    }
                }
            }
        }

        private void SleepAndDeathScreen_Update(On.Menu.SleepAndDeathScreen.orig_Update orig, Menu.SleepAndDeathScreen self)
        {
            orig(self);

            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is StoryGameMode storyGameMode)
            {
                self.continueButton.buttonBehav.greyedOut = !isPlayerReady;
            }
        }

        private void SleepAndDeathScreen_ctor(On.Menu.SleepAndDeathScreen.orig_ctor orig, Menu.SleepAndDeathScreen self, ProcessManager manager, ProcessManager.ProcessID ID)
        {

            RainMeadow.Debug("In SleepAndDeath Screen");
            orig(self, manager, ID);

            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is StoryGameMode storyGameMode)
            { 
                isPlayerReady = false;

                //Create the READY button
                var buttonPosX = self.ContinueAndExitButtonsXPos - 180f - self.manager.rainWorld.options.SafeScreenOffset.x;
                var buttonPosY = Mathf.Max(self.manager.rainWorld.options.SafeScreenOffset.y, 53f);
                var readyButton = new SimplerButton(self, self.pages[0], "READY",
                    new Vector2(buttonPosX, buttonPosY),
                    new Vector2(110f, 30f));

                readyButton.OnClick += ReadyButton_OnClick;

                self.pages[0].subObjects.Add(readyButton);
                readyButton.black = 0;
                self.pages[0].lastSelectedObject = readyButton;

            }
        }

        private void ReadyButton_OnClick(SimplerButton obj)
        {
            if (isStoryMode(out var gameMode) && gameMode.saveStateProgressString != null){
                isPlayerReady = true;
            }
        }

        private void RegionGate_Update(On.RegionGate.orig_Update orig, RegionGate self, bool eu)
        {
            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not StoryGameMode)
            {
                orig(self, eu);
                return;
            }

            foreach (var playerAvatar in OnlineManager.lobby.playerAvatars)
            {
                if (playerAvatar.Value.realizedCreature.room != self.room) // Everyone must be present
                {
                    return;
                }
            }

            orig(self, eu);
        }
        private bool AllPlayersThroughtoOtherSide(On.RegionGate.orig_AllPlayersThroughToOtherSide orig, RegionGate self)
        {

            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not StoryGameMode)
            {
                orig(self);
                return true;
            }

            foreach (var playerAvatar in OnlineManager.lobby.playerAvatars)
            {
                if (playerAvatar.Value.apo.pos.room == self.room.abstractRoom.index && (playerAvatar.Value.apo.pos.x < self.room.TileWidth / 2 + 3) && (playerAvatar.Value.apo.pos.x > self.room.TileWidth / 2 - 4))
                {
                    return false;
                }
            }
            return true;
        }


        private int PlayersInZone(On.RegionGate.orig_PlayersInZone orig, RegionGate self)
        {
            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not StoryGameMode)
            {
                return orig(self);
            }
            int regionGateZone = -1;
            foreach (var playerAvatar in OnlineManager.lobby.playerAvatars)
            {
                var abstractCreature = playerAvatar.Value.realizedCreature.abstractCreature;
                if (abstractCreature.Room == self.room.abstractRoom)
                {
                    int zone = self.DetectZone(abstractCreature);
                    if (zone != regionGateZone && regionGateZone != -1)
                    {
                        return -1;
                    }
                    regionGateZone = zone;
                }
            }

            return regionGateZone;
            // TODO: HUD stuff

        }

        private bool PlayersStandingStill(On.RegionGate.orig_PlayersStandingStill orig, RegionGate self)
        {

            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not StoryGameMode)
            {
                return orig(self);
            }

            foreach (var kv in OnlineManager.lobby.playerAvatars)
            {
                if (kv.Value.realizedCreature.abstractCreature.Room != self.room.abstractRoom
                    || (kv.Value.realizedCreature as Player).touchedNoInputCounter < (ModManager.MMF ? 40 : 20))
                {
                    RainMeadow.Debug("Player(s) missing in region gate location");
                    return false;
                }
            }

            return true;
        }

    }

}







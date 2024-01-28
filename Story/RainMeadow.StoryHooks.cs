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

            On.RegionGate.AllPlayersThroughToOtherSide += RegionGate_AllPlayersThroughToOtherSide; ;
            On.RegionGate.PlayersStandingStill += PlayersStandingStill;
            On.RegionGate.PlayersInZone += RegionGate_PlayersInZone; ;

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

        private bool RegionGate_AllPlayersThroughToOtherSide(On.RegionGate.orig_AllPlayersThroughToOtherSide orig, RegionGate self)
        {

            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is StoryGameMode) {
                foreach (var playerAvatar in OnlineManager.lobby.playerAvatars)
                {
                    var ac = (playerAvatar.Value.apo as AbstractCreature);
                    if (ac.pos.room == self.room.abstractRoom.index && (!self.letThroughDir || ac.pos.x < self.room.TileWidth / 2 + 3) && (self.letThroughDir || ac.pos.x > self.room.TileWidth / 2 - 4))
                    {
                        return false;
                    }
                }
                return true;
            }
            return orig(self);

        }


        private int RegionGate_PlayersInZone(On.RegionGate.orig_PlayersInZone orig, RegionGate self)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is StoryGameMode)
            {
                int regionGateZone = -1;
                foreach (var playerAvatar in OnlineManager.lobby.playerAvatars)
                {
                    var abstractCreature = playerAvatar.Value.apo as AbstractCreature;
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
            }
            return orig(self);
        }

        private bool PlayersStandingStill(On.RegionGate.orig_PlayersStandingStill orig, RegionGate self)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is StoryGameMode) {
                foreach (var kv in OnlineManager.lobby.playerAvatars)
                {
                    if (kv.Value.realizedCreature.abstractCreature.Room != self.room.abstractRoom
                        || (kv.Value.realizedCreature as Player).touchedNoInputCounter < (ModManager.MMF ? 40 : 20))
                    {
                        return false;
                    }
                }

                return true;
            }
            return orig(self);
        }

    }

}







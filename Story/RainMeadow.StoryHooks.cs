using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using static RainMeadow.MeadowCustomization;
using MonoMod.Cil;
using System;
using Mono.Cecil.Cil;
using HUD;
using static Sony.PS4.SaveData.Dialogs;
using UnityEngine.Android;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        private bool isPlayerReady = false;
        private bool isStoryMode()
        {
            return OnlineManager.lobby != null && OnlineManager.lobby.gameMode is StoryGameMode;
        }

        private void StoryHooks()
        {
            On.Menu.SleepAndDeathScreen.ctor += SleepAndDeathScreen_ctor;
            On.Menu.SleepAndDeathScreen.Update += SleepAndDeathScreen_Update;

            On.Menu.KarmaLadderScreen.Singal += KarmaLadderScreen_Singal;

            On.SaveState.SessionEnded += SaveState_SessionEnded;
            On.Player.Update += Player_Update;
            On.RegionGate.Update += RegionGateTransition;
            On.RegionGate.AllPlayersThroughToOtherSide += AllPlayersThroughtoOtherSide;
            On.RegionGate.PlayersStandingStill += PlayersStandingStill;
            On.RegionGate.PlayersInZone += PlayersInZone;

        }

        private void KarmaLadderScreen_Singal(On.Menu.KarmaLadderScreen.orig_Singal orig, Menu.KarmaLadderScreen self, Menu.MenuObject sender, string message)
        {
            if (isStoryMode())
            {
                if (message == "CONTINUE" && !OnlineManager.lobby.isOwner)
                {
                    if (!OnlineManager.lobby.isReadyForNextCycle)
                    {
                        return;
                    }
                }
            }
            orig(self, sender, message);
        }

        private void SaveState_SessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
        {
            if (isStoryMode() && OnlineManager.lobby.isOwner)
            {
                OnlineManager.lobby.isReadyForNextCycle = false;
            }
            orig(self, game, survived, newMalnourished);

        }


        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (isStoryMode())
            {
                //fetch the online entity and check if it is mine. 
                //If it is mine run the below code
                //If not, update from the lobby state
                //self.readyForWin = OnlineMAnager.lobby.playerid === fetch if this is ours. 

                if (OnlinePhysicalObject.map.TryGetValue(self.abstractCreature, out var oe))
                {
                    if (!oe.isMine)
                    {
                        self.readyForWin = OnlineManager.lobby.readyForWinPlayers.Contains(oe.owner.inLobbyId);
                        return;
                    }
                }

                if (self.readyForWin
                    && self.touchedNoInputCounter > (ModManager.MMF ? 40 : 20)
                    && RWCustom.Custom.ManhattanDistance(self.abstractCreature.pos.Tile, self.room.shortcuts[0].StartTile) > 3)
                {
                    if (!OnlineManager.lobby.readyForWinPlayers.Contains(OnlineManager.mePlayer.inLobbyId))
                    {
                        if (!(OnlineManager.lobby.owner.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(RPCs.AddReadyToWinPlayer))))
                        {
                            OnlineManager.lobby.owner.InvokeRPC(RPCs.AddReadyToWinPlayer);
                        }
                    }
                }
                else
                {
                    if (OnlineManager.lobby.readyForWinPlayers.Contains(OnlineManager.mePlayer.inLobbyId))
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

            //if OnlineManager.lobby.onlineStorySaveState isReady
            self.continueButton.buttonBehav.greyedOut = !isPlayerReady;
        }

        private void SleepAndDeathScreen_ctor(On.Menu.SleepAndDeathScreen.orig_ctor orig, Menu.SleepAndDeathScreen self, ProcessManager manager, ProcessManager.ProcessID ID)
        {
            isPlayerReady = false;

            RainMeadow.Debug("In SleepAndDeath Screen");
            orig(self, manager, ID);
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is StoryGameMode storyGameMode)
            {
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
            //OnlineManager.mePlayer
            isPlayerReady = true;
        }

        private void RegionGateTransition(On.RegionGate.orig_Update orig, RegionGate self, bool eu)
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
            // Do not call original method. 
        }


        private int PlayersInZone(On.RegionGate.orig_PlayersInZone orig, RegionGate self)
        {
            int num = -1;

            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not StoryGameMode)
            {
                orig(self);
                return num;

            }

            foreach (var playerAvatar in OnlineManager.lobby.playerAvatars)
            {

                int num2 = self.DetectZone(playerAvatar.Value.realizedCreature.abstractCreature);

                if (playerAvatar.Value.realizedCreature.room != self.room)
                {

                    break;
                }

                if (num2 == num || num == -1)
                {
                    num = num2;
                    continue;
                }
                num = -1;
                break;

            }


            if (num < 0 && self.room.BeingViewed)
            {
                foreach (var playerAvatar in OnlineManager.lobby.playerAvatars)
                {

                    if (self.DetectZone(playerAvatar.Value.realizedCreature.abstractCreature) == -1)
                    {
                        try
                        {
                            // TODO: HUD
                            print("REGIONGATE - PlayersInZone Not all players in zone");
                        }
                        catch (Exception ex)
                        {
                            Error(ex);
                        }
                    }

                }
                return num;
            }
            print($"PLAYERS IN ZONE: {num}");
            return num;
            // Do not call the original method
        }

        private bool PlayersStandingStill(On.RegionGate.orig_PlayersStandingStill orig, RegionGate self)
        {
            bool notifyKeepDoorShut = false;

            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not StoryGameMode)
            {
                orig(self);
                return notifyKeepDoorShut;
            }


            List<AbstractCreature> playersInRegionGateRoomList = new List<AbstractCreature>();
            List<AbstractCreature> playersInGameRoomList = new List<AbstractCreature>();

            foreach (var playerAvatar in OnlineManager.lobby.playerAvatars)
            {

                if (playerAvatar.Value.realizedCreature.abstractCreature == null)
                {
                    continue;
                }


                if (playerAvatar.Value.realizedCreature.room != self.room)
                {
                    playersInGameRoomList.Add(playerAvatar.Value.realizedCreature.abstractCreature); // List of everyone not present

                }
                playersInRegionGateRoomList.Add(playerAvatar.Value.realizedCreature.abstractCreature); // List of everyone present in RegionGate

                if (!playersInGameRoomList.Contains(playerAvatar.Value.realizedCreature.abstractCreature))
                {
                  print("REGIONGATE - PlayersStandingStill: Not everyone is here");

                    try
                    {
                        // TODO: ILHook into  HUD of players not present for blink red
                        // TODO: Custom HUD for multiplayer showing 4 slug cat sprites?
                    }
                    catch (Exception ex)
                    {
                        Error(ex);
                    }
                    notifyKeepDoorShut = true;
                }

                // TODO: Access player inputs to see if standing still
/*                if ()
                {

                }*/

            }
            if (notifyKeepDoorShut)
            {
                return false;
            }
            return true;
            // Do not call the original method
        }

    }

}







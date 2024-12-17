using Menu;
using System;
using UnityEngine;

namespace RainMeadow
{
    internal class MeadowPauseMenu : PauseMenu
    {
        private readonly MeadowGameMode mgm;
        private Creature avatarCreature;
        private int targetHub;
        private int suco4;

        public MeadowPauseMenu(ProcessManager manager, RainWorldGame game, MeadowGameMode mgm) : base(manager, game)
        {
            RainMeadow.DebugMe();
            this.continueButton.RemoveSprites();
            this.pages[0].RemoveSubObject(this.continueButton);
            this.continueButton = null;
            this.exitButton.RemoveSprites();
            this.pages[0].RemoveSubObject(this.exitButton);
            this.exitButton = null;

            this.mgm = mgm;
            this.avatarCreature = mgm.avatars[0].realizedCreature;

            this.pauseWarningActive = false;
            game.cameras[0].hud.textPrompt.pausedWarningText = false;
            game.cameras[0].hud.textPrompt.pausedMode = false;

            var world = game.world;
            var room = game.cameras[0].room;
            bool isShelter = room.abstractRoom.shelter;
            targetHub = -1;
            if (MeadowMusic.activeZonesDict != null)
            {
                var hubZone = MeadowMusic.activeZonesDict.MinBy(v => (world.RoomToWorldPos(Vector2.zero, room.abstractRoom.index) - world.RoomToWorldPos(Vector2.zero, v.Key)).magnitude);
                RainMeadow.Debug($"hubzone: {hubZone.Value.room} : {hubZone.Key}");
                targetHub = hubZone.Key;
            }
            suco4 = RainWorld.roomNameToIndex.TryGetValue("SU_C04", out var val) ? val : -1;
            RainMeadow.Debug("suco4 is " + suco4);

            int buttonCount = 0;
            SimplerButton AddButton(string localizedText, string localizedDescription, Action<SimplerButton> onClick, bool active = true, string emotesprite = null)
            {
                Vector2 pos = new Vector2(
                    this.ContinueAndExitButtonsXPos - 250.2f - this.moveLeft - this.manager.rainWorld.options.SafeScreenOffset.x,
                    Mathf.Max(manager.rainWorld.options.SafeScreenOffset.y, 15f) + 540.2f
                );
                pos.y -= (buttonCount) * 40f; 
                SimplerButton button = new FloatyButton(this, this.pages[0], localizedText, pos, new Vector2(110f, 30f), localizedDescription);
                button.OnClick += onClick;
                button.nextSelectable[0] = button;
                button.nextSelectable[2] = button;
                button.buttonBehav.greyedOut = !active;

                if (emotesprite != null)
                {
                    button.subObjects.Add(new MenuSprite(this, button, new FSprite(emotesprite) { scale = 30f / 240f }, new Vector2(-30f, 12f)));
                }

                this.pages[0].subObjects.Add(button);
                buttonCount += 1;
                return button;
            }
            this.pages[this.currentPage].lastSelectedObject = this.continueButton = 
                AddButton(this.Translate("CONTINUE"), this.Translate("Close this menu"), this.Continue);
            AddButton(this.Translate("TO HUB"), this.Translate("Teleport to the closest hub"), this.ToHub, targetHub != -1, emotesprite: MeadowProgression.Emote.symbolTree.value.ToLowerInvariant());
            AddButton(this.Translate("TO OUTSKIRTS"), this.Translate("Teleport to outskirts"), this.ToOutskirts, suco4 != -1, emotesprite: MeadowProgression.Emote.symbolSurvivor.value.ToLowerInvariant());
            AddButton(this.Translate("PASSAGE"), this.Translate("Passage to another shelter"), this.Passage, isShelter, emotesprite: MeadowProgression.Emote.symbolShelter.value.ToLowerInvariant());
            AddButton(this.Translate("UNSTUCK"), this.Translate("Teleport to a nearby pipe"), this.Unstuck);
            AddButton(this.Translate("LOBBY"), this.Translate("Go back to the charater selection screen"), this.ToLobby);
            this.exitButton = AddButton(this.Translate("QUIT"), this.Translate("Exit back to the main menu"), this.ToMainMenu);

            // Removes the tutorial sprites 
            this.controlMap.RemoveSprites();
            this.pages[0].subObjects.Remove(this.controlMap);
            //this.blackSprite.scaleX = manager.rainWorld.options.ScreenSize.x / 4f;
        }

        private void Continue(SimplerButton button)
        {
            RainMeadow.DebugMe();
            this.wantToContinue = true;
            base.PlaySound(SoundID.HUD_Unpause_Game);
        }

        private void ToMainMenu(SimplerButton button)
        {
            RainMeadow.DebugMe();
            base.PlaySound(SoundID.HUD_Exit_Game);
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
        }

        private void ToLobby(SimplerButton button)
        {
            RainMeadow.DebugMe();
            base.PlaySound(SoundID.HUD_Exit_Game);
            OnlineManager.lobby.owner.InvokeRPC(MeadowMusic.AskNowLeave);
            manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.MeadowMenu);
        }

        private void Passage(SimplerButton button)
        {
            RainMeadow.DebugMe();
            base.PlaySound(SoundID.MENU_Passage_Button);
            OnlineManager.lobby.owner.InvokeRPC(MeadowMusic.AskNowLeave);
            this.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.FastTravelScreen);
        }

        private void ToOutskirts(SimplerButton button)
        {
            RainMeadow.DebugMe();
            MeadowProgression.progressionData.currentCharacterProgress.saveLocation = new WorldCoordinate(suco4, -1, -1, 0);
            manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.RegionSelect;
            manager.menuSetup.regionSelectRoom = MeadowProgression.progressionData.currentCharacterProgress.saveLocation.ResolveRoomName();
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }

        private void ToHub(SimplerButton button)
        {
            RainMeadow.DebugMe();
            MeadowProgression.progressionData.currentCharacterProgress.saveLocation = new WorldCoordinate(targetHub, -1, -1, 0);
            manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.RegionSelect;
            manager.menuSetup.regionSelectRoom = MeadowProgression.progressionData.currentCharacterProgress.saveLocation.ResolveRoomName();
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }

        private void Unstuck(SimplerButton button)
        {
            RainMeadow.DebugMe();
            var creature = mgm.avatars[0].realizedCreature;
            if (creature.room != null)
            {
                RainMeadow.Debug("found in room");
                var room = creature.room;
                creature.RemoveFromRoom();
                room.CleanOutObjectNotInThisRoom(creature); // we need it this frame
                var node = creature.coord.abstractNode;
                if (node > room.abstractRoom.exits) node = UnityEngine.Random.Range(0, room.abstractRoom.exits);
                creature.SpitOutOfShortCut(room.ShortcutLeadingToNode(node).startCoord.Tile, room, true);
            }
        }

        public override string UpdateInfoText()
        {
            if (this.selectedObject is IHaveADescription ihad)
            {
                return ihad.Description;
            }
            return base.UpdateInfoText();
        }
    }
}
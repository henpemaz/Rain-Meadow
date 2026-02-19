using Menu;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    internal class MeadowPauseMenu : PauseMenu, Menu.CheckBox.IOwnCheckBox
    {
        private readonly MeadowGameMode mgm;
        private Creature avatarCreature;
        private int targetHub;
        private int suco4;

        private HorizontalSlider2 hubVolumeSlider;
        
        public static Slider.SliderID HubVolume = new Slider.SliderID("Thingy", true);
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
            if (MeadowMusic.regionVibeZonesDict != null)
            {
                var hubZone = MeadowMusic.regionVibeZonesDict.MinBy(v => (world.RoomToWorldPos(Vector2.zero, room.abstractRoom.index) - world.RoomToWorldPos(Vector2.zero, v.Key)).magnitude);
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
                SimplerButton button = new SimplerButton(this, this.pages[0], localizedText, pos, new Vector2(110f, 30f), localizedDescription);
                button.OnClick += onClick;
                button.nextSelectable[0] = button;
                button.nextSelectable[2] = button;
                button.buttonBehav.greyedOut = !active;

                if (emotesprite != null)
                {
                    button.subObjects.Add(new MenuSprite(this, button, new FSprite(emotesprite) { scale = 30f / 240f }, new Vector2(-30f, 12f)));
                }
                button.subObjects.Add(new Floater(this, button, new Vector2(750f, 0f), new Vector2(3f, 2.75f), new Vector2(3f, 1f)));
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

            Vector2 pos = new Vector2(
                    this.ContinueAndExitButtonsXPos - 250.2f - this.moveLeft - this.manager.rainWorld.options.SafeScreenOffset.x,
                    Mathf.Max(manager.rainWorld.options.SafeScreenOffset.y, 15f) + 540.2f
                );
            
            pos.y += 40f;
            var text = new MusicTitleDisplay(this, this.pages[0], "", pos, new Vector2(0f, 30f)); //110
            text.subObjects.Add(new Floater(this, text, new Vector2(750f, 0f), new Vector2(3f, 2.75f), new Vector2(3f, 1f)));
            pages[0].subObjects.Add(text);
            pos.y -= 40f;

            pos.y -= (buttonCount) * 40f;
            hubVolumeSlider = new HorizontalSlider2(this, pages[0], this.Translate("Hub zone volume"), pos, new Vector2(60f, 10f), HubVolume, false);
            hubVolumeSlider.subObjects.Add(new Floater(this, hubVolumeSlider, new Vector2(750f, 0f), new Vector2(3f, 2.75f), new Vector2(0f, 1f)));
            pages[0].subObjects.Add(hubVolumeSlider);

            pos.y -= 40f;
            var namesCb = new CheckBox(this, pages[0], this, pos, 70f, this.Translate("Display names"), "NAMES", true);
            namesCb.subObjects.Add(new Floater(this, namesCb, new Vector2(750f, 0f), new Vector2(3f, 2.75f), new Vector2(3f, 1f)));
            pages[0].subObjects.Add(namesCb);
            pos.y -= 40f;
            var colCb = new CheckBox(this, pages[0], this, pos, 70f, this.Translate("Collision"), "COLLISION", true);
            colCb.subObjects.Add(new Floater(this, colCb, new Vector2(750f, 0f), new Vector2(3f, 2.75f), new Vector2(3f, 1f)));
            pages[0].subObjects.Add(colCb);

            pos.y -= 40f;
            var timelineSprite = new MenuSprite(this, pages[0], new FSprite(MeadowProgression.Emote.symbolTime.value.ToLowerInvariant()) { scale = 30f / 240f }, new Vector2(pos.x + 10f, pos.y + 10f));
            pages[0].subObjects.Add(timelineSprite);

            string timelineText = Translate($"Timeline: {OnlineManager.lobby.meadowTimeline}");
            var timelineLabel = new MenuLabel(this, pages[0], timelineText, new Vector2(pos.x + 20f, pos.y), new Vector2(50f, 20f), bigText: false);
            timelineLabel.label.alignment = FLabelAlignment.Left;
            timelineLabel.label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
            pages[0].subObjects.Add(timelineLabel);

            if (SpecialEvents.IsSpecialEvent)
            {
                pos.y -= 40f;
                SpecialEvents.LoadElement("meadowcoin");
                var meadowCoinSprite = new MenuSprite(this, pages[0], new FSprite("meadowcoin") { scale = 0.10f, color=  Color.yellow }, new Vector2(pos.x + 10f, pos.y + 10f));
                pages[0].subObjects.Add(meadowCoinSprite);

                string meadowCoinValue = Translate($"¤{RainMeadow.rainMeadowOptions.MeadowCoins.Value}");
                var meadowCoinlabel = new MenuLabel(this, pages[0], meadowCoinValue, new Vector2(pos.x + 20f, pos.y), new Vector2(50f, 20f), bigText: false);
                meadowCoinlabel.label.alignment = FLabelAlignment.Left;
                meadowCoinlabel.label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
                pages[0].subObjects.Add(meadowCoinlabel);
                pages[0].subObjects.Add(new Floater(this, meadowCoinSprite, new Vector2(750f, 0f), new Vector2(3f, 2.75f), new Vector2(3f, 1f)));
                pages[0].subObjects.Add(new Floater(this, meadowCoinlabel, new Vector2(750f, 0f), new Vector2(3f, 2.75f), new Vector2(3f, 1f)));

            }

            pages[0].subObjects.Add(new Floater(this, timelineSprite, new Vector2(750f, 0f), new Vector2(3f, 2.75f), new Vector2(3f, 1f)));
            pages[0].subObjects.Add(new Floater(this, timelineLabel, new Vector2(750f, 0f), new Vector2(3f, 2.75f), new Vector2(3f, 1f)));
            pages[0].subObjects.Add(new Floater(this, timelineLabel, new Vector2(750f, 0f), new Vector2(3f, 2.75f), new Vector2(3f, 1f)));

            // Removes the tutorial sprites 
            this.controlMap.RemoveSprites();
            this.pages[0].subObjects.Remove(this.controlMap);
            //this.blackSprite.scaleX = manager.rainWorld.options.ScreenSize.x / 4f;

            CreateElementBinds();
        }

        public override void SliderSetValue(Slider slider, float f)
        {
            if (slider.ID == HubVolume)
            {
                MeadowMusic.defaultPlopVolume = f * 0.575f;
            }
        }
        public override float ValueOfSlider(Slider slider)
        {
            return MeadowMusic.defaultPlopVolume/0.575f;
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
                ShortcutData closestShortcut = room.shortcuts.Where(thing => thing.shortCutType == ShortcutData.Type.RoomExit).MinBy(cut => (IntVector2.ToVector2(cut.startCoord.Tile - creature.coord.Tile)).magnitude);
                creature.SpitOutOfShortCut(closestShortcut.startCoord.Tile, room, true);
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

        // IOwnCheckBox
        public bool GetChecked(CheckBox box)
        {
            if (box.IDString == "NAMES") return MeadowProgression.progressionData.displayNames;
            if (box.IDString == "COLLISION") return MeadowProgression.progressionData.collisionOn;
            return false;
        }

        public void SetChecked(CheckBox box, bool c)
        {
            if (box.IDString == "NAMES")
            {
                MeadowProgression.progressionData.displayNames = c;
            }
            if (box.IDString == "COLLISION")
            {
                MeadowProgression.progressionData.collisionOn = c;
                avatarCreature.ChangeCollisionLayer(MeadowProgression.progressionData.collisionOn ? 1 : 0);
            }
        }
        public void CreateElementBinds()
        {
            //Group up elements
            List<MenuObject> PauseElements = pages[0].subObjects.Where(MenuObject => MenuObject.GetType() == typeof(SimplerButton)).ToList();
            PauseElements.Add(hubVolumeSlider.subObjects[0]); //oh you special little snowflake
            PauseElements.AddRange(pages[0].subObjects.Where(MenuObject => MenuObject.GetType() == typeof(CheckBox)).ToList());
            //Apply new binds
            Extensions.TryMassDeleteBind(PauseElements, left: true, right: true); //Fix left/right causing nonsense by just removing left/right.
            Extensions.TrySequentialMutualBind(this, PauseElements, bottomTop: true, loopLastIndex: true, reverseList: true); //Fix the up/down binds not linking properly.
            Extensions.TryBind(continueButton, exitButton, top: true); //When pressing up at Continue, move to Quit instead of the collision checkbox, so Quit is easier to get to.
        }
    }
}
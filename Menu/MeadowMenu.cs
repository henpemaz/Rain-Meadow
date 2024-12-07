using Menu;
using Menu.Remix;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowMenu : SmartMenu, SelectOneButton.SelectOneButtonOwner
    {
        public MeadowGameMode mgm;
        RainEffect rainEffect;

        EventfulHoldButton startButton;
        EventfulBigArrowButton prevButton;
        EventfulBigArrowButton nextButton;

        SlugcatSelectMenu ssm;
        List<SlugcatSelectMenu.SlugcatPage> characterPages;

        MenuLabel skinsLabel;
        EventfulSelectOneButton[] skinButtons;

        List<MeadowProgression.Character> playableCharacters;
        Dictionary<MeadowProgression.Character, List<MeadowProgression.Skin>> characterSkins;

        int skinIndex;
        float tintAmount;
        FSprite tintPreview;
        MeadowAvatarData personaSettings;
        OpTinyColorPicker colorpicker;
        TokenMenuDisplayer skinProgressIcon;
        private SubtleSlider2 tintSlider;
        private MenuLabel tintLabel;

        public override MenuScene.SceneID GetScene => null;
        public MeadowMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.MeadowMenu)
        {
            RainMeadow.DebugMe();
            backTarget = RainMeadow.Ext_ProcessID.LobbySelectMenu;

            this.mgm = OnlineManager.lobby.gameMode as MeadowGameMode;

            this.rainEffect = new RainEffect(this, this.pages[0]);
            this.pages[0].subObjects.Add(this.rainEffect);
            this.rainEffect.rainFade = 0.3f;
            this.characterPages = new List<SlugcatSelectMenu.SlugcatPage>();

            ssm = (SlugcatSelectMenu)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SlugcatSelectMenu));
            ssm.container = container;
            ssm.slugcatPages = characterPages;
            ssm.ID = ProcessManager.ProcessID.MultiplayerMenu;
            ssm.cursorContainer = cursorContainer;
            ssm.manager = manager;
            ssm.pages = pages;

            playableCharacters = MeadowProgression.AllAvailableCharacters();
            characterSkins = new();
            for (int j = 0; j < this.playableCharacters.Count; j++)
            {
                this.characterPages.Add(new MeadowCharacterSelectPage(this, ssm, 1 + j, this.playableCharacters[j]));
                this.pages.Add(this.characterPages[j]);

                characterSkins[playableCharacters[j]] = MeadowProgression.AllAvailableSkins(this.playableCharacters[j]);
            }
            if (MeadowProgression.NextUnlockableCharacter() is MeadowProgression.Character character)
            {
                this.characterPages.Add(new MeadowCharacterSelectPage(this, ssm, characterPages.Count + 1, character, locked: true));
                this.pages.Add(this.characterPages[characterPages.Count - 1]);
            }

            this.startButton = new EventfulHoldButton(this, this.pages[0], base.Translate("ENTER"), new Vector2(683f, 85f), 40f);
            this.startButton.OnClick += (_) => { StartGame(); };

            this.pages[0].subObjects.Add(this.startButton);
            this.prevButton = new EventfulBigArrowButton(this, this.pages[0], new Vector2(345f, 50f), -1);
            this.prevButton.OnClick += (_) =>
            {
                ssm.quedSideInput = Math.Max(-3, ssm.quedSideInput - 1);
                base.PlaySound(SoundID.MENU_Next_Slugcat);
            };
            this.pages[0].subObjects.Add(this.prevButton);
            this.nextButton = new EventfulBigArrowButton(this, this.pages[0], new Vector2(985f, 50f), 1);
            this.nextButton.OnClick += (_) =>
            {
                ssm.quedSideInput = Math.Min(3, ssm.quedSideInput + 1);
                base.PlaySound(SoundID.MENU_Next_Slugcat);
            };
            this.pages[0].subObjects.Add(this.nextButton);
            this.mySoundLoopID = SoundID.MENU_Main_Menu_LOOP;

            this.pages[0].subObjects.Add(this.skinsLabel = new MenuLabel(this, mainPage, this.Translate("SKINS"), new Vector2(194, 553), new(110, 30), true));

            colorpicker = new OpTinyColorPicker(this, new Vector2(800, 60), Color.white);
            var wrapper = new UIelementWrapper(this.tabWrapper, colorpicker);

            colorpicker.OnValueChangedEvent += Colorpicker_OnValueChangedEvent;

            tintLabel = new MenuLabel(this, mainPage, this.Translate("Tint color"), new Vector2(845, 60), new(0, 30), false);
            tintLabel.label.alignment = FLabelAlignment.Left;
            this.pages[0].subObjects.Add(tintLabel);

            tintSlider = new SubtleSlider2(this, mainPage, "Tint amount", new Vector2(800, 30), new Vector2(100, 30));
            this.pages[0].subObjects.Add(tintSlider);

            colorpicker.wrapper.nextSelectable[3] = tintSlider;
            tintSlider.nextSelectable[1] = colorpicker.wrapper;

            tintPreview = new FSprite("Futile_White") { x = 940, y = 60, scale = 1.4142f };
            this.container.AddChild(tintPreview);

            this.skinProgressIcon = new TokenMenuDisplayer(this, this.pages[0], new Vector2(-1000f, -1000f), MeadowProgression.TokenBlueColor, $"{0}/{MeadowProgression.skinProgressTreshold}");
            this.pages[0].subObjects.Add(skinProgressIcon);

            var cheatButton = new SimplerButton(this, mainPage, "CHEAT", new Vector2(200f, 90f), new Vector2(110f, 30f));
            cheatButton.OnClick += (_) =>
            {
                if (manager.upcomingProcess != null) return;
                for (int i = 0; i < 80; i++)
                {
                    MeadowProgression.CharacterProgress();
                }
                foreach (var character in MeadowProgression.allCharacters)
                {
                    MeadowProgression.progressionData.currentlySelectedCharacter = character;
                    for (int i = 0; i < 80; i++)
                    {
                        MeadowProgression.EmoteProgress();
                        MeadowProgression.SkinProgress();
                    }
                }
                MeadowProgression.progressionData.currentlySelectedCharacter = MeadowProgression.allCharacters[ssm.slugcatPageIndex];
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.MeadowMenu);
            };
            mainPage.subObjects.Add(cheatButton);

            var resetButton = new SimplerButton(this, mainPage, "RESET", new Vector2(200f, 140f), new Vector2(110f, 30f));
            resetButton.OnClick += (_) =>
            {
                if (manager.upcomingProcess != null) return;
                MeadowProgression.progressionData = null;
                MeadowProgression.LoadDefaultProgression();
                playableCharacters = MeadowProgression.AllAvailableCharacters();
                ssm.slugcatPageIndex = 0;
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.MeadowMenu);
            };
            mainPage.subObjects.Add(resetButton);

            // read page state from progression
            ssm.slugcatPageIndex = playableCharacters.IndexOf(MeadowProgression.progressionData.currentlySelectedCharacter);
            if (ssm.slugcatPageIndex == -1)
            {
                ssm.slugcatPageIndex = 0;
                MeadowProgression.progressionData.currentlySelectedCharacter = playableCharacters[0];
            }
            MeadowProgression.progressionData.characterProgress[playableCharacters[ssm.slugcatPageIndex]].everSeenInMenu = true;

            this.personaSettings = mgm.avatarData;
            ReadCharacterSettings();

            UpdateCharacterUI();

            if (manager.musicPlayer != null)
            {
                manager.musicPlayer.MenuRequestsSong("me", 1, 0);
            }
        }

        private void UpdateCharacterUI()
        {
            if (skinButtons != null)
            {
                var oldSkinButtons = skinButtons;
                for (int i = 0; i < oldSkinButtons.Length; i++)
                {
                    var btn = oldSkinButtons[i];
                    btn.RemoveSprites();
                    mainPage.RemoveSubObject(btn);
                }
            }

            var skins = ssm.slugcatPageIndex < playableCharacters.Count ? characterSkins[playableCharacters[ssm.slugcatPageIndex]] : new List<MeadowProgression.Skin>();
            skinButtons = new EventfulSelectOneButton[skins.Count];
            for (int i = 0; i < skins.Count; i++)
            {
                var skin = skins[i];
                var btn = new EventfulSelectOneButton(this, mainPage, MeadowProgression.skinData[skin].displayName, "skinButtons", new Vector2(194, 515) - i * new Vector2(0, 38), new(110, 30), skinButtons, i);
                mainPage.subObjects.Add(btn);
                skinButtons[i] = btn;
            }

            if (ssm.slugcatPageIndex < playableCharacters.Count) // only for unlocked characters [locked one will be last index]
            {
                skinsLabel.label.alpha = 1f;
                skinProgressIcon.alpha = 1f;
                skinProgressIcon.pos = new Vector2(194 + 55, 515) - skinButtons.Length * new Vector2(0, 38);
                skinProgressIcon.text = $"{MeadowProgression.progressionData.currentCharacterProgress.skinUnlockProgress}/{MeadowProgression.skinProgressTreshold}";

                colorpicker.Show();
                tintLabel.label.alpha = 1f;
                tintSlider.Hidden = false;
                UpdateTintPreview();
            }
            else
            {
                skinsLabel.label.alpha = 0f;
                skinProgressIcon.alpha = 0f;

                colorpicker.Hide();
                tintLabel.label.alpha = 0f;
                tintSlider.Hidden = true;
                tintPreview.isVisible = false;
            }
        }

        public override void Update()
        {
            base.Update();

            if (this.rainEffect != null)
            {
                this.rainEffect.rainFade = Mathf.Min(0.3f, this.rainEffect.rainFade + 0.006f);
            }

            ssm.lastScroll = ssm.scroll;
            ssm.scroll = ssm.NextScroll;
            if (Mathf.Abs(ssm.lastScroll) > 0.5f && Mathf.Abs(ssm.scroll) <= 0.5f)
            {
                if (ssm.slugcatPageIndex < playableCharacters.Count)
                {
                    this.startButton.buttonBehav.greyedOut = false;
                    MeadowProgression.progressionData.currentlySelectedCharacter = playableCharacters[ssm.slugcatPageIndex];
                    MeadowProgression.progressionData.characterProgress[playableCharacters[ssm.slugcatPageIndex]].everSeenInMenu = true;
                    ReadCharacterSettings();
                }
                else
                {
                    this.startButton.buttonBehav.greyedOut = true;
                }
                this.UpdateCharacterUI();
            }
            if (ssm.scroll == 0f && ssm.lastScroll != 0f) // just hit zero
            {
                if (playableCharacters.Any(x => !MeadowProgression.progressionData.characterProgress[x].everSeenInMenu))
                {
                    ssm.quedSideInput = 1;
                }
            }
            if (ssm.scroll == 0f && ssm.lastScroll == 0f) // one frame later
            {
                if (ssm.quedSideInput != 0)
                {
                    var sign = (int)Mathf.Sign(ssm.quedSideInput);
                    ssm.slugcatPageIndex += sign;
                    ssm.slugcatPageIndex = (ssm.slugcatPageIndex + ssm.slugcatPages.Count) % ssm.slugcatPages.Count;
                    ssm.scroll = -sign;
                    ssm.lastScroll = -sign;
                    ssm.quedSideInput -= sign;
                    return;
                }
            }
            if (UnityEngine.Input.GetKey(KeyCode.L))
            {
                RainMeadow.Debug("skinIndex: " + skinIndex);
                RainMeadow.Debug("valuecolor: " + colorpicker.valuecolor);
                RainMeadow.Debug("tintAmount: " + tintAmount);
                RainMeadow.Debug("personaSettings.skin: " + personaSettings.skin);
                RainMeadow.Debug("personaSettings.tint: " + personaSettings.tint);
                RainMeadow.Debug("personaSettings.tintAmount: " + personaSettings.tintAmount);
            }
        }

        private void ReadCharacterSettings()
        {
            skinIndex = characterSkins[playableCharacters[ssm.slugcatPageIndex]].IndexOf(MeadowProgression.progressionData.currentCharacterProgress.selectedSkin);
            if (skinIndex == -1)
            {
                skinIndex = 0;
                MeadowProgression.progressionData.currentCharacterProgress.selectedSkin = characterSkins[playableCharacters[ssm.slugcatPageIndex]][0];
            }
            colorpicker.valuecolor = MeadowProgression.progressionData.currentCharacterProgress.tintColor;
            tintAmount = MeadowProgression.progressionData.currentCharacterProgress.tintAmount;

            personaSettings.skin = characterSkins[playableCharacters[ssm.slugcatPageIndex]][skinIndex];
            personaSettings.tint = colorpicker.valuecolor;
            personaSettings.tintAmount = tintAmount;

            personaSettings.Updated();
        }

        private void StartGame()
        {
            RainMeadow.DebugMe();
            if (OnlineManager.lobby == null || !OnlineManager.lobby.isActive) return;

            MeadowProgression.SaveProgression();

            manager.arenaSitting = null;
            manager.rainWorld.progression.ClearOutSaveStateFromMemory();
            manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat = RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer;
            manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.RegionSelect;
            manager.menuSetup.regionSelectRoom = MeadowProgression.progressionData.currentCharacterProgress.saveLocation.ResolveRoomName();
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }

        public override void ShutDownProcess()
        {
            RainMeadow.DebugMe();
            if (manager.upcomingProcess != ProcessManager.ProcessID.Game && manager.upcomingProcess != RainMeadow.Ext_ProcessID.MeadowMenu)
            {
                OnlineManager.LeaveLobby();
            }
            base.ShutDownProcess();
        }

        public int GetCurrentlySelectedOfSeries(string series) // SelectOneButton.SelectOneButtonOwner
        {
            return skinIndex;
        }

        public void SetCurrentlySelectedOfSeries(string series, int to) // SelectOneButton.SelectOneButtonOwner
        {
            skinIndex = to;
            personaSettings.skin = characterSkins[playableCharacters[ssm.slugcatPageIndex]][to];
            personaSettings.Updated();
            MeadowProgression.progressionData.currentCharacterProgress.selectedSkin = personaSettings.skin;

            UpdateTintPreview();
        }

        // Slider owner
        public override void SliderSetValue(Slider slider, float f)
        {
            tintAmount = f;
            personaSettings.tintAmount = f;
            personaSettings.Updated();
            MeadowProgression.progressionData.currentCharacterProgress.tintAmount = f;

            UpdateTintPreview();
        }

        public override float ValueOfSlider(Slider slider)
        {
            return tintAmount;
        }

        private void Colorpicker_OnValueChangedEvent()
        {
            personaSettings.tint = colorpicker.valuecolor;
            personaSettings.Updated();
            MeadowProgression.progressionData.currentCharacterProgress.tintColor = personaSettings.tint;

            UpdateTintPreview();
        }

        private void UpdateTintPreview()
        {
            tintPreview.SetElementByName(CreatureSymbol.SpriteNameOfCreature(new IconSymbol.IconSymbolData(personaSettings.skinData.creatureType, AbstractPhysicalObject.AbstractObjectType.Creature, 0)));
            var color = personaSettings.skinData.baseColor ?? personaSettings.skinData.previewColor;
            if (color.HasValue)
            {
                tintPreview.isVisible = true;
                var c = color.Value;
                personaSettings.ModifyBodyColor(ref c);
                tintPreview.color = c;
            }
            else
            {
                tintPreview.isVisible = false;
            }
        }
    }
}

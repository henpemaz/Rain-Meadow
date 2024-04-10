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
        MeadowAvatarSettings personaSettings;
        OpTinyColorPicker colorpicker;
        TokenMenuDisplayer skinProgressIcon;

        public override MenuScene.SceneID GetScene => null;
        public MeadowMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.MeadowMenu)
        {
            RainMeadow.DebugMe();
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
            ssm.slugcatPageIndex = playableCharacters.IndexOf(MeadowProgression.progressionData.CurrentlySelectedCharacter);

            characterSkins = new();
            for (int j = 0; j < this.playableCharacters.Count; j++)
            {
                this.characterPages.Add(new MeadowCharacterSelectPage(this, ssm, 1 + j, this.playableCharacters[j]));
                this.pages.Add(this.characterPages[j]);

                characterSkins[playableCharacters[j]] = MeadowProgression.AllAvailableSkins(this.playableCharacters[j]);
            }
            if(MeadowProgression.NextUnlockableCharacter() is MeadowProgression.Character character)
            {
                this.characterPages.Add(new MeadowCharacterSelectPage(this, ssm, characterPages.Count + 1, character, locked:true));
                this.pages.Add(this.characterPages[characterPages.Count - 1]);
            }

            this.startButton = new EventfulHoldButton(this, this.pages[0], base.Translate("ENTER"), new Vector2(683f, 85f), 40f);
            this.startButton.OnClick += (_) => { StartGame(); };

            this.pages[0].subObjects.Add(this.startButton);
            this.prevButton = new EventfulBigArrowButton(this, this.pages[0], new Vector2(345f, 50f), -1);
            this.prevButton.OnClick += (_) => {
                ssm.quedSideInput = Math.Max(-3, ssm.quedSideInput - 1);
                base.PlaySound(SoundID.MENU_Next_Slugcat);
            };
            this.pages[0].subObjects.Add(this.prevButton);
            this.nextButton = new EventfulBigArrowButton(this, this.pages[0], new Vector2(985f, 50f), 1);
            this.nextButton.OnClick += (_) => {
                ssm.quedSideInput = Math.Min(3, ssm.quedSideInput + 1);
                base.PlaySound(SoundID.MENU_Next_Slugcat);
            };
            this.pages[0].subObjects.Add(this.nextButton);
            this.mySoundLoopID = SoundID.MENU_Main_Menu_LOOP;

            this.pages[0].subObjects.Add(this.skinsLabel = new MenuLabel(this, mainPage, this.Translate("SKINS"), new Vector2(194, 553), new(110, 30), true));

            colorpicker = new OpTinyColorPicker(this, new Vector2(800, 60), "FFFFFF"); // todo read stored
            var wrapper = new UIelementWrapper(this.tabWrapper, colorpicker);

            colorpicker.OnValueChangedEvent += Colorpicker_OnValueChangedEvent;
            // todo update a preview of some sort for the resulting tinted color!

            var label = new MenuLabel(this, mainPage, this.Translate("Tint color"), new Vector2(845, 60), new(0, 30), false);
            label.label.alignment = FLabelAlignment.Left;
            this.pages[0].subObjects.Add(label);

            var slider = new SubtleSlider2(this, mainPage, "Tint amount", new Vector2(800, 30), new Vector2(100, 30));
            this.pages[0].subObjects.Add(slider);

            colorpicker.wrapper.nextSelectable[3] = slider;
            slider.nextSelectable[1] = colorpicker.wrapper;

            this.skinProgressIcon = new TokenMenuDisplayer(this, this.pages[0], new Vector2(-1000f, -1000f), MeadowProgression.TokenBlueColor, $"{0}/{MeadowProgression.skinProgressTreshold}");
            this.pages[0].subObjects.Add(skinProgressIcon);

            UpdateCharacterUI();

            BindSettings();

            if (manager.musicPlayer != null)
            {
                manager.musicPlayer.MenuRequestsSong("me",1,2);
            }
        }

        private void Colorpicker_OnValueChangedEvent()
        {
            if (personaSettings != null) personaSettings.tint = colorpicker.valuecolor;
        }

        private void UpdateCharacterUI()
        {
            if(skinButtons != null)
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
            if(skinButtons.Length > 0)
            {
                skinsLabel.label.alpha = 1f;
                skinProgressIcon.alpha = 1f;
                skinProgressIcon.pos = new Vector2(194 + 55, 515) - skinButtons.Length * new Vector2(0, 38);
                skinProgressIcon.text = $"{MeadowProgression.progressionData.currentCharacterProgress.skinUnlockProgress}/{MeadowProgression.skinProgressTreshold}";
            }
            else
            {
                skinsLabel.label.alpha = 0f;
                skinProgressIcon.alpha = 0f;
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
                    MeadowProgression.progressionData.CurrentlySelectedCharacter = playableCharacters[ssm.slugcatPageIndex];
                    MeadowProgression.progressionData.characterProgress[playableCharacters[ssm.slugcatPageIndex]].everSeenInMenu = true;
                }
                else
                {
                    this.startButton.buttonBehav.greyedOut = true;
                }
                this.UpdateCharacterUI();
            }
            if (ssm.scroll == 0f && ssm.lastScroll == 0f)
            {
                if (playableCharacters.Any(x => !MeadowProgression.progressionData.characterProgress[x].everSeenInMenu))
                {
                    ssm.quedSideInput = 1;
                }
                if(ssm.quedSideInput != 0)
                {
                    var sign = (int)Mathf.Sign(ssm.quedSideInput);
                    ssm.slugcatPageIndex += sign;
                    ssm.slugcatPageIndex = (ssm.slugcatPageIndex + ssm.slugcatPages.Count) % ssm.slugcatPages.Count;
                    if (ssm.slugcatPageIndex < playableCharacters.Count)
                    {
                        skinIndex = Mathf.Min(skinIndex, characterSkins[playableCharacters[ssm.slugcatPageIndex]].Count - 1);
                        if (personaSettings != null && skinIndex > -1) personaSettings.skin = characterSkins[playableCharacters[ssm.slugcatPageIndex]][skinIndex];
                    }
                    ssm.scroll = -sign;
                    ssm.lastScroll = -sign;
                    ssm.quedSideInput -= sign;
                    return;
                }
            }
        }

        private void BindSettings()
        {
            this.personaSettings = (MeadowAvatarSettings)OnlineManager.lobby.gameMode.clientSettings;
            personaSettings.skin = characterSkins[playableCharacters[ssm.slugcatPageIndex]][skinIndex];
            personaSettings.tint = colorpicker.valuecolor;
            personaSettings.tintAmount = this.tintAmount;
        }

        private void StartGame()
        {
            RainMeadow.DebugMe();
            if (OnlineManager.lobby == null || !OnlineManager.lobby.isActive) return;

            MeadowProgression.SaveProgression();

            manager.arenaSitting = null;
            manager.rainWorld.progression.ClearOutSaveStateFromMemory();
            manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.RegionSelect;
            manager.menuSetup.regionSelectRoom = MeadowProgression.progressionData.currentCharacterProgress.saveLocation.ResolveRoomName();
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }

        public override void ShutDownProcess()
        {
            RainMeadow.DebugMe();
            if (manager.upcomingProcess != ProcessManager.ProcessID.Game)
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
            if(personaSettings != null) personaSettings.skin = characterSkins[playableCharacters[ssm.slugcatPageIndex]][to];
        }

        // Slider owner
        public override void SliderSetValue(Slider slider, float f)
        {
            tintAmount = f;
            if (personaSettings != null) personaSettings.tintAmount = f;
        }
        public override float ValueOfSlider(Slider slider)
        {
            return tintAmount;
        }
    }
}

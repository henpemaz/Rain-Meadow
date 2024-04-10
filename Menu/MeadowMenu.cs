using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowMenu : SmartMenu, SelectOneButton.SelectOneButtonOwner
    {
        private RainEffect rainEffect;

        private EventfulHoldButton startButton;
        private EventfulBigArrowButton prevButton;
        private EventfulBigArrowButton nextButton;

        private SlugcatSelectMenu ssm;
        private List<SlugcatSelectMenu.SlugcatPage> characterPages;
        private EventfulSelectOneButton[] skinButtons;

        private List<MeadowProgression.Character> playableCharacters;
        private Dictionary<MeadowProgression.Character, List<MeadowProgression.Skin>> characterSkins;

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
                if (characterPages[j].sceneOffset.y == 0) characterPages[j].sceneOffset = new Vector2(-10f, 100f);
                this.pages.Add(this.characterPages[j]);

                var skins = MeadowProgression.AllAvailableSkins(this.playableCharacters[j]);
                RainMeadow.Debug(skins);
                RainMeadow.Debug(skins.Select(s => s.ToString()).Aggregate((a, b) => (a + b)));
                characterSkins[playableCharacters[j]] = skins;
            }
            if(MeadowProgression.NextUnlockableCharacter() is MeadowProgression.Character character)
            {
                this.characterPages.Add(new MeadowCharacterSelectPage(this, ssm, characterPages.Count + 1, character, locked:true));
                if (characterPages[characterPages.Count - 1].sceneOffset.y == 0) characterPages[characterPages.Count - 1].sceneOffset = new Vector2(-10f, 100f);
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

            this.unlockProgres = new MeadowMenu.TokenMenuDisplayer(this, this.pages[0], new Vector2(-1000f, -1000f), MeadowProgression.TokenBlueColor, $"{0}/{MeadowProgression.skinProgressTreshold}");
            this.pages[0].subObjects.Add(unlockProgres);

            UpdateCharacterUI();

            BindSettings();

            if (manager.musicPlayer != null)
            {
                manager.musicPlayer.MenuRequestsSong("me",1,2);
            }
        }

        public class TokenMenuDisplayer : PositionedMenuObject
        {
            public float alpha;
            private MeadowHud.TokenSparkIcon token;
            private MenuLabel label;
            internal string text;

            public TokenMenuDisplayer(Menu.Menu menu, MenuObject owner, Vector2 pos, Color color, string text) : base(menu, owner, pos)
            {
                Shader.SetGlobalVector(RainWorld.ShadPropSpriteRect, new Vector4(0, 0, 1, 1)); // only ever set in game smh
                Shader.SetGlobalVector(RainWorld.ShadPropScreenSize, menu.manager.rainWorld.screenSize);
                this.text = text;
                this.token = new MeadowHud.TokenSparkIcon(this.Container, color, pos, 1.5f);
                this.label = new MenuLabel(menu, owner, text, pos + new Vector2(0, 24f), Vector2.zero, false);
                this.subObjects.Add(label);
                alpha = 1f;
            }

            public override void Update()
            {
                base.Update();
                token.Update();
                label.text = text;
            }

            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                token.Draw(timeStacker);
                token.container.SetPosition(pos);
                token.container.alpha = alpha;
                token.container.isVisible = alpha > 0f;
                label.pos = pos + new Vector2(0, 24f);
                label.label.alpha = alpha;
            }

            public override void RemoveSprites()
            {
                base.RemoveSprites();
                token.ClearSprites();
            }
        }

        private void Colorpicker_OnValueChangedEvent()
        {
            if (personaSettings != null) personaSettings.tint = colorpicker.valuecolor;
        }

        float tintAmount;
        public override void SliderSetValue(Slider slider, float f)
        {
            tintAmount = f;
            if (personaSettings != null) personaSettings.tintAmount = f;
        }
        public override float ValueOfSlider(Slider slider)
        {
            return tintAmount;
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
                unlockProgres.alpha = 1f;
                unlockProgres.pos = new Vector2(194 + 55, 515) - skinButtons.Length * new Vector2(0, 38);
                unlockProgres.text = $"{MeadowProgression.progressionData.currentCharacterProgress.skinUnlockProgress}/{MeadowProgression.skinProgressTreshold}";
            }
            else
            {
                skinsLabel.label.alpha = 0f;
                unlockProgres.alpha = 0f;
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
                }
                else
                {
                    this.startButton.buttonBehav.greyedOut = true;
                }
                this.UpdateCharacterUI();
                
            }
            if (ssm.scroll == 0f && ssm.lastScroll == 0f)
            {
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
            manager.arenaSitting = null;
            manager.rainWorld.progression.ClearOutSaveStateFromMemory();
            manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }

        public override void ShutDownProcess()
        {
            RainMeadow.DebugMe();
            if (manager.upcomingProcess != ProcessManager.ProcessID.Game)
            {
                MatchmakingManager.instance.LeaveLobby();
            }
            base.ShutDownProcess();
        }

        int skinIndex;
        private MeadowAvatarSettings personaSettings;
        private OpTinyColorPicker colorpicker;
        private TokenMenuDisplayer unlockProgres;
        private MenuLabel skinsLabel;

        public int GetCurrentlySelectedOfSeries(string series) // SelectOneButton.SelectOneButtonOwner
        {
            return skinIndex;
        }

        public void SetCurrentlySelectedOfSeries(string series, int to) // SelectOneButton.SelectOneButtonOwner
        {
            skinIndex = to;
            if(personaSettings != null) personaSettings.skin = characterSkins[playableCharacters[ssm.slugcatPageIndex]][to];
        }
    }
}

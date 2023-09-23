using Menu;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowMenu : SmartMenu
    {
        private Vector2 btns = new Vector2(350, 100);
        private Vector2 btnsize = new Vector2(100, 20);
        private SimplerButton startbtn;
        private RainEffect rainEffect;
        private List<SlugcatSelectMenu.SlugcatPage> characterPages;
        private List<SlugcatStats.Name> playableCharacters;
        private HoldButton startButton;
        private EventfulBigArrowButton prevButton;
        private EventfulBigArrowButton nextButton;
        private SlugcatSelectMenu ssm;

        public class CharacterSelectPage : SlugcatSelectMenu.SlugcatPage
        {
            public CharacterSelectPage(Menu.Menu menu, int pageIndex, SlugcatStats.Name slugcatNumber) : base(menu, null, pageIndex, slugcatNumber)
            {
                base.AddImage(false);
            }
        }

        public override MenuScene.SceneID GetScene => null;
        public MeadowMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.MeadowMenu)
        {
            RainMeadow.DebugMe();

            pages[0].subObjects.Add(startbtn = new SimplerButton(this, pages[0], "Meadow deez nuts", btns, btnsize));
            startbtn.buttonBehav.greyedOut = !OnlineManager.lobby.isAvailable;
            OnlineManager.lobby.OnLobbyAvailable += OnLobbyAvailable;
            startbtn.OnClick += (SimplerButton obj) => { StartGame(); };

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

            playableCharacters = this.GetPlayableCharacters();
            for (int j = 0; j < this.playableCharacters.Count; j++)
            {
                this.characterPages.Add(new CharacterSelectPage(ssm, 1 + j, this.playableCharacters[j]));
                if (characterPages[j].sceneOffset.y == 0) characterPages[0].sceneOffset = new Vector2(-10f, 100f);
                this.pages.Add(this.characterPages[j]);
            }
            this.startButton = new HoldButton(this, this.pages[0], "", "START", new Vector2(683f, 85f), 40f);
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
            this.UpdateStartButtonText();
            this.mySoundLoopID = SoundID.MENU_Main_Menu_LOOP;
        }

        private void UpdateStartButtonText()
        {
            // todo
            //throw new NotImplementedException();
        }

        private List<SlugcatStats.Name> GetPlayableCharacters()
        {
            // todo
            return new() { RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer, SlugcatStats.Name.Yellow };
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
            // todo
            //this.startButton.GetButtonBehavior.greyedOut = (Mathf.Abs(ssm.scroll) > 0.1f || !this.SlugcatUnlocked(this.colorFromIndex(this.slugcatPageIndex)));
            if (Mathf.Abs(ssm.lastScroll) > 0.5f && Mathf.Abs(ssm.scroll) <= 0.5f)
            {
                this.UpdateStartButtonText();
            }
            if (ssm.scroll == 0f && ssm.lastScroll == 0f)
            {
                if(ssm.quedSideInput != 0)
                {
                    ssm.slugcatPageIndex += (int)Mathf.Sign(ssm.quedSideInput);
                    ssm.slugcatPageIndex = (ssm.slugcatPageIndex + ssm.slugcatPages.Count) % ssm.slugcatPages.Count;
                    ssm.scroll = -(int)Mathf.Sign(ssm.quedSideInput);
                    ssm.lastScroll = -(int)Mathf.Sign(ssm.quedSideInput);
                    ssm.quedSideInput -= (int)Mathf.Sign(ssm.quedSideInput);
                    return;
                }
            }
        }

        private void OnLobbyAvailable()
        {
            startbtn.buttonBehav.greyedOut = false;
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
            if (OnlineManager.lobby != null) OnlineManager.lobby.OnLobbyAvailable -= OnLobbyAvailable;
            if (manager.upcomingProcess != ProcessManager.ProcessID.Game)
            {
                MatchmakingManager.instance.LeaveLobby();
            }
            base.ShutDownProcess();
        }
    }
}

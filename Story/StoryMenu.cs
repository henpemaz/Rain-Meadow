using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class StoryMenu : SmartMenu, SelectOneButton.SelectOneButtonOwner
    {
        private RainEffect rainEffect;

        private EventfulHoldButton startButton;
        private EventfulBigArrowButton prevButton;
        private EventfulBigArrowButton nextButton;
        private PlayerInfo[] players;

        private SlugcatSelectMenu ssm;
        private SlugcatSelectMenu.SlugcatPage sp;

        private List<SlugcatSelectMenu.SlugcatPage> characterPages;
        private EventfulSelectOneButton[] playerButtons;

        public override MenuScene.SceneID GetScene => null;
        public StoryMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.StoryMenu)
        {
            RainMeadow.DebugMe();
            this.rainEffect = new RainEffect(this, this.pages[0]);
            this.pages[0].subObjects.Add(this.rainEffect);
            this.rainEffect.rainFade = 0.3f;



            ssm = (SlugcatSelectMenu)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SlugcatSelectMenu));
            sp = (SlugcatSelectMenu.SlugcatPageNewGame)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SlugcatSelectMenu.SlugcatPageNewGame));

            ssm.container = container;
            ssm.slugcatPages = characterPages;
            ssm.ID = ProcessManager.ProcessID.MultiplayerMenu;
            ssm.cursorContainer = cursorContainer;
            ssm.manager = manager;
            ssm.pages = pages;

            MenuScene.SceneID sceneID = MenuScene.SceneID.Slugcat_White;

            ssm.slugcatColorOrder = AllAvailableCharacters();
            sp.slugcatImage = new InteractiveMenuScene(this, this.pages[0], sceneID);
            string characterName = ""; // Character name
            string subtitle = ""; // subtitle
            sp.imagePos = new Vector2(683f, 484f);

            // TODO: Multiple Characters
            for (int j = 0; j < ssm.slugcatColorOrder.Count; j++)
            {
                sp.slugcatNumber = ssm.slugcatColorOrder[j];

                // TODO: Background images
                if (sp.slugcatNumber == SlugcatStats.Name.White)
                {
                    sceneID = MenuScene.SceneID.Slugcat_White;
                    sp.sceneOffset = new Vector2(-10f, 100f);
                    sp.slugcatDepth = 3.1000001f;
                    sp.markOffset = new Vector2(-15f, -2f);
                    sp.glowOffset = new Vector2(-30f, -50f);

                }

            }
            this.pages[0].subObjects.Add(sp.slugcatImage);

            // TODO: Alignment issues.

            /*            s = RWCustom.Custom.ReplaceLineDelimeters(s);
                        int num = s.Count((char f) => f == '\n');
                        float num2 = 0f;
                        if (num > 1)
                        {
                            num2 = 30f;
                        }
                        var characterName = new MenuLabel(this, pages[0], text, new Vector2(sp.imagePos.x, sp.imagePos.y - 400f), new Vector2(200f, 30f), bigText: true);
                        characterName.label.alignment = FLabelAlignment.Center;
                        this.pages[0].subObjects.Add(characterName);

                        var infoLabel = new MenuLabel(this, pages[0], s, new Vector2(-1000f, sp.imagePos.y - 249f - 60f + num2 / 2f), new Vector2(400f, 60f), bigText: true);
                        infoLabel.label.alignment = FLabelAlignment.Center;
                        this.pages[0].subObjects.Add(infoLabel);

                        *//*            characterName.label.color = MenuRGB(MenuColors.MediumGrey);
                                    infoLabel.label.color = MenuRGB(MenuColors.DarkGrey);
                        */

            this.startButton = new EventfulHoldButton(this, this.pages[0], base.Translate("ENTER"), new Vector2(683f, 85f), 40f);
            this.startButton.OnClick += (_) => { StartGame(); };
            startButton.buttonBehav.greyedOut = !OnlineManager.lobby.isAvailable;

            this.pages[0].subObjects.Add(this.startButton);
            this.prevButton = new EventfulBigArrowButton(this, this.pages[0], new Vector2(345f, 50f), -1);
            this.prevButton.OnClick += (_) =>
            {
                return; // Protect the users until all characters are fixed
                ssm.quedSideInput = Math.Max(-3, ssm.quedSideInput - 1);
                base.PlaySound(SoundID.MENU_Next_Slugcat);
            };
            this.pages[0].subObjects.Add(this.prevButton);



            this.nextButton = new EventfulBigArrowButton(this, this.pages[0], new Vector2(985f, 50f), 1);
            this.nextButton.OnClick += (_) =>
            {
                return;
                ssm.quedSideInput = Math.Min(3, ssm.quedSideInput + 1);
                base.PlaySound(SoundID.MENU_Next_Slugcat);
            };
            this.pages[0].subObjects.Add(this.nextButton);


            this.nextButton.OnClick += (_) =>
            {
                return;
                ssm.quedSideInput = Math.Min(3, ssm.quedSideInput + 1);
                base.PlaySound(SoundID.MENU_Next_Slugcat);
            };
            this.pages[0].subObjects.Add(this.nextButton);

 
            this.mySoundLoopID = SoundID.MENU_Main_Menu_LOOP;


            this.pages[0].subObjects.Add(new MenuLabel(this, mainPage, this.Translate("LOBBY"), new Vector2(194, 553), new(110, 30), true));

            List<PlayerInfo> players = new List<PlayerInfo>();
            foreach (OnlinePlayer player in OnlineManager.players)
            {
                CSteamID playerId;
                if (player.id is LocalMatchmakingManager.LocalPlayerId)
                {
                    playerId = default;
                }
                else
                {
                    playerId = (player.id as SteamMatchmakingManager.SteamPlayerId).steamID;
                }
                players.Add(new PlayerInfo(playerId, player.id.name));
            }
            this.players = players.ToArray();

            var friendsList = new EventfulSelectOneButton[1];
            friendsList[0] = new EventfulSelectOneButton(this, mainPage, Translate("Invite Friends"), "friendsList", new(1150f, 50f), new(110, 50), friendsList, 0);
            this.pages[0].subObjects.Add(friendsList[0]);
            friendsList[0].OnClick += (_) =>
            {
                SteamFriends.ActivateGameOverlay("friends");
            };

            // TODO: Skin + Eye customization

            UpdateCharacterUI();
            MatchmakingManager.instance.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;

            if (OnlineManager.lobby.isAvailable)
            {
                OnLobbyAvailable();
            }
            else
            {
                OnlineManager.lobby.OnLobbyAvailable += OnLobbyAvailable;
            }

        }

        private void UpdateCharacterUI()
        {

            playerButtons = new EventfulSelectOneButton[players.Length];
            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                var btn = new EventfulSelectOneButton(this, mainPage, player.name, "playerButtons", new Vector2(194, 515) - i * new Vector2(0, 38), new(110, 30), playerButtons, i);
                mainPage.subObjects.Add(btn);
                playerButtons[i] = btn;
                btn.OnClick+= (_) => {
                    string url = $"https://steamcommunity.com/profiles/{player.id}";
                    SteamFriends.ActivateGameOverlayToWebPage(url);
                };

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
                this.UpdateCharacterUI();
            }
            if (ssm.scroll == 0f && ssm.lastScroll == 0f)
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
        }

        private void OnLobbyAvailable()
        {
            startButton.buttonBehav.greyedOut = false;

        }

        private void StartGame()
        {
            RainMeadow.DebugMe();
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

        int skinIndex;
        private OpTinyColorPicker colorpicker;

        public int GetCurrentlySelectedOfSeries(string series) // SelectOneButton.SelectOneButtonOwner
        {
            return skinIndex;
        }

        public void SetCurrentlySelectedOfSeries(string series, int to) // SelectOneButton.SelectOneButtonOwner
        {
            skinIndex = to;
        }

        private void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
        {
            this.players = players;
            UpdateCharacterUI();
        }

        public static List<SlugcatStats.Name> AllAvailableCharacters()
        {

            return SlugcatStats.Name.values.entries.Select(s => new SlugcatStats.Name(s)).ToList();
        }


    }
}

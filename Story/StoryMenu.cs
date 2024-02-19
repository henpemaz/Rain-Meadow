using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class StoryMenu : SmartMenu, SelectOneButton.SelectOneButtonOwner
    {
        private readonly RainEffect rainEffect;

        private EventfulHoldButton hostStartButton;
        private EventfulHoldButton clientWaitingButton;


        private EventfulBigArrowButton prevButton;
        private EventfulBigArrowButton nextButton;
        private SimplerButton backButton;
        private PlayerInfo[] players;

        private SlugcatSelectMenu ssm;
        private SlugcatSelectMenu.SlugcatPage sp;
        private StoryAvatarSettings personaSettings;

        private List<SlugcatSelectMenu.SlugcatPage> characterPages;
        private EventfulSelectOneButton[] playerButtons;
        int skinIndex;
        private OpTinyColorPicker bodyColorPicker;
        private OpTinyColorPicker eyeColorPicker;

        public override MenuScene.SceneID GetScene => null;
        public StoryMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.StoryMenu)
        {

            // TODO: Find the source of the little white half circle on the bottom right
            RainMeadow.DebugMe();
            this.rainEffect = new RainEffect(this, this.pages[0]);
            this.pages[0].subObjects.Add(this.rainEffect);
            this.rainEffect.rainFade = 0.3f;


            // Initial setup for slugcat menu & pages
            ssm = (SlugcatSelectMenu)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SlugcatSelectMenu));
            sp = (SlugcatSelectMenu.SlugcatPageNewGame)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SlugcatSelectMenu.SlugcatPageNewGame));
            ssm.container = container;
            ssm.slugcatPages = characterPages;
            ssm.ID = ProcessManager.ProcessID.MultiplayerMenu;
            ssm.cursorContainer = cursorContainer;
            ssm.manager = manager;
            ssm.pages = pages;

            MenuScene.SceneID HostSceneID = MenuScene.SceneID.Slugcat_White;
            MenuScene.SceneID ClientSceneID = MenuScene.SceneID.Landscape_SU;


            // Custom images for host vs clients
            if (OnlineManager.lobby.isOwner)
            {
                sp.slugcatImage = new InteractiveMenuScene(this, this.pages[0], HostSceneID);

                this.pages[0].subObjects.Add(sp.slugcatImage);

            }
            else
            {
                sp.slugcatImage = new InteractiveMenuScene(this, this.pages[0], ClientSceneID);

                this.pages[0].subObjects.Add(sp.slugcatImage);

            }


            ssm.slugcatColorOrder = AllAvailableCharacters();
            sp.imagePos = new Vector2(683f, 484f);
            /*
                        // TODO: Multiple Characters
                        for (int j = 0; j < ssm.slugcatColorOrder.Count; j++)
                        {
                            sp.slugcatNumber = ssm.slugcatColorOrder[j];

                            // TODO: Background images
                            if (sp.slugcatNumber == SlugcatStats.Name.White)
                            {
                                //HostSceneID = MenuScene.SceneID.Slugcat_White;
                                sp.sceneOffset = new Vector2(-10f, 100f);
                                sp.slugcatDepth = 3.1000001f;
                                sp.markOffset = new Vector2(-15f, -2f);
                                sp.glowOffset = new Vector2(-30f, -50f);

                            }

                        }*/


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


            // Setup host / client buttons & general view

            SetupMenuItems();

            if (OnlineManager.lobby.isOwner)
            {

                this.hostStartButton = new EventfulHoldButton(this, this.pages[0], base.Translate("ENTER"), new Vector2(683f, 85f), 40f);
                this.hostStartButton.OnClick += (_) => { StartGame(); };
                hostStartButton.buttonBehav.greyedOut = false;
                this.pages[0].subObjects.Add(this.hostStartButton);


            }

            if (!OnlineManager.lobby.isOwner)
            {

                this.pages[0].subObjects.Add(this.backButton);

                this.clientWaitingButton = new EventfulHoldButton(this, this.pages[0], base.Translate("ENTER"), new Vector2(683f, 85f), 40f);
                this.clientWaitingButton.OnClick += (_) => { StartGame(); };
                clientWaitingButton.buttonBehav.greyedOut = !(OnlineManager.lobby.gameMode as StoryGameMode).didStartGame; // True to begin

                this.pages[0].subObjects.Add(this.clientWaitingButton);


            }
            SteamSetup();
            SetupCharacterCustomization();
            UpdateCharacterUI();



            if (OnlineManager.lobby.isAvailable)
            {
                OnLobbyActive();
            }
            else
            {
                OnlineManager.lobby.gameMode.OnLobbyActive += OnLobbyActive;
            }


            MatchmakingManager.instance.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;


        }


        private void StartGame()
        {
            RainMeadow.DebugMe();
            manager.arenaSitting = null;
            manager.rainWorld.progression.ClearOutSaveStateFromMemory();
            manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
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

            if (!OnlineManager.lobby.isOwner)
            {
                this.clientWaitingButton.buttonBehav.greyedOut = !(OnlineManager.lobby.gameMode as StoryGameMode).didStartGame;
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
        private void UpdateCharacterUI()
        {

            playerButtons = new EventfulSelectOneButton[players.Length];
            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                var btn = new EventfulSelectOneButton(this, mainPage, player.name, "playerButtons", new Vector2(194, 515) - i * new Vector2(0, 38), new(110, 30), playerButtons, i);
                mainPage.subObjects.Add(btn);
                playerButtons[i] = btn;
                btn.OnClick += (_) =>
                {
                    string url = $"https://steamcommunity.com/profiles/{player.id}";
                    SteamFriends.ActivateGameOverlayToWebPage(url);
                };

            }
        }

        public override void ShutDownProcess()
        {
            RainMeadow.DebugMe();

            if (OnlineManager.lobby != null) OnlineManager.lobby.gameMode.OnLobbyActive -= OnLobbyActive;

            if (manager.upcomingProcess != ProcessManager.ProcessID.Game)
            {
                MatchmakingManager.instance.LeaveLobby();
            }
            base.ShutDownProcess();
        }

        private void SetupCharacterCustomization()
        {
            var bodyLabel = new MenuLabel(this, mainPage, this.Translate("Body color"), new Vector2(1190, 553), new(0, 30), false);
            bodyLabel.label.alignment = FLabelAlignment.Right;
            this.pages[0].subObjects.Add(bodyLabel);


            var eyeLabel = new MenuLabel(this, mainPage, this.Translate("Eye color"), new Vector2(1181, 500), new(0, 30), false);
            eyeLabel.label.alignment = FLabelAlignment.Right;
            this.pages[0].subObjects.Add(eyeLabel);

            bodyColorPicker = new OpTinyColorPicker(this, new Vector2(1094, 553), "FFFFFF");
            var wrapper = new UIelementWrapper(this.tabWrapper, bodyColorPicker);
            tabWrapper._tab.AddItems(bodyColorPicker.colorPicker);
            bodyColorPicker.colorPicker.wrapper = wrapper;
            bodyColorPicker.colorPicker.Hide();
            bodyColorPicker.OnValueChangedEvent += Colorpicker_OnValueChangedEvent;

            eyeColorPicker = new OpTinyColorPicker(this, new Vector2(1094, 500), "000000");
            var wrapper2 = new UIelementWrapper(this.tabWrapper, eyeColorPicker);
            tabWrapper._tab.AddItems(eyeColorPicker.colorPicker);
            eyeColorPicker.colorPicker.wrapper = wrapper2;
            eyeColorPicker.colorPicker.Hide();
            eyeColorPicker.OnValueChangedEvent += Colorpicker_OnValueChangedEvent;
        }
        private void SetupMenuItems()
        {

            // Previous
            this.prevButton = new EventfulBigArrowButton(this, this.pages[0], new Vector2(345f, 50f), -1);
            this.prevButton.OnClick += (_) =>
            {
                return; // TODO: Protect the users until all characters are fixed
                ssm.quedSideInput = Math.Max(-3, ssm.quedSideInput - 1);
                base.PlaySound(SoundID.MENU_Next_Slugcat);
            };
            this.pages[0].subObjects.Add(this.prevButton);


            // Next
            this.nextButton = new EventfulBigArrowButton(this, this.pages[0], new Vector2(985f, 50f), 1);
            this.nextButton.OnClick += (_) =>
            {
                return;
                ssm.quedSideInput = Math.Min(3, ssm.quedSideInput + 1);
                base.PlaySound(SoundID.MENU_Next_Slugcat);
            };
            this.pages[0].subObjects.Add(this.nextButton);


            // Back button doesn't highlight?
            this.backButton = new SimplerButton(this, pages[0], "BACK", new Vector2(200f, 50f), new Vector2(110f, 30f));
            this.backButton.OnClick += (_) =>
            {
                manager.RequestMainProcessSwitch(this.backTarget);
            };


            // Music
            this.mySoundLoopID = SoundID.MENU_Main_Menu_LOOP;

            // Player lobby label
            this.pages[0].subObjects.Add(new MenuLabel(this, mainPage, this.Translate("LOBBY"), new Vector2(194, 553), new(110, 30), true));


        }

        private void SteamSetup()
        {

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

            // TODO: Add Friend's list back when I'm done testing

            friendsList[0].OnClick += (_) =>
            {
                SteamFriends.ActivateGameOverlay("friends");

            };



        }



        public int GetCurrentlySelectedOfSeries(string series)
        {
            return skinIndex;
        }

        public void SetCurrentlySelectedOfSeries(string series, int to)
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

        private void BindSettings()
        {
            this.personaSettings = (StoryAvatarSettings)OnlineManager.lobby.gameMode.avatarSettings;
            personaSettings.playingAs = SlugcatStats.Name.White;
            personaSettings.bodyColor = Color.white;
            personaSettings.eyeColor = Color.black;

        }

        private void OnLobbyActive()
        {
            BindSettings();
        }

        private void Colorpicker_OnValueChangedEvent()
        {
            if (personaSettings != null) personaSettings.bodyColor = bodyColorPicker.valuecolor;
            if (personaSettings != null) personaSettings.eyeColor = eyeColorPicker.valuecolor;

        }

    }
}

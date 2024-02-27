using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using MoreSlugcats;
using static RainMeadow.RainMeadow;

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
        private SlugcatStats.Name currentCampaign;
        private string currentCampaignName = "";
        private MenuLabel campaignContainer;

        private bool fakeOptionForCustomSlugcatChoice = false; // ADD REMIX OPTION HERE

        private SlugcatStats.Name customSelectedSlugcat = Ext_SlugcatStatsName.OnlineStoryWhite;



        public override MenuScene.SceneID GetScene => null;
        public StoryMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.StoryMenu)
        {
            RainMeadow.DebugMe();
            this.rainEffect = new RainEffect(this, this.pages[0]);
            this.pages[0].subObjects.Add(this.rainEffect);
            this.rainEffect.rainFade = 0.3f;
            this.characterPages = new List<SlugcatSelectMenu.SlugcatPage>();

            // Initial setup for slugcat menu & pages
            ssm = (SlugcatSelectMenu)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SlugcatSelectMenu));
            sp = (SlugcatSelectMenu.SlugcatPageNewGame)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SlugcatSelectMenu.SlugcatPageNewGame));
            ssm.container = container;
            ssm.slugcatPages = characterPages;
            ssm.ID = ProcessManager.ProcessID.MultiplayerMenu;
            ssm.cursorContainer = cursorContainer;
            ssm.manager = manager;
            ssm.pages = pages;



            ssm.slugcatColorOrder = AllSlugcats();
            sp.imagePos = new Vector2(683f, 484f);



            for (int j = 0; j < ssm.slugcatColorOrder.Count; j++)
            {
                this.characterPages.Add(new SlugcatCustomSelection(this, ssm, 1 + j, ssm.slugcatColorOrder[j]));
                this.pages.Add(this.characterPages[j]);

            }


            // Setup host / client buttons & general view

            SetupMenuItems();

            if (OnlineManager.lobby.isOwner)
            {

                this.hostStartButton = new EventfulHoldButton(this, this.pages[0], base.Translate("ENTER"), new Vector2(683f, 85f), 40f);
                this.hostStartButton.OnClick += (_) => { StartGame(); };
                hostStartButton.buttonBehav.greyedOut = false;
                this.pages[0].subObjects.Add(this.hostStartButton);


                // Previous
                this.prevButton = new EventfulBigArrowButton(this, this.pages[0], new Vector2(345f, 50f), -1);
                this.prevButton.OnClick += (_) =>
                {
                    if (!rainMeadowOptions.SlugcatCustomToggle.Value)
                    { // I don't want to choose unstable slugcats
                        return;
                    }

                    ssm.quedSideInput = Math.Max(-3, ssm.quedSideInput - 1);
                    base.PlaySound(SoundID.MENU_Next_Slugcat);

                };
                this.pages[0].subObjects.Add(this.prevButton);


                // Next

                this.nextButton = new EventfulBigArrowButton(this, this.pages[0], new Vector2(985f, 50f), 1);
                this.nextButton.OnClick += (_) =>
                {
                    if (!rainMeadowOptions.SlugcatCustomToggle.Value)
                    {
                        return;
                    }
                    ssm.quedSideInput = Math.Min(3, ssm.quedSideInput + 1);
                    base.PlaySound(SoundID.MENU_Next_Slugcat);
                };
                this.pages[0].subObjects.Add(this.nextButton);

            }

            if (!OnlineManager.lobby.isOwner)
            {
                campaignContainer = new MenuLabel(this, mainPage, this.Translate(currentCampaignName), new Vector2(583f, sp.imagePos.y - 268f), new Vector2(200f, 30f), true);

                this.pages[0].subObjects.Add(campaignContainer);


                // Back button doesn't highlight?
                this.backButton = new SimplerButton(this, pages[0], "BACK", new Vector2(200f, 50f), new Vector2(110f, 30f));
                this.backButton.OnClick += (_) =>
                {
                    manager.RequestMainProcessSwitch(this.backTarget);
                };
                this.pages[0].subObjects.Add(this.backButton);

                this.clientWaitingButton = new EventfulHoldButton(this, this.pages[0], base.Translate("ENTER"), new Vector2(683f, 85f), 40f);
                this.clientWaitingButton.OnClick += (_) => { StartGame(); };
                clientWaitingButton.buttonBehav.greyedOut = !(OnlineManager.lobby.gameMode as StoryGameMode).didStartGame; // True to begin

                this.pages[0].subObjects.Add(this.clientWaitingButton);


            }
            SteamSetup();
            SetupCharacterCustomization();
            UpdateCharacterUI();

            if (!OnlineManager.lobby.isOwner && rainMeadowOptions.SlugcatCustomToggle.Value)
            {
                CustomSlugcatSetup();
            }



            if (OnlineManager.lobby.isActive)
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
            if (!OnlineManager.lobby.isOwner) // I'm a client
            {
                if (!rainMeadowOptions.SlugcatCustomToggle.Value) // I'm a client and I want to match the hosts
                {

                    personaSettings.playingAs = (OnlineManager.lobby.gameMode as StoryGameMode).currentCampaign;
                }
                else // I'm a client and I want my own Slugcat
                {
                    personaSettings.playingAs = customSelectedSlugcat;

                }
            }
            else //I'm the host
            {
                personaSettings.playingAs = ssm.slugcatPages[ssm.slugcatPageIndex].slugcatNumber;
                (OnlineManager.lobby.gameMode as StoryGameMode).currentCampaign = ssm.slugcatPages[ssm.slugcatPageIndex].slugcatNumber; // I decide the campaign
            }
            Ext_SlugcatStatsName.OnlineSessionPlayer = personaSettings.playingAs;
            RainMeadow.Debug("PLAYING AS: " + personaSettings.playingAs);
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
                if ((OnlineManager.lobby.gameMode as StoryGameMode).didStartGame)
                {
                    currentCampaign = (OnlineManager.lobby.gameMode as StoryGameMode).currentCampaign ?? Ext_SlugcatStatsName.OnlineStoryWhite;
                    campaignContainer.text = $"Current Campaign: {GetCampaignName(currentCampaign)}";
                }
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


            // Music
            this.mySoundLoopID = SoundID.MENU_Main_Menu_LOOP;

            // Player lobby label
            this.pages[0].subObjects.Add(new MenuLabel(this, mainPage, this.Translate("LOBBY"), new Vector2(194, 553), new(110, 30), true));

            if (rainMeadowOptions.SlugcatCustomToggle.Value && !OnlineManager.lobby.isOwner)
            {
                this.pages[0].subObjects.Add(new MenuLabel(this, mainPage, this.Translate("Slugcat Select"), new Vector2(394, 553), new(110, 30), true));
            }


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

            friendsList[0].OnClick += (_) =>
            {
                SteamFriends.ActivateGameOverlay("friends");

            };



        }
        private void CustomSlugcatSetup()
        {
            var slugList = AllSlugcats();
            var slugButtons = new EventfulSelectOneButton[slugList.Count];
            for (int i = 0; i < slugButtons.Length; i++)
            {
                var slug = slugList[i];
                var slugStringName = GetCampaignName(slugList[i]);
                var btn = new EventfulSelectOneButton(this, mainPage, slugStringName, "slugButtons", new Vector2(394, 515) - i * new Vector2(0, 38), new(110, 30), slugButtons, i);
                mainPage.subObjects.Add(btn);
                slugButtons[i] = btn;
                btn.OnClick += (_) =>
                {
                    customSelectedSlugcat = slug;
                };

            }
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

        public static List<SlugcatStats.Name> AllSlugcats()
        {
            // List<string> namesToExclude = new List<string> { "Night", "MeadowOnline", "MeadowOnlineRemote" }; // TODO: follow up on these
            var filteredList = new List<SlugcatStats.Name>();


            if (!ModManager.MSC)
            {
                filteredList.Add(SlugcatStats.Name.White);
                filteredList.Add(SlugcatStats.Name.Yellow);// TODO: The most recent entry in the list is what the player becomes despite selecting another slugcat
                filteredList.Add(SlugcatStats.Name.Red);
            }
            else // I have more slugs for you
            {
                filteredList.Add(SlugcatStats.Name.White);
                filteredList.Add(SlugcatStats.Name.Yellow);
                filteredList.Add(SlugcatStats.Name.Red);


                // TODO: MSC isUnlocked check

                /*              filteredList.Add(MoreSlugcatsEnums.SlugcatStatsName.Spear);
                 *              
                                filteredList.Add(MoreSlugcatsEnums.SlugcatStatsName.Artificer);

                                filteredList.Add(MoreSlugcatsEnums.SlugcatStatsName.Gourmand);

                                filteredList.Add(MoreSlugcatsEnums.SlugcatStatsName.Rivulet);

                                filteredList.Add(MoreSlugcatsEnums.SlugcatStatsName.Spear);*/

                // filteredList.Add(MoreSlugcatsEnums.SlugcatStatsName.Saint);

            }
            return filteredList;

        }


        private void BindSettings()
        {
            this.personaSettings = (StoryAvatarSettings)OnlineManager.lobby.gameMode.avatarSettings;
            personaSettings.playingAs = ssm.slugcatPages[ssm.slugcatPageIndex].slugcatNumber;
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

        public  string GetCampaignName(SlugcatStats.Name name)
        {
            this.currentCampaignName = "";
            if (name == SlugcatStats.Name.White)
            {

                currentCampaignName =  "SURVIVOR";
            }
            else if (name == SlugcatStats.Name.Yellow)
            {
                currentCampaignName = "MONK";
            }
            else if (name == SlugcatStats.Name.Red)
            {
                currentCampaignName = "HUNTER";
            }
            else
            {
                currentCampaignName  = "";
            }

            return currentCampaignName;
        }

    }
}

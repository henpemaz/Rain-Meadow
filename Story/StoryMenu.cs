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
using RWCustom;
using static RainMeadow.RainMeadow;
using RainMeadow.Properties;

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
        private StoryClientSettings personaSettings;

        private List<SlugcatSelectMenu.SlugcatPage> characterPages;
        private EventfulSelectOneButton[] playerButtons = new EventfulSelectOneButton[0];
        int skinIndex;
        private OpTinyColorPicker bodyColorPicker;
        private OpTinyColorPicker eyeColorPicker;
        private SlugcatStats.Name currentCampaign;
        private string currentCampaignName = "";
        private MenuLabel campaignContainer;


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

            // Grab Host Remix Settings
            if (OnlineManager.lobby.isOwner)
            {
                (OnlineManager.lobby.gameMode as StoryGameMode).storyRemixSettings = GetHostStoryRemixSettings();


            }

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

                SetClientStoryRemixSettings((OnlineManager.lobby.gameMode as StoryGameMode).storyRemixSettings); // Set client remix settings to Host's on StartGame()


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
            for (int i = 0; i < playerButtons.Length; i++)
            {
                var playerbtn = playerButtons[i];
                playerbtn.RemoveSprites();
                mainPage.RemoveSubObject(playerbtn);
            }

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
                this.pages[0].subObjects.Add(new MenuLabel(this, mainPage, this.Translate("Slugcats"), new Vector2(394, 553), new(110, 30), true));
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
                var btn = new SimplerButton(this, mainPage, slugStringName, new Vector2(394, 515) - i * new Vector2(0, 38), new Vector2(110, 30));
                btn.toggled = false;
                mainPage.subObjects.Add(btn);

                // Store the current button in a variable accessible by the lambda
                var currentBtn = btn;
                btn.OnClick += (_) =>
                {
                    // Set the clicked button to true
                    currentBtn.toggled = !currentBtn.toggled;
                    customSelectedSlugcat = slug;

                    // Set all other buttons to false
                    foreach (var otherBtn in mainPage.subObjects.OfType<SimplerButton>())
                    {
                        if (otherBtn != currentBtn)
                        {
                            otherBtn.toggled = false;
                        }
                    }
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
                filteredList.Add(Ext_SlugcatStatsName.OnlineStoryWhite);
                filteredList.Add(Ext_SlugcatStatsName.OnlineStoryYellow);
                filteredList.Add(Ext_SlugcatStatsName.OnlineStoryRed);
            }
            else // I have more slugs for you
            {
                filteredList.Add(Ext_SlugcatStatsName.OnlineStoryWhite);
                filteredList.Add(Ext_SlugcatStatsName.OnlineStoryYellow);
                filteredList.Add(Ext_SlugcatStatsName.OnlineStoryRed);

            }

            return filteredList;

        }



        private void BindSettings()
        {
            this.personaSettings = (StoryClientSettings)OnlineManager.lobby.gameMode.clientSettings;
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

        public string GetCampaignName(SlugcatStats.Name name)
        {
            this.currentCampaignName = "";
            if (name == Ext_SlugcatStatsName.OnlineStoryWhite)
            {

                currentCampaignName = "SURVIVOR";
            }
            else if (name == Ext_SlugcatStatsName.OnlineStoryYellow)
            {
                currentCampaignName = "MONK";
            }
            else if (name == Ext_SlugcatStatsName.OnlineStoryRed)
            {
                currentCampaignName = "HUNTER";
            }
            else
            {
                currentCampaignName = "";
            }

            return currentCampaignName;
        }

        internal void SetClientStoryRemixSettings(List<bool> hostRemixSettings)
        {
            List<Configurable<bool>> configurables = new List<Configurable<bool>>();

            if (ModManager.MMF)
            {

                configurables.Add(MoreSlugcats.MMF.cfgAlphaRedLizards);
                configurables.Add(MoreSlugcats.MMF.cfgBreathTimeVisualIndicator);
                configurables.Add(MoreSlugcats.MMF.cfgClearerDeathGradients);
                configurables.Add(MoreSlugcats.MMF.cfgClimbingGrip);
                configurables.Add(MoreSlugcats.MMF.cfgCreatureSense);
                configurables.Add(MoreSlugcats.MMF.cfgDeerBehavior);
                configurables.Add(MoreSlugcats.MMF.cfgDisableGateKarma);
                configurables.Add(MoreSlugcats.MMF.cfgDisableScreenShake);
                configurables.Add(MoreSlugcats.MMF.cfgExtraLizardSounds);
                configurables.Add(MoreSlugcats.MMF.cfgExtraTutorials);
                configurables.Add(MoreSlugcats.MMF.cfgFasterShelterOpen);
                configurables.Add(MoreSlugcats.MMF.cfgFastMapReveal);
                configurables.Add(MoreSlugcats.MMF.cfgFreeSwimBoosts);
                configurables.Add(MoreSlugcats.MMF.cfgGlobalMonkGates);
                configurables.Add(MoreSlugcats.MMF.cfgGraspWiggling);
                configurables.Add(MoreSlugcats.MMF.cfgHideRainMeterNoThreat);
                configurables.Add(MoreSlugcats.MMF.cfgHunterBackspearProtect);
                configurables.Add(MoreSlugcats.MMF.cfgHunterBatflyAutograb);
                //configurables.Add(MoreSlugcats.MMF.cfgHunterBonusCycles);
                //configurables.Add(MoreSlugcats.MMF.cfgHunterCycles);
                configurables.Add(MoreSlugcats.MMF.cfgIncreaseStuns);
                configurables.Add(MoreSlugcats.MMF.cfgJetfishItemProtection);
                configurables.Add(MoreSlugcats.MMF.cfgKeyItemPassaging);
                configurables.Add(MoreSlugcats.MMF.cfgKeyItemTracking);
                configurables.Add(MoreSlugcats.MMF.cfgLargeHologramLight);
                configurables.Add(MoreSlugcats.MMF.cfgLoadingScreenTips);
                configurables.Add(MoreSlugcats.MMF.cfgMonkBreathTime);
                configurables.Add(MoreSlugcats.MMF.cfgNewDynamicDifficulty);
                configurables.Add(MoreSlugcats.MMF.cfgNoArenaFleeing);
                configurables.Add(MoreSlugcats.MMF.cfgNoRandomCycles);
                configurables.Add(MoreSlugcats.MMF.cfgOldTongue);
                configurables.Add(MoreSlugcats.MMF.cfgQuieterGates);
                // configurables.Add(MoreSlugcats.MMF.cfgRainTimeMultiplier);
                configurables.Add(MoreSlugcats.MMF.cfgSafeCentipedes);
                configurables.Add(MoreSlugcats.MMF.cfgSandboxItemStems);
                configurables.Add(MoreSlugcats.MMF.cfgScavengerKillSquadDelay);
                configurables.Add(MoreSlugcats.MMF.cfgShowUnderwaterShortcuts);
                //configurables.Add(MoreSlugcats.MMF.cfgSlowTimeFactor);
                configurables.Add(MoreSlugcats.MMF.cfgSpeedrunTimer);
                configurables.Add(MoreSlugcats.MMF.cfgSurvivorPassageNotRequired);
                configurables.Add(MoreSlugcats.MMF.cfgSwimBreathLeniency);
                configurables.Add(MoreSlugcats.MMF.cfgThreatMusicPulse);
                configurables.Add(MoreSlugcats.MMF.cfgTickTock);
                configurables.Add(MoreSlugcats.MMF.cfgUpwardsSpearThrow);
                configurables.Add(MoreSlugcats.MMF.cfgVanillaExploits);
                configurables.Add(MoreSlugcats.MMF.cfgVulnerableJellyfish);
                configurables.Add(MoreSlugcats.MMF.cfgWallpounce);


                for (int i = 0; i < hostRemixSettings.Count; i++)
                {
                    configurables[i]._typedValue = hostRemixSettings[i];
                }

            }
        }

        internal List<bool> GetHostStoryRemixSettings()
        {
            List<bool> configurables = new List<bool>();

            if (ModManager.MMF)
            {

                configurables.Add(MoreSlugcats.MMF.cfgAlphaRedLizards._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgBreathTimeVisualIndicator._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgClearerDeathGradients._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgClimbingGrip._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgCreatureSense._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgDeerBehavior._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgDisableGateKarma._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgDisableScreenShake._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgExtraLizardSounds._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgExtraTutorials._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgFasterShelterOpen._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgFastMapReveal._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgFreeSwimBoosts._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgGlobalMonkGates._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgGraspWiggling._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgHideRainMeterNoThreat._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgHunterBackspearProtect._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgHunterBatflyAutograb._typedValue);
                //configurables.Add(MoreSlugcats.MMF.cfgHunterBonusCycles);
                //configurables.Add(MoreSlugcats.MMF.cfgHunterCycles);
                configurables.Add(MoreSlugcats.MMF.cfgIncreaseStuns._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgJetfishItemProtection._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgKeyItemPassaging._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgKeyItemTracking._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgLargeHologramLight._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgLoadingScreenTips._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgMonkBreathTime._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgNewDynamicDifficulty._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgNoArenaFleeing._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgNoRandomCycles._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgOldTongue._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgQuieterGates._typedValue);
                // configurables.Add(MoreSlugcats.MMF.cfgRainTimeMultiplier);
                configurables.Add(MoreSlugcats.MMF.cfgSafeCentipedes._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgSandboxItemStems._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgScavengerKillSquadDelay._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgShowUnderwaterShortcuts._typedValue);
                //configurables.Add(MoreSlugcats.MMF.cfgSlowTimeFactor);
                configurables.Add(MoreSlugcats.MMF.cfgSpeedrunTimer._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgSurvivorPassageNotRequired._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgSwimBreathLeniency._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgThreatMusicPulse._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgTickTock._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgUpwardsSpearThrow._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgVanillaExploits._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgVulnerableJellyfish._typedValue);
                configurables.Add(MoreSlugcats.MMF.cfgWallpounce._typedValue);

                return configurables;
            }
            return configurables;
        }


    }
}
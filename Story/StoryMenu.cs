using Menu;
using Menu.Remix;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RainMeadow
{
    public class StoryMenu : SmartMenu, SelectOneButton.SelectOneButtonOwner, CheckBox.IOwnCheckBox
    {
        private readonly RainEffect rainEffect;

        private StoryGameMode gameMode;

        private EventfulHoldButton hostStartButton;
        private EventfulHoldButton clientWaitingButton;

        private EventfulBigArrowButton prevButton;
        private EventfulBigArrowButton nextButton;
        private SimplerButton backButton;
        private PlayerInfo[] players;

        private SlugcatSelectMenu ssm;
        private SlugcatSelectMenu.SlugcatPage sp;
        private SlugcatCustomization personaSettings;

        private List<SlugcatSelectMenu.SlugcatPage> characterPages;
        private EventfulSelectOneButton[] playerButtons = new EventfulSelectOneButton[0];
        int skinIndex;
        private OpTinyColorPicker bodyColorPicker;
        private OpTinyColorPicker eyeColorPicker;
        private MenuLabel campaignContainer;
        private CheckBox resetSaveCheckbox;
        private CheckBox clientWantsToOverwriteSave;

        private bool resetSave;

        public static List<string> nonCampaignSlugcats = new List<string> { "Night", "Inv", "Slugpup", "MeadowOnline", "MeadowOnlineRemote" };

        public static List<string> nonGameplayRemixSettings = new List<string> { "cfgSpeedrunTimer", "cfgHideRainMeterNoThreat", "cfgLoadingScreenTips", "cfgExtraTutorials", "cfgClearerDeathGradients", "cfgShowUnderwaterShortcuts", "cfgBreathTimeVisualIndicator", "cfgCreatureSense", "cfgTickTock", "cfgFastMapReveal", "cfgThreatMusicPulse", "cfgExtraLizardSounds", "cfgQuieterGates", "cfgDisableScreenShake", "cfgHunterBatflyAutograb", "cfgNoMoreTinnitus" };

        private SlugcatStats.Name customSelectedSlugcat = SlugcatStats.Name.White;

        public override MenuScene.SceneID GetScene => null;
        public StoryMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.StoryMenu)
        {
            RainMeadow.DebugMe();
            this.backTarget = RainMeadow.Ext_ProcessID.LobbySelectMenu;

            this.rainEffect = new RainEffect(this, this.pages[0]);
            this.pages[0].subObjects.Add(this.rainEffect);
            this.rainEffect.rainFade = 0.3f;
            this.characterPages = new List<SlugcatSelectMenu.SlugcatPage>();

            gameMode = (StoryGameMode)OnlineManager.lobby.gameMode;

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

            gameMode.currentCampaign = ssm.slugcatPages[ssm.slugcatPageIndex].slugcatNumber;
            // Setup host / client buttons & general view

            SetupMenuItems();

            if (OnlineManager.lobby.isOwner)
            {
                gameMode.saveToDisk = true;
                SetupHostMenu();
            }
            else
            {
                SetupClientMenu();
                if (RainMeadow.rainMeadowOptions.SlugcatCustomToggle.Value)
                {
                    CustomSlugcatSetup();
                }
            }

            SteamSetup();
            SetupCharacterCustomization();
            UpdateCharacterUI();

            // Grab Host Remix Settings
            if (OnlineManager.lobby.isOwner) // ModManager.MMF should be in the check but serializing a nullable dictionary is not a thing at the moment so I'm cheating inside GetHostBoolStoryRemixSettings().
            {
                var hostSettings = GetHostBoolStoryRemixSettings();
                gameMode.storyBoolRemixSettings = hostSettings.hostBoolSettings;
                gameMode.storyFloatRemixSettings = hostSettings.hostFloatSettings;
                gameMode.storyIntRemixSettings = hostSettings.hostIntSettings;
            }

            BindSettings();
            SanitizeStoryClientSettings(gameMode.storyClientData);
            SanitizeStoryGameMode(gameMode);

            MatchmakingManager.instance.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;
        }

        private void SanitizeStoryClientSettings(StoryClientSettingsData clientSettings)
        {
            clientSettings.readyForWin = false;
            clientSettings.isDead = false;
        }

        private void SanitizeStoryGameMode(StoryGameMode gameMode)
        {
            gameMode.isInGame = false;
            gameMode.changedRegions = false;
            gameMode.didStartCycle = false;
            gameMode.defaultDenPos = null;
            gameMode.ghostsTalkedTo = new();
            gameMode.consumedItems = new();
            gameMode.myLastDenPos = null;
            gameMode.hasSheltered = false;
        }

        private void SetupHostMenu()
        {
            this.hostStartButton = new EventfulHoldButton(this, this.pages[0], base.Translate("ENTER"), new Vector2(683f, 85f), 40f);
            this.hostStartButton.OnClick += (_) => { StartGame(); };
            hostStartButton.buttonBehav.greyedOut = false;
            this.pages[0].subObjects.Add(this.hostStartButton);

            resetSaveCheckbox = new CheckBox(this, mainPage, this, new Vector2(903, 30f), 70f, Translate("Reset Save"), "RESETSAVE", false);
            this.pages[0].subObjects.Add(resetSaveCheckbox);

            // Previous
            this.prevButton = new EventfulBigArrowButton(this, this.pages[0], new Vector2(345f, 50f), -1);
            this.prevButton.OnClick += (_) =>
            {
                if (!RainMeadow.rainMeadowOptions.SlugcatCustomToggle.Value)
                { // I don't want to choose unstable slugcats
                    return;
                }

                ssm.quedSideInput = Math.Max(-3, ssm.quedSideInput - 1);
                base.PlaySound(SoundID.MENU_Next_Slugcat);
                var index = ssm.slugcatPageIndex - 1 < 0 ? ssm.slugcatPages.Count - 1 : ssm.slugcatPageIndex - 1;
                gameMode.currentCampaign = ssm.slugcatPages[index].slugcatNumber;
                UpdateStartButton();
            };
            this.pages[0].subObjects.Add(this.prevButton);

            // Next
            this.nextButton = new EventfulBigArrowButton(this, this.pages[0], new Vector2(985f, 50f), 1);
            this.nextButton.OnClick += (_) =>
            {
                if (!RainMeadow.rainMeadowOptions.SlugcatCustomToggle.Value)
                {
                    return;
                }
                ssm.quedSideInput = Math.Min(3, ssm.quedSideInput + 1);
                base.PlaySound(SoundID.MENU_Next_Slugcat);
                var index = ssm.slugcatPageIndex + 1 >= ssm.slugcatPages.Count ? 0 : ssm.slugcatPageIndex + 1;
                gameMode.currentCampaign = ssm.slugcatPages[index].slugcatNumber;
                UpdateStartButton();
            };
            this.pages[0].subObjects.Add(this.nextButton);

            UpdateStartButton();
        }

        private void UpdateStartButton()
        {
            if (!manager.rainWorld.progression.IsThereASavedGame(gameMode.currentCampaign))
            {
                hostStartButton.menuLabel.text = "NEW GAME";
            }
            else
            {
                hostStartButton.menuLabel.text = "CONTINUE";
            }
        }

        private void SetupClientMenu()
        {
            campaignContainer = new MenuLabel(this, mainPage, this.Translate(gameMode.currentCampaign.value), new Vector2(583f, sp.imagePos.y - 268f), new Vector2(200f, 30f), true);

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
            clientWaitingButton.buttonBehav.greyedOut = !(gameMode.isInGame && !gameMode.changedRegions);

            this.pages[0].subObjects.Add(this.clientWaitingButton);

            clientWantsToOverwriteSave = new CheckBox(this, mainPage, this, new Vector2(907, 30f), 70f, Translate("Overwrite save progress"), "OVERWRITECLIENTSAVE", true);

            this.pages[0].subObjects.Add(clientWantsToOverwriteSave);
        }

        private void StartGame()
        {
            RainMeadow.DebugMe();
            if (!OnlineManager.lobby.isOwner) // I'm a client
            {
                if (ModManager.MMF)
                {
                    SetClientStoryRemixSettings(gameMode.storyBoolRemixSettings, gameMode.storyFloatRemixSettings, gameMode.storyIntRemixSettings); // Set client remix settings to Host's on StartGame()
                }
                if (!RainMeadow.rainMeadowOptions.SlugcatCustomToggle.Value) // I'm a client and I want to match the hosts
                {
                    personaSettings.playingAs = gameMode.currentCampaign;
                }
                else // I'm a client and I want my own Slugcat
                {
                    personaSettings.playingAs = customSelectedSlugcat;

                }
            }
            else //I'm the host
            {
                personaSettings.playingAs = ssm.slugcatPages[ssm.slugcatPageIndex].slugcatNumber;
                RainMeadow.Debug("CURRENT CAMPAIGN: " + GetCurrentCampaignName());
            }

            manager.arenaSitting = null;
            if (resetSave)
            {
                manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;
            }
            else
            {
                manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;
            }
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

            if (OnlineManager.lobby.isOwner && hostStartButton is not null)
            {
                hostStartButton.buttonBehav.greyedOut = OnlineManager.lobby.clientSettings.Values.Any(cs => cs.inGame);
            }
            else
            {
                campaignContainer.text = $"Current Campaign: The {GetCurrentCampaignName()}";
                clientWaitingButton.buttonBehav.greyedOut = !(gameMode.isInGame && !gameMode.changedRegions);
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
            if (manager.upcomingProcess != ProcessManager.ProcessID.Game) // if join on sleep/deathscreen this needs to be added here as well
            {
                OnlineManager.LeaveLobby();
            }
            RainMeadow.rainMeadowOptions._SaveConfigFile(); // save colors
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

            bodyColorPicker = new OpTinyColorPicker(this, new Vector2(1094, 553), RainMeadow.rainMeadowOptions.BodyColor.Value);
            var wrapper = new UIelementWrapper(this.tabWrapper, bodyColorPicker);
            bodyColorPicker.OnValueChangedEvent += Colorpicker_OnValueChangedEvent;

            eyeColorPicker = new OpTinyColorPicker(this, new Vector2(1094, 500), RainMeadow.rainMeadowOptions.EyeColor.Value);
            var wrapper2 = new UIelementWrapper(this.tabWrapper, eyeColorPicker);
            eyeColorPicker.OnValueChangedEvent += Colorpicker_OnValueChangedEvent;
        }

        private void SetupMenuItems()
        {
            // Music
            this.mySoundLoopID = SoundID.MENU_Main_Menu_LOOP;

            // Player lobby label
            this.pages[0].subObjects.Add(new MenuLabel(this, mainPage, this.Translate("LOBBY"), new Vector2(194, 553), new(110, 30), true));

            if (RainMeadow.rainMeadowOptions.SlugcatCustomToggle.Value && !OnlineManager.lobby.isOwner)
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
                var btn = new SimplerButton(this, mainPage, SlugcatStats.getSlugcatName(slug), new Vector2(394, 515) - i * new Vector2(0, 38), new Vector2(110, 30));
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

        public string GetCurrentCampaignName()
        {
            return SlugcatStats.getSlugcatName(gameMode.currentCampaign);
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

        private void BindSettings()
        {
            this.personaSettings = (OnlineManager.lobby.gameMode as StoryGameMode).avatarSettings;
            personaSettings.playingAs = ssm.slugcatPages[ssm.slugcatPageIndex].slugcatNumber;
            personaSettings.bodyColor = Extensions.SafeColorRange(RainMeadow.rainMeadowOptions.BodyColor.Value);
            personaSettings.eyeColor = Extensions.SafeColorRange(RainMeadow.rainMeadowOptions.EyeColor.Value);
        }

        private void Colorpicker_OnValueChangedEvent()
        {
            RainMeadow.rainMeadowOptions.BodyColor.Value = personaSettings.bodyColor = Extensions.SafeColorRange(bodyColorPicker.valuecolor);
            RainMeadow.rainMeadowOptions.EyeColor.Value = personaSettings.eyeColor = Extensions.SafeColorRange(eyeColorPicker.valuecolor);
        }

        private List<SlugcatStats.Name> AllSlugcats()
        {
            var filteredList = new List<SlugcatStats.Name>();
            foreach (var name in SlugcatStats.Name.values.entries.Except(nonCampaignSlugcats))
            {
                if (ExtEnumBase.TryParse(typeof(SlugcatStats.Name), name, false, out var rawEnumBase))
                {
                    filteredList.Add((SlugcatStats.Name)rawEnumBase);
                }
            }
            return filteredList;
        }

        internal void SetClientStoryRemixSettings(Dictionary<string, bool> hostBoolRemixSettings, Dictionary<string, float> hostFloatRemixSettings, Dictionary<string, int> hostIntRemixSettings)
        {

            Type type = typeof(MoreSlugcats.MMF);

            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            var sortedFields = fields.OrderBy(f => f.Name);

            foreach (FieldInfo field in sortedFields)
            {
                var reflectedValue = field.GetValue(null);
                if (reflectedValue is Configurable<bool> boolOption)
                {
                    for (int i = 0; i < hostBoolRemixSettings.Count; i++)
                    {
                        if (field.Name == hostBoolRemixSettings.Keys.ElementAt(i) && boolOption._typedValue != hostBoolRemixSettings.Values.ElementAt(i))
                        {
                            RainMeadow.Debug($"Remix Key: {field.Name} with value {boolOption._typedValue} does not match host's, setting to {hostBoolRemixSettings.Values.ElementAt(i)}");
                            boolOption._typedValue = hostBoolRemixSettings.Values.ElementAt(i);

                        }
                    }
                }

                if (reflectedValue is Configurable<float> floatOption)
                {
                    for (int i = 0; i < hostFloatRemixSettings.Count; i++)
                    {
                        if (field.Name == hostFloatRemixSettings.Keys.ElementAt(i) && floatOption._typedValue != hostFloatRemixSettings.Values.ElementAt(i))
                        {
                            RainMeadow.Debug($"Remix Key: {field.Name} with value {floatOption._typedValue} does not match host's, setting to {hostFloatRemixSettings.Values.ElementAt(i)}");
                            floatOption._typedValue = hostFloatRemixSettings.Values.ElementAt(i);

                        }
                    }
                }

                if (reflectedValue is Configurable<int> intOption)
                {
                    for (int i = 0; i < hostIntRemixSettings.Count; i++)
                    {

                        if (field.Name == hostIntRemixSettings.Keys.ElementAt(i) && intOption._typedValue != hostIntRemixSettings.Values.ElementAt(i))
                        {
                            RainMeadow.Debug($"Remix Key: {field.Name} with value {intOption._typedValue} does not match host's, setting to {hostIntRemixSettings.Values.ElementAt(i)}");
                            intOption._typedValue = hostIntRemixSettings.Values.ElementAt(i);

                        }
                    }
                }
            }
        }

        internal (Dictionary<string, bool> hostBoolSettings, Dictionary<string, float> hostFloatSettings, Dictionary<string, int> hostIntSettings) GetHostBoolStoryRemixSettings()
        {
            Dictionary<string, bool> configurableBools = new Dictionary<string, bool>();
            Dictionary<string, float> configurableFloats = new Dictionary<string, float>();
            Dictionary<string, int> configurableInts = new Dictionary<string, int>();

            if (ModManager.MMF)
            {
                Type type = typeof(MoreSlugcats.MMF);

                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
                var sortedFields = fields.OrderBy(f => f.Name);

                foreach (FieldInfo field in sortedFields)
                {
                    if (nonGameplayRemixSettings.Contains(field.Name)) continue;
                    var reflectedValue = field.GetValue(null);
                    if (reflectedValue is Configurable<bool> boolOption)
                    {
                        configurableBools.Add(field.Name, boolOption._typedValue);
                    }

                    if (reflectedValue is Configurable<float> floatOption)
                    {
                        configurableFloats.Add(field.Name, floatOption._typedValue);
                    }

                    if (reflectedValue is Configurable<int> intOption)
                    {
                        configurableInts.Add(field.Name, intOption._typedValue);
                    }
                }
                return (configurableBools, configurableFloats, configurableInts);
            }
            return (configurableBools, configurableFloats, configurableInts);
        }

        public bool GetChecked(CheckBox box)
        {
            string idstring = box.IDString;
            if (idstring != null)
            {
                if (idstring == "RESETSAVE")
                {
                    return resetSave;
                }
                if (idstring == "OVERWRITECLIENTSAVE")
                {
                    return gameMode.saveToDisk;
                }
            }
            return false;
        }
        public void SetChecked(CheckBox box, bool c)
        {
            string idstring = box.IDString;
            if (idstring != null)
            {
                if (idstring == "RESETSAVE")
                {
                    resetSave = !resetSave;
                }

                if (idstring == "OVERWRITECLIENTSAVE")
                {
                    gameMode.saveToDisk = !gameMode.saveToDisk;
                }
            }
        }
    }
}

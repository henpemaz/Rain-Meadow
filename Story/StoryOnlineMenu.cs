using HarmonyLib;
using Menu;
using Menu.Remix.MixedUI;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.Linq;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public class StoryOnlineMenu : SlugcatSelectMenu, IChatSubscriber
    {
        private CheckBox clientWantsToOverwriteSave;
        private CheckBox friendlyFire;
        private CheckBox reqCampaignSlug;
        private MenuLabel? lobbyLabel, slugcatLabel;
        private ButtonScroller? playerScrollBox;
        private StoryMenuSlugcatSelector? slugcatSelector;
        private SlugcatCustomization personaSettings;
        private SlugcatStats.Name[] selectableSlugcats;
        private SlugcatStats.Name? currentSlugcat, playerSelectedSlugcat;
        private StoryGameMode storyGameMode;
        private MenuLabel onlineDifficultyLabel;
        private Vector2 restartCheckboxPos;
        
        //Chat constants
        private const int maxVisibleMessages = 13;
        //Chat variables
        private List<MenuObject> chatSubObjects = [];
        private List<(string, string)> chatLog = [];
        private int currentLogIndex = 0;
        private bool isChatToggled = false;
        private ChatTextBox chatTextBox;
        private Vector2 chatTextBoxPos;

        public SlugcatStats.Name[] SelectableSlugcats
        {
            get
            {
                SetupSelectableSlugcats();
                return selectableSlugcats;
            }
        }
        public SlugcatStats.Name PlayerSelectedSlugcat
        {
            get
            {
                return playerSelectedSlugcat ?? storyGameMode.currentCampaign;
            }
            set
            {
                playerSelectedSlugcat = value == slugcatColorOrder[slugcatPageIndex]? null : value;
                CurrentSlugcat = PlayerSelectedSlugcat;
            }
        }
        public SlugcatStats.Name CurrentSlugcat
        {
            get
            {
                return currentSlugcat ?? slugcatColorOrder[slugcatPageIndex];
            }
            set
            {
                if (currentSlugcat != value)
                {
                    RemoveColorButtons();
                    currentSlugcat = value;
                    UpdateUponChangingSlugcat(currentSlugcat);
                }
            }
        }
        public static int MaxVisibleOnList => 8;
        public static float ButtonSpacingOffset => 8;
        public static float ButtonSizeWithSpacing => ButtonSize + ButtonSpacingOffset;
        public static float ButtonSize => 30;


        public StoryOnlineMenu(ProcessManager manager) : base(manager)
        {
            SetupSelectableSlugcats();
            ID = OnlineManager.lobby.gameMode.MenuProcessId();
            storyGameMode = (StoryGameMode)OnlineManager.lobby.gameMode;
            storyGameMode.Sanitize();
            storyGameMode.currentCampaign = slugcatPages[slugcatPageIndex].slugcatNumber;
            restartCheckboxPos = restartCheckbox.pos;       
            RemoveExcessStoryObjects();
            ModifyExistingMenuItems();

            if (OnlineManager.lobby.isOwner)
            {
                storyGameMode.requireCampaignSlugcat = false; // Default option is in remix menu.
                storyGameMode.saveToDisk = true;
            }
            else
            {
                SetupClientOptions();
            }

            // HACK: force-register MMFEnums.SliderID because god is dead and we have killed them
            // turns out god not dead
            //MoreSlugcats.MMFEnums.SliderID.RegisterValues();

            SetupOnlineCustomization();

            SetupOnlineMenuItems();
            UpdatePlayerList();

            MatchmakingManager.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;

            ChatTextBox.OnShutDownRequest += ResetChatInput;
            ChatLogManager.Subscribe(this);
        }

        public void SetupSelectableSlugcats() {
            if (selectableSlugcats == null) {
                var SelectableSlugcatsEnumerable = slugcatColorOrder.AsEnumerable();
                if (ModManager.MSC) {
                    if (!SelectableSlugcatsEnumerable.Contains(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup)) {
                        SelectableSlugcatsEnumerable = SelectableSlugcatsEnumerable.Append(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup);
                    }
                }
                selectableSlugcats = SelectableSlugcatsEnumerable.ToArray();
            }
        }   

        public new void StartGame(SlugcatStats.Name storyGameCharacter)
        {
            if (OnlineManager.lobby.isOwner)
            {
                storyGameMode.currentCampaign = storyGameCharacter;
            }
            personaSettings.playingAs = storyGameMode.requireCampaignSlugcat ? storyGameMode.currentCampaign : PlayerSelectedSlugcat; //double check just incase

            // TODO: figure out how to reuse vanilla StartGame
            // * override singleplayer custom colours
            // * fix intro cutscenes messing with resource acquisition
            // ? how to deal with statistics screen (not supposed to continue, we should require wipe)
            personaSettings.currentColors = this.manager.rainWorld.progression.GetCustomColors(personaSettings.playingAs); //abt colors, color config updates to campaign when required campaign is on. Client side, the host still needs to be in the menu to update it so they will notice the color config update
            manager.arenaSitting = null;

            if ((OnlineManager.lobby.isOwner && restartChecked) || (!OnlineManager.lobby.isOwner && clientWantsToOverwriteSave.Checked))
            {
                manager.rainWorld.progression.WipeSaveState(storyGameMode.currentCampaign);
                manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;
            }

            else
            {
                manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;
            }
            manager.rainWorld.progression.ClearOutSaveStateFromMemory();
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }

        public override void Update()
        {
            if (ChatTextBox.blockInput)
            {
                ChatTextBox.blockInput = false;
                if ((RWInput.CheckPauseButton(0) || Input.GetKeyDown(KeyCode.Escape)) && !lastPauseButton)
                {
                    PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                    ToggleChat(false);
                    lastPauseButton = true;
                }
                ChatTextBox.blockInput = true;
            }
            base.Update();

            if (this.isChatToggled)
            {
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    if (currentLogIndex < chatLog.Count - 1)
                    {
                        currentLogIndex++;
                        UpdateLogDisplay();
                    }
                }
                else if (Input.GetKey(KeyCode.DownArrow))
                {
                    if (currentLogIndex > 0)
                    {
                        currentLogIndex--;
                        UpdateLogDisplay();
                    }
                }
            }

            if (OnlineManager.lobby == null) return;
            if (OnlineManager.lobby.isOwner)
            {
                storyGameMode.currentCampaign = slugcatPages[slugcatPageIndex].slugcatNumber;
                storyGameMode.region = CurrentRegion();
                if (startButton != null)
                {
                    startButton.buttonBehav.greyedOut = OnlineManager.lobby.clientSettings.Values.Any(cs => cs.inGame);
                }

            }
            else
            {
                if (onlineDifficultyLabel == null)
                {
                    onlineDifficultyLabel = new MenuLabel(this, pages[0], $"{GetCurrentCampaignName()}", new Vector2(startButton.pos.x - 100f, startButton.pos.y + 100f), new Vector2(200f, 30f), bigText: true);
                    onlineDifficultyLabel.label.alignment = FLabelAlignment.Center;
                    onlineDifficultyLabel.label.alpha = 0.5f;
                    pages[0].subObjects.Add(onlineDifficultyLabel);
                }
                if (startButton != null)
                {
                    startButton.buttonBehav.greyedOut = !storyGameMode.canJoinGame;
                }
                if (onlineDifficultyLabel != null)
                {
                    onlineDifficultyLabel.text = GetCurrentCampaignName() + (string.IsNullOrEmpty(storyGameMode.region) ? Translate(" - New Game") : " - " + Translate(storyGameMode.region));
                }
            }
            if (storyGameMode.requireCampaignSlugcat)
            {
                RemoveSlugcatList();
                CurrentSlugcat = storyGameMode.currentCampaign;
            }
            else
            {
                SetupSlugcatList();
                CurrentSlugcat = PlayerSelectedSlugcat;
            }
            if (slugcatSelector != null)
            {
                slugcatSelector.Slug = CurrentSlugcat;
            }

        }

        public override void ShutDownProcess()
        {
            this.isChatToggled = false;
            ResetChatInput(); //ensure chat input is properly shutdown
            ChatTextBox.OnShutDownRequest -= ResetChatInput;
            ChatLogManager.Unsubscribe(this);

            RainMeadow.DebugMe();
            if (manager.upcomingProcess != ProcessManager.ProcessID.Game) // if join on sleep/deathscreen this needs to be added here as well
            {
                OnlineManager.LeaveLobby();
            }
            RainMeadow.rainMeadowOptions._SaveConfigFile(); //im just gonna allow this for now, since idk updates in slider set value
            base.ShutDownProcess();
        }
        public override string UpdateInfoText()
        {
            return selectedObject is IHaveADescription descObj ? descObj.Description : base.UpdateInfoText();
        }
        private void UpdatePlayerList()
        {
            playerScrollBox?.RemoveAllButtons(false);
            if (playerScrollBox == null)
            {
                playerScrollBox = new(this, pages[0], new(194, 553 - 30 - ButtonScroller.CalculateHeightBasedOnAmtOfButtons(MaxVisibleOnList, ButtonSize, ButtonSpacingOffset)), MaxVisibleOnList, 200, ButtonSize, ButtonSpacingOffset);
                pages[0].subObjects.Add(playerScrollBox);
            }
            foreach (OnlinePlayer player in OnlineManager.players)
            {
                StoryMenuPlayerButton playerButton = new(this, playerScrollBox, player, OnlineManager.lobby.isOwner && player != OnlineManager.lobby.owner);
                playerScrollBox.AddScrollObjects(playerButton);
            }
            playerScrollBox.ConstrainScroll();

        }

        private void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
        {
            if (RainMeadow.isStoryMode(out var _))
            {
                UpdatePlayerList();
            }
        }

        public string GetCurrentCampaignName() => Translate("Current Campaign: ") + Translate(SlugcatStats.getSlugcatName(storyGameMode.currentCampaign));

        private string CurrentRegion()
        {
            try
            {
                var shelterName = saveGameData[storyGameMode.currentCampaign]?.shelterName;
                if (shelterName != null && shelterName.Length > 2)
                {
                    var s = Region.GetRegionFullName(shelterName.Substring(0, 2), storyGameMode.currentCampaign);
                    if (s == "Unknown Region")
                    {
                        // watcher regions
                        if (shelterName.Length > 4)
                        {
                            return Region.GetRegionFullName(shelterName.Substring(0, 4), storyGameMode.currentCampaign);
                        }
                        return s;
                    }
                }
            }
            catch (Exception e)
            {
                RainMeadow.Debug($"Error getting region name: {e}");
            }
            return "";
        }

        private void SetupSlugcatList()
        {
            Vector2 pos = new(394, 553);
            if (slugcatLabel == null)
            {
                slugcatLabel = new(this, pages[0], Translate("Selected Slugcat"), pos, new(110, 30), true);
                pages[0].subObjects.Add(slugcatLabel);
            }
            if (slugcatSelector == null)
            {
                //first player button is 30 pos below size of list. and list top part is 30 below the title. Plus
                slugcatSelector = new(this, pages[0], new(pos.x, pos.y - (ButtonSize * 2)), MaxVisibleOnList, ButtonSpacingOffset, CurrentSlugcat, GetSlugcatSelectionButtons);
                pages[0].subObjects.Add(slugcatSelector);
            }

        }
        private void RemoveSlugcatList()
        {
            pages[0].ClearMenuObject(ref slugcatLabel);
            pages[0].ClearMenuObject(ref slugcatSelector);
        }

        private void SetupOnlineCustomization()
        {
            personaSettings = storyGameMode.avatarSettings;
        }

        private void RemoveExcessStoryObjects()
        {
            if (!OnlineManager.lobby.isOwner)
            {
                StoryMenuHelpers.RemoveMenuObjects(restartCheckbox, nextButton, prevButton);

                foreach (var page in slugcatPages)
                {
                    switch (page)
                    {
                        case SlugcatPageContinue continuePage:
                            StoryMenuHelpers.RemoveMenuObjects(continuePage.regionLabel);
                            foreach (var part in continuePage.hud.parts.Where(x => x is HUD.KarmaMeter or HUD.FoodMeter).ToList())
                            {
                                part.slatedForDeletion = true;
                                part.ClearSprites();
                                continuePage.hud.parts.Remove(part);
                            }
                            break;
                        case SlugcatPageNewGame newPage:
                            StoryMenuHelpers.RemoveMenuObjects(newPage.infoLabel, newPage.difficultyLabel);
                            break;
                    }
                }
            }
        }

        private void SetupOnlineMenuItems()
        {
            // Player lobby label
            lobbyLabel = new MenuLabel(this, pages[0], Translate("LOBBY"), new Vector2(194, 553), new(110, 30), true);
            pages[0].subObjects.Add(lobbyLabel);

            var invite = new SimplerButton(this, pages[0], Translate("Invite Friends"), new(nextButton.pos.x + 80f, 50f), new(110, 35));
            invite.OnClick += (_) => MatchmakingManager.currentInstance.OpenInvitationOverlay();
            pages[0].subObjects.Add(invite);

            this.chatTextBoxPos = new Vector2(this.manager.rainWorld.options.ScreenSize.x * 0.001f + (1366f - this.manager.rainWorld.options.ScreenSize.x) / 2f, 0);
            var toggleChat = new SimplerSymbolButton(this, pages[0], "Kill_Slugcat", "", this.chatTextBoxPos);
            toggleChat.OnClick += (_) =>
            {
                ToggleChat(!this.isChatToggled);
                if (input.controllerType == Options.ControlSetup.Preset.KeyboardSinglePlayer)
                {
                    selectedObject = null;
                }
            };
            pages[0].subObjects.Add(toggleChat);

            var sameSpotOtherSide = restartCheckboxPos.x - startButton.pos.x;
            friendlyFire = new CheckBox(this, pages[0], this, new Vector2(startButton.pos.x - sameSpotOtherSide, restartCheckboxPos.y + 30), 70f, Translate("Friendly Fire"), "ONLINEFRIENDLYFIRE", false);
            reqCampaignSlug = new CheckBox(this, pages[0], this, new Vector2(startButton.pos.x - sameSpotOtherSide, restartCheckboxPos.y), 150f, Translate("Require Campaign Slugcat"), "CAMPAIGNSLUGONLY", false);
            if (!OnlineManager.lobby.isOwner)
            {
                friendlyFire.buttonBehav.greyedOut = true;
                reqCampaignSlug.buttonBehav.greyedOut = true;
            }
            pages[0].subObjects.Add(friendlyFire);
            pages[0].subObjects.Add(reqCampaignSlug);
        }

        public void ToggleChat(bool toggled)
        {
            this.isChatToggled = toggled;
            this.ResetChatInput();
            this.UpdateLogDisplay();
        }

        private void ModifyExistingMenuItems()
        {
            foreach (var obj in pages[0].subObjects) // unfortunate locally declared variable.
            {
                if (obj is SimpleButton button && button.signalText == "BACK")
                {
                    button.pos = new Vector2(prevButton.pos.x - 140f, 50f);
                }
            }
        }

        private void SetupClientOptions()
        {
            clientWantsToOverwriteSave = new CheckBox(this, pages[0], this, restartCheckboxPos, 70f, Translate("Match save"), "CLIENTSAVERESET", false);
            pages[0].subObjects.Add(clientWantsToOverwriteSave);
        }
        public StoryMenuSlugcatButton[] GetSlugcatSelectionButtons(StoryMenuSlugcatSelector slugcatSelector, ButtonScroller buttonScroller)
        {
            List<StoryMenuSlugcatButton> slugcatButtons = [];
            for (int i = 0; i < SelectableSlugcats.Length; i++)
            {
                if (SelectableSlugcats[i] != slugcatSelector.Slug)
                {
                    StoryMenuSlugcatButton storyMenuSlugcatButton = new(this, buttonScroller, SelectableSlugcats[i], (scug) =>
                    {
                        PlayerSelectedSlugcat = scug;
                        slugcatSelector.OpenCloseList(false, true, true);
                    });
                    slugcatButtons.Add(storyMenuSlugcatButton);
                }
            }
            return [.. slugcatButtons];
        }
        private void UpdateUponChangingSlugcat(SlugcatStats.Name scug)
        {
            if (colorsCheckbox != null)
            {
                colorsCheckbox.Checked = colorChecked; //this.IsCustomColorEnabled(scug); //automatically opens color interface if enabled
            }
        }

        public void AddMessage(string user, string message)
        {
            if (OnlineManager.lobby == null) return;
            if (OnlineManager.lobby.gameMode.mutedPlayers.Contains(user)) return;
            this.chatLog.Add((user, message));
            this.UpdateLogDisplay();
        }

        internal void ResetChatInput()
        {
            this.chatTextBox?.DelayedUnload(0.1f);
            pages[0].ClearMenuObject(ref this.chatTextBox);
            if (this.isChatToggled && this.chatTextBox is null)
            {
                this.chatTextBox = new ChatTextBox(this, pages[0], "", new Vector2(this.chatTextBoxPos.x + 24, 0), new(575, 30));
                pages[0].subObjects.Add(this.chatTextBox);
            }
        }

        internal void UpdateLogDisplay()
        {
            if (!this.isChatToggled)
            {
                var list = new List<MenuObject>();
                foreach (var e in chatSubObjects)
                {
                    e.RemoveSprites();
                    list.Add(e);
                }
                foreach (var e in list) pages[0].RemoveSubObject(e);
                chatSubObjects.Clear(); //do not keep gc stuff!
                return;
            }
            if (chatLog.Count > 0)
            {
                int startIndex = Mathf.Clamp(chatLog.Count - maxVisibleMessages - currentLogIndex, 0, chatLog.Count - maxVisibleMessages);
                var logsToRemove = new List<MenuObject>();

                // First, collect all the logs to remove
                foreach (var log in chatSubObjects)
                {
                    log.RemoveSprites();
                    logsToRemove.Add(log);
                }

                // Now remove the logs from the original collection
                foreach (var log in logsToRemove)
                {
                    chatSubObjects.Remove(log);
                    pages[0].RemoveSubObject(log);
                }

                ChatLogManager.UpdatePlayerColors();

                float yOffSet = 0;
                var visibleLog = chatLog.Skip(startIndex).Take(maxVisibleMessages);
                foreach (var (username, message) in visibleLog)
                {
                    if (username is null or "")
                    {
                        // system message
                        var messageLabel = new MenuLabel(this, pages[0], message,
                            new Vector2(1366f - manager.rainWorld.screenSize.x - 660f, 330f - yOffSet),
                            new Vector2(manager.rainWorld.screenSize.x, 30f), false);
                        messageLabel.label.alignment = FLabelAlignment.Left;
                        messageLabel.label.color = ChatLogManager.defaultSystemColor;
                        chatSubObjects.Add(messageLabel);
                        pages[0].subObjects.Add(messageLabel);
                    }
                    else
                    {
                        var color = ChatLogManager.GetDisplayPlayerColor(username);

                        var usernameLabel = new MenuLabel(this, pages[0], username,
                            new Vector2(1366f - manager.rainWorld.screenSize.x - 660f, 330f - yOffSet),
                            new Vector2(manager.rainWorld.screenSize.x, 30f), false);
                        usernameLabel.label.alignment = FLabelAlignment.Left;
                        usernameLabel.label.color = color;
                        chatSubObjects.Add(usernameLabel);
                        pages[0].subObjects.Add(usernameLabel);

                        var usernameWidth = LabelTest.GetWidth(usernameLabel.label.text);
                        var messageLabel = new MenuLabel(this, pages[0], $": {message}",
                            new Vector2(1366f - manager.rainWorld.screenSize.x - 660f + usernameWidth + 2f, 330f - yOffSet),
                            new Vector2(manager.rainWorld.screenSize.x, 30f), false);
                        messageLabel.label.alignment = FLabelAlignment.Left;
                        chatSubObjects.Add(messageLabel);
                        pages[0].subObjects.Add(messageLabel);
                    }
                    yOffSet += 20f;
                }
            }
        }
    }
}

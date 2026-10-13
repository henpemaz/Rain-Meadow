using Expedition;
using Menu;
using MoreSlugcats;

using RainMeadow.UI.Components;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

using static Menu.SlugcatSelectMenu;

namespace RainMeadow
{
    public partial class ExpeditionOnlineMenu : ExpeditionMenu
    {
        private SlugcatStats.Name[] selectableSlugcats;
        public SlugcatStats.Name?[] playerSelectedSlugcats;
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
                return playerSelectedSlugcats?[0] ?? ExpeditionGame.playableCharacters[currentSelection];
            }
            set
            {
                SetSelectedSlugcat(0, value);
            }
        }

        SimplerSymbolButton toggleChat;
        
        public static int MaxVisibleOnList => 8 - 3;
        public static float ButtonSpacingOffset => 8;
        public static float ButtonSizeWithSpacing => ButtonSize + ButtonSpacingOffset;
        public static float ButtonSize => 30;

        private ButtonScroller? playerScrollBox;
        private Vector2 playerScrollBoxPos;
        private int _currentPage = 1;
        
        private StoryMenuSlugcatSelector? slugcatSelector;

        private Vector2 lobbylabelPos;
        private ChatMenuBox chatMenuBox;
        private Vector2 chatTextBoxPos;

        private ScrollSymbolButton colorConfigButton;
        private Dialog colorConfigDialog;
        private MenuLabel colorConfigLabel;
        
        private CheckBox friendlyFire;
        private CheckBox reqCampaignSlug;
        private CheckBox syncSave;

        private bool _pagesMoving;

        void SetupOnlineMenuItens()
        {
            lobbylabelPos = new Vector2(rightAnchor - 160, 620);
            lobbyLabel = new MenuLabel(this, pages[_currentPage], Translate("LOBBY"), lobbylabelPos, new(110, 30), true);
            pages[_currentPage].subObjects.Add(lobbyLabel);

            this.chatTextBoxPos = new Vector2(leftAnchor, 0);
            toggleChat = new SimplerSymbolButton(this, pages[_currentPage], "Kill_Slugcat", "", this.chatTextBoxPos);
            toggleChat.OnClick += (_) =>
            {
                ToggleChat(!this.isChatToggled);
                if (input.controllerType == Options.ControlSetup.Preset.KeyboardSinglePlayer)
                {
                    selectedObject = null;
                }
            };
            pages[_currentPage].subObjects.Add(toggleChat);

            SetupColorMenu();
            SetupCheckbox();
        }

        private void UpdatePlayerList()
        {
            playerScrollBox?.RemoveAllButtons(false);
            if (playerScrollBox == null)
            {
                playerScrollBoxPos = new(rightAnchor - 160, 620 - 30 - ButtonScroller.CalculateHeightBasedOnAmtOfButtons(MaxVisibleOnList, ButtonSize, ButtonSpacingOffset));
                playerScrollBox = new(this, pages[_currentPage], playerScrollBoxPos, MaxVisibleOnList, 200, new(ButtonSize, ButtonSpacingOffset));
                pages[_currentPage].subObjects.Add(playerScrollBox);
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
            if (RainMeadow.isExpeditionMode(out var _))
            {
                UpdatePlayerList();
            }
        }
  
        float offscreen = 0;
        private void UpdateUI()
        {
            if (ModManager.JollyCoop && OnlineManager.lobby.isOwner)
            {
                expeditionGameMode.friendlyFire = manager.rainWorld.options.friendlyFire;
            }

            if (this.pagesMoving || _pagesMoving)
            {
                int usePagePos = (pagesMoving || !_pagesMoving) ? 1 : 0;

                if (_currentPage == 3) offscreen = Mathf.Lerp(offscreen, 1, .06f);
                else offscreen = Mathf.Lerp(offscreen, 0, .2f);

                Mathf.Clamp(offscreen, 0f, 1f);

                Vector2 pagePos = new Vector2(pages[_currentPage].pos.x + leftAnchor, 0) * usePagePos;
                Vector2 pageLastPos = new Vector2(pages[_currentPage].lastPos.x + leftAnchor, 0) * usePagePos;

                Vector2 makeOffscreen = new Vector2(300, 0) * offscreen;

                lobbyLabel.pos = lobbylabelPos - pagePos + makeOffscreen;
                lobbyLabel.lastPos = lobbylabelPos - pageLastPos + makeOffscreen;

                chatMenuBox?.pos = chatTextBoxPos + new Vector2(24, 0) - pagePos;
                chatMenuBox?.lastPos = chatTextBoxPos + new Vector2(24, 0) - pageLastPos;

                toggleChat.pos = chatTextBoxPos - pagePos;
                toggleChat.lastPos = chatTextBoxPos - pageLastPos;

                playerScrollBox.pos = playerScrollBoxPos - pagePos + makeOffscreen;
                playerScrollBox.lastPos = playerScrollBoxPos - pageLastPos + makeOffscreen;

                Vector2 slugcatLabelpos = new(leftAnchor + 70, 620);
                Vector2 slugcatSelectorpos = new(slugcatLabelpos.x, slugcatLabelpos.y - (ButtonSize * 2));

                slugcatLabel?.pos = slugcatLabelpos - pagePos - makeOffscreen;
                slugcatLabel?.lastPos = slugcatLabelpos - pageLastPos - makeOffscreen;
                slugcatSelector?.pos = slugcatSelectorpos - pagePos - makeOffscreen;
                slugcatSelector?.lastPos = slugcatSelectorpos - pageLastPos - makeOffscreen;

                float restartTextWidth = GetRestartTextWidth(base.CurrLang);
                
                Vector2 pos = new(rightAnchor - 110, 190 + 20);

                colorConfigButton?.pos = pos - pagePos + makeOffscreen;
                colorConfigButton?.lastPos = pos - pageLastPos + makeOffscreen;

                colorConfigLabel?.pos = pos + new Vector2(colorConfigButton.size.x / 2f, -20f) - pagePos + makeOffscreen;
                colorConfigLabel?.lastPos = pos + new Vector2(colorConfigButton.size.x / 2f, -20f) - pageLastPos + makeOffscreen;

                pos = new(rightAnchor - 60, 90);
                syncSave?.pos = pos - pagePos + makeOffscreen;
                syncSave?.lastPos = pos - pageLastPos + makeOffscreen;

                pos = new(rightAnchor -60, 60);
                friendlyFire.pos = pos - pagePos + makeOffscreen;
                friendlyFire.lastPos = pos - pageLastPos + makeOffscreen;

                pos = new(rightAnchor - 60, 30);
                reqCampaignSlug.pos = pos - pagePos + makeOffscreen;
                reqCampaignSlug.lastPos = pos - pageLastPos + makeOffscreen;

                _pagesMoving = pagesMoving;
            }            
        }
    
        void UpdateOnlinePage(int pageIndex)
        {
            _currentPage = pageIndex;

            pages[currentPage].ClearMenuObject(ref lobbyLabel);

            pages[currentPage].ClearMenuObject(ref toggleChat);

            playerScrollBox?.RemoveAllButtons(false);
            pages[currentPage].ClearMenuObject(ref playerScrollBox);

            pages[currentPage].ClearMenuObject(ref colorConfigButton);
            pages[currentPage].ClearMenuObject(ref colorConfigLabel);

            pages[currentPage].ClearMenuObject(ref friendlyFire);
            pages[currentPage].ClearMenuObject(ref reqCampaignSlug);
            pages[currentPage].ClearMenuObject(ref syncSave);

            if (!expeditionGameMode.requireCampaignSlugcat)
            {
                RemoveSlugcatList();
                SetupSlugcatList();
            }

            SetupOnlineMenuItens();
            UpdatePlayerList();
            ResetChatInput();
        }

        // custom scugs

        private void SetupSlugcatList()
        {
            Vector2 pos = new(leftAnchor + 70, 620);
            if (slugcatLabel == null)
            {
                slugcatLabel = new(this, pages[_currentPage], Translate("Selected Slugcat").Replace("<LINE>", "\n"), pos, new(110, 30), true);
                pages[_currentPage].subObjects.Add(slugcatLabel);
            }
            if (slugcatSelector == null)
            {
                slugcatSelector = new(this, pages[_currentPage], new(pos.x, pos.y - (ButtonSize * 2)), MaxVisibleOnList, ButtonSpacingOffset, PlayerSelectedSlugcat, GetSlugcatSelectionButtons);
                pages[_currentPage].subObjects.Add(slugcatSelector);
            }
            if (expeditionGameMode.preferredSlug != null)
            {
                SetSelectedSlugcat(0, expeditionGameMode.preferredSlug);
            }
        }

        private void RemoveSlugcatList()
        {
            pages[currentPage].ClearMenuObject(ref slugcatLabel);
            pages[currentPage].ClearMenuObject(ref slugcatSelector);
        }

        public void SetupSelectableSlugcats()
        {
            if (selectableSlugcats == null)
            {
                var SelectableSlugcatsEnumerable = ExpeditionGame.playableCharacters.AsEnumerable();//slugcatColorOrder.AsEnumerable();
                if (ModManager.MSC)
                {
                    if (!SelectableSlugcatsEnumerable.Contains(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel))
                    {
                        SelectableSlugcatsEnumerable = SelectableSlugcatsEnumerable.Append(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel);
                    }
                    if (!SelectableSlugcatsEnumerable.Contains(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup))
                    {
                        SelectableSlugcatsEnumerable = SelectableSlugcatsEnumerable.Append(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup);
                    }
                }
                if (ModManager.Watcher)
                {
                    if (!SelectableSlugcatsEnumerable.Contains(Watcher.WatcherEnums.SlugcatStatsName.Watcher))
                    {
                        SelectableSlugcatsEnumerable = SelectableSlugcatsEnumerable.Append(Watcher.WatcherEnums.SlugcatStatsName.Watcher);
                    }
                }
                selectableSlugcats = SelectableSlugcatsEnumerable.ToArray();
            }
        }

        public void SetSelectedSlugcat(int player, SlugcatStats.Name slugcat)
        {
            if ((playerSelectedSlugcats[player] != slugcat && playerSelectedSlugcats[player] != null) || (playerSelectedSlugcats[player] == null && ExpeditionGame.playableCharacters[currentSelection] != slugcat))
            {
                if (ModManager.JollyCoop)
                {
                    manager.rainWorld.options.jollyPlayerOptionsArray[player].playerClass = slugcat;
                }
                playerSelectedSlugcats[player] = slugcat == ExpeditionGame.playableCharacters[currentSelection] ? null : slugcat; // slugcatColorOrder[slugcatPageIndex] ? null : slugcat;
                expeditionGameMode.preferredSlug = slugcat;
            }
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
    }
    public partial class ExpeditionOnlineMenu : CheckBox.IOwnCheckBox
    {
        public bool colorChecked;
        public bool restartChecked;

        public HorizontalSlider hueSlider;
        public HorizontalSlider satSlider;
        public HorizontalSlider litSlider;

        public SimpleButton defaultColorButton;

        public int activeColorChooser;

        public void SetupColorMenu()
        {            
            if (ModManager.MMF && !ModManager.JollyCoop)
            {
                Vector2 pos = new(rightAnchor - 110, 190 + 20);

                float restartTextWidth = GetRestartTextWidth(base.CurrLang);
                Futile.atlasManager.LogAllElementNames();
                
                colorConfigButton = new(this, pages[_currentPage], "Meadow_Menu_BigColorBucket", "COLOR_SLUGCAT", pos + new Vector2(0, 0), new(45, 45));
                colorConfigLabel = new MenuLabel(this, pages[_currentPage], Translate("Custom colors"), pos + new Vector2(colorConfigButton.size.x / 2f, -20f), Vector2.zero, false, null);

                pages[_currentPage].subObjects.Add(colorConfigButton);
                pages[_currentPage].subObjects.Add(colorConfigLabel);
            }
        }

        public void OpenColorConfig(SlugcatStats.Name? slugcat)
        {
            if (!ModManager.MMF)
            {
                PlaySound(SoundID.MENU_Checkbox_Uncheck);
                colorConfigDialog = new DialogNotify(
                    this.LongTranslate("You cant color without Remix on!"),
                    new Vector2(500f, 200f),
                    manager, () =>
                    {
                        PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                    }
                );
                manager.ShowDialog(colorConfigDialog);
                return;
            }

            PlaySound(SoundID.MENU_Checkbox_Check);
            colorConfigDialog = new ColorMultipleSlugcatsDialog(
                manager, () =>
                {
                    PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                },
                selectableSlugcats.ToList(),
                slugcat
            );
            manager.ShowDialog(colorConfigDialog);
        }

        public void SetupCheckbox()
        {
            
            float restartTextWidth = GetRestartTextWidth(base.CurrLang);
            float restartTextOffset = GetRestartTextOffset(base.CurrLang);

            Vector2 pos = new(rightAnchor - 60, 0);
            if (!OnlineManager.lobby.isOwner)
            {
                syncSave = new CheckBox(this, pages[_currentPage], this, pos + new Vector2(0, 90), restartTextWidth, Translate("Sync Save"), "SYNCSAVE");
                syncSave.label.pos.x += restartTextWidth - syncSave.label.label.textRect.width - 5f;
                pages[_currentPage].subObjects.Add(syncSave);
            }

            friendlyFire = new CheckBox(this, pages[_currentPage], this, pos + new Vector2(0, 60), restartTextWidth, Translate("Friendly Fire"), "ONLINEFRIENDLYFIRE");
            friendlyFire.label.pos.x += restartTextWidth - friendlyFire.label.label.textRect.width - 5f;
           
            reqCampaignSlug = new CheckBox(this, pages[_currentPage], this, pos + new Vector2(0, 30), restartTextWidth, Translate("Require Campaign Slugcat"), "CAMPAIGNSLUGONLY");
            reqCampaignSlug.label.pos.x += restartTextWidth - reqCampaignSlug.label.label.textRect.width - 5f;
           
            if (!OnlineManager.lobby.isOwner)
            {
                friendlyFire.buttonBehav.greyedOut = true;
                reqCampaignSlug.buttonBehav.greyedOut = true;
            }

            pages[_currentPage].subObjects.Add(friendlyFire);
            pages[_currentPage].subObjects.Add(reqCampaignSlug);
        }

        public bool GetChecked(CheckBox box)
        {
            if (box.IDString == "ONLINEFRIENDLYFIRE")
            {
                return expeditionGameMode.friendlyFire;
            }
            if (box.IDString == "CAMPAIGNSLUGONLY")
            {
                return expeditionGameMode.requireCampaignSlugcat;
            }
            if (box.IDString == "SYNCSAVE")
            {
                return expeditionGameMode.saveToDisk;
            }
            return false;
        }

        public void SetChecked(CheckBox box, bool c)
        {
            if (box.IDString == "ONLINEFRIENDLYFIRE")
            {
                if (ModManager.JollyCoop) manager.rainWorld.options.friendlyFire = c;
                expeditionGameMode.friendlyFire = c;
                return;
            }
            if (box.IDString == "CAMPAIGNSLUGONLY")
            {
                expeditionGameMode.requireCampaignSlugcat = c;
                return;
            }
            if (box.IDString == "SYNCSAVE")
            {
                expeditionGameMode.saveToDisk = c;
                return;
            }
        }
    }
}

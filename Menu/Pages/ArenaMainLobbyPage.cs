using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using RainMeadow.UI.Components;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RainMeadow.UI.Components.TabContainer;
using static RainMeadow.UI.Components.OnlineSlugcatAbilitiesInterface;
using RainMeadow.UI.Interfaces;


namespace RainMeadow.UI.Pages;

public class ArenaMainLobbyPage : PositionedMenuObject, IDynamicBindHandler
{
    public SimplerButton readyButton;
    public SimplerButton? startButton;
    public SimplerSymbolButton arenaInfoButton, arenaGameStatsButton;
    public MenuLabel activeGameModeLabel, readyPlayerCounterLabel, playlistProgressLabel;
    public FSprite chatLobbyStateDivider;
    public TabContainer tabContainer;
    public ArenaLevelSelector levelSelector;
    public ChatMenuBox chatMenuBox;
    public OnlineArenaSettingsInferface arenaSettingsInterface;
    public OnlineSlugcatAbilitiesInterface? slugcatAbilitiesInterface;
    public PlayerDisplayer playerDisplayer;
    public Dialog? slugcatDialog;
    public Tab playListTab, matchSettingsTab;
    public Tab? slugabilitiesTab;


    public Dialog? dialog;
    public int painCatIndex, holdSlugcatBtnCounter;
    private ArenaOnlineGameMode Arena => (ArenaOnlineGameMode)OnlineManager.lobby.gameMode;
    public ArenaOnlineLobbyMenu? ArenaMenu => menu as ArenaOnlineLobbyMenu;

    public ArenaMainLobbyPage(Menu.Menu menu, MenuObject owner, Vector2 pos, string painCatName, int painCatIndex) : base(menu, owner, pos)
    {
        this.painCatIndex = painCatIndex;
        var scugslotsHint = UnityEngine.Random.Range(0, 21);


        readyButton = new SimplerButton(menu, this, Utils.Translate("READY?"), new Vector2(1056f, 50f), new Vector2(110f, 30f));
        readyButton.OnClick += btn =>
        {
            if (!RainMeadow.isArenaMode(out var _)) return;
            Arena.arenaClientSettings.ready = !Arena.arenaClientSettings.ready;
        };
        arenaGameStatsButton = new(menu, this, "Multiplayer_Bones", "", new(readyButton.pos.x + readyButton.size.x + 10, readyButton.pos.y))
        {
            description = menu.Translate("View Post-Game Stats"),
            size = new(30, 30)
        };
        arenaGameStatsButton.roundedRect.size = arenaGameStatsButton.size;
        arenaGameStatsButton.OnClick += _ => OpenGameStatsDialog();
        readyButton.description = Utils.Translate(scugslotsHint == 20 ? SlugcatSelector.slugcatSelectorHints[UnityEngine.Random.Range(0, SlugcatSelector.slugcatSelectorHints.Count)]: "Ready up to join the host when the match begins");
        chatMenuBox = new(menu, this, new(100f, 125f), new(300, 425));
        chatMenuBox.roundedRect.size.y = 475f;

        float chatRectPosSizeY = chatMenuBox.pos.y + chatMenuBox.roundedRect.size.y;
        activeGameModeLabel = new MenuLabel(menu, this, "", new Vector2(chatMenuBox.pos.x + chatMenuBox.size.x / 2, chatRectPosSizeY - 15), Vector2.zero, false);
        readyPlayerCounterLabel = new MenuLabel(menu, this, "", new Vector2(chatMenuBox.pos.x + chatMenuBox.size.x / 4, chatRectPosSizeY - 35), Vector2.zero, false);
        playlistProgressLabel = new MenuLabel(menu, this, "", new Vector2(chatMenuBox.pos.x + chatMenuBox.size.x / 4 * 3 - 20, chatRectPosSizeY - 35), Vector2.zero, false);

        chatLobbyStateDivider = new FSprite("pixel")
        {
            color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.VeryDarkGrey),
            scaleX = chatMenuBox.size.x - 40,
            scaleY = 2,
        };
        Container.AddChild(chatLobbyStateDivider);

        BuildPlayerDisplay();
        MatchmakingManager.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;
        arenaInfoButton = new(menu, this, "Meadow_Menu_SmallQuestionMark", "", new Vector2(chatMenuBox.pos.x + chatMenuBox.size.x / 2 - 12, playerDisplayer!.pos.y + playerDisplayer.scrollUpButton!.pos.y), "")
        {
            description = menu.Translate("View Current Mode Info"),
        };
        arenaInfoButton.OnClick += _ => OpenInfoDialog();

        tabContainer = new TabContainer(menu, this, new Vector2(470f, 125f), new Vector2(450, 475));
        playListTab = new(menu, tabContainer);
        playListTab.AddObjects(levelSelector = new ArenaLevelSelector(menu, playListTab, new Vector2(65f, 7.5f)));
        tabContainer.AddTab(playListTab, menu.Translate("Arena Playlist"));

        matchSettingsTab = new(menu, tabContainer);
        arenaSettingsInterface = new OnlineArenaSettingsInferface(menu, matchSettingsTab, new Vector2(120f, 0f), Arena.currentGameMode, [.. Arena.registeredGameModes.Keys.Select(v => new ListItem(v, menu.Translate(v)))]);
        arenaSettingsInterface.CallForSync();
        matchSettingsTab.AddObjects(arenaSettingsInterface);
        tabContainer.AddTab(matchSettingsTab, menu.Translate("Match Settings"));


        if (ShouldOpenSlugcatAbilitiesTab())
        {
            slugabilitiesTab = new(menu, tabContainer);
            slugcatAbilitiesInterface = new OnlineSlugcatAbilitiesInterface(menu, slugabilitiesTab, new(0,0), menu.Translate(painCatName));
            slugcatAbilitiesInterface.CallForSync();
            slugabilitiesTab.AddObjects(slugcatAbilitiesInterface); //the tab will be hidden at the start anyways so no need to call selectables update
            tabContainer.AddTab(slugabilitiesTab, menu.Translate("Slugcat Abilities"));
        }

        this.SafeAddSubobjects(readyButton, tabContainer, activeGameModeLabel, readyPlayerCounterLabel, playlistProgressLabel, chatMenuBox, arenaInfoButton, arenaGameStatsButton);

        menu.MutualVerticalButtonBind(chatMenuBox.chatTypingBox, arenaInfoButton);
    }
    public bool ShouldOpenSlugcatAbilitiesTab() => ModManager.MSC || ModManager.Watcher;
    public void BuildPlayerDisplay()
    {
        playerDisplayer = new PlayerDisplayer(menu, this, new Vector2(960f, 130f), [.. OnlineManager.players.OrderByDescending(x => x.isMe)], GetPlayerButton, 4, ArenaPlayerBox.DefaultSize.x, new(ArenaPlayerBox.DefaultSize.y, 0), new(ArenaPlayerSmallBox.DefaultSize.y, 10));
        subObjects.Add(playerDisplayer);
        playerDisplayer.CallForRefresh();
    }

    public void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
    {
        RainMeadow.DebugMe();
        playerDisplayer?.UpdatePlayerList([.. OnlineManager.players.OrderByDescending(x => x.isMe)]);
    }
    public ButtonScroller.IPartOfButtonScroller GetPlayerButton(PlayerDisplayer playerDisplay, bool isLargeDisplay, OnlinePlayer player, Vector2 pos)
    {
        if (isLargeDisplay)
        {
            ArenaPlayerBox playerBox = new(menu, playerDisplay, player, OnlineManager.lobby?.isOwner == true, pos); //buttons init prevents kick button if isMe
            return playerBox;
        }
        ArenaPlayerSmallBox playerSmallBox = new(menu, playerDisplay, player, OnlineManager.lobby?.isOwner == true, pos);
        return playerSmallBox;
    }
    public void OpenInfoDialog()
    {
        if (!RainMeadow.isArenaMode(out _)) return;
        menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
        dialog = Arena.externalArenaGameMode?.AddGameModeInfo(Arena, menu);
        menu.manager.ShowDialog(dialog);
    }
    public void OpenGameStatsDialog()
    {
        if (!RainMeadow.isArenaMode(out _)) return;
        menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
        dialog = Arena.externalArenaGameMode?.AddPostGameStatsFeed(Arena, menu);
        menu.manager.ShowDialog(dialog);
    }
    public void OpenColorConfig(SlugcatStats.Name? slugcat)
    {
        if (!ModManager.MMF)
        {
            menu.PlaySound(SoundID.MENU_Checkbox_Uncheck);
            dialog = new DialogNotify(menu.LongTranslate("You cant color without Remix on!"), new Vector2(500f, 200f), menu.manager, () => { menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed); });
            menu.manager.ShowDialog(dialog);
            return;
        }

        menu.PlaySound(SoundID.MENU_Checkbox_Check);
        dialog = new ColorMultipleSlugcatsDialog(menu.manager, () => { menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed); }, ArenaHelpers.allSlugcats, slugcat);
        menu.manager.ShowDialog(dialog);
    }
    public void OnTabButtonsCreated()
    {
        TabButtonsContainer tabBtnContainer = tabContainer.tabButtonContainer;
        var tabButtons = tabBtnContainer.activeTabButtons;
        for (int i = 0; i < tabBtnContainer.activeTabButtons.Count; i++)
        {
            if (i == 0)
                menu.MutualHorizontalButtonBind(chatMenuBox.messageScroller.scrollSlider, tabButtons[i].wrapper);
            else tabBtnContainer.activeTabButtons[i].wrapper.TryBind(chatMenuBox.messageScroller.scrollSlider, left: true);
        }
        menu.TrySequentialMutualBind([arenaInfoButton, tabContainer.tabButtonContainer.topArrowButton, playerDisplayer.scrollUpButton], true, loopLastIndex: true);
        tabBtnContainer.bottomArrowButton.TryBind(chatMenuBox.messageScroller.scrollSlider, left: true);
    } 
    public void BindPlaylistTabSelectables()
    {
        if (playListTab.IsActuallyHidden)
        {
            menu.TrySequentialMutualBind([arenaInfoButton, tabContainer.tabButtonContainer.topArrowButton, playerDisplayer.scrollUpButton], true, loopLastIndex: true);
            playerDisplayer.scrollDownButton.RemoveMutualBind(leftRight: true, inverted: true);
            return;
        }
        List<TabButton> tabButtons = tabContainer.tabButtonContainer.activeTabButtons;
        menu.TrySequentialMutualBind([arenaInfoButton, tabContainer.tabButtonContainer.topArrowButton, levelSelector.allLevelsPlaylist.scrollUpButton, levelSelector.selectedLevelsPlaylist.scrollUpButton, playerDisplayer.scrollUpButton], true, loopLastIndex: true);
        menu.MutualHorizontalButtonBind(levelSelector.selectedLevelsPlaylist.scrollDownButton, playerDisplayer.scrollDownButton);

        TabButton? tabBtnToBind = tabButtons.Find(x => x.myTab == playListTab) ?? tabButtons[0];
        foreach (var lvlBtn in levelSelector.allLevelsPlaylist.LevelItems)
            lvlBtn.TryBind(tabBtnToBind.wrapper, left: true);
    }
    public void BindSelectSettingsPage(bool isHidden)
    {
        List<TabButton> tabButtons = tabContainer.tabButtonContainer.activeTabButtons;
        TabButton? abilitiesTabBtn = tabButtons.Find(x => x.myTab == slugabilitiesTab);                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           //This technically should return non-null but just in case
        SelectSettingsPage selectSettings = slugcatAbilitiesInterface!.selectSettings!;
        List<SelectSettingsPage.SettingsButton> settingBtns = selectSettings!.SettingBtns;
        if (isHidden)
        {
            foreach (var btn in tabButtons)
                btn.wrapper.RemoveBind(right: true);
            return;
        }
        foreach (var btn in tabButtons)
            btn.wrapper.TryBind(selectSettings.scroller.scrollSlider, right: true);
        menu.MutualHorizontalButtonBind((abilitiesTabBtn ?? tabButtons[0]).wrapper, selectSettings.scroller.scrollSlider);
    }
    public void BindMSCSettingsPage(bool isHidden)
    {
        List<TabButton> tabButtons = tabContainer.tabButtonContainer.activeTabButtons;
        TabButton? abilitiesTabBtn = tabButtons.Find(t => t.myTab == slugabilitiesTab);
        MSCSettingsPage mscSettings = slugcatAbilitiesInterface!.mscSettingsTab!;
        if (isHidden)
        {
            foreach (var btn in tabButtons)
                btn.wrapper.RemoveBind(right: true);
            mscSettings.backButton.RemoveMutualBind(leftRight: true, inverted: true);
            return;
        }
        foreach (var tabBtn in tabButtons)
            tabBtn.wrapper.TryBind(mscSettings.blockMaulCheckBox, right: true);
        var btnToBind = abilitiesTabBtn ?? tabButtons.Last();
        mscSettings.backButton.TryBind((abilitiesTabBtn ?? tabButtons.Last()).wrapper, left: true);

    }
    public void BindWatcherSettingsPage(bool isHidden)
    {
        List<TabButton> tabButtons = tabContainer.tabButtonContainer.activeTabButtons;
        TabButton? abilitiesTabBtn = tabButtons.Find(t => t.myTab == slugabilitiesTab);
        WatcherSettingsPage watcherSettings = slugcatAbilitiesInterface!.watcherSettingsTab!;
        if (isHidden)
        {
            foreach (var tabBtn in tabButtons)
                tabBtn.wrapper.RemoveBind(right: true);
            watcherSettings.backButton.RemoveMutualBind(leftRight: true, inverted: true);
            return;
        }
        foreach (var tabBtn in tabButtons)
            tabBtn.wrapper.TryBind(watcherSettings.watcherCamoLimitTextBox.wrapper, right: true);
        watcherSettings.backButton.TryBind((abilitiesTabBtn ?? tabButtons.Last()).wrapper, left: true);
    }
    public void BindSlugcatAbilitiesSelectables(SettingsPage settingsPage, bool isHidden)
    {
        if (settingsPage == slugcatAbilitiesInterface!.selectSettings)
            BindSelectSettingsPage(isHidden);
        if (settingsPage == slugcatAbilitiesInterface.mscSettingsTab)
            BindMSCSettingsPage(isHidden);
        if (settingsPage == slugcatAbilitiesInterface.watcherSettingsTab)
            BindWatcherSettingsPage(isHidden);
    }
    public void SaveInterfaceOptions()
    {
        RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value = arenaSettingsInterface.countdownTimerTextBox.valueInt;
        RainMeadow.rainMeadowOptions.ArenaItemSteal.Value = arenaSettingsInterface.stealItemCheckBox.Checked;
        RainMeadow.rainMeadowOptions.ArenaAllowMidJoin.Value = arenaSettingsInterface.allowMidGameJoinCheckbox.Checked;
        RainMeadow.rainMeadowOptions.EnablePiggyBack.Value = arenaSettingsInterface.piggyBackCheckbox.Checked;

        slugcatAbilitiesInterface?.SaveAllInterfaceOptions();
    }
    public void UpdatePlayerButtons(ButtonScroller.IPartOfButtonScroller button)
    {
        if (button is ArenaPlayerBox playerBox)
        {
            ArenaClientSettings? clientSettings = ArenaHelpers.GetArenaClientSettings(playerBox.profileIdentifier);
            bool slugSlots = clientSettings?.gotSlugcat == true;

            if (ModManager.MSC && clientSettings?.playingAs == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
            {
                if (playerBox.profileIdentifier.isMe)
                    playerBox.slugcatButton.LoadNewSlugcat(clientSettings?.playingAs, painCatIndex, false);

                else if (playerBox.slugcatButton.slugcat != clientSettings?.playingAs)
                    playerBox.slugcatButton.LoadNewSlugcat(clientSettings?.playingAs, UnityEngine.Random.Range(0, 5), false);
            }
            else playerBox.slugcatButton.LoadNewSlugcat(clientSettings?.playingAs, clientSettings != null && clientSettings.slugcatColor != Color.black, false);

            playerBox.ToggleTextOverlay("Got All<LINE>ScugSlots!!", slugSlots);
            if (clientSettings?.ready == true) playerBox.ToggleTextOverlay(Arena.isInGame && Arena.allowJoiningMidRound ? "Joining<LINE>soon!" : "Ready!", true);
            if (clientSettings?.selectingSlugcat == true) playerBox.ToggleTextOverlay("Selecting<LINE>Slugcat", true);
            if (Arena.arenaSittingOnlineOrder.Contains(playerBox.profileIdentifier.inLobbyId) && Arena.isInGame) playerBox.ToggleTextOverlay("In Game!", true);

            Color color = playerBox.slugcatButton.isColored && clientSettings != null ? clientSettings.slugcatColor : Color.white;
            if (Arena.externalArenaGameMode != null) color = Arena.externalArenaGameMode.GetPortraitColor(Arena, playerBox.profileIdentifier, color);
            playerBox.slugcatButton.portraitColor = color;

            playerBox.showRainbow = Arena.externalArenaGameMode?.DidPlayerWinRainbow(Arena, playerBox.profileIdentifier) == true || slugSlots;
        }
        if (button is ArenaPlayerSmallBox smallPlayerBox)
            smallPlayerBox.slugcatButton.slug = ArenaHelpers.GetArenaClientSettings(smallPlayerBox.profileIdentifier)?.playingAs;
    }
    public void UpdateMatchButtons()
    {
        readyButton.buttonBehav.greyedOut = (!Arena.allowJoiningMidRound && Arena.arenaClientSettings.ready) || (OnlineManager.lobby.isOwner && Arena.initiateLobbyCountdown);
        readyButton.menuLabel.text = menu.Translate(Arena.arenaClientSettings.ready ? !Arena.allowJoiningMidRound ? "WAITING" : "UNREADY" : "READY?");

        if (startButton == null) return;

        startButton.buttonBehav.greyedOut = !Arena.arenaClientSettings.ready || levelSelector.SelectedPlayList.Count == 0 || Arena.initiateLobbyCountdown;
        startButton.menuLabel.text = Arena.initiateLobbyCountdown ? menu.Translate(Arena.lobbyCountDown.ToString()) : menu.Translate("START MATCH!");
        startButton.signalText = "START_MATCH";

    }
    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);
        if (message == "CHANGE_SLUGCAT")
            ArenaMenu?.GoToChangeCharacter();
        if (message == "COLOR_SLUGCAT")
        {
            SlugcatStats.Name? slug = sender?.owner is ArenaPlayerBox playerBox ? playerBox.slugcatButton.slugcat : sender?.owner is ArenaPlayerSmallBox smallPlayerBox ? smallPlayerBox.slugcatButton.slug : null;
            OpenColorConfig(slug);
        }
        if (message == "START_MATCH")
            ArenaMenu?.StartGame();
    }
    public override void Update()
    {
        base.Update();
        if (menu.holdButton && menu.lastHoldButton && menu.selectedObject != null)
        {
            if (menu.selectedObject.Selected && ((menu.selectedObject is SimpleButton btn && btn.signalText == "CHANGE_SLUGCAT") || (menu.selectedObject is SlugcatColorableButton col && col.signalText == "CHANGE_SLUGCAT")))
                holdSlugcatBtnCounter = Mathf.Max(holdSlugcatBtnCounter, 0);
            else holdSlugcatBtnCounter = -1;
        }
        else holdSlugcatBtnCounter = -1;
        if (holdSlugcatBtnCounter >= 0) holdSlugcatBtnCounter++;
        if (holdSlugcatBtnCounter >= 40)
        {
            ArenaMenu?.GoToSlugcatSelector();
            holdSlugcatBtnCounter = -1;
        }

        if (!RainMeadow.isArenaMode(out _)) return;

        ChatLogManager.UpdatePlayerColors();
        if (playerDisplayer != null)
        {
            foreach (ButtonScroller.IPartOfButtonScroller button in playerDisplayer.buttons)
                UpdatePlayerButtons(button);
        }

        activeGameModeLabel.text = LabelTest.TrimText($"{menu.Translate("Current Mode:")} {menu.Translate(Arena.currentGameMode)}", chatMenuBox.size.x - 10, true);
        readyPlayerCounterLabel.text = $"{menu.Translate("Ready:")} {ArenaHelpers.GetReadiedPlayerCount(OnlineManager.players)}/{OnlineManager.players.Count}";
        int amtOfRooms = ArenaMenu?.GetGameTypeSetup?.playList != null ? ArenaMenu.GetGameTypeSetup.playList.Count : 0,
            amtOfRoomsRepeat = arenaSettingsInterface?.roomRepeatArray != null ? arenaSettingsInterface.roomRepeatArray.CheckedButton + 1 : 0;
        playlistProgressLabel.text = $"{menu.Translate("Playlist Progress:")} {Arena.currentLevel}/{(Arena.isInGame ? Arena.totalLevelCount : (amtOfRooms * amtOfRoomsRepeat))}";

        if (OnlineManager.lobby.isOwner)
        {
            if (menu.manager.upcomingProcess == null) levelSelector.LoadNewPlaylist(Arena.playList, false); //dont replace playlist when starting game
            if (startButton is null)
            {
                startButton = new SimplerButton(menu, this, menu.Translate("START MATCH!"), new Vector2(936f, 50f), new Vector2(110f, 30f))
                {
                    signalText = "START_MATCH"
                };
                subObjects.Add(startButton);
                ArenaMenu?.UpdateElementBindings();
            }
            Arena.shufflePlayList = levelSelector.selectedLevelsPlaylist.ShuffleStatus;
        }
        else
        {
            levelSelector.LoadNewPlaylist(Arena.playList, true);
            levelSelector.selectedLevelsPlaylist.ShuffleStatus = Arena.shufflePlayList;
            levelSelector.selectedLevelsPlaylist.shuffleButton.label.text = menu.Translate(levelSelector.selectedLevelsPlaylist.ShuffleStatus ? "Shuffling Levels" : "Playing in order");
            levelSelector.selectedLevelsPlaylist.shuffleButton.UpdateSymbol(levelSelector.selectedLevelsPlaylist.ShuffleStatus ? "Menu_Symbol_Shuffle" : "Menu_Symbol_Dont_Shuffle");
            this.ClearMenuObject(ref startButton);
        }
        UpdateMatchButtons();
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        chatLobbyStateDivider.x = chatMenuBox.DrawX(timeStacker) + (chatMenuBox.size.x / 2);
        chatLobbyStateDivider.y = chatMenuBox.DrawY(timeStacker) + chatMenuBox.roundedRect.size.y - 50;
    }
    public void BindDynamicSelectable(MenuObject objRequested)
    {
        if (objRequested == tabContainer.tabButtonContainer)
            OnTabButtonsCreated();
        if (objRequested == playListTab)
            BindPlaylistTabSelectables();
        else if (objRequested is SettingsPage settingsPage && slugcatAbilitiesInterface != null)
            BindSlugcatAbilitiesSelectables(settingsPage, settingsPage.IsActuallyHidden);

    }
}

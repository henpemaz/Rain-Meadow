using System.Collections.Generic;
using System.Linq;
using Menu;
using Menu.Remix.MixedUI;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using RainMeadow.UI.Components;
using RWCustom;
using UnityEngine;
using Menu.Remix;
using System;


namespace RainMeadow.UI.Pages;

public class ArenaMainLobbyPage : PositionedMenuObject
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
    
    public PlayerDisplayer? playerDisplayer;
    public Dialog? slugcatDialog;
    public TabContainer.Tab? externalTabContainer;


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
        arenaInfoButton = new(menu, this, "Meadow_Menu_SmallQuestionMark", "", new Vector2(chatMenuBox.pos.x + chatMenuBox.size.x / 2 - 12, playerDisplayer!.pos.y + playerDisplayer.scrollUpButton!.pos.y), "");
        arenaInfoButton.OnClick += _ => OpenInfoDialog();

        tabContainer = new TabContainer(menu, this, new Vector2(470f, 125f), new Vector2(450, 475));
        TabContainer.Tab playListTab = tabContainer.AddTab(menu.Translate("Arena Playlist")),
            matchSettingsTab = tabContainer.AddTab(menu.Translate("Match Settings"));

        playListTab.AddObjects(levelSelector = new ArenaLevelSelector(menu, playListTab, new Vector2(65f, 7.5f)));


        if (ModManager.MSC || ModManager.Watcher)
        {
            TabContainer.Tab slugabilitiesTab = tabContainer.AddTab(menu.Translate("Slugcat Abilities"));
            slugcatAbilitiesInterface = new OnlineSlugcatAbilitiesInterface(menu, slugabilitiesTab, new Vector2(360f, 380f), new Vector2(0f, 50f), menu.Translate(painCatName));
            slugcatAbilitiesInterface.CallForSync();
            slugabilitiesTab.AddObjects(slugcatAbilitiesInterface);
        }
        arenaSettingsInterface = new OnlineArenaSettingsInferface(menu, matchSettingsTab, new Vector2(120f, 0f), Arena.currentGameMode, [.. Arena.registeredGameModes.Keys.Select(v => new ListItem(v, menu.Translate(v)))]);
        arenaSettingsInterface.CallForSync();
        matchSettingsTab.AddObjects(arenaSettingsInterface);


        this.SafeAddSubobjects(readyButton, tabContainer, activeGameModeLabel, readyPlayerCounterLabel, playlistProgressLabel, chatMenuBox, arenaInfoButton, arenaGameStatsButton);
    }

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
            playerBox.slugcatButton.TryBind(playerDisplay.scrollSlider, true, false, false, false);
            return playerBox;
        }
        ArenaPlayerSmallBox playerSmallBox = new(menu, playerDisplay, player, OnlineManager.lobby?.isOwner == true, pos);
        playerSmallBox.playerButton.TryBind(playerDisplay.scrollSlider, true, false, false, false);
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
    public void SaveInterfaceOptions()
    {
        RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value = arenaSettingsInterface.countdownTimerTextBox.valueInt;
        RainMeadow.rainMeadowOptions.ArenaItemSteal.Value = arenaSettingsInterface.stealItemCheckBox.Checked;
        RainMeadow.rainMeadowOptions.ArenaAllowMidJoin.Value = arenaSettingsInterface.allowMidGameJoinCheckbox.Checked;
        if (slugcatAbilitiesInterface != null)
        {
            if (ModManager.MSC)
            {
                RainMeadow.rainMeadowOptions.BlockMaul.Value = slugcatAbilitiesInterface.blockMaulCheckBox.Checked;
                RainMeadow.rainMeadowOptions.BlockArtiStun.Value = slugcatAbilitiesInterface.blockArtiStunCheckBox.Checked;
                RainMeadow.rainMeadowOptions.ArenaSAINOT.Value = slugcatAbilitiesInterface.sainotCheckBox.Checked;
                RainMeadow.rainMeadowOptions.PainCatEgg.Value = slugcatAbilitiesInterface.painCatEggCheckBox.Checked;
                RainMeadow.rainMeadowOptions.PainCatThrows.Value = slugcatAbilitiesInterface.painCatThrowsCheckBox.Checked;
                RainMeadow.rainMeadowOptions.PainCatLizard.Value = slugcatAbilitiesInterface.painCatLizardCheckBox.Checked;
                RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer.Value = slugcatAbilitiesInterface.saintAscendDurationTimerTextBox.valueInt;
            }
            if (ModManager.Watcher)
            {
                RainMeadow.rainMeadowOptions.ArenaWatcherCamoTimer.Value = slugcatAbilitiesInterface.watcherCamoLimitLabelTextBox.valueInt;

            }
        }
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
}

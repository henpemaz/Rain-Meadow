using System.Linq;
using Menu;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components;
using RWCustom;
using UnityEngine;

namespace RainMeadow.UI.Pages;

public class ArenaMainLobbyPage : PositionedMenuObject
{
    public SimplerButton readyButton;
    public SimplerButton? startButton;
    public MenuLabel activeGameModeLabel, readyPlayerCounterLabel, playlistProgressLabel;
    public FSprite chatLobbyStateDivider;
    public TabContainer tabContainer;
    public ArenaLevelSelector levelSelector;
    public ChatMenuBox chatMenuBox;
    public OnlineArenaSettingsInferface arenaSettingsInterface;
    public OnlineSlugcatAbilitiesInterface? slugcatAbilitiesInterface;
    public PlayerDisplayer? playerDisplayer;
    public Dialog? slugcatDialog;
    private ArenaOnlineGameMode Arena => (ArenaOnlineGameMode)OnlineManager.lobby.gameMode;
    public ArenaOnlineLobbyMenu? ArenaMenu => menu as ArenaOnlineLobbyMenu;

    public ArenaMainLobbyPage(Menu.Menu menu, MenuObject owner, Vector2 pos, string painCatName) : base(menu, owner, pos)
    {
        readyButton = new SimplerButton(menu, this, Utils.Translate("READY?"), new Vector2(1056f, 50f), new Vector2(110f, 30f));
        readyButton.OnClick += btn =>
        {
            if (!RainMeadow.isArenaMode(out var _)) return;
            Arena.arenaClientSettings.ready = !Arena.arenaClientSettings.ready;
            btn.menuLabel.text = Utils.Translate(Arena.arenaClientSettings.ready ? "UNREADY" : "READY?");
        };

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

        tabContainer = new TabContainer(menu, this, new Vector2(470f, 125f), new Vector2(450, 475));
        TabContainer.Tab playListTab = tabContainer.AddTab("Arena Playlist"),
            matchSettingsTab = tabContainer.AddTab("Match Settings");

        playListTab.AddObjects(levelSelector = new ArenaLevelSelector(menu, playListTab, new Vector2(65f, 7.5f)));

        arenaSettingsInterface = new OnlineArenaSettingsInferface(menu, matchSettingsTab, new Vector2(120f, 0f), Arena.currentGameMode, [.. Arena.registeredGameModes.Values.Select(v => new ListItem(v))]);
        arenaSettingsInterface.CallForSync();
        matchSettingsTab.AddObjects(arenaSettingsInterface);

        if (ModManager.MSC)
        {
            TabContainer.Tab slugabilitiesTab = tabContainer.AddTab("Slugcat Abilities");
            slugcatAbilitiesInterface = new OnlineSlugcatAbilitiesInterface(menu, slugabilitiesTab, new Vector2(360f, 380f), new Vector2(0f, 50f), painCatName);
            slugcatAbilitiesInterface.CallForSync();
            slugabilitiesTab.AddObjects(slugcatAbilitiesInterface);
        }

        this.SafeAddSubobjects(readyButton, tabContainer, activeGameModeLabel, readyPlayerCounterLabel, playlistProgressLabel, chatMenuBox);
    }

    public void BuildPlayerDisplay()
    {
        playerDisplayer = new PlayerDisplayer(menu, this, new Vector2(960f, 130f), OnlineManager.players, GetPlayerButton, 4, ArenaPlayerBox.DefaultSize.x, new(ArenaPlayerBox.DefaultSize.y, 0), new(ArenaPlayerSmallBox.DefaultSize.y, 10));
        subObjects.Add(playerDisplayer);
        playerDisplayer.CallForRefresh();
    }

    public void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
    {
        RainMeadow.DebugMe();
        playerDisplayer?.UpdatePlayerList(OnlineManager.players);
    }
    public ButtonScroller.IPartOfButtonScroller GetPlayerButton(PlayerDisplayer playerDisplay, bool isLargeDisplay, OnlinePlayer player, Vector2 pos)
    {
        void changeCharacter()
        {
            if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && !Arena.arenaClientSettings.ready)
            {
                var index = ArenaHelpers.selectableSlugcats.IndexOf(Arena.arenaClientSettings.playingAs);
                if (index == -1) index = 0;
                else
                {
                    index += 1;
                    index %= ArenaHelpers.selectableSlugcats.Count;
                }

                ArenaMenu?.arenaSlugcatSelectPage.SwitchSelectedSlugcat(ArenaHelpers.selectableSlugcats[index]);
            }
            else
            {
                ArenaMenu?.MovePage(new Vector2(-1500f, 0f), 1);
            }
        }

        if (isLargeDisplay)
        {
            ArenaPlayerBox playerBox = new(menu, playerDisplay, player, OnlineManager.lobby?.isOwner == true, pos); //buttons init prevents kick button if isMe
            if (player.isMe)
            {
                playerBox.slugcatButton.OnClick += _ => changeCharacter();
                playerBox.colorInfoButton.OnClick += _ => OpenColorConfig(playerBox.slugcatButton.slugcat);
            }
            playerBox.slugcatButton.TryBind(playerDisplay.scrollSlider, true, false, false, false);
            return playerBox;
        }

        ArenaPlayerSmallBox playerSmallBox = new(menu, playerDisplay, player, OnlineManager.lobby?.isOwner == true, pos);

        if (player.isMe)
        {
            playerSmallBox.slugcatButton.OnClick += _ => changeCharacter();
            playerSmallBox.colorKickButton!.OnClick += _ => OpenColorConfig(playerSmallBox.slugcatButton.slug);
        }

        playerSmallBox.playerButton.TryBind(playerDisplay.scrollSlider, true, false, false, false);
        return playerSmallBox;
    }
    public void OpenColorConfig(SlugcatStats.Name? slugcat)
    {
        if (!ModManager.MMF)
        {
            menu.PlaySound(SoundID.MENU_Checkbox_Uncheck);
            slugcatDialog = new DialogNotify(menu.Translate("You cant color without Remix on!"), new Vector2(500f, 200f), menu.manager, () => { });
            menu.manager.ShowDialog(slugcatDialog);
            return;
        }

        menu.PlaySound(SoundID.MENU_Checkbox_Check);
        slugcatDialog = new ColorMultipleSlugcatsDialog(menu.manager, () => { }, ArenaHelpers.allSlugcats, slugcat);
        menu.manager.ShowDialog(slugcatDialog);
    }

    public void SaveInterfaceOptions()
    {
        RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value = arenaSettingsInterface.countdownTimerTextBox.valueInt;
        RainMeadow.rainMeadowOptions.ArenaItemSteal.Value = arenaSettingsInterface.stealItemCheckBox.Checked;

        if (slugcatAbilitiesInterface != null)
        {
            RainMeadow.rainMeadowOptions.BlockMaul.Value = slugcatAbilitiesInterface.blockMaulCheckBox.Checked;
            RainMeadow.rainMeadowOptions.BlockArtiStun.Value = slugcatAbilitiesInterface.blockArtiStunCheckBox.Checked;
            RainMeadow.rainMeadowOptions.ArenaSAINOT.Value = slugcatAbilitiesInterface.sainotCheckBox.Checked;
            RainMeadow.rainMeadowOptions.PainCatEgg.Value = slugcatAbilitiesInterface.painCatEggCheckBox.Checked;
            RainMeadow.rainMeadowOptions.PainCatThrows.Value = slugcatAbilitiesInterface.painCatThrowsCheckBox.Checked;
            RainMeadow.rainMeadowOptions.PainCatLizard.Value = slugcatAbilitiesInterface.painCatLizardCheckBox.Checked;
            RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer.Value = slugcatAbilitiesInterface.saintAscendDurationTimerTextBox.valueInt;
        }
    }

    public override void Update()
    {
        base.Update();
        if (!RainMeadow.isArenaMode(out _)) return;
        ChatLogManager.UpdatePlayerColors();
        if (playerDisplayer != null)
        {
            foreach (ButtonScroller.IPartOfButtonScroller button in playerDisplayer.buttons)
            {
                if (button is ArenaPlayerBox playerBox)
                {
                    ArenaClientSettings? clientSettings = ArenaHelpers.GetArenaClientSettings(playerBox.profileIdentifier);
                    playerBox.slugcatButton.LoadNewSlugcat(clientSettings?.playingAs, clientSettings != null && clientSettings.slugcatColor != Color.black, false);
                    playerBox.ToggleTextOverlay("Ready!", clientSettings?.ready ?? false);
                    if (clientSettings?.selectingSlugcat ?? false) playerBox.ToggleTextOverlay(Custom.ReplaceLineDelimeters("Selecting<LINE>Slugcat"), true);

                    if (playerBox.slugcatButton.isColored) playerBox.slugcatButton.portraitColor = (clientSettings?.slugcatColor ?? Color.white);
                    else playerBox.slugcatButton.portraitColor = Color.white;
                }
                if (button is ArenaPlayerSmallBox smallPlayerBox)
                    smallPlayerBox.slugcatButton.slug = ArenaHelpers.GetArenaClientSettings(smallPlayerBox.profileIdentifier)?.playingAs;
            }
        }

        activeGameModeLabel.text = LabelTest.TrimText($"Current Mode: {Arena.currentGameMode}", chatMenuBox.size.x - 10, true);
        readyPlayerCounterLabel.text = $"Ready: {ArenaHelpers.GetReadiedPlayerCount(OnlineManager.players)}/{OnlineManager.players.Count}";
        playlistProgressLabel.text = $"Playlist Progress: {Arena.currentLevel}/{(Arena.isInGame ? Arena.totalLevelCount : (ArenaMenu?.GetGameTypeSetup.playList.Count * ArenaMenu?.GetGameTypeSetup.levelRepeats) ?? 0)}";

        if (OnlineManager.lobby.isOwner)
        {
            levelSelector.LoadNewPlaylist(Arena.playList, false);
            if (startButton is null)
            {
                startButton = new SimplerButton(menu, this, "START MATCH!", new Vector2(936f, 50f), new Vector2(110f, 30f));
                startButton.OnClick += btn => ArenaMenu?.StartGame();
                subObjects.Add(startButton);
            }

            startButton.buttonBehav.greyedOut = !Arena.arenaClientSettings.ready || levelSelector.SelectedPlayList.Count == 0;
        }
        else
        {
            levelSelector.LoadNewPlaylist(Arena.playList, true);
            if (startButton is not null)
            {
                this.ClearMenuObject(startButton);
                startButton = null;
            }
        }
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        chatLobbyStateDivider.x = chatMenuBox.DrawX(timeStacker) + (chatMenuBox.size.x / 2);
        chatLobbyStateDivider.y = chatMenuBox.DrawY(timeStacker) + chatMenuBox.roundedRect.size.y - 50;
    }
}
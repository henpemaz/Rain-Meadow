using System.Linq;
using Menu;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components;
using UnityEngine;

namespace RainMeadow.UI.Pages;

public class ArenaMainLobbyPage : PositionedMenuObject
{
    public SimplerButton playButton;
    public TabContainer tabContainer;
    public ArenaLevelSelector levelSelector;
    public ChatMenuBox chatMenuBox;
    public OnlineArenaSettingsInferface arenaSettingsInterface;
    public OnlineSlugcatAbilitiesInterface? slugcatAbilitiesInterface;
    public PlayerDisplayer? playerDisplayer;
    public Dialog? slugcatDialog;
    private ArenaOnlineGameMode Arena => (ArenaOnlineGameMode)OnlineManager.lobby.gameMode;

    public ArenaMainLobbyPage(Menu.Menu menu, MenuObject owner, Vector2 pos, string painCatName) : base(menu, owner, pos)
    {
        playButton = new SimplerButton(menu, this, Utils.Translate("READY?"), new Vector2(1056f, 50f), new Vector2(110f, 30f));
        chatMenuBox = new(menu, this, new(100f, 125f), new(300, 475));
        tabContainer = new TabContainer(menu, this, new Vector2(470f, 125f), new Vector2(450, 475));
        TabContainer.Tab playListTab = tabContainer.AddTab("Arena Playlist"),
            matchSettingsTab = tabContainer.AddTab("Match Settings");

        playListTab.AddObjects(levelSelector = new ArenaLevelSelector(menu, playListTab, new Vector2(65f, 7.5f), false));

        arenaSettingsInterface = new OnlineArenaSettingsInferface(menu, matchSettingsTab, new Vector2(120f, 205f), Arena.currentGameMode, [.. Arena.registeredGameModes.Values.Select(v => new ListItem(v))]);
        arenaSettingsInterface.CallForSync();
        matchSettingsTab.AddObjects(arenaSettingsInterface);

        if (ModManager.MSC)
        {
            TabContainer.Tab slugabilitiesTab = tabContainer.AddTab("Slugcat Abilities");
            slugcatAbilitiesInterface = new OnlineSlugcatAbilitiesInterface(menu, slugabilitiesTab, new Vector2(360f, 380f), new Vector2(0f, 50f), painCatName);
            slugcatAbilitiesInterface.CallForSync();
            slugabilitiesTab.AddObjects(slugcatAbilitiesInterface);
        }

        BuildPlayerDisplay();
        MatchmakingManager.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;
        this.SafeAddSubobjects(playButton, tabContainer, chatMenuBox);
    }

    public void BuildPlayerDisplay()
    {
        if (playerDisplayer != null) return;

        playerDisplayer = new PlayerDisplayer(menu, this, new Vector2(960f, 130f), OnlineManager.players, GetPlayerButton, 4, ArenaPlayerBox.DefaultSize.x + 30, ArenaPlayerBox.DefaultSize.y, 0, ArenaPlayerSmallBox.DefaultSize.y, 10);
        subObjects.Add(playerDisplayer);
        playerDisplayer.CallForRefresh();
    }

    public void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
    {
        if (!RainMeadow.isArenaMode(out _)) return;

        RainMeadow.DebugMe();
        BuildPlayerDisplay();
        playerDisplayer?.UpdatePlayerList(OnlineManager.players);
    }
    public ButtonScroller.IPartOfButtonScroller GetPlayerButton(PlayerDisplayer playerDisplay, bool isLargeDisplay, OnlinePlayer player, Vector2 pos)
    {
        ArenaOnlineLobbyMenu? dustySaidThatThisWouldStopTheCompilerFromComplainingAboutVariablesInEachBranchBeingNamedTheSameThingEvenThoughTheyWouldNeverBeInitializedTogether = menu as ArenaOnlineLobbyMenu;

        if (isLargeDisplay)
        {
            ArenaPlayerBox playerBox = new(menu, playerDisplay, player, OnlineManager.lobby?.isOwner == true, pos); //buttons init prevents kick button if isMe
            if (player.isMe)
            {
                playerBox.slugcatButton.OnClick += _ =>
                    dustySaidThatThisWouldStopTheCompilerFromComplainingAboutVariablesInEachBranchBeingNamedTheSameThingEvenThoughTheyWouldNeverBeInitializedTogether?.MovePage(new Vector2(-1500f, 0f), 1);
                playerBox.colorInfoButton.OnClick += _ => OpenColorConfig(playerBox.slugcatButton.slugcat);
            }
            playerBox.slugcatButton.TryBind(playerDisplay.scrollSlider, true, false, false, false);
            return playerBox;
        }

        ArenaPlayerSmallBox playerSmallBox = new(menu, playerDisplay, player, OnlineManager.lobby?.isOwner == true, pos);

        if (player.isMe)
        {
            playerSmallBox.slugcatButton.OnClick += _ => dustySaidThatThisWouldStopTheCompilerFromComplainingAboutVariablesInEachBranchBeingNamedTheSameThingEvenThoughTheyWouldNeverBeInitializedTogether?.MovePage(new Vector2(-1500f, 0f), 1);
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
            slugcatDialog = new DialogNotify("You cant color without Remix on!", new Vector2(500f, 200f), menu.manager, () => { });
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
                    playerBox.isSelectingSlugcat = clientSettings?.selectingSlugcat ?? false;

                    if (playerBox.slugcatButton.isColored) playerBox.slugcatButton.portraitColor = (clientSettings?.slugcatColor ?? Color.white);
                    else playerBox.slugcatButton.portraitColor = Color.white;
                }
                if (button is ArenaPlayerSmallBox smallPlayerBox)
                    smallPlayerBox.slugcatButton.slug = ArenaHelpers.GetArenaClientSettings(smallPlayerBox.profileIdentifier)?.playingAs;
            }
        }

    }
}
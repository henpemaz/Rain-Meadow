using Menu;
using RainMeadow.UI.Components;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.UI.Pages;

public class ArenaModerationPage : PositionedMenuObject, SelectOneButton.SelectOneButtonOwner
{
    public SimplerButton backButton;
    public ModerationPlayerDisplayer recentDisplayer;
    public ModerationPlayerDisplayer banDisplayer;

    public MenuLabel bannedLabel, recentsLabel, readyWarningLabel, userModerationLabel;

    public string defaultReadyWarningText = "You have been unreadied. Switch back to re-ready yourself automatically";
    public bool readyWarning;
    public int warningCounter;

    public ArenaOnlineGameMode? Arena => OnlineManager.lobby?.gameMode as ArenaOnlineGameMode;
    public ArenaOnlineLobbyMenu? ArenaMenu => menu as ArenaOnlineLobbyMenu;
    public ArenaModerationPage(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
    {

        backButton = new SimplerButton(menu, this, menu.Translate("Back To Lobby"), new Vector2(200f, 50f), new Vector2(110f, 30f), menu.Translate("Go back to main lobby"));
        backButton.OnClick += _ =>
        {
            if (ArenaMenu == null) return;
            ArenaMenu.MovePage(new Vector2(-1500f, 0f), 0);
            ArenaMenu.selectedObject = ArenaMenu.arenaMainLobbyPage.readyButton;
        };

        userModerationLabel = new(menu, this, menu.Translate("USER MODERATION"), new Vector2(680f, 575f), default, true);
        userModerationLabel.label.color = new Color(0.5f, 0.5f, 0.5f);
        userModerationLabel.label.shader = menu.manager.rainWorld.Shaders["MenuTextCustom"];

        recentsLabel = new MenuLabel(menu, this, "RECENT PLAYERS", new Vector2(365f, 555), default, true);
        recentsLabel.label.shader = menu.manager.rainWorld.Shaders["MenuText"];

        bannedLabel = new MenuLabel(menu, this, "BLOCKED PLAYERS", new Vector2(1005f, 555), default, true);
        bannedLabel.label.shader = menu.manager.rainWorld.Shaders["MenuText"];

        readyWarningLabel = new MenuLabel(menu, this, menu.LongTranslate(defaultReadyWarningText), new Vector2(680f, 620f), Vector2.zero, true);

        BuildPlayerDisplay();

        this.SafeAddSubobjects(backButton, recentDisplayer, banDisplayer, readyWarningLabel, userModerationLabel, recentsLabel, bannedLabel);

        if (ArenaMenu != null)
        {
            ArenaMenu.ChangeScene();
        }
    }

    public void BuildPlayerDisplay()
    {
        recentDisplayer = new ModerationPlayerDisplayer(
            menu,
            this,
            new Vector2(220f, 130f),
            BanHammer.recentUsers,
            GetPlayerButton,
            3,
            ArenaPlayerBox.DefaultSize.x,
            new(ArenaPlayerBox.DefaultSize.y, 0),
            new(ArenaPlayerSmallBox.DefaultSize.y, 10)
        );
        subObjects.Add(recentDisplayer);
        recentDisplayer.CallForRefresh();

        banDisplayer = new ModerationPlayerDisplayer(
            menu,
            this,
            new Vector2(860f, 130f),
            BanHammer.bannedUsers,
            GetPlayerButton,
            3,
            ArenaPlayerBox.DefaultSize.x,
            new(ArenaPlayerBox.DefaultSize.y, 0),
            new(ArenaPlayerSmallBox.DefaultSize.y, 10),
            isBanList: true
        );
        subObjects.Add(banDisplayer);
        banDisplayer.CallForRefresh();

        BanHammer.OnRefresh += BanHammer_OnRefresh;

        banDisplayer.UpdatePlayerList(BanHammer.bannedUsers);
        recentDisplayer.UpdatePlayerList(BanHammer.recentUsers);
    }

    private void BanHammer_OnRefresh(List<SteamPlayerRep> recents, List<SteamPlayerRep> banned)
    {
        recentDisplayer.UpdatePlayerList(recents);
        banDisplayer.UpdatePlayerList(banned);
    }

    public ButtonScroller.IPartOfButtonScroller GetPlayerButton(
        ModerationPlayerDisplayer playerDisplay,
        bool banList,
        SteamPlayerRep player,
        Vector2 pos
    )
    {
        ModerationPlayerBox playerBox = new(
                menu,
                playerDisplay,
                player,
                banList,
                pos
            );
        return playerBox;
    }

    public override void Update()
    {
        base.Update();

        if (warningCounter >= 0) warningCounter++;
        if (readyWarning)
            warningCounter = Mathf.Max(warningCounter, 0);
        else warningCounter = -1;
        if (readyWarningLabel != null)
        {
            readyWarningLabel.text = Arena != null && Arena.initiateLobbyCountdown && Arena.lobbyCountDown > 0 ? menu.LongTranslate($"The match is starting in <COUNTDOWN>! Ready up!!").Replace("<COUNTDOWN>", Arena.lobbyCountDown.ToString()) : menu.LongTranslate(defaultReadyWarningText);
        }

        if (recentDisplayer != null)
        {
            foreach(var button in recentDisplayer.buttons)
            {
                UpdatePlayerButtons(button);
            }
        }

        if (banDisplayer != null)
        {
            foreach (var button in banDisplayer.buttons)
            {
                UpdatePlayerButtons(button, true);
            }
        }
    }

    public void UpdatePlayerList()
    {
        recentDisplayer?.UpdatePlayerList(BanHammer.recentUsers);
        banDisplayer?.UpdatePlayerList(BanHammer.recentUsers);
    }

    public void UpdatePlayerButtons(ButtonScroller.IPartOfButtonScroller button, bool banList = false)
    {
        if (button is ModerationPlayerBox playerBox)
        {
            var playerRep = playerBox.profileIdentifier;

            SlugcatStats.Name selected = SlugcatStats.Name.White;
            if (!string.IsNullOrEmpty(playerRep.Selected)) 
                selected = new SlugcatStats.Name(playerRep.Selected);
            if (playerBox.slugcatButton.slugcat != selected)
            {
                playerBox.slugcatButton.LoadNewSlugcat(
                    selected,
                    !string.IsNullOrEmpty(playerRep.SlugcatColor),
                    banList || BanHammer.bannedUsers.Contains(playerRep)
                );

                playerBox.slugcatButton.portraitColor = playerBox.slugcatButton.isColored ? Custom.hexToColor(playerRep.SlugcatColor) : Color.white;
            }
        }
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        if (owner is not PositionedMenuObject positionedOwner) return;

        if (readyWarning)
        {
            readyWarningLabel.label.color = Color.Lerp(MenuColorEffect.rgbWhite, new Color(0.85f, 0.35f, 0.4f), 0.5f - 0.5f * Mathf.Sin((timeStacker + warningCounter) / 30 * Mathf.PI * 2));
            readyWarningLabel.label.alpha = 1;
        }
        else readyWarningLabel.label.alpha = 0;
    }

    public int GetCurrentlySelectedOfSeries(string series) => 0;

    public void SetCurrentlySelectedOfSeries(string series, int to)
    {
        //TODO implement
    }
}

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

    public string defaultReadyWarningText = "You have been unreadied. Switch back to re-ready yourself automatically";

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

        BuildPlayerDisplay();

        this.SafeAddSubobjects(backButton, recentDisplayer, banDisplayer);

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
            4,
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
            4,
            ArenaPlayerBox.DefaultSize.x,
            new(ArenaPlayerBox.DefaultSize.y, 0),
            new(ArenaPlayerSmallBox.DefaultSize.y, 10),
            isBanList: true
        );
        subObjects.Add(banDisplayer);
        banDisplayer.CallForRefresh();

        BanHammer.OnRecentsRefresh += BanHammer_OnRecentsRefresh;
        BanHammer.OnBannedRefresh += BanHammer_OnBannedRefresh;

        banDisplayer.UpdatePlayerList(BanHammer.bannedUsers);
        recentDisplayer.UpdatePlayerList(BanHammer.recentUsers);
    }

    private void BanHammer_OnBannedRefresh(SteamPlayerRep[] players)
    {
        banDisplayer.UpdatePlayerList(players.ToList());
    }

    private void BanHammer_OnRecentsRefresh(SteamPlayerRep[] players)
    {
        recentDisplayer.UpdatePlayerList(players.ToList());
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
                    banList
                );

                playerBox.slugcatButton.portraitColor = playerBox.slugcatButton.isColored ? Custom.hexToColor(playerRep.SlugcatColor) : Color.white;
            }
        }
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        if (owner is not PositionedMenuObject positionedOwner) return;

        
    }

    public int GetCurrentlySelectedOfSeries(string series)
    {
        throw new NotImplementedException();
    }

    public void SetCurrentlySelectedOfSeries(string series, int to)
    {
        throw new NotImplementedException();
    }
}

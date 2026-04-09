using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using RainMeadow.UI.Pages;
using UnityEngine;
namespace RainMeadow.UI.Components
{
    public class PlayerDisplayer : ButtonDisplayer
    {
        public ArenaOnlineGameMode? Arena => OnlineManager.lobby?.gameMode as ArenaOnlineGameMode;
        public ArenaOnlineLobbyMenu? ArenaMenu => menu as ArenaOnlineLobbyMenu;

        public PlayerDisplayer(Menu.Menu menu, MenuObject owner, Vector2 pos, List<OnlinePlayer> players, Func<PlayerDisplayer, bool, OnlinePlayer, Vector2, IPartOfButtonScroller> getPlayerButton, int numOfLargeButtonsToView, float xListSize, (float, float) largeButtonHeightSpacing, (float, float) smallButtonHeightSpacing, float scrollSliderSizeYOffset = -40) : base(menu, owner, pos, numOfLargeButtonsToView, xListSize, largeButtonHeightSpacing)
        {
            this.getPlayerButton = getPlayerButton;
            onlinePlayers = players;
            this.largeButtonHeightSpacing = largeButtonHeightSpacing;
            this.smallButtonHeightSpacing = smallButtonHeightSpacing;
            inviteFriends = this.AddSideButton("Meadow_Menu_InviteFriends", description: menu.Translate("Invite Friends"), signal: "INVITE_FRIENDS");
            inviteFriends.OnClick += (_) =>
            {
                SimpleDialogBoxNotify dialogBox = new(menu, owner, "The Steam invite feature is currently unstable, and may not work properly.\nConsider using a public lobby with a password instead.", buttonText: "OKAY");
                MatchmakingManager.currentInstance.OpenInvitationOverlay();
            };
            moderationMenu = this.AddSideButton("Meadow_Menu_MutePlayerChat10", description: menu.Translate("Moderation Options"), signal: "MODERATION");
            moderationMenu.OnClick += (_) =>
            {
                if (ArenaMenu != null) ArenaMenu.GoToModerationPage();
            };
            refreshDisplayButtons = PopulatePlayerDisplays;
            UpdatePlayerList(onlinePlayers);
        }

        public void UpdatePlayerList(List<OnlinePlayer> lobbyOnlinePlayers)
        {
            onlinePlayers = lobbyOnlinePlayers;
            CallForRefresh();
        }
        public IPartOfButtonScroller[] PopulatePlayerDisplays(ButtonDisplayer buttonDisplayer, bool isLargeDisplay)
        {
            buttonHeight = isLargeDisplay ? largeButtonHeightSpacing.Item1 : smallButtonHeightSpacing.Item1;
            buttonSpacing = isLargeDisplay ? largeButtonHeightSpacing.Item2 : smallButtonHeightSpacing.Item2;
            List<IPartOfButtonScroller> scrollButtons = [];
            for (int i = 0; i < onlinePlayers?.Count; i++)
            {
                IPartOfButtonScroller? scrollButton = getPlayerButton?.Invoke(this, isLargeDisplay, onlinePlayers[i], GetIdealPosWithScrollForButton(scrollButtons.Count));
                if (scrollButton == null)
                {
                    RainMeadow.Debug("Player button gotten was null!");
                    continue;
                }
                scrollButtons.Add(scrollButton);
            }
            return [.. scrollButtons];
        }
        public override string DescriptionOfDisplayButton() => menu.Translate(isCurrentlyLargeDisplay ? "Showing players in thumbnail view" : "Showing players in list view");

        public (float, float) largeButtonHeightSpacing, smallButtonHeightSpacing;
        public List<OnlinePlayer> onlinePlayers;
        public Func<PlayerDisplayer, bool, OnlinePlayer, Vector2, IPartOfButtonScroller> getPlayerButton;
        public SideButton inviteFriends;
        public SideButton moderationMenu;
    }
}

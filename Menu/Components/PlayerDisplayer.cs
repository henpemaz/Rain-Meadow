using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using UnityEngine;
namespace RainMeadow.UI.Components
{
    public class PlayerDisplayer : ButtonDisplayer
    {
        public PlayerDisplayer(Menu.Menu menu, MenuObject owner, Vector2 pos, List<OnlinePlayer> players, Func<PlayerDisplayer, bool, OnlinePlayer, Vector2, IPartOfButtonScroller> getPlayerButton, int numOfLargeButtonsToView, float xListSize, float largeButtonHeight, float largeButtonSpacing, float smallButtonHeight, float smallButtonSpacing) : base(menu, owner, pos, numOfLargeButtonsToView, xListSize, largeButtonHeight, largeButtonSpacing)
        {
            this.getPlayerButton = getPlayerButton;
            onlinePlayers = players;
            this.largeButtonHeight = largeButtonHeight;
            this.largeButtonSpacing = largeButtonSpacing;
            this.smallButtonHeight = smallButtonHeight;
            this.smallButtonSpacing = smallButtonSpacing;
            refreshDisplayButtons = PopulatePlayerDisplays;
            UpdatePlayerList(onlinePlayers);
        }
        public void UpdatePlayerList(List<OnlinePlayer> lobbyOnlinePlayers)
        {
            onlinePlayers = lobbyOnlinePlayers;
            CallForRefresh(false);
        }
        public IPartOfButtonScroller[] PopulatePlayerDisplays(ButtonDisplayer buttonDisplayer, bool isLargeDisplay)
        {
            buttonHeight = isLargeDisplay ? largeButtonHeight : smallButtonHeight;
            buttonSpacing = isLargeDisplay ? largeButtonSpacing : smallButtonSpacing;
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

        public float largeButtonHeight, smallButtonHeight, largeButtonSpacing, smallButtonSpacing;
        public List<OnlinePlayer> onlinePlayers;
        public Func<PlayerDisplayer, bool, OnlinePlayer, Vector2, IPartOfButtonScroller> getPlayerButton;
    }
}

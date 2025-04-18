using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow
{
    public class ArenaOnlineSlugcatButtons : PositionedMenuObject //uponPopulate changes have to be on the parameter especially for ctor
    {
        public float XOffset => 120;
        public int CurrentOffset { get => currentOffset; set => currentOffset = Mathf.Clamp(value, 0, Mathf.Max(OtherOnlinePlayers.Count - 1, 0) / PerPage); }
        public int PerPage { get => perPage; set => perPage = Mathf.Max(perPage, 1); } //excludes first button
        public bool PagesOn =>  OtherOnlinePlayers?.Count > PerPage;
        public List<OnlinePlayer> OtherOnlinePlayers => [..onlinePlayers.Where(x => !x.isMe)];
        public ArenaOnlineSlugcatButtons(Menu.Menu menu, MenuObject owner, Vector2 pos, List<OnlinePlayer> onlinePlayers, Action<ArenaOnlineSlugcatButtons> uponPopulate) : base(menu, owner, pos)
        {
            uponPopulatingPage = uponPopulate;
            PerPage = 3;
            this.onlinePlayers = onlinePlayers;
            meButton = GetArenaPlayerButton(Vector2.zero, OnlineManager.mePlayer);
            subObjects.Add(meButton);
            PopulatePage(CurrentOffset);
            if (PagesOn)
            {
                ActivateButtons();
            }
        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            this.ClearMenuObject(ref meButton);
            DeactivateButtons();
            ClearInterface();
        }
        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == PREVSINGAL)
            {
                PrevPage();
            }
            if (message == NEXTSINGAL)
            {
                NextPage();
            }
        }
        public void PrevPage()
        {
            menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            List<OnlinePlayer> otherOnlinePlayers = OtherOnlinePlayers;
            PopulatePage((CurrentOffset - 1 < 0) ? ((otherOnlinePlayers?.Count > 0) ? ((otherOnlinePlayers.Count - 1) / PerPage) : 0) : (CurrentOffset - 1));
        }
        public void NextPage()
        {
            menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            List<OnlinePlayer> otherOnlinePlayers = OtherOnlinePlayers;
            PopulatePage(otherOnlinePlayers == null || otherOnlinePlayers.Count == 0 || CurrentOffset + 1 > (otherOnlinePlayers.Count - 1) / PerPage ? 0 : (CurrentOffset + 1));
        }
        public void PopulatePage(int offset)
        {
            ClearInterface(true);
            CurrentOffset = offset;
            int num = CurrentOffset * PerPage;
            List<ArenaOnlinePlayerJoinButton> newArenaPlayerButtons = [];
            List<OnlinePlayer> otherOnlinePlayers = OtherOnlinePlayers;
            while (num < otherOnlinePlayers.Count && num < (CurrentOffset + 1) * PerPage)
            {
                ArenaOnlinePlayerJoinButton playerButton = GetArenaPlayerButton(new(XOffset + (num % PerPage * XOffset), meButton.pos.y), OnlineManager.mePlayer);
                subObjects.Add(playerButton);
                menu.TryMutualBind(newArenaPlayerButtons.GetValueOrDefault((num - 1) % PerPage), playerButton, true);
                newArenaPlayerButtons.Add(playerButton);
                num++;
            }
            otherArenaPlayerButtons = [..newArenaPlayerButtons];
            RefreshSelectables();
            uponPopulatingPage(this);
        }
        public void ClearInterface(bool refresh = false)
        {
            this.ClearMenuObjectIList(otherArenaPlayerButtons);
            otherArenaPlayerButtons = refresh ? [] : null;
        }
        public void ActivateButtons()
        {
            if (viewPrevPlayer == null)
            {
                viewPrevPlayer = new(menu, owner, "Menu_Symbol_Arrow", "VIEWPREV", new((perPage + 2) * XOffset, 20));
                viewPrevPlayer.symbolSprite.rotation = 270f;
                subObjects.Add(viewPrevPlayer);
            }
            if (viewNextPlayer == null)
            {
                viewNextPlayer = new(menu, owner, "Menu_Symbol_Arrow", "VIEWNEXT", new(viewPrevPlayer.pos.x, viewPrevPlayer.pos.y + 40));
                viewNextPlayer.symbolSprite.rotation = 90f;
                subObjects.Add(viewNextPlayer);
            }
            RefreshSelectables();
        }
        public void DeactivateButtons()
        {
            this.ClearMenuObject(ref viewPrevPlayer);
            this.ClearMenuObject(ref viewNextPlayer);
        }
        public void RefreshSelectables()
        {
            menu.TryMutualBind(meButton, otherArenaPlayerButtons.FirstOrDefault(), true);
            menu.TryMutualBind(viewPrevPlayer, viewNextPlayer, true);
            menu.TryMutualBind(otherArenaPlayerButtons.LastOrDefault(), viewNextPlayer, true);
            viewPrevPlayer.TryBind(otherArenaPlayerButtons.LastOrDefault(), left: true);
        }
        public ArenaOnlinePlayerJoinButton GetArenaPlayerButton(Vector2 pos, OnlinePlayer onlinePlayer)
        {
            ArenaOnlinePlayerJoinButton playerButton = new(menu, this, pos, 0, onlinePlayer, OnlineManager.lobby.isOwner && !onlinePlayer.isMe)
            {
                profileIdentifier = onlinePlayer
            };
            if (playerButton.profileIdentifier == OnlineManager.mePlayer)
            {
                playerButton.buttonBehav.greyedOut = false;
                playerButton.readyForCombat = true;
            }
            return playerButton;
        }
        public const string PREVSINGAL = "ARENAONLINESLUGCATBUTTONS_PREV", NEXTSINGAL = "ARENAONLINESLUGCATBUTTONS_NEXT";
        private int currentOffset, perPage;
        public SimplerSymbolButton? viewNextPlayer, viewPrevPlayer;
        public ArenaOnlinePlayerJoinButton? meButton;
        public ArenaOnlinePlayerJoinButton[]? otherArenaPlayerButtons = [];
        public List<OnlinePlayer> onlinePlayers;
        public Action<ArenaOnlineSlugcatButtons> uponPopulatingPage;
    }
}

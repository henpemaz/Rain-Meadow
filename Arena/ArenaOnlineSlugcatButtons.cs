using Menu;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
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
        public int CurrentOffset { get => currentOffset; set => currentOffset = Mathf.Clamp(value, 0, MaxOffset); }
        public int MaxOffset => (otherOnlinePlayers?.Count > 0? otherOnlinePlayers.Count -1 : 0) / PerPage;
        public int PerPage { get => perPage; set => perPage = Mathf.Max(value, 1); } //excludes first button
        public bool PagesOn
        {
            get
            {
                int count = otherOnlinePlayers?.Count ?? 0;
                RainMeadow.Debug($"PlayerCount: {count}");
                return otherOnlinePlayers?.Count > PerPage;
            }
        }
        public ArenaOnlineSlugcatButtons(Menu.Menu menu, MenuObject owner, Vector2 pos, List<OnlinePlayer> otherOnlinePlayers, Action<ArenaOnlineSlugcatButtons> uponPopulate) : base(menu, owner, pos)
        {
            currentOffset = 0;
            PerPage = 3;
            uponPopulatingPage = uponPopulate;
            this.otherOnlinePlayers = otherOnlinePlayers;
            meButton = GetArenaPlayerButton(Vector2.zero, OnlineManager.mePlayer);
            subObjects.Add(meButton);
            PopulatePage(CurrentOffset);
        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            this.ClearMenuObject(ref meButton);
          
            DeactivatePageButtons();
            ClearInterface();
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (pageStater != null)
            {
                pageStater.text = $"{CurrentOffset + 1}/{MaxOffset + 1}";
            }
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
            PopulatePage((CurrentOffset - 1 < 0) ? ((otherOnlinePlayers?.Count > 0) ? ((otherOnlinePlayers.Count - 1) / PerPage) : 0) : (CurrentOffset - 1));
        }
        public void NextPage()
        {
            menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            PopulatePage(otherOnlinePlayers == null || otherOnlinePlayers.Count == 0 || CurrentOffset + 1 > (otherOnlinePlayers.Count - 1) / PerPage ? 0 : (CurrentOffset + 1));
        }
        public void PopulatePage(int offset)
        {
            ClearInterface(true);
            CurrentOffset = offset;
            List<ArenaOnlinePlayerJoinButton> newArenaPlayerButtons = [];
            int num = CurrentOffset * PerPage;
            RainMeadow.Debug($"Player count without me check: {otherOnlinePlayers?.Count ?? 0}");
            while (num < otherOnlinePlayers?.Count && num < (CurrentOffset + 1) * PerPage)
            {
                ArenaOnlinePlayerJoinButton playerButton = GetArenaPlayerButton(new(((num % PerPage) + 1) * XOffset, 0), otherOnlinePlayers[num]);
                subObjects.Add(playerButton);
                menu.TryMutualBind(newArenaPlayerButtons.GetValueOrDefault((num - 1) % PerPage), playerButton, true);
                newArenaPlayerButtons.Add(playerButton);
                num++;
            }
            otherArenaPlayerButtons = [.. newArenaPlayerButtons];
            uponPopulatingPage(this);
            if (PagesOn)
            {
                ActivatePageButtons();
                return;
            }
            DeactivatePageButtons();
        }
        public void ClearInterface(bool refresh = false)
        {
            this.ClearMenuObjectIList(otherArenaPlayerButtons);
            otherArenaPlayerButtons = refresh ? [] : null;
        }
        public void ActivatePageButtons()
        {
            if (viewPrevPlayer == null)
            {
                viewPrevPlayer = new(menu, this, "Menu_Symbol_Arrow", PREVSINGAL, new((PerPage + 1) * XOffset - 10, 20));
                viewPrevPlayer.symbolSprite.rotation = 270;
                subObjects.Add(viewPrevPlayer);
            }
            if (viewNextPlayer == null)
            {
                viewNextPlayer = new(menu, this, "Menu_Symbol_Arrow", NEXTSINGAL, new(viewPrevPlayer.pos.x, viewPrevPlayer.pos.y + 40));
                viewNextPlayer.symbolSprite.rotation = 90;
                subObjects.Add(viewNextPlayer);
            }
            if (pageStater == null)
            {
                pageStater = new(menu, this, "", new((PerPage + 2) * XOffset, 70), new(70, 30), false);
                subObjects.Add(pageStater);
            }
            RefreshSelectables();
        }
        public void DeactivatePageButtons()
        {
            this.ClearMenuObject(ref viewPrevPlayer);
            this.ClearMenuObject(ref viewNextPlayer);
            this.ClearMenuObject(ref pageStater);
            RefreshSelectables();
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
            ArenaOnlinePlayerJoinButton playerButton = new(menu, this, pos, 0, onlinePlayer, OnlineManager.lobby?.isOwner == true && !onlinePlayer.isMe);
            if (playerButton.profileIdentifier?.isMe == true)
            {
                playerButton.buttonBehav.greyedOut = false;
                playerButton.readyForCombat = true;
            }
            return playerButton;
        }
        public const string PREVSINGAL = "ARENAONLINESLUGCATBUTTONS_PREV", NEXTSINGAL = "ARENAONLINESLUGCATBUTTONS_NEXT";
        private int currentOffset, perPage;
        public MenuLabel? pageStater;
        public SimplerSymbolButton? viewNextPlayer, viewPrevPlayer;
        public ArenaOnlinePlayerJoinButton? meButton;
        public ArenaOnlinePlayerJoinButton[]? otherArenaPlayerButtons = [];
        public List<OnlinePlayer>? otherOnlinePlayers;
        public Action<ArenaOnlineSlugcatButtons> uponPopulatingPage;
    }
}

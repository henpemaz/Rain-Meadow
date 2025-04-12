using Menu;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class SpectatorOverlay : Menu.Menu
    {
        public static int MaxVisibleOnList => 8;
        public static float ButtonSpacingOffset => 8;
        public static float ButtonSize => 30;
        public List<PlayerButton> PlayerButtons => playerScroller.GetSpecificButtons<PlayerButton>();
        public SpectatorOverlay(ProcessManager manager, RainWorldGame game) : base(manager, RainMeadow.Ext_ProcessID.SpectatorMode)
        {
            this.game = game;
            pages.Add(new Page(this, null, "spectator", 0));
            selectedObject = null;
            pos = new Vector2(1180, 553);
            pages[0].subObjects.Add(new Menu.MenuLabel(this, this.pages[0], this.Translate("PLAYERS"), this.pos, new(110, 30), true));
            playerScroller = new(this, pages[0], new(pos.x, pos.y - 38 - ButtonScroller.CalculateHeightBasedOnAmtOfButtons(MaxVisibleOnList, ButtonSize, ButtonSpacingOffset)), MaxVisibleOnList, 200, ButtonSize, ButtonSpacingOffset);
            pages[0].subObjects.Add(playerScroller);
            playerScroller.ConstrainScroll();
        }

        private bool UpdateList()
        {
            List<PlayerButton> playerButtons = PlayerButtons;
            List<OnlinePlayer> newPlayers = [..OnlineManager.players.OrderBy(onlineP => onlineP.isMe ? 0 : 1)]; // will keep this logic for LAN
            List<OnlinePhysicalObject> realizedPlayers = [.. OnlineManager.lobby.playerAvatars.Select(kv => kv.Value.FindEntity(true)).OfType<OnlinePhysicalObject>().OrderBy(opo => opo.isMine ? 0 : 1)];
            if (newPlayers.Count == playerButtons.Count) return false; // race condition that will Never Happen(TM)

            for (int i = playerButtons.Count - 1; i >= 0; i--)
            {
                PlayerButton button = playerButtons[i];
                if (newPlayers.Contains(button.player))
                {
                    newPlayers.Remove(button.player);
                    continue;
                }
                playerScroller.RemoveButton(i, false);
            }
            foreach (OnlinePlayer player in newPlayers)
            {
                OnlinePhysicalObject? foundPlayer = realizedPlayers.FirstOrDefault(x => x.owner == player);
                PlayerButton button = foundPlayer == null? new(this, playerScroller, player, null, OnlineManager.lobby.isOwner && !player.isMe) : new(this, playerScroller, player, foundPlayer, OnlineManager.lobby.isOwner && !foundPlayer.isMine);
                playerScroller.AddScrollObjects(button);
            }
            playerScroller.ConstrainScroll();
            return true;
        }
        public override void Update()
        {
            base.Update();
            UpdateList();
            foreach (PlayerButton button in PlayerButtons)
            {
                AbstractCreature? ac = button.opo?.apo as AbstractCreature;
                button.toggled = ac != null && ac == spectatee;
                button.buttonBehav.greyedOut = ac is null || (ac.state.dead || (ac.realizedCreature != null && ac.realizedCreature.State.dead));
            }
        }
        public override string UpdateInfoText()
        {
            return GetIHaveDescriptionObject(selectedObject)?.Description ?? base.UpdateInfoText();
        }
        public IHaveADescription? GetIHaveDescriptionObject(MenuObject obj)
        {
            MenuObject menuObj = obj;
            while (menuObj != null && menuObj is not IHaveADescription)
            {
                menuObj = menuObj.owner;
            }
            return menuObj as IHaveADescription;
        }
        public void SpectatePlayer(AbstractCreature? aC)
        {
            spectatee = aC;
        }
        public AbstractCreature? spectatee;
        public Vector2 pos;
        public RainWorldGame game;
        public ButtonScroller playerScroller;
        public class PlayerButton : ButtonScroller.ScrollerButton
        {
            public bool BanClickedOnce
            {
                get
                {
                    return banClickedOnce;
                }
                set
                {
                    if (value != banClickedOnce)
                    {
                        HoldKick = true;
                        banClickedOnce = value;
                        labelColor = banClickedOnce ? MenuColor(MenuColors.DarkRed) : MenuColor(MenuColors.MediumGrey);
                        Description = banClickedOnce ? "Press again to ban player! Select another button if you dont want to!" : "";
                        menu.infolabelDirty = true;
                        HoldKick = false;
                    }
                }
            }
            public bool HoldKick
            {
                set
                {
                    if (kickbutton != null)
                    {
                        kickbutton.buttonBehav.greyedOut = value;
                    }
                }
            }
            public bool MutePlayer
            {
                get => OnlineManager.lobby.gameMode.mutedPlayers.Contains(player.id.name);
                set
                {
                    string name = player.id.name;
                    if (value != MutePlayer)
                    {
                        if (value)
                        {
                            RainMeadow.Debug($"Added {name} to mute list");
                            OnlineManager.lobby.gameMode.mutedPlayers.Add(name);
                        }
                        else
                        {
                            RainMeadow.Debug($"Removed {name} from mute list");
                            OnlineManager.lobby.gameMode.mutedPlayers.Remove(name);
                        }
                        menu.PlaySound(value? SoundID.MENU_Checkbox_Check : SoundID.MENU_Checkbox_Uncheck);
                        kickbutton?.UpdateSymbol(ClientMuteSymbol);
                    }
                }
            }
            private string ClientMuteSymbol => MutePlayer ? "FriendA" : "Menu_Symbol_Clear_All";
            public SpectatorOverlay? Overlay
            {
                get => menu as SpectatorOverlay;
            }
            public PlayerButton(SpectatorOverlay menu, MenuObject owner, OnlinePlayer player, OnlinePhysicalObject? opo, bool canKick = false, Vector2 size = default) : base(menu, owner, player.id.name, Vector2.zero, size == default ? new(110, 30) : size)
            {
                this.player = player;
                this.opo = opo;
                OnClick += (_) =>
                {
                    toggled ^= true;
                    Overlay?.SpectatePlayer(toggled ? this.opo?.apo as AbstractCreature : null);
                };
                if (canKick)
                {
                    kickbutton = new(menu, this, "Menu_Symbol_Clear_All", "KICKPLAYER", new(this.size.x + 10, 0));
                    kickbutton.OnClick += (_) =>
                    {
                        if (BanClickedOnce)
                        {
                            BanHammer.BanUser(player);
                            menu.PlaySound(SoundID.MENU_Remove_Level);
                        }
                        else
                        {
                            BanClickedOnce = true;
                            menu.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
                        }
                    };
                    subObjects.Add(kickbutton);
                }
                if (kickbutton == null && player != OnlineManager.mePlayer)  //prevent double sprites and double updating
                {
                    kickbutton = new(menu, this, ClientMuteSymbol, "MUTEPLAYER", new(this.size.x + 10, 0));
                    kickbutton.OnClick += (_) =>
                    {
                        MutePlayer ^= true;
                    };
                    subObjects.Add(kickbutton);
                }
                menu.TryMutualBind(this, kickbutton, true);
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                this.ClearMenuObject(ref kickbutton);
            }
            public override void UpdateAlpha(float alpha)
            {
                base.UpdateAlpha(alpha);
                if (kickbutton != null)
                {
                    kickbutton.symbolSprite.alpha = alpha;
                    for (int i = 0; i < kickbutton.roundedRect.sprites.Length; i++)
                    {
                        kickbutton.roundedRect.sprites[i].alpha = alpha;
                        kickbutton.roundedRect.fillAlpha = alpha / 2;
                    }
                    kickbutton.GetButtonBehavior.greyedOut = alpha < 1;
                }
            }
            public override void Update()
            {
                base.Update();
                BanClickedOnce = menu.selectedObject == kickbutton && BanClickedOnce;
            }

            public bool banClickedOnce;
            public OnlinePlayer player;
            public OnlinePhysicalObject? opo;
            public SimplerSymbolButton? kickbutton;
        }
    }
}

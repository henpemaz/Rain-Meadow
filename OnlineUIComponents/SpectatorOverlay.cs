using Menu;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class SpectatorOverlay : Menu.Menu
    {
        public AbstractCreature? spectatee;

        public Vector2 pos;

        public class PlayerButton
        {
            public OnlinePlayer player;
            public OnlinePhysicalObject opo;
            public SimplerButton button;
            public SimplerSymbolButton? kickbutton;
            public bool mutePlayer
            {
                get => OnlineManager.lobby.gameMode.mutedPlayers.Contains(player.id.name);
                set
                {
                    var name = player.id.name;
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
                }
            }
            private string clientMuteSymbol => mutePlayer ? "FriendA" : "Menu_Symbol_Clear_All";
            public Vector2 pos
            {
                set
                {
                    button.pos = value;
                    if (kickbutton != null)
                        kickbutton.pos = value + new Vector2(120, 0);
                }
            }
            public SpectatorOverlay overlay;

            public PlayerButton(SpectatorOverlay menu, OnlinePlayer player, OnlinePhysicalObject? opo, Vector2 pos, bool canKick = false)
            {
                this.overlay = menu;
                this.player = player;
                this.opo = opo ?? null;
                this.button = new SimplerButton(menu, menu.pages[0], player.id.name, pos, new Vector2(110, 30));

                this.button.OnClick += (_) =>
                {
                    this.button.toggled ^= true;
                    overlay.spectatee = this.button.toggled ? opo?.apo as AbstractCreature : null;
                };
                this.button.owner.subObjects.Add(button);
                if (canKick)
                {
                    this.kickbutton = new SimplerSymbolButton(menu, menu.pages[0], "Menu_Symbol_Clear_All", "KICKPLAYER", pos + new Vector2(120, 0));
                    this.kickbutton.OnClick += (_) => BanHammer.BanUser(player);
                    this.kickbutton.owner.subObjects.Add(kickbutton);
                }
                if (player != OnlineManager.mePlayer)
                {
                    this.kickbutton = new SimplerSymbolButton(menu, menu.pages[0], clientMuteSymbol, "MUTEPLAYER", pos + new Vector2(120, 0));
                    this.kickbutton.OnClick += (_) =>
                    {
                        mutePlayer ^= true;
                        this.kickbutton.UpdateSymbol(clientMuteSymbol);
                    };
                    this.kickbutton.owner.subObjects.Add(kickbutton);
                }
                this.pos = pos;
            }

            public void Destroy()
            {
                this.button.RemoveSprites();
                this.button.page.RemoveSubObject(this.button);
                if (this.kickbutton != null)
                {
                    this.kickbutton.RemoveSprites();
                    this.kickbutton.page.RemoveSubObject(this.kickbutton);
                }
            }
        }

        public RainWorldGame game;
        public List<PlayerButton> playerButtons;

        public SpectatorOverlay(ProcessManager manager, RainWorldGame game) : base(manager, RainMeadow.Ext_ProcessID.SpectatorMode)
        {
            this.game = game;
            this.pages.Add(new Page(this, null, "spectator", 0));
            this.selectedObject = null;
            this.playerButtons = new();
            this.pos = new Vector2(1180, 553);
            this.pages[0].subObjects.Add(new Menu.MenuLabel(this, this.pages[0], this.Translate("PLAYERS"), this.pos, new(110, 30), true));
        }

        private bool UpdateList()
        {
            List<OnlinePlayer> newPlayers = OnlineManager.players
                .OrderBy(onlineP => onlineP.isMe ? 0 : 1)
                .ToList(); // will keep this logic for LAN

            List<OnlinePhysicalObject> realizedPlayers = OnlineManager.lobby.playerAvatars
                .Select(kv => kv.Value.FindEntity(true))
                .OfType<OnlinePhysicalObject>()
                .OrderBy(opo => opo.isMine ? 0 : 1)
                .ToList();

            if (newPlayers.Count == playerButtons.Count) return false; // race condition that will Never Happen(TM)

            var offset = new Vector2(0, 38);
            var pos = this.pos - offset;

            for (var i = playerButtons.Count - 1; i >= 0; i--)
            {
                var button = playerButtons[i];
                if (newPlayers.Contains(button.player))
                {
                    newPlayers.Remove(button.player);
                    button.pos = pos;
                    pos -= offset;
                }
                else
                {
                    button.Destroy();
                    playerButtons.RemoveAt(i);
                }
            }

            foreach (var player in newPlayers)
            {
                var foundPlayer = realizedPlayers.FirstOrDefault(x => x.owner == player);
                if (foundPlayer == null) // player joined but is unrealized, null them out
                {
                    playerButtons.Add(new PlayerButton(this, player, null, pos, OnlineManager.lobby.isOwner && !player.isMe));
                } else
                {
                    playerButtons.Add(new PlayerButton(this, player, foundPlayer, pos, OnlineManager.lobby.isOwner && !foundPlayer.isMine));

                }
                pos -= offset;

            }

            return true;
        }

        public override void Update()
        {
            base.Update();
            UpdateList();

            foreach (var button in playerButtons)
            {
                var ac = button.opo.apo as AbstractCreature;
                button.button.toggled = ac != null && ac == spectatee;
                button.button.buttonBehav.greyedOut = ac is null || (ac.state.dead || (ac.realizedCreature != null && ac.realizedCreature.State.dead));
            }
        }
    }
}

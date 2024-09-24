using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class SpectatorOverlay : Menu.Menu
    {
        public RainWorldGame game;
        public SpectatorOverlay spectatorOverlay;
        public HashSet<AbstractCreature> uniqueACs;

        public SpectatorOverlay(ProcessManager manager, RainWorldGame game) : base(manager, RainMeadow.Ext_ProcessID.SpectatorMode)

        {
            this.game = game;
            this.pages.Add(new Page(this, null, "spectator", 0));
            this.selectedObject = null;
            uniqueACs = new HashSet<AbstractCreature>();

            InitSpectatorMode();

        }

        public override void Update()
        {
            base.Update();

            foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Values)
            {
                if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue;

                if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac)
                {
                    uniqueACs.Add(ac);
                }
            }

            List<SimplerButton> existingButtons = this.pages[0].subObjects.OfType<SimplerButton>().ToList();
            int existingButtonCount = existingButtons.Count;
            int newAcListCount = uniqueACs.Count;

            if (existingButtonCount != newAcListCount)
            {
                // Remove all existing buttons when our AC list changes
                foreach (var button in existingButtons)
                {
                    button.RemoveSprites();
                    this.pages[0].RemoveSubObject(button);
                }

                // Create and add new buttons
                for (int i = 0; i < newAcListCount; i++)
                {
                    var ac = uniqueACs.ElementAt(i);
                    var username = "";
                    if (!OnlinePhysicalObject.map.TryGetValue(uniqueACs.ElementAt(i), out var onlinePlayer))
                    {
                        RainMeadow.Error("Error getting onlineplayer in spectator hud");
                    }

                    username = onlinePlayer.owner.id.name;

                    this.pages[0].subObjects.Add(new Menu.MenuLabel(this, this.pages[0], this.Translate("PLAYERS"), new UnityEngine.Vector2(1180, 553), new(110, 30), true));
                    var btn = new SimplerButton(this, this.pages[0], username, new UnityEngine.Vector2(1180, 515) - i * new UnityEngine.Vector2(0, 38), new(110, 30));
                    this.pages[0].subObjects.Add(btn);
                    btn.toggled = false;

                    if (OnlineManager.lobby.isOwner && i > 0)
                    {


                        var kickPlayer = new SimplerSymbolButton(this, this.pages[0], "Menu_Symbol_Clear_All", "KICKPLAYER", new Vector2(1300, 515) - i * new UnityEngine.Vector2(0, 38));
                        kickPlayer.OnClick += (_) => BanHammer.BanUser(onlinePlayer);
                        this.pages[0].subObjects.Add(kickPlayer);
                    }
                }
            }

            // Update button states
            bool hasAnyAcInGateRoom = false;

            for (int i = 0; i < newAcListCount; i++)
            {
                var ac = uniqueACs.ElementAt(i);
                var button = this.pages[0].subObjects.OfType<SimplerButton>().ElementAt(i);

                // Check if any AC is in the gate room
                if (ac.Room.gate)
                {
                    hasAnyAcInGateRoom = true;

                }

                // Disable button if AC is dead or if we're in gate room mode
                if ((ac.state.dead ||
                     (ac.realizedCreature != null && ac.realizedCreature.State.dead)) ||
                    (hasAnyAcInGateRoom && !ac.Room.gate))
                {
                    button.inactive = true;
                    button.buttonBehav.greyedOut = true;
                    button.OnClick += (_) =>
                    {
                        /* No action on click, slugs are dead or in gate room mode */
                    };
                }
                else
                {
                    button.inactive = false;

                }

            }


        }

        public void InitSpectatorMode()
        {
            if (OnlineManager.lobby.gameMode is StoryGameMode)
            {
                foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Values)
                {
                    if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue;
                    if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac && !uniqueACs.Contains(ac))
                    {

                        uniqueACs.Add(ac);

                    }
                }


                for (int i = 0; i < uniqueACs.Count; i++)
                {
                    var username = "";
                    if (!OnlinePhysicalObject.map.TryGetValue(uniqueACs.ElementAt(i), out var onlinePlayer))
                    {
                        RainMeadow.Error("Error getting onlineplayer in spectator hud");
                    }

                    username = onlinePlayer.owner.id.name;


                    this.pages[0].subObjects.Add(new Menu.MenuLabel(this, this.pages[0], this.Translate("PLAYERS"), new UnityEngine.Vector2(1180, 553), new(110, 30), true));
                    var btn = new SimplerButton(this, this.pages[0], username, new UnityEngine.Vector2(1180, 515) - i * new UnityEngine.Vector2(0, 38), new(110, 30));
                    this.pages[0].subObjects.Add(btn);
                    btn.toggled = false;

                    if (OnlineManager.lobby.isOwner && i > 0)
                    {

                        var kickPlayer = new SimplerSymbolButton(this, this.pages[0], "Menu_Symbol_Clear_All", "KICKPLAYER", new Vector2(1300, 515) - i * new UnityEngine.Vector2(0, 38));
                        kickPlayer.OnClick += (_) => BanHammer.BanUser(onlinePlayer);
                        this.pages[0].subObjects.Add(kickPlayer);
                    }

                    bool hasAnyAcInGateRoom = false;
                    var ac = uniqueACs.ElementAt(i);
                    // Disable button if AC is dead or if we're in gate room mode. We cannot have a situation where a realizedCreature becomes null during a room check
                    if ((ac.state.dead ||
                    (ac.realizedCreature != null && ac.realizedCreature.State.dead)) ||
                    (hasAnyAcInGateRoom && !ac.Room.gate))
                    {
                        btn.inactive = true;
                        btn.buttonBehav.greyedOut = true;
                        btn.OnClick += (_) => { /* No action on click, slugs are dead or in gate room mode */ };
                    }
                    else
                    {
                        btn.inactive = false;
                        btn.buttonBehav.greyedOut = false;
                        btn.OnClick += (_) =>
                        {

                            this.game.cameras[0].followAbstractCreature = ac;

                            if (ac.Room.realizedRoom == null)
                            {
                                this.game.world.ActivateRoom(ac.Room);
                            }

                            RainMeadow.Debug("Following " + ac);

                            if ((this.game.cameras[0].room.abstractRoom != ac.Room))
                            {
                                this.game.cameras[0].MoveCamera(ac.Room.realizedRoom, -1);
                            }
                            btn.toggled = !btn.toggled;


                            // Set all other buttons to false
                            foreach (var otherBtn in this.pages[0].subObjects.OfType<SimplerButton>())
                            {
                                if (otherBtn != btn)
                                {
                                    otherBtn.toggled = false;
                                }
                            }

                        };
                    }
                }
            }

        }


    }
}
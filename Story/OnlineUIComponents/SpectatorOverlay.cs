using Menu;
using System.Collections.Generic;
using System.Linq;
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


        private void ScanAndUpdateACList()
        {
            if (OnlineManager.lobby.playerAvatars.Count > uniqueACs.Count)
            {
                foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
                {
                    if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue;

                    if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac)
                    {
                        uniqueACs.Add(ac);
                    }
                }
            }
            if (OnlineManager.lobby.playerAvatars.Count < uniqueACs.Count)
            {
                uniqueACs.Clear();
            }
        }

        private void InitSpectatorMode()
        {
            if (OnlineManager.lobby.gameMode is StoryGameMode)
            {
                ScanAndUpdateACList();

                for (int i = 0; i < uniqueACs.Count; i++)
                {
                    var username = "";
                    if (!OnlinePhysicalObject.map.TryGetValue(uniqueACs.ElementAt(i), out var onlinePlayer))
                    {
                        RainMeadow.Error("Error getting onlineplayer in spectator hud");
                    }

                    username = onlinePlayer.owner.id.name;
                    var ac = uniqueACs.ElementAt(i);


                    this.pages[0].subObjects.Add(new Menu.MenuLabel(this, this.pages[0], this.Translate("PLAYERS"), new UnityEngine.Vector2(1180, 553), new(110, 30), true));
                    var btn = new SimplerButton(this, this.pages[0], username, new UnityEngine.Vector2(1180, 515) - i * new UnityEngine.Vector2(0, 38), new(110, 30));
                    this.pages[0].subObjects.Add(btn);
                    btn.toggled = false;

                    if (OnlineManager.lobby.isOwner && i > 0)
                    {

                        var kickPlayer = new SimplerSymbolButton(this, this.pages[0], "Menu_Symbol_Clear_All", "KICKPLAYER", new Vector2(1300, 515) - i * new UnityEngine.Vector2(0, 38));
                        kickPlayer.OnClick += (_) =>
                        {
                            BanHammer.BanUser(onlinePlayer.owner as OnlinePlayer);
                        };
                        this.pages[0].subObjects.Add(kickPlayer);
                    }

                    bool hasAnyAcInGateRoom = false;
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
                           

                            if (!OnlinePhysicalObject.map.TryGetValue(ac, out var onlineACOwner))
                            {
                                RainMeadow.Error("Error getting online AC during spectate call!");
                                return; 
                            }


                            if (onlineACOwner.owner != OnlineManager.mePlayer)
                            {
                                OnlineManager.mePlayer.isActuallySpectating = true; // I want to view a remote player outside my current position

                            }
                            else
                            {
                                OnlineManager.mePlayer.isActuallySpectating = false; // I want to regain control of where I am

                            }


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

        public override void Update()
        {
            base.Update();

            ScanAndUpdateACList();

            List<SimplerButton> playerList = this.pages[0].subObjects.OfType<SimplerButton>().ToList();
            List<SimplerSymbolButton> xButtons = this.pages[0].subObjects.OfType<SimplerSymbolButton>().ToList();
            if (playerList.Count != uniqueACs.Count)
            {
                // Remove all existing buttons when our AC list changes
                foreach (var button in playerList)
                {
                    button.RemoveSprites();
                    this.pages[0].RemoveSubObject(button);
                }

                foreach (var button in xButtons)
                {
                    button.RemoveSprites();
                    this.pages[0].RemoveSubObject(button);
                }

                // Create and add new buttons
                for (int i = 0; i < uniqueACs.Count; i++)
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
                        kickPlayer.OnClick += (_) =>
                        {
                            BanHammer.BanUser(onlinePlayer.owner as OnlinePlayer);

                        };

                        this.pages[0].subObjects.Add(kickPlayer);
                    }
                }
            }
            // Update button states
            bool hasAnyAcInGateRoom = false;

            for (int i = 0; i < uniqueACs.Count; i++)
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
                        // noop
                    };
                }
                else
                {
                    button.inactive = false;

                }

            }


        }


    }
}
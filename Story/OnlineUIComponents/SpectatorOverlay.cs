using System.Collections.Generic;
using System.Linq;
using Menu;

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
                    try
                    {
                        OnlinePhysicalObject.map.TryGetValue(uniqueACs.ElementAt(i), out var alivePlayer);
                        username = (alivePlayer.owner.id as SteamMatchmakingManager.SteamPlayerId).name;
                    }
                    catch
                    {
                        username = $"{uniqueACs.ElementAt(i)}";
                    }

                    this.pages[0].subObjects.Add(new Menu.MenuLabel(this, this.pages[0], this.Translate("PLAYERS"), new UnityEngine.Vector2(1190, 553), new(110, 30), true));
                    var btn = new SimplerButton(this, this.pages[0], username, new UnityEngine.Vector2(1190, 515) - i * new UnityEngine.Vector2(0, 38), new(110, 30));
                    this.pages[0].subObjects.Add(btn);
                    btn.toggled = false;
                }
            }

            // Update button states
            for (int i = 0; i < newAcListCount; i++)
            {
                var ac = uniqueACs.ElementAt(i);
                var button = this.pages[0].subObjects.OfType<SimplerButton>().ElementAt(i);

                if (ac.state.dead || (ac.realizedCreature != null && ac.realizedCreature.State.dead))
                {
                    button.inactive = true;
                    button.OnClick += (_) => { /* No action on click, slugs are dead */ };
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
                    try
                    {
                        OnlinePhysicalObject.map.TryGetValue(uniqueACs.ElementAt(i), out var alivePlayer);
                        username = (alivePlayer.owner.id as SteamMatchmakingManager.SteamPlayerId).name;
                    }
                    catch
                    {
                        username = $"{uniqueACs.ElementAt(i)}";
                    }

                    this.pages[0].subObjects.Add(new Menu.MenuLabel(this, this.pages[0], this.Translate("PLAYERS"), new UnityEngine.Vector2(1190, 553), new(110, 30), true));
                    var btn = new SimplerButton(this, this.pages[0], username, new UnityEngine.Vector2(1190, 515) - i * new UnityEngine.Vector2(0, 38), new(110, 30));
                    this.pages[0].subObjects.Add(btn);
                    btn.toggled = false;

                    // Introduce a local variable to hold the current index
                    int currentCreature = i;
                    if (uniqueACs.ElementAt(currentCreature).state.dead || uniqueACs.ElementAt(currentCreature).realizedCreature != null && uniqueACs.ElementAt(currentCreature).realizedCreature.State.dead)
                    {
                        btn.inactive = true;
                        btn.OnClick += (_) =>
                        {
                            { /* No action on click */ };
                        };
                    }
                    else
                    {
                        btn.inactive = false;
                        btn.OnClick += (_) =>
                        {
                            this.game.cameras[0].followAbstractCreature = uniqueACs.ElementAt(currentCreature);

                            if (uniqueACs.ElementAt(currentCreature).Room.realizedRoom == null)
                            {
                                this.game.world.ActivateRoom(uniqueACs.ElementAt(currentCreature).Room);
                            }

                            RainMeadow.Debug("Following " + uniqueACs.ElementAt(currentCreature));

                            if ((this.game.cameras[0].room.abstractRoom != uniqueACs.ElementAt(currentCreature).Room))
                            {
                                this.game.cameras[0].MoveCamera(uniqueACs.ElementAt(currentCreature).Room.realizedRoom, -1);
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

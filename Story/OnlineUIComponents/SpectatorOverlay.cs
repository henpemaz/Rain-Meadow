using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Menu;

namespace RainMeadow
{
    public class SpectatorOverlay : Menu.Menu
    {
        public RainWorldGame game;
        private List<AbstractCreature> acList;
        public SpectatorOverlay spectatorOverlay;

        public SpectatorOverlay(ProcessManager manager, RainWorldGame game) : base(manager, RainMeadow.Ext_ProcessID.SpectatorMode)

        {
            this.game = game;
            this.pages.Add(new Page(this, null, "spectator", 0));
            this.selectedObject = null;
            this.acList = new List<AbstractCreature>();
            InitSpectatorMode();



        }

        public override void Update()
        {
            base.Update();

            //InitSpectatorMode();


        }
        public void InitSpectatorMode()
        {
            if (OnlineManager.lobby.gameMode is StoryGameMode)
            {

                foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Values)
                {
                    if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue;
                    if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac && !acList.Contains(ac))
                    {

                        acList.Add(ac);
                    }
                }


                for (int i = 0; i < acList.Count; i++)
                {
                    var username = "";
                    try
                    {
                        OnlinePhysicalObject.map.TryGetValue(acList[i], out var alivePlayer);
                        username = (alivePlayer.owner.id as SteamMatchmakingManager.SteamPlayerId).name;
                    }
                    catch
                    {
                        username = $"{acList[i]}";
                    }

                    this.pages[0].subObjects.Add(new Menu.MenuLabel(this, this.pages[0], this.Translate("PLAYERS"), new UnityEngine.Vector2(1190, 553), new(110, 30), true));
                    var btn = new SimplerButton(this, this.pages[0], username, new UnityEngine.Vector2(1190, 515) - i * new UnityEngine.Vector2(0, 38), new(110, 30));
                    this.pages[0].subObjects.Add(btn);
                    btn.toggled = false;

                    // Introduce a local variable to hold the current index
                    int currentCreature = i;
                    if (acList[currentCreature].state.dead)
                    {
                        btn.inactive = true;
                        btn.OnClick += (_) =>
                        {
                            return;
                        };
                    }
                    else
                    {
                        btn.inactive = false;
                        btn.OnClick += (_) =>
                        {
                            this.game.cameras[0].followAbstractCreature = acList[currentCreature];

                            if (acList[currentCreature].Room.realizedRoom == null)
                            {
                                this.game.world.ActivateRoom(acList[currentCreature].Room);
                            }

                            RainMeadow.Debug("Following " + acList[currentCreature]);

                            if ((this.game.cameras[0].room.abstractRoom != acList[currentCreature].Room))
                            {
                                this.game.cameras[0].MoveCamera(acList[currentCreature].Room.realizedRoom, -1);
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

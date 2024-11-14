using HUD;
using UnityEngine;

namespace RainMeadow
{
    public class StoreHUD : HudPart
    {
        private RoomCamera camera;
        private RainWorldGame game;
        private StoreOverlay? spectatorOverlay;

        public StoreHUD(HUD.HUD hud, RoomCamera camera) : base(hud)
        {
            this.camera = camera;
            this.game = camera.game;
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);

            if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.SpectatorKey.Value))
            {
                if (spectatorOverlay == null)
                {
                    RainMeadow.Debug("Creating spectator overlay");
                    spectatorOverlay = new StoreOverlay(game.manager, game);
                }
                else
                {
                    RainMeadow.Debug("Spectate destroy!");
                    spectatorOverlay.ShutDownProcess();
                    spectatorOverlay = null;
                }
            }

            if (spectatorOverlay != null)
            {
                spectatorOverlay.GrafUpdate(timeStacker);
            }
        }

        public override void Update()
        {
            base.Update();
            if (spectatorOverlay != null)
            {
                if (RainMeadow.isStoryMode(out var _))
                {
                    if ((game.pauseMenu != null || camera.hud.map.visible || game.manager.upcomingProcess != null))
                    {
                        RainMeadow.Debug("Shutting down spectator overlay due to another process request");
                        spectatorOverlay.ShutDownProcess();
                        spectatorOverlay = null;
                        return;
                    }


                }

                if (RainMeadow.isArenaMode(out var _))
                {

                    if (game.arenaOverlay != null || game.pauseMenu != null || game.manager.upcomingProcess != null)
                    {
                        RainMeadow.Debug("Shutting down spectator overlay due to another process request");
                        spectatorOverlay.ShutDownProcess();
                        spectatorOverlay = null;
                        return;
                    }
                }

                spectatorOverlay.Update();
            }
        }
    }
}

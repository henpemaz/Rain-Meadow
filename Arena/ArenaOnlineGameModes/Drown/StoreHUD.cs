using Drown;
using HUD;
using RainMeadow;
using UnityEngine;
namespace RainMeadow
{
    public class StoreHUD : HudPart
    {
        private RoomCamera camera;
        private RainWorldGame game;
        private DrownMode drown;
        private DrownStoreOverlay? storeOverlay;

        public StoreHUD(HUD.HUD hud, RoomCamera camera, DrownMode drown) : base(hud)
        {
            this.camera = camera;
            this.game = camera.game;
            this.drown = drown;
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            if (RainMeadow.isArenaMode(out var arena))
            {
                if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.DrownStoreKey.Value))
                {
                    if (storeOverlay == null)
                    {
                        RainMeadow.Debug("Creating storeOverlay overlay");
                        storeOverlay = new DrownStoreOverlay(game.manager, game, drown, arena);
                    }
                    else
                    {
                        RainMeadow.Debug("storeOverlay destroy!");
                        storeOverlay.ShutDownProcess();
                        storeOverlay = null;
                    }
                }

                if (OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs) && cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings))
                {
                    clientSettings.isInStore = this.storeOverlay != null;
                }
                storeOverlay?.GrafUpdate(timeStacker);
            }

        }

        public override void Update()
        {
            base.Update();
            if (storeOverlay != null)
            {
                if (RainMeadow.isArenaMode(out var _))
                {

                    if (game.arenaOverlay != null || game.pauseMenu != null || game.manager.upcomingProcess != null)
                    {
                        RainMeadow.Debug("Shutting down storeOverlay overlay due to another process request");
                        storeOverlay.ShutDownProcess();
                        storeOverlay = null;
                        return;
                    }
                }

                storeOverlay.Update();
            }
        }
    }
}
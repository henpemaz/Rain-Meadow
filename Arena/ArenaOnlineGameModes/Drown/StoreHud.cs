using HUD;
using RainMeadow;
using UnityEngine;
namespace RainMeadow.Arena.ArenaOnlineGameModes.Drown
{
    public class StoreHUD : HudPart
    {
        private RoomCamera camera;
        private RainWorldGame game;
        private DrownMode drown;
        private StoreOverlay? storeOverlay;
        public bool active;

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
                if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.OpenStore.Value))
                {
                    if (storeOverlay == null)
                    {
                        RainMeadow.Debug("Creating storeOverlay overlay");
                        storeOverlay = new StoreOverlay(game.manager, game, drown, arena);
                        this.active = true;
                        this.drown.isInStore = true;
                        OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs);
                        if (cs != null)
                        {

                            cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                            if (clientSettings != null)
                            {
                                clientSettings.isInStore = true;
                            }
                        }


                    }
                    else
                    {
                        RainMeadow.Debug("storeOverlay destroy!");
                        this.drown.isInStore = false;
                        this.active = false;
                        OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs);
                        if (cs != null)
                        {

                            cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                            if (clientSettings != null)
                            {
                                clientSettings.isInStore = false;

                            }
                        }
                        storeOverlay.ShutDownProcess();
                        storeOverlay = null;
                    }
                }
            }

            if (storeOverlay != null)
            {
                storeOverlay.GrafUpdate(timeStacker);
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
                        this.drown.isInStore = false;
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
using HUD;
using UnityEngine;

namespace RainMeadow
{
    public class SpectatorHud : HudPart
    {
        private RoomCamera camera;
        private RainWorldGame game;
        private readonly OnlineGameMode onlineGameMode;
        private SpectatorOverlay? spectatorOverlay;
        private AbstractCreature? spectatee;

        public SpectatorHud(HUD.HUD hud, RoomCamera camera, OnlineGameMode onlineGameMode) : base(hud)
        {
            this.camera = camera;
            this.onlineGameMode = onlineGameMode;
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
                    spectatorOverlay = new SpectatorOverlay(game.manager, game);
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
                if (game.pauseMenu != null || camera.hud.map.visible || game.manager.upcomingProcess != null)
                {
                    RainMeadow.Debug("Shutting down spectator overlay due to another process request");
                    spectatorOverlay.ShutDownProcess();
                    spectatorOverlay = null;
                    return;
                }
                spectatorOverlay.Update();
                spectatee = spectatorOverlay.spectatee;
            }
            if (spectatee is AbstractCreature ac)
            {
                camera.followAbstractCreature = spectatee;
                if (spectatee.Room.realizedRoom == null)
                {
                    this.game.world.ActivateRoom(spectatee.Room);
                }
                if (camera.room.abstractRoom != spectatee.Room)
                {
                    camera.MoveCamera(spectatee.Room.realizedRoom, -1);
                }
            }
        }
    }
}

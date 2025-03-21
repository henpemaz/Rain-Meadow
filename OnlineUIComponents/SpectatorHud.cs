using HUD;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class SpectatorHud : HudPart
    {
        private RoomCamera camera;
        private RainWorldGame game;
        public  SpectatorOverlay? spectatorOverlay;
        private AbstractCreature? spectatee;
        public bool isActive;

        public SpectatorHud(HUD.HUD hud, RoomCamera camera) : base(hud)
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
                    spectatorOverlay = new SpectatorOverlay(game.manager, game);
                    isActive = true;
                }
                else
                {
                    RainMeadow.Debug("Spectate destroy!");
                    spectatorOverlay.ShutDownProcess();
                    spectatorOverlay = null;
                    isActive = false;
                }
            }

            if (spectatorOverlay != null)
            {
                spectatorOverlay.GrafUpdate(timeStacker);
                spectatorOverlay.pages[0].pos.y = InputOverride.MoveMenuItemFromYInput(spectatorOverlay.pages[0].pos.y);
            }
        }

        public override void Update()
        {
            base.Update();
            if (spectatorOverlay != null)
            {
                if (RainMeadow.isStoryMode(out _) || RainMeadow.isArenaMode(out _))
                {
                    if (game.pauseMenu != null || game.arenaOverlay != null || camera.hud?.map?.visible is true || game.manager.upcomingProcess != null)
                    {
                        RainMeadow.Debug("Shutting down spectator overlay due to another process request");
                        spectatorOverlay.ShutDownProcess();
                        spectatorOverlay = null;
                        return;
                    }

                }

                spectatorOverlay.Update();
                spectatee = spectatorOverlay.spectatee;
            }

            OnlineManager.mePlayer.isActuallySpectating = spectatee != null && !spectatee.IsLocal();
            if (spectatee != null)
            {
                camera.followAbstractCreature = spectatee;
                if (spectatee.Room == null)
                {
                    RainMeadow.Debug($"spectatee {spectatee} not in room!");
                }
                else
                {
                    if (spectatee.Room.realizedRoom == null)
                    {
                        this.game.world.ActivateRoom(spectatee.Room);
                    }
                    if (spectatee.Room.realizedRoom != null && camera.room.abstractRoom != spectatee.Room)
                    {
                        camera.MoveCamera(spectatee.Room.realizedRoom, -1);
                    }
                }
            }
        }
    }
}

using HUD;
using UnityEngine;

namespace RainMeadow
{
    public class SpectatorHud : HudPart
    {
        private RoomCamera camera;
        private RainWorldGame game;
        private SpectatorOverlay? spectatorOverlay;
        private AbstractCreature? spectatee;

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
                spectatee = spectatorOverlay.spectatee;
            }
            if (spectatee is AbstractCreature ac)
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

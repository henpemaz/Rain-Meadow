using HUD;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using HarmonyLib;

namespace RainMeadow
{
    public class SpectatorHud : HudPart
    {

        private RoomCamera camera;
        private readonly OnlineGameMode onlineGameMode;
        private Menu.Menu spectatorMode;
        private RainWorldGame game;
        private List<AbstractCreature> acList;
        private int spectateInitCoolDown;


        public SpectatorHud(HUD.HUD hud, RoomCamera camera, OnlineGameMode onlineGameMode) : base(hud)
        {
            this.camera = camera;
            this.onlineGameMode = onlineGameMode;
            acList = new List<AbstractCreature>();
            this.game = camera.game;
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            if (spectatorMode != null)
            {
                spectatorMode.GrafUpdate(timeStacker);
            }
        }


        public override void Update()
        {
            base.Update();
            if (OnlineManager.lobby.gameMode is StoryGameMode)
            {
                if ((game.pauseMenu != null || camera.hud.map.visible || game.manager.upcomingProcess != null) && spectatorMode != null)
                {
                    RainMeadow.Debug("Shutting down spectator overlay due to another process request");

                    spectatorMode.ShutDownProcess();
                    spectatorMode = null;
                }
                if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.SpectatorKey.Value) && spectatorMode == null)
                {

                    RainMeadow.Debug("Creating spectator overlay");
                    spectatorMode = new SpectatorOverlay(game.manager, game);
                    spectateInitCoolDown = 20;
                }


                if (spectatorMode != null)
                {
                    spectatorMode.Update();

                    if (spectateInitCoolDown > 0)
                    {
                        spectateInitCoolDown--;
                    }
                }

                if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.SpectatorKey.Value) && spectatorMode != null && spectateInitCoolDown == 0)
                {
                    RainMeadow.Debug("Spectate destroy!");
                    spectatorMode.ShutDownProcess();
                    spectatorMode = null;
                }

            }
        }
    }
}
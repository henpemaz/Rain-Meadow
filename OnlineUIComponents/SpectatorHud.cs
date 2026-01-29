using HUD;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class SpectatorHud : HudPart
    {
        private RoomCamera camera;
        private RainWorldGame game;
        public SpectatorOverlay? spectatorOverlay;
        private AbstractCreature? spectatee;
        public bool isActive;

        public bool isSpectating { get => spectatee is not null; }

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
                    spectatorOverlay = new SpectatorOverlay(game.manager, game, camera);
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
            spectatorOverlay?.GrafUpdate(timeStacker);
        }

        public void ClearSpectatee()
        {
            if (spectatee != null)
            {
                ReturnCameraToPlayer();
            }
            spectatee = null;
            spectatorOverlay?.ShutDownProcess();
            spectatorOverlay = null;
            isActive = false;

        }

        public void ReturnCameraToPlayer()
        {
            RainMeadow.DebugMe();
            AbstractCreature? return_to_player = null;
            foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
            {
                if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac && ac.realizedCreature != null && ac.realizedCreature.State.alive)
                {
                    if (opo.owner == OnlineManager.mePlayer)
                    {
                        return_to_player = ac;
                        RainMeadow.Debug($"Me Player: Setting return to player to: {return_to_player}");
                        break;
                    }
                    //return_to_player = ac;
                    //RainMeadow.Debug($"Setting return to player to: {return_to_player}");
                    //break;

                }

            }


            if (return_to_player?.Room == null)
            {
                RainMeadow.Debug($"spectatee {return_to_player} not in room!");
            }
            else
            {
                camera.followAbstractCreature = return_to_player;
                if (return_to_player.Room.realizedRoom == null)
                {
                    this.game.world.ActivateRoom(return_to_player.Room);
                }
                if (return_to_player.Room.realizedRoom != null && camera.room.abstractRoom != return_to_player.Room)
                {
                    AbstractRoom oldRoom = camera.room.abstractRoom;
                    camera.MoveCamera(return_to_player.Room.realizedRoom, -1);
                    oldRoom.Abstractize();
                }
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
                spectatorOverlay.forceNonMouseSelectFreeze = hud.parts.Find(x => x is ChatHud) is ChatHud { chatInputActive: true };
                spectatorOverlay.Update();
                spectatee = spectatorOverlay.spectatee;
            }

            if (camera.InCutscene)
            {
                return;
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
                        AbstractRoom oldRoom = camera.room.abstractRoom;
                        camera.MoveCamera(spectatee.Room.realizedRoom, -1);
                        
                    }
                }
            }
        }
    }
}

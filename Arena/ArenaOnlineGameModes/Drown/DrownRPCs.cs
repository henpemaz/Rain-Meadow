using RainMeadow;
using System;

namespace RainMeadow.Arena.ArenaOnlineGameModes.Drown
{
    public static class DrownModeRPCs
    {
        [RPCMethod]
        public static void Drown_Killing(RPCEvent rpcEvent, OnlinePhysicalObject p, OnlinePhysicalObject c)
        {

            var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
            if (game.manager.upcomingProcess != null)
            {
                return;
            }

            try
            {
                Player player = (p.apo.realizedObject as Player);
                Creature creature = (c.apo.realizedObject as Creature);

                game.GetArenaGameSession.Killing(player, creature);
            }
            catch (Exception e)
            {
             RainMeadow.Error($"Error in Drown_Killing RPC: {e}");
            }
        }

        [RPCMethod]
        public static void Arena_OpenDen(bool denOpen)
        {
            if (RainMeadow.isArenaMode(out var arena) && DrownMode.isDrownMode(arena, out var drown))
            {
                drown.openedDen = denOpen;
                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
                if (game.manager.upcomingProcess != null)
                {
                    return;
                }

                if (!game.GetArenaGameSession.GameTypeSetup.spearsHitPlayers)
                {
                    game.cameras[0].hud.PlaySound(SoundID.UI_Multiplayer_Player_Revive);
                    OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs);
                    if (cs != null)
                    {

                        cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                        if (clientSettings != null)
                        {
                            clientSettings.iOpenedDen = denOpen;
                        }
                    }
                }
            }
        }
    }
}

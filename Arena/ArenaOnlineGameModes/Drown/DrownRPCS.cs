using Drown;
using RainMeadow;
using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public static class DrownModeRPCs
    {

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

        [RPCMethod]
        public static void Arena_RemoveAbstractCreatureFromList(RPCEvent e)
        {
            if (RainMeadow.isArenaMode(out var arena) && DrownMode.isDrownMode(arena, out var drown))
            {
                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
                if (game.manager.upcomingProcess != null)
                {
                    return;
                }

                if (game.session is ArenaGameSession s)
                {
                    s.Players.RemoveAll(x =>
                        x.realizedCreature == null || x.realizedCreature.dead || x.state.dead
                    );
                }
            }
        }

    }
}

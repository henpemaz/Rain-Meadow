using Drown;
using RainMeadow;
using System;

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


        // This is basically the same as UpdatePlayerScore, but it removes the < operator on the score check. I'm scared to remove that before tourney so we're making another RPC instead
        [RPCMethod]
        public static void UpdatePlayerScoreDrown(int playerNumber, int newScore)
        {
            RainMeadow.DebugMe();
            if (!RainMeadow.isArenaMode(out var arena)) return;

            OnlinePlayer? onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, playerNumber);
            if (onlinePlayer == null)
            {
                return;
            }
            var game = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;
            if (game == null)
            {
                RainMeadow.Error("Arena: RainWorldGame is null!");
                return;
            }

            if (game.session is ArenaGameSession a && a.arenaSitting.players.Contains(a.arenaSitting.players[playerNumber]))
            {
                if (a.arenaSitting.players[playerNumber].playerClass == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                {
                    return; // no points for you
                }

                a.arenaSitting.players[playerNumber].score = newScore;
                if (OnlineManager.lobby.isOwner)
                {
                    arena.playerNumberWithScore[onlinePlayer.inLobbyId] = a.arenaSitting.players[playerNumber].score;
                }
            }

        }
    }
}

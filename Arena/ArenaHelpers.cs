using System.Collections.Generic;

namespace RainMeadow
{
    internal static class ArenaHelpers
    {

        public static readonly List<string> nonArenaSlugs = new List<string> { "Inv", "Slugpup", "MeadowOnline", "MeadowOnlineRemote" };
        public static void CheckHostClientStates(ArenaCompetitiveGameMode arena)
        {

            if (!OnlineManager.lobby.isOwner && arena.nextLevel && !OnlineManager.lobby.worldSessions["arena"].isAvailable) // clients are waiting for host to finish.
            {
                CheckHostClientStates(arena);
                return;
            }


            if (OnlineManager.lobby.isOwner && arena.clientWaiting != 0 && arena.clientWaiting != OnlineManager.players.Count - 1) // host is waiting for clients.
            {
                RainMeadow.Debug("Owner waiting : " + OnlineManager.lobby.worldSessions["arena"].participants.Count);
                RainMeadow.Debug("Owner count : " + arena.clientWaiting);
                CheckHostClientStates(arena);
                return;

            }


        }
    }


}

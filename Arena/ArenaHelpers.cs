using System.Collections.Generic;
using System.Threading;

namespace RainMeadow
{
    internal static class ArenaHelpers
    {

        public static readonly List<string> nonArenaSlugs = new List<string> { "Inv", "Slugpup", "MeadowOnline", "MeadowOnlineRemote" };
        public static void CheckHostClientStates(ArenaCompetitiveGameMode arena)
        {


            if (!OnlineManager.lobby.isOwner) // client
            {
                while (arena.nextLevel && !OnlineManager.lobby.worldSessions["arena"].isAvailable)
                {
                    RainMeadow.Debug("Arena: Client waiting...");
                    Thread.Sleep(250);
                }

            }

            if (OnlineManager.lobby.isOwner)
            {

                while (arena.clientWaiting != 0 && arena.clientWaiting != OnlineManager.players.Count - 1) // host is waiting for clients.
                {
                    RainMeadow.Debug("Arena: Host waiting...");
                    Thread.Sleep(250);

                }


            }
        }
    }


}

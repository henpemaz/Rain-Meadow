using System.Collections.Generic;

namespace RainMeadow
{
    internal static class ArenaHelpers
    {

        public static readonly List<string> nonArenaSlugs = new List<string> { "Inv", "Slugpup", "MeadowOnline", "MeadowOnlineRemote" };

        // I need a way to order ArenaSitting by the host without serializing a ton of data, so I just serialize the ushort of the inLobbyId
        public static OnlinePlayer FindOnlinePlayerByLobbyId(ushort lobbyId)
        {
            foreach (var player in OnlineManager.players)
            {
                if (player.inLobbyId == lobbyId)
                {
                    return player;
                }
            }

            return null;
        }


    }


}

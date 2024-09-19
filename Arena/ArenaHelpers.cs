using RainMeadow.GameModes;
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

        public static OnlinePlayer FindOnlinePlayerByFakePlayerNumber(ArenaCompetitiveGameMode arena, int playerNumber)
        {

            for (int i = 0; i < arena.arenaSittingOnlineOrder.Count; i++)
            {
                if (playerNumber == i)
                {
                    return ArenaHelpers.FindOnlinePlayerByLobbyId(arena.arenaSittingOnlineOrder[i]);
                }
            }

            return null;
        }

        public static void SetupOnlineArenaStting(ArenaCompetitiveGameMode arena, ProcessManager manager) {
            manager.arenaSitting.players = new List<ArenaSitting.ArenaPlayer>();
            for (int i = 0; i < arena.arenaSittingOnlineOrder.Count; i++)
            {

                var currentPlayer = ArenaHelpers.FindOnlinePlayerByLobbyId(arena.arenaSittingOnlineOrder[i]);

                // Create a new ArenaPlayer
                ArenaSitting.ArenaPlayer newPlayer = new ArenaSitting.ArenaPlayer(i)
                {
                    playerNumber = i,
                    playerClass = ((OnlineManager.lobby.clientSettings[currentPlayer] as ArenaClientSettings).playingAs), // Set the playerClass to the OnlinePlayer
                    hasEnteredGameArea = true
                };

                // Add the new player to the list
                manager.arenaSitting.players.Add(newPlayer);

            }
        }

        public static List<SlugcatStats.Name> AllSlugcats()
        {
            var filteredList = new List<SlugcatStats.Name>();
            for (int i = 0; i < SlugcatStats.Name.values.entries.Count; i++)
            {
                var slugcatName = SlugcatStats.Name.values.entries[i];

                if (slugcatName.Contains(":"))
                {
                    continue;
                }


                if (ArenaHelpers.nonArenaSlugs.Contains(slugcatName))
                {
                    continue;
                }


                if (ExtEnumBase.TryParse(typeof(SlugcatStats.Name), slugcatName, false, out var enumBase))
                {
                    var temp = (SlugcatStats.Name)enumBase;
                    RainMeadow.Debug("Filtered list:" + slugcatName);
                    filteredList.Add(temp);
                }
            }
            return filteredList;
        }


    }


}

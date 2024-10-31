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

        public static void ResetReadyUpLogic(ArenaCompetitiveGameMode arena, ArenaLobbyMenu lobby)
        {
            if (lobby.playButton != null)
            {
                lobby.playButton.menuLabel.text = "READY?";
                lobby.playButton.inactive = false;

            }
            arena.allPlayersReadyLockLobby = false;
            arena.clientsAreReadiedUp = 0;
            foreach (var player in OnlineManager.players)
            {
                arena.playersReadiedUp[player.id.name] = false;
            }
            
            arena.isInGame = false;
            arena.returnToLobby = false;
            arena.playerEnteredGame = 0;
            //arena.allPlayersAreNowInGame = false;
            arena.countdownInitiatedHoldFire = true;
            arena.setupTime = 300;
            lobby.manager.rainWorld.options.DeleteArenaSitting();

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

        public static void SetupOnlineArenaStting(ArenaCompetitiveGameMode arena, ProcessManager manager)
        {
            manager.arenaSitting.players = new List<ArenaSitting.ArenaPlayer>();
            for (int i = 0; i < arena.arenaSittingOnlineOrder.Count; i++)
            {

                var currentPlayer = ArenaHelpers.FindOnlinePlayerByLobbyId(arena.arenaSittingOnlineOrder[i]);
                ArenaSitting.ArenaPlayer newPlayer = new ArenaSitting.ArenaPlayer(i)
                {
                    playerNumber = i,
                    playerClass = ((OnlineManager.lobby.clientSettings[currentPlayer].GetData<ArenaClientSettings>()).playingAs), // Set the playerClass to the OnlinePlayer. TODO: Try and find a way to go through avatarSettings for this
                    hasEnteredGameArea = true
                };

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

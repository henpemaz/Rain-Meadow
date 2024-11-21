using System.Collections.Generic;
using RainMeadow.Arena.Nightcat;
using UnityEngine;

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
            if (OnlineManager.lobby.isOwner)
            {
                arena.allPlayersReadyLockLobby = false;
            }
            arena.clientsAreReadiedUp = 0;
            lobby.clientReadiedUp = false;
            foreach (var player in OnlineManager.players)
            {
                arena.playersReadiedUp[player.id.name] = false;
                if (player != OnlineManager.lobby.owner)
                {

                    player.InvokeOnceRPC(ArenaRPCs.Arena_InitialSetupTimers, arena.setupTime, arena.arenaSaintAscendanceTimer);
                }
            }

            arena.isInGame = false;
            // arena.returnToLobby = false;
            lobby.manager.rainWorld.options.DeleteArenaSitting();

            //Nightcat.ResetNightcat();


        }


        public static OnlinePlayer FindOnlinePlayerByFakePlayerNumber(ArenaCompetitiveGameMode arena, int playerNumber)
        {
            try
            {
                for (int i = 0; i < arena.arenaSittingOnlineOrder.Count; i++)
                {
                    if (playerNumber == i)
                    {
                        return ArenaHelpers.FindOnlinePlayerByLobbyId(arena.arenaSittingOnlineOrder[i]);
                    }
                }
            }
            catch
            {
                RainMeadow.Error("Error finding player");

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

        public static void OverideSlugcatClassAbilities(Player player, ArenaCompetitiveGameMode arena)
        {

            if (player.SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint && !arena.countdownInitiatedHoldFire)
            {
                if (player.wantToJump > 0 && player.input[0].pckp && player.canJump <= 0 && !player.monkAscension && !player.tongue.Attached && player.bodyMode != Player.BodyModeIndex.Crawl && player.bodyMode != Player.BodyModeIndex.CorridorClimb && player.bodyMode != Player.BodyModeIndex.ClimbIntoShortCut && player.animation != Player.AnimationIndex.HangFromBeam && player.animation != Player.AnimationIndex.ClimbOnBeam && player.bodyMode != Player.BodyModeIndex.WallClimb && player.bodyMode != Player.BodyModeIndex.Swimming && player.Consious && !player.Stunned && player.animation != Player.AnimationIndex.AntlerClimb && player.animation != Player.AnimationIndex.VineGrab && player.animation != Player.AnimationIndex.ZeroGPoleGrab)
                {
                    player.maxGodTime = arena.arenaSaintAscendanceTimer;
                    player.ActivateAscension();
                }
                if (player.wantToJump > 0 && player.monkAscension)
                {
                    player.DeactivateAscension();
                }
            }

            //if (player.SlugCatClass == SlugcatStats.Name.Night)
            //{
            //    Nightcat.CheckInputForActivatingNightcat(player);
            //}

        }
    }


}

﻿using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    public static class ArenaHelpers
    {



        public static readonly List<string> nonArenaSlugs = new List<string> { "MeadowOnline", "MeadowOnlineRemote" };

        public static void SetProfileColor(ArenaOnlineGameMode arena)
        {
            int profileColor = 0;
            for (int i = 0; i < arena.arenaSittingOnlineOrder.Count; i++)
            {
                var currentPlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, i);
                if (ArenaHelpers.BaseGameSlugcats().Contains(arena.avatarSettings.playingAs) && ModManager.MSC)
                {
                    profileColor = Random.Range(0, 4);
                    arena.playerResultColors[currentPlayer.id.name] = profileColor;
                }
                else
                {
                    arena.playerResultColors[currentPlayer.id.name] = profileColor;
                }
            }
        }

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

        public static void ResetOnReturnToMenu(ArenaOnlineGameMode arena, ArenaLobbyMenu lobby)
        {
            arena.arenaSittingOnlineOrder = new List<ushort>();
            arena.ResetGameTimer();
            arena.currentLevel = 0;
            arena.playersReadiedUp.list.Clear();

        }

        public static void ResetReadyUpLogic(ArenaOnlineGameMode arena, ArenaLobbyMenu lobby)
        {
            if (lobby.playButton != null)
            {
                lobby.playButton.menuLabel.text = Utils.Translate("READY?");
                lobby.playButton.inactive = false;

            }
            if (OnlineManager.lobby.isOwner)
            {
                arena.allPlayersReadyLockLobby = arena.playersReadiedUp.list.Count == OnlineManager.players.Count;
                arena.isInGame = false;
            }
            if (arena.returnToLobby)
            {
                lobby.clientReadiedUp = false;

                arena.playersReadiedUp.list.Clear();

                arena.returnToLobby = false;
            }


            lobby.manager.rainWorld.options.DeleteArenaSitting();
            //Nightcat.ResetNightcat();


        }
        public static OnlinePlayer FindOnlinePlayerByStringUsername(string username)
        {
            foreach (var player in OnlineManager.players)
            {
                if (player.id.name == username)
                {
                    return player;
                }
            }

            return OnlineManager.mePlayer;
        }


        public static OnlinePlayer? FindOnlinePlayerByFakePlayerNumber(ArenaOnlineGameMode arena, int playerNumber)
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

        public static int FindOnlinePlayerNumber(ArenaOnlineGameMode arena, OnlinePlayer player)
        {

            return arena.arenaSittingOnlineOrder.IndexOf(player.inLobbyId);


        }
        public static void SetupOnlineArenaStting(ArenaOnlineGameMode arena, ProcessManager manager)
        {

            manager.arenaSitting.players = new List<ArenaSitting.ArenaPlayer>();
            for (int i = 0; i < arena.arenaSittingOnlineOrder.Count; i++)
            {

                var currentPlayer = ArenaHelpers.FindOnlinePlayerByLobbyId(arena.arenaSittingOnlineOrder[i]);
                ArenaSitting.ArenaPlayer newPlayer = new ArenaSitting.ArenaPlayer(i)
                {
                    playerNumber = i,
                    playerClass = ((OnlineManager.lobby.clientSettings[currentPlayer].GetData<ArenaClientSettings>()).playingAs), // Set the playerClass to the OnlinePlayer. This is for the PlayerResult profile pics
                    hasEnteredGameArea = true
                };


                manager.arenaSitting.players.Add(newPlayer);

            }
        }

        public static List<SlugcatStats.Name> AllSlugcats()
        {
            var filteredList = new List<SlugcatStats.Name>();
            for (int i = 0; i < SlugcatStats.Name.values.Count; i++)
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

                    RainMeadow.Debug("Filtered list:" + slugcatName);
                    SlugcatStats.Name slugcatStatSlug = (SlugcatStats.Name)enumBase;
                    if (ModManager.Watcher && slugcatStatSlug == SlugcatStats.Name.Night)
                    {
                        RainMeadow.Debug("Filtered out Night slugcat");
                        continue; // Skip the Night slugcat if Watcher mod is active
                    }
                    filteredList.Add(slugcatStatSlug);
                    if (SlugcatStats.HiddenOrUnplayableSlugcat(slugcatStatSlug))
                    {
                        if (BaseGameSlugcats().Contains(slugcatStatSlug))
                        {
                            continue;
                        }
                        else
                        {
                            filteredList.Remove(slugcatStatSlug);
                        }
                    }
                }
            }
            return filteredList;
        }

        public static void SetHandler(SimplerButton[] classButtons, int localIndex)
        {
            var button = classButtons[localIndex]; // Get the button you want to pass


        }

        public static List<SlugcatStats.Name> BaseGameSlugcats()
        {
            var baseGameSlugs = new List<SlugcatStats.Name>();
            baseGameSlugs.Add(SlugcatStats.Name.White);
            baseGameSlugs.Add(SlugcatStats.Name.Yellow);
            baseGameSlugs.Add(SlugcatStats.Name.Red);
            baseGameSlugs.Add(SlugcatStats.Name.Night);

            if (ModManager.MSC)
            {
                baseGameSlugs.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Rivulet);
                baseGameSlugs.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer);
                baseGameSlugs.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint);
                baseGameSlugs.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear);
                baseGameSlugs.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Gourmand);
                baseGameSlugs.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup);
                baseGameSlugs.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel);


            }
            if (ModManager.Watcher)
            {
                baseGameSlugs.Remove(SlugcatStats.Name.Night);
                baseGameSlugs.Add(Watcher.WatcherEnums.SlugcatStatsName.Watcher);

            }

            return baseGameSlugs;

        }

        public static List<SlugcatStats.Name> VanillaSlugs()
        {
            var vanilla = new List<SlugcatStats.Name>();
            vanilla.Add(SlugcatStats.Name.White);
            vanilla.Add(SlugcatStats.Name.Yellow);
            vanilla.Add(SlugcatStats.Name.Red);
            vanilla.Add(SlugcatStats.Name.Night);

            return vanilla;
        }

        public static List<SlugcatStats.Name> MSCSlugs()
        {
            var msc = new List<SlugcatStats.Name>();
            msc.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Rivulet);
            msc.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer);
            msc.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint);
            msc.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear);
            msc.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Gourmand);
            msc.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup);
            msc.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel);

            return msc;
        }
        public static void OverideSlugcatClassAbilities(Player player, ArenaOnlineGameMode arena)
        {
            if (player.SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint)
            {
                if (!arena.sainot)
                {
                    if (!arena.countdownInitiatedHoldFire)
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

                        if (player.monkAscension == false && player.godTimer != player.maxGodTime)
                        {

                            if (player.tongue.mode == Player.Tongue.Mode.Retracted && (player.input[0].x != 0 || player.input[0].y != 0 || player.input[0].jmp))
                            {
                                player.godTimer += 0.8f;
                            }
                            else
                            {
                                player.godTimer -= 0.8f;
                            }
                        }

                    }

                }

            }

            if (player.SlugCatClass == Watcher.WatcherEnums.SlugcatStatsName.Watcher)
            {
                if (player.activateCamoTimer == ((player.startingCamoStateOnActivate == 0) ? player.enterIntoCamoDuration : player.exitOutOfCamoDuration) && player.performingActivationTimer == 0)
                {
                    player.ToggleCamo();
                    player.reachedCamoToggle = true;
                }
            }
            //if (player.SlugCatClass == SlugcatStats.Name.Night)
            //{
            //    Nightcat.CheckInputForActivatingNightcat(player);
            //}

        }
    }

}

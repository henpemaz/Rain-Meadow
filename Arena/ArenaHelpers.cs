using Menu;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MSCScene = MoreSlugcats.MoreSlugcatsEnums.MenuSceneID;
using UnityEngine;

namespace RainMeadow
{
    public static class ArenaHelpers
    {
        public static List<SlugcatStats.Name> allSlugcats = new List<SlugcatStats.Name>();
        public static List<SlugcatStats.Name> baseGameSlugcats = new List<SlugcatStats.Name>();
        public static List<SlugcatStats.Name> vanillaSlugcats = new List<SlugcatStats.Name>();
        public static List<SlugcatStats.Name> mscSlugcats = new List<SlugcatStats.Name>();
        public static List<MenuScene.SceneID> randomScenes = [];
        public static readonly List<string> nonArenaSlugs = new List<string> { "MeadowOnline", "MeadowOnlineRemote" };

        public static void RecreateSlugcatCache()
        {
            // reinitialize
            vanillaSlugcats.Clear();
            baseGameSlugcats.Clear();
            mscSlugcats.Clear();
            allSlugcats.Clear();
            //
            vanillaSlugcats.Add(SlugcatStats.Name.White);
            vanillaSlugcats.Add(SlugcatStats.Name.Yellow);
            vanillaSlugcats.Add(SlugcatStats.Name.Red);
            vanillaSlugcats.Add(SlugcatStats.Name.Night);
            // basegame
            baseGameSlugcats.AddRange(vanillaSlugcats);
            if (ModManager.MSC)
            {
                mscSlugcats.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Rivulet);
                mscSlugcats.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer);
                mscSlugcats.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint);
                mscSlugcats.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear);
                mscSlugcats.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Gourmand);
                mscSlugcats.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup);
                mscSlugcats.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel);
                baseGameSlugcats.AddRange(mscSlugcats);
            }
            if (ModManager.Watcher)
            {
                baseGameSlugcats.Remove(SlugcatStats.Name.Night);
                baseGameSlugcats.Add(Watcher.WatcherEnums.SlugcatStatsName.Watcher);
            }
            // all slugcats
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
                    allSlugcats.Add(slugcatStatSlug);
                    if (SlugcatStats.HiddenOrUnplayableSlugcat(slugcatStatSlug))
                    {
                        if (baseGameSlugcats.Contains(slugcatStatSlug))
                        {
                            continue;
                        }
                        else
                        {
                            allSlugcats.Remove(slugcatStatSlug);
                        }
                    }
                }
            }
        }
        public static void RecreateRandomScenes(bool isFlat)
        {
            randomScenes =
            [
                MenuScene.SceneID.MainMenu_Downpour,
                MenuScene.SceneID.Intro_1_Tree,
                MenuScene.SceneID.Intro_3_In_Tree,
                MenuScene.SceneID.Intro_4_Walking,
                MenuScene.SceneID.Intro_5_Hunting,
                MenuScene.SceneID.Intro_8_Climbing,
                MenuScene.SceneID.Intro_11_Drowning,
                MenuScene.SceneID.Landscape_CC,
                MenuScene.SceneID.Landscape_DS,
                MenuScene.SceneID.Landscape_GW,
                MenuScene.SceneID.Landscape_HI,
                MenuScene.SceneID.Landscape_LF,
                MenuScene.SceneID.Landscape_SB,
                MenuScene.SceneID.Landscape_SH,
                MenuScene.SceneID.Landscape_SI,
                MenuScene.SceneID.Landscape_SL,
                MenuScene.SceneID.Landscape_SU,
                MenuScene.SceneID.Dream_Moon_Friend,
                MenuScene.SceneID.Dream_Pebbles,
            ];
            if (isFlat)
            {
                randomScenes.AddRange([MenuScene.SceneID.Intro_2_Branch, MenuScene.SceneID.Intro_9_Rainy_Climb, MenuScene.SceneID.Intro_10_Fall, MenuScene.SceneID.Intro_10_5_Separation]);
            }
            if (ModManager.MSC) randomScenes.AddRange([MSCScene.Landscape_MS, MSCScene.Landscape_LC, MSCScene.Landscape_OE, MSCScene.Landscape_HR, MSCScene.Landscape_UG, MSCScene.Landscape_VS, MSCScene.Landscape_CL]);
            if (ModManager.Watcher) randomScenes.AddRange([Watcher.WatcherEnums.MenuSceneID.MainMenu_Watcher]);
        }
        public static void SetProfileColor(ArenaOnlineGameMode arena)
        {
            int profileColor = 0;
            for (int i = 0; i < arena.arenaSittingOnlineOrder.Count; i++)
            {
                var currentPlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, i);

                if (ArenaHelpers.baseGameSlugcats.Contains(arena.avatarSettings.playingAs) && ModManager.MSC)
                {
                    profileColor = UnityEngine.Random.Range(0, 4);
                    arena.playerResultColors[currentPlayer.GetUniqueID()] = profileColor;
                }
                else
                {
                    arena.playerResultColors[currentPlayer.GetUniqueID()] = profileColor;
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
            arena.ResetGameTimer();
            arena.currentLevel = 0;
            arena.arenaSittingOnlineOrder.Clear();
            arena.playersReadiedUp.list.Clear();
            arena.playerNumberWithDeaths.Clear();
            arena.playerNumberWithKills.Clear();
            arena.playerNumberWithWins.Clear();
            arena.playersLateWaitingInLobbyForNextRound.Clear();


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
                arena.leaveForNextLevel = false;
            }
            if (arena.returnToLobby)
            {
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
        public static void SetHandler(SimplerButton[] classButtons, int localIndex)
        {
            var button = classButtons[localIndex]; // Get the button you want to pass
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

        }
        public static T GetOptionFromArena<T>(string ID, T defaultIfNonExistant)
        {
            if (RainMeadow.isArenaMode(out ArenaOnlineGameMode arena))
            {
                if (typeof(T) == typeof(bool) && arena.onlineArenaSettingsInterfaceeBool.ContainsKey(ID))
                {
                    return (T)(object)arena.onlineArenaSettingsInterfaceeBool[ID];
                }
                if (typeof(T) == typeof(int) && arena.onlineArenaSettingsInterfaceMultiChoice.ContainsKey(ID))
                {
                    return (T)(object)arena.onlineArenaSettingsInterfaceMultiChoice[ID];
                }
            }
            return defaultIfNonExistant;
        }
        public static void SaveOptionToArena(string ID, object obj)
        {
            if (!RainMeadow.isArenaMode(out ArenaOnlineGameMode arena)) return;
            if (!OnlineManager.lobby.isOwner) return;
            if (obj is bool c)
            {
                arena.onlineArenaSettingsInterfaceeBool[ID] = c;
            }
            if (obj is int i)
            {
                arena.onlineArenaSettingsInterfaceMultiChoice[ID] = i;
            }
        }
        public static ArenaClientSettings? GetArenaClientSettings(OnlinePlayer? player)
        {
            if (OnlineManager.lobby == null)
            {
                RainMeadow.Error("Lobby is null!");
                return null;
            }
            if (player == null) return null;
            return OnlineManager.lobby.clientSettings.TryGetValue(player, out ClientSettings settings) ? settings.GetData<ArenaClientSettings>() : null;
        }
        public static void ParseArenaSetupSaveString(string text, Action<string, string> action)
        {
            if (text == null) return;
            string[] array = Regex.Split(text, "<msuA>");
            for (int i = 0; i < array.Length; i++)
            {
                string[] array2 = Regex.Split(array[i], "<msuB>");
                action.Invoke(array2[0], array[1]);
            }
        }

    }

}

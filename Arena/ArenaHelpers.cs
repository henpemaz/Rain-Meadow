using Menu;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MSCScene = MoreSlugcats.MoreSlugcatsEnums.MenuSceneID;
using UnityEngine;
using System.Linq;

namespace RainMeadow
{
    public static class ArenaHelpers
    {
        public static List<SlugcatStats.Name> allSlugcats = new List<SlugcatStats.Name>();
        public static List<SlugcatStats.Name> baseGameSlugcats = new List<SlugcatStats.Name>();
        public static List<SlugcatStats.Name> vanillaSlugcats = new List<SlugcatStats.Name>();
        public static List<SlugcatStats.Name> mscSlugcats = new List<SlugcatStats.Name>();
        public static List<SlugcatStats.Name> selectableSlugcats = new List<SlugcatStats.Name?>();
        public static readonly List<string> nonArenaSlugs = new List<string> { "MeadowOnline", "MeadowRandom" };

        public static void RecreateSlugcatCache()
        {
            // reinitialize
            vanillaSlugcats.Clear();
            baseGameSlugcats.Clear();
            mscSlugcats.Clear();
            allSlugcats.Clear();
            selectableSlugcats.Clear();
            //
            vanillaSlugcats.Add(SlugcatStats.Name.White);
            vanillaSlugcats.Add(SlugcatStats.Name.Yellow);
            vanillaSlugcats.Add(SlugcatStats.Name.Red);
            vanillaSlugcats.Add(SlugcatStats.Name.Night);
            // basegame
            baseGameSlugcats.AddRange(vanillaSlugcats);
            if (ModManager.MSC)
            {
                mscSlugcats.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Gourmand);
                mscSlugcats.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer);
                mscSlugcats.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Rivulet);
                mscSlugcats.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear);
                mscSlugcats.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint);

                mscSlugcats.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel);
                mscSlugcats.Add(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup);
                baseGameSlugcats.AddRange(mscSlugcats);
            }
            if (ModManager.Watcher)
            {
                baseGameSlugcats.Remove(SlugcatStats.Name.Night);
                baseGameSlugcats.Add(Watcher.WatcherEnums.SlugcatStatsName.Watcher);
            }

            allSlugcats.AddRange(baseGameSlugcats);

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

                    if (allSlugcats.Contains(slugcatStatSlug))
                    {
                        continue;
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

            selectableSlugcats.AddRange(allSlugcats);
            selectableSlugcats.Add(RainMeadow.Ext_SlugcatStatsName.OnlineRandomSlugcat);
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

            manager.arenaSitting.players = [];
            for (int i = 0; i < arena.arenaSittingOnlineOrder.Count; i++)
            {
                var currentPlayer = FindOnlinePlayerByLobbyId(arena.arenaSittingOnlineOrder[i]);
                ArenaSitting.ArenaPlayer newPlayer = new(i)
                {
                    playerNumber = i,
                    playerClass = GetArenaClientSettings(currentPlayer)!.playingAs, // Set the playerClass to the OnlinePlayer. This is for the PlayerResult profile pics
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
                            player.maxGodTime = arena.arenaSaintAscendanceTimer * 40;
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
        public static ArenaClientSettings? GetArenaClientSettings(OnlinePlayer? player) => GetDataSettings<ArenaClientSettings>(player);
        public static T? GetDataSettings<T>(OnlinePlayer? player) where T : OnlineEntity.EntityData
        {
            if (OnlineManager.lobby == null)
            {
                RainMeadow.Error("Lobby is null!");
                return null;
            }
            if (player == null) return null;
            return OnlineManager.lobby.clientSettings.TryGetValue(player, out ClientSettings settings) ? settings.GetData<T>() : null;
        }
        public static void ParseArenaSetupSaveString(string text, Action<string[]> action)
        {
            if (text == null) return;
            string[] array = Regex.Split(text, "<msuA>");
            for (int i = 0; i < array.Length; i++)
                action.Invoke(Regex.Split(array[i], "<msuB>"));
        }

        public static bool CheckSameTeam(OnlinePlayer? A, OnlinePlayer? B, Creature creature, Creature friend)
        {
            if (A is not null && B is not null)
            {
                if (OnlineManager.lobby.clientSettings[A].TryGetData<ArenaTeamClientSettings>(out var tb1))
                {
                    if (OnlineManager.lobby.clientSettings[B].TryGetData<ArenaTeamClientSettings>(out var tb2))
                    {
                        if (tb1.team == tb2.team)
                        {
                            RainMeadow.Debug("Same team! No hits");
                        }
                        return tb1.team == tb2.team && creature.State.alive && friend.State.alive;
                    }
                }         
            }

            RainMeadow.Debug("Different teams or Player is null");
            return false;
        }

        public static int GetReadiedPlayerCount(List<OnlinePlayer> players) => players.Where(player => GetArenaClientSettings(player)?.ready ?? false).Count();
    }

}

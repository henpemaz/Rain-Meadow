using BepInEx;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RainMeadow
{
    public static class Moderation
    {
        public static string BlockedUsers => "meadow-blockedusers.json";
        public static string RecentUsers => "meadow-recentusers.json";

        public static List<RecentPlayer> blocked = new();
        public static List<RecentPlayer> recents = new();
        public const int MAX_RECENTS = 25;

        public static bool dirty;

        public static bool IsBlocked(OnlinePlayer player)
        {
            if (player.id is SteamMatchmakingManager.SteamPlayerId steamID && blocked.Any(x => x.id == steamID.steamID.m_SteamID)) 
                return true;
            return false;
        }
        public static bool IsBlocked(RecentPlayer recent) => blocked.Any(x => x.id == recent.id);

        public static void UpdateCustomization(OnlinePlayer player, SlugcatCustomization customization)
        {
            if (player.id is not SteamMatchmakingManager.SteamPlayerId steamID) return;
            foreach (var recent in recents.Where(x => x.id == steamID.steamID.m_SteamID))
            {
                var newCustom = new RecentPlayer.Customization(customization);
                if (recent.customization != newCustom) dirty = true;
                recent.customization = newCustom;
            }
        }

        public static void AddRecent(RecentPlayer recent)
        {
            var lastOrder = recents.ToArray();
            recents.RemoveAll(x => x.id == recent.id);
            recents.Add(recent);
            recents = recents.OrderByDescending(x => x.id == recent.id).Take(MAX_RECENTS).ToList();
            if (!recents.SequenceEqual(lastOrder))
            {
                dirty = true;
            }
        }

        public static void AddLobbyList(OnlinePlayer[] players)
        {
            // TODO optimize me
            RainMeadow.DebugMe();
            foreach(var player in players)
            {
                if (player.isMe) continue;
                var recent = new RecentPlayer(player, null);

                var lastOrder = recents.ToArray();

                bool exists = recents.Any(x => x.id == recent.id);
                if (exists)
                {
                    foreach (var entry in recents.Where(x => x.id == recent.id))
                    {
                        if (entry.name != recent.name)
                        {
                            entry.name = recent.name;
                            dirty = true;
                        }
                    }
                    recents = recents.OrderByDescending(x => x.id == recent.id).ToList();
                    if (!recents.SequenceEqual(lastOrder))
                    {
                        dirty = true;
                    }
                }
                else
                {
                    recents.Add(recent);
                    recents = recents.OrderByDescending(x => x.id == recent.id).Take(MAX_RECENTS).ToList();
                    dirty = true;
                }
            }
        }

        public static void Block(RecentPlayer recent)
        {
            blocked.RemoveAll(x => x.id == recent.id);
            blocked.Add(recent);
            blocked = blocked.OrderByDescending(x => x.id == recent.id).ToList();

            dirty = true;
        }
        public static void Block(OnlinePlayer player)
        {
            Block(new RecentPlayer(player));
        }

        public static void Unblock(RecentPlayer recent)
        {
            blocked.RemoveAll(x => x.id == recent.id);
            recents.RemoveAll(x => x.id == recent.id);
            recents.Add(recent);
            recents = recents.OrderByDescending(x => x.id == recent.id).Take(MAX_RECENTS).ToList();

            dirty = true;
        }

        public static void ReloadAll()
        {
            Reload(BlockedUsers, ref blocked);
            Reload(RecentUsers, ref recents);
        }

        public static void Update()
        {
            if (!dirty) return;
            Save(BlockedUsers, blocked);
            Save(RecentUsers, recents);
            dirty = false;
        }

        public static void Reload(string file, ref List<RecentPlayer> list)
        {
            string path = AssetManager.ResolveFilePath(file);
            try
            {
                // If the file doesn't exist, create an empty one
                if (!File.Exists(path))
                {
                    // We use WriteAllText with an empty string to initialize the file
                    File.WriteAllText(path, "");
                }

                if (File.ReadAllText(path).IsNullOrWhiteSpace()) list = new();

                list = JsonConvert.DeserializeObject<List<RecentPlayer>>(File.ReadAllText(path)) ?? new();

            }
            catch (Exception ex)
            {
                RainMeadow.Error($"There was an error reading {file} " + ex);
            }
        }

        public static void Save(string file, List<RecentPlayer> list)
        {
            string path = AssetManager.ResolveFilePath(file);
            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(list, Formatting.Indented));
            }
            catch (Exception ex)
            {
                RainMeadow.Error($"There was an error saving {file} " + ex);
            }
        }
    }
}

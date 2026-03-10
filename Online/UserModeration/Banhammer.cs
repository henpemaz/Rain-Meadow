using Menu;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using BepInEx;

namespace RainMeadow
{
    public class BanHammer
    {
        public static string BannedUsers => "meadow-bannedusers.json";
        public static string RecentUsers => "meadow-recentusers.json";

        public static List<SteamPlayerRep> bannedUsers = new();
        public static List<SteamPlayerRep> recentUsers = new();

        public const int MAX_RECENTS = 25;

        public static void RefreshAll()
        {
            bannedUsers = RefreshBannedUsers();
            recentUsers = RefreshRecentUsers();
        }

        public static List<SteamPlayerRep> RefreshBannedUsers()
        {
            string path = AssetManager.ResolveFilePath(BannedUsers);

            try
            {
                // If the file doesn't exist, create an empty one
                if (!File.Exists(path))
                {
                    // We use WriteAllText with an empty string to initialize the file
                    File.WriteAllText(path, "");
                    return new();
                }

                if (File.ReadAllText(path).IsNullOrWhiteSpace()) return new();

                return JsonConvert.DeserializeObject<List<SteamPlayerRep>>(File.ReadAllText(path)) ?? new();

            }
            catch (Exception ex)
            {
                RainMeadow.Error("There was an error reading meadow-bannedusers.json " + ex);
                return new();
            }
        }

        public static List<SteamPlayerRep> RefreshRecentUsers()
        {
            string path = AssetManager.ResolveFilePath(RecentUsers);

            try
            {
                // If the file doesn't exist, create an empty one
                if (!File.Exists(path))
                {
                    // We use WriteAllText with an empty string to initialize the file
                    File.WriteAllText(path, "");
                    return new();
                }

                if (File.ReadAllText(path).IsNullOrWhiteSpace()) return new();

                return JsonConvert.DeserializeObject<List<SteamPlayerRep>>(File.ReadAllText(path)) ?? new();

            }
            catch (Exception ex)
            {
                RainMeadow.Error("There was an error reading meadow-recentusers.json " + ex);
                return new();
            }
        }

        public static string[] GetBannedUsers()
        {
            string path = AssetManager.ResolveFilePath(BannedUsers);

            // If the file doesn't exist, create an empty one
            if (!File.Exists(path))
            {
                // We use WriteAllText with an empty string to initialize the file
                File.WriteAllText(path, "");
                return new string[0];
            }

            // Read the file and filter out empty lines or spaces
            return File.ReadAllLines(path)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToArray();
        }

        public static void ShowBan(ProcessManager manager)
        {

            Action confirmProceed = () =>
            {
                manager.dialog = null;
                if (OnlineManager.lobby != null)
                {
                    OnlineManager.LeaveLobby(); // kill anything leftover
                }
            };

            DialogNotify informBadUser = new DialogNotify(Utils.Translate("You were removed from the previous online game"), manager, confirmProceed);

            if (manager.dialog != null)
            {

                manager.dialog = null;
            }


            manager.ShowDialog(informBadUser);

        }

        public static void BanUser(OnlinePlayer steamUser)
        {
            steamUser.InvokeRPC(RPCs.KickToLobby);
            if (OnlineManager.lobby.bannedUsers == null)
            {
                OnlineManager.lobby.bannedUsers = new();
            }
            if (!OnlineManager.lobby.bannedUsers.list.Contains(steamUser.id))
            {
                OnlineManager.lobby.bannedUsers.list.Add(steamUser.id);
            }

        }

        public static void PermaBanUser(OnlinePlayer steamUser)
        {
            BanUser(steamUser);

            bannedUsers.Add(SteamPlayerRep.FromOnlinePlayer(steamUser));

            string path = AssetManager.ResolveFilePath(BannedUsers);

            File.WriteAllText(path, JsonConvert.SerializeObject(bannedUsers));

            RefreshBannedUsers();

            OnBannedRefresh?.Invoke(bannedUsers.ToArray());
        }

        public static void PermaBanUser(SteamPlayerRep steamUser)
        {
            bannedUsers.Add(steamUser);

            string path = AssetManager.ResolveFilePath(BannedUsers);

            File.WriteAllText(path, JsonConvert.SerializeObject(bannedUsers));

            RefreshBannedUsers();

            OnBannedRefresh?.Invoke(bannedUsers.ToArray());
        }

        public static void UnpermabanUser(SteamPlayerRep steamUser)
        {
            if (bannedUsers.Remove(steamUser))
            {
                string path = AssetManager.ResolveFilePath(BannedUsers);
                File.WriteAllText(path, JsonConvert.SerializeObject(bannedUsers));
                RefreshBannedUsers();
                OnBannedRefresh?.Invoke(bannedUsers.ToArray());

                UpdateRecents(steamUser);
            }
        }

        public static void UpdateRecents(OnlinePlayer steamUser)
        {
            if (steamUser.isMe) return;
            var steamRep = SteamPlayerRep.FromOnlinePlayer(steamUser);

            bool exists = false;

            foreach (var userRep in recentUsers.Where(x => x.SteamID == steamRep.SteamID))
            {
                exists = true;
                userRep.SlugcatColor = steamRep.SlugcatColor;
                userRep.Selected = steamRep.Selected;
            }
            if (exists)
            {
                recentUsers = recentUsers.OrderByDescending(x => x.SteamID == steamRep.SteamID).ToList();
                return;
            }

            recentUsers.Add(steamRep);

            if (recentUsers.Count > MAX_RECENTS)
            {
                while (recentUsers.Count > MAX_RECENTS) recentUsers.RemoveAt(recentUsers.Count - 1);
            }

            string path = AssetManager.ResolveFilePath(RecentUsers);
            File.WriteAllText(path, JsonConvert.SerializeObject(recentUsers));
            RefreshRecentUsers();

            OnRecentsRefresh?.Invoke(bannedUsers.ToArray());
        }

        public static void UpdateRecents(SteamPlayerRep steamRep)
        {
            bool exists = false;

            foreach (var userRep in recentUsers.Where(x => x.SteamID == steamRep.SteamID))
            {
                exists = true;
                userRep.SlugcatColor = steamRep.SlugcatColor;
                userRep.Selected = steamRep.Selected;
            }
            if (exists)
            {
                recentUsers = recentUsers.OrderByDescending(x => x.SteamID == steamRep.SteamID).ToList();
                return;
            }

            recentUsers.Add(steamRep);

            if (recentUsers.Count > MAX_RECENTS)
            {
                while (recentUsers.Count > MAX_RECENTS) recentUsers.RemoveAt(recentUsers.Count - 1);
            }

            string path = AssetManager.ResolveFilePath(RecentUsers);
            File.WriteAllText(path, JsonConvert.SerializeObject(recentUsers));
            RefreshRecentUsers();

            OnRecentsRefresh?.Invoke(bannedUsers.ToArray());
        }

        public static void UpdateRecents(OnlinePlayer[] steamUsers)
        {
            foreach (var steamUser in steamUsers)
            {
                if (steamUser.isMe) continue;
                var steamRep = SteamPlayerRep.FromOnlinePlayer(steamUser);

                bool exists = false;

                foreach (var userRep in recentUsers.Where(x => x.SteamID == steamRep.SteamID))
                {
                    exists = true;
                    userRep.SlugcatColor = steamRep.SlugcatColor;
                    userRep.Selected = steamRep.Selected;
                }
                if (exists)
                {
                    recentUsers = recentUsers.OrderByDescending(x => x.SteamID == steamRep.SteamID).ToList();
                    continue;
                }

                recentUsers.Add(steamRep);

                if (recentUsers.Count > MAX_RECENTS)
                {
                    while (recentUsers.Count > MAX_RECENTS) recentUsers.RemoveAt(recentUsers.Count - 1);
                }

            }

            string path = AssetManager.ResolveFilePath(RecentUsers);
            File.WriteAllText(path, JsonConvert.SerializeObject(recentUsers));
            RefreshRecentUsers();

            OnRecentsRefresh?.Invoke(bannedUsers.ToArray());
        }

        public static event ModerationRefresh OnRecentsRefresh = delegate { };
        public static event ModerationRefresh OnBannedRefresh = delegate { };
        public delegate void ModerationRefresh(SteamPlayerRep[] players);
    }
}
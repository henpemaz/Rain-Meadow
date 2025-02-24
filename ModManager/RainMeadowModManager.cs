using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace RainMeadow
{
    public static class RainMeadowModManager
    {
        private static void UpdateFromOrWriteToFile(string path, ref string[] lines)
        {
            path = Path.Combine(Custom.RootFolderDirectory(), path);
            if (File.Exists(path))
            {
                lines = File.ReadAllLines(path);
            }
            else
            {
                File.WriteAllLines(path, lines);
            }
        }

        public static string[] highImpactMods = {
            "rwremix",
            "moreslugcats",
        };

        public static string[] GetRequiredMods()
        {
            UpdateFromOrWriteToFile("meadow-highimpactmods.txt", ref highImpactMods);

            var requiredMods = highImpactMods.Union(RainMeadowModInfoManager.MergedModInfo.SyncRequiredMods.Except(RainMeadowModInfoManager.MergedModInfo.SyncRequiredModsOverride)).ToList();
            requiredMods.AddDistinctRange(ModManager.ActiveMods.Where(mod => Directory.Exists(Path.Combine(mod.path, "modify", "world"))).Select(mod => mod.id));
            
            //add dependencies
            foreach (var mod in ModManager.ActiveMods)
            {
                if (requiredMods.Contains(mod.id))
                    requiredMods.AddDistinctRange(mod.requirements);
            }

            //the mod lists are combined first, then ActiveMods is combed through for ids, because that ensures the load order is correct
            return ModManager.ActiveMods
                .Where(mod => requiredMods.Contains(mod.id))
                .OrderBy(mod => mod.loadOrder)
                .Select(mod => mod.id)
                .ToArray();
        }

        public static string ModIdToName(string id)
        {
            foreach (var mod in ModManager.ActiveMods)
            {
                if (mod.id == id)
                    return mod.name;
            }
            return id; //default case: if the name isn't found, the ID should hopefully be a better replacement than "null" or something
        }

        public static string ModArrayToString(string[] requiredMods)
        {
            return string.Join("\n", requiredMods);
        }
        public static string[] ModStringToArray(string requiredMods)
        {
            return requiredMods.Split('\n');
        }

        public static string[] bannedMods = {
            "maxi-mol.mousedrag",
            "fyre.BeastMaster",
            "slime-cubed.devconsole",
            "zrydnoob.UnityExplorer",
            "warp",
            "presstopup",
            "CandleSign.debugvisualizer",
            "maxi-mol.freecam",
            "henpemaz_spawnmenu",  //gotta be safe
            "autodestruct",
            "DieButton",
            "emeralds_features",
            "flirpy.rivuletunscammedlungcapacity",
            "Aureuix.Kaboom",
            "TM.PupMagnet",
            "iwantbread.slugpupstuff",
            "blujai.rocketficer",
            "slugcatstatsconfig",
            "explorite.slugpups_cap_configuration",
            "slime-cubed.slugbase",
        };

        public static string[] GetBannedMods()
        {
            UpdateFromOrWriteToFile("meadow-highimpactmods.txt", ref highImpactMods);
            UpdateFromOrWriteToFile("meadow-bannedmods.txt", ref bannedMods);

            var effectiveHighImpactMods = highImpactMods.Union(RainMeadowModInfoManager.MergedModInfo.SyncRequiredMods.Except(RainMeadowModInfoManager.MergedModInfo.SyncRequiredModsOverride)).ToList();
            var effectiveBannedMods = bannedMods.Union(RainMeadowModInfoManager.MergedModInfo.BannedOnlineMods.Except(RainMeadowModInfoManager.MergedModInfo.BannedOnlineModsOverride)).ToList();

            // (high impact + banned) - enabled
            return effectiveHighImpactMods.Concat(effectiveBannedMods)
                .Except(ModManager.ActiveMods.Select(mod => mod.id))
                .ToArray();
        }

        /// <summary>
        /// Checks the user's mod list with the lobby, and makes his alter his mod list if necessary.
        /// </summary>
        /// <param name="requiredMods">Mods that the user MUST have in order to join the lobby</param>
        /// <param name="bannedMods">Mods that the user must NOT have, unless the mods are in <paramref name="requiredMods"/></param>
        /// <param name="ignoreReorder">Whether the lobby should accept users with the same mods but in a different order</param>
        /// <param name="password">The lobby's password. Used solely for rejoining lobbies after a restart.</param>
        /// <param name="lanEndpoint">The IPEndPoint of the LAN lobby (if applicable). Used solely for rejoining lobbies after a restart.</param>
        /// <returns>True if the mods were successfully applied (or didn't need to be applied) AND the game does not require a restart.</returns>
        internal static bool CheckMods(string[] requiredMods, string[] bannedMods, bool ignoreReorder = false, string? password = null, IPEndPoint? lanEndpoint = null)
        {
            try
            {
                RainMeadow.Debug($"required: [ {string.Join(", ", requiredMods)} ]");
                RainMeadow.Debug($"banned:   [ {string.Join(", ", bannedMods)} ]");
                var active = ModManager.ActiveMods.Select(mod => mod.id).ToList();
                bool reorder = true; //or change mods whatsoever
                var disable = GetRequiredMods().Union(bannedMods).Except(requiredMods).Intersect(active).ToList();
                var enable = requiredMods.Except(active).ToList();

                //clear phony entries to the mod list
                enable.RemoveAll(mod => mod == null || mod == "");
                disable.RemoveAll(mod => mod == null || mod == "");

                //determine whether a reorder is necessary
                if (!disable.Any() && !enable.Any())
                {
                    reorder = false;
                    if (!ignoreReorder)
                    {
                        int prevIdx = Int32.MinValue;
                        for (int i = 0; i < requiredMods.Length; i++)
                        {
                            if (requiredMods[i] == "henpemaz_rainmeadow")
                                continue; //ignore Rain Meadow when determining whether mods need to be reordered
                            int newIdx = ModManager.ActiveMods.Find(mod => requiredMods[i] == mod.id).loadOrder;
                            if (newIdx < prevIdx)
                            {
                                reorder = true;
                                RainMeadow.Debug($"Reorder necessary. Idx: {i}");
                                break;
                            }
                            prevIdx = newIdx;
                        }
                    }
                }

                RainMeadow.Debug($"active:  [ {string.Join(", ", active)} ]");
                RainMeadow.Debug($"enable:  [ {string.Join(", ", enable)} ]");
                RainMeadow.Debug($"disable: [ {string.Join(", ", disable)} ]");
                RainMeadow.Debug($"reorder: {reorder}");

                if (!reorder) return true;

                var lobbyID = MatchmakingManager.currentInstance.GetLobbyID();

                List<bool> pendingEnabled = ModManager.InstalledMods.ConvertAll(mod => mod.enabled);
                List<int> pendingLoadOrder = ModManager.InstalledMods.ConvertAll(mod => mod.loadOrder);

                List<string> missingMods = new(), modNamesToEnable = new(), modNamesToDisable = new();

                foreach (var id in enable)
                {
                    int index = ModManager.InstalledMods.FindIndex(mod => mod.id == id);
                    if (index < 0) missingMods.Add(id);
                    else
                    {
                        pendingEnabled[index] = true;

                        modNamesToEnable.Add(ModManager.InstalledMods[index].LocalizedName);
                    }
                }

                //add mods that have their dependencies disabled to the disable list (e.g: a mod that requires Slugbase)
                foreach (var mod in ModManager.ActiveMods)
                    if (!disable.Contains(mod.id)) //ignore mods that are already in disable; no change necessary
                        if (disable.Exists(id => mod.requirements.Contains(id))) //if one of its dependencies is being disabled
                            disable.Add(mod.id); //disable the mod

                //clear phony entries to the mod list again, just in case
                enable.RemoveAll(mod => mod == null || mod == "");
                disable.RemoveAll(mod => mod == null || mod == "");

                foreach (var id in disable)
                {
                    int index = ModManager.InstalledMods.FindIndex(mod => mod.id == id);
                    if (index < 0)
                    {
                        RainMeadow.Debug($"Couldn't find instance of {id} in InstalledMods??");
                        continue;
                    }
                    pendingEnabled[index] = false;
                    modNamesToDisable.Add(ModManager.InstalledMods[index].LocalizedName);
                }

                //occasionally there will somehow be blank/nonexistent mods in the mod lists. This messes stuff up
                modNamesToEnable.RemoveAll(id => id == "" || id == null);
                modNamesToDisable.RemoveAll(id => id == "" || id == null);
                missingMods.RemoveAll(id => id == "" || id == null);

                if (missingMods.Count < 1) //there's no need to care about load order if we can't apply the mods anyway
                {
                    //reorder mods
                    //try using negative indices, just to simplify things? Will that even work??
                    int lowestLoadIdx = ModManager.InstalledMods.MinBy(mod => mod.loadOrder).loadOrder;
                    for (int i = 0; i < requiredMods.Length; i++)
                    {
                        int idx = ModManager.InstalledMods.FindIndex(mod => mod.id == requiredMods[i]);
                        if (idx >= 0) pendingLoadOrder[idx] = i - requiredMods.Length + lowestLoadIdx;
                        else RainMeadow.Debug($"Couldn't find instance of {requiredMods[i]} in InstalledMods");
                    }

                    string loadOrderString = "Load Order: "; //log the load order
                    for (int i = 0; i < pendingLoadOrder.Count; i++)
                        if (pendingEnabled[i]) loadOrderString += pendingLoadOrder[i] + "-" + ModManager.InstalledMods[i].id + ", ";
                    RainMeadow.Debug(loadOrderString);
                }

                //check for missing DLC
                List<string> missingDLC = new();
                for (int i = 0; i < pendingEnabled.Count; i++)
                    if (pendingEnabled[i] && ModManager.InstalledMods[i].DLCMissing)
                        missingDLC.Add(ModManager.InstalledMods[i].LocalizedName);

                ModApplier modApplier = new(RWCustom.Custom.rainWorld.processManager, pendingEnabled, pendingLoadOrder);

                //mod applier code moved to a task so the game doesn't get frozen?
                Task.Run(() =>
                {
                    RainMeadow.Debug("Showing mod check popups");
                    if (missingDLC.Count > 0)
                        modApplier.ShowMissingDLCMessage(missingDLC);
                    else if (enable.Any() || disable.Any() || missingMods.Count > 0)
                        modApplier.ShowConfirmation(modNamesToEnable, modNamesToDisable, missingMods);
                    else
                        modApplier.ConfirmReorder();

                    modApplier.OnFinish += (ModApplier modApplyer) =>
                    {
                        RainMeadow.Debug("Finished applying");

                        if (modApplier.requiresRestart)
                        {
                            if (lanEndpoint != null)
                            {
                                try
                                {
                                    Utils.Restart($"+connect_lan_lobby {lanEndpoint.Address.Address} {lanEndpoint.Port}" + (password == null ? "" : $" +lobby_password {password}"));
                                }
                                catch (Exception ex) { RainMeadow.Debug(ex); }
                            }
                            else if (lobbyID != "Unknown Lan Lobby")
                            {
                                Utils.Restart($"+connect_steam_lobby {lobbyID}" + (password == null ? "" : $" +lobby_password {password}"));
                            }
                            else
                                Utils.Restart();
                        }
                    };
                });

                //wait until mod applier finishes
                while (!modApplier.ended)
                    Thread.Sleep(5);

                bool successful = !modApplier.cancelled && !modApplier.requiresRestart;

                RainMeadow.Debug($"Returning successful = {successful}");

                return successful;
            }
            catch (Exception ex)
            {
                RainMeadow.Debug(ex);
                return false;
            }
        }

        internal static void Reset()
        {
            if (ModManager.MMF)
            {
                RainMeadow.Debug("Restoring config settings");

                var mmfOptions = MachineConnector.GetRegisteredOI(MoreSlugcats.MMF.MOD_ID);
                MachineConnector.ReloadConfig(mmfOptions);
            }
        }
    }
}

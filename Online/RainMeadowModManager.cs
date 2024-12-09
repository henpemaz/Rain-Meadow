using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace RainMeadow
{
    public class RainMeadowModManager
    {
        public static string[] GetActiveMods()
        {
            return ModManager.ActiveMods
                .Where(mod => Directory.Exists(Path.Combine(mod.path, "modify", "world"))
                    || blacklistMods.Contains(mod.id))
                .Select(mod => mod.id)
                .ToArray();
        }


        public static List<string> blacklistMods = new List<string>();

        public static string[] GetBlackList(string[] mods)
        {
            if (mods is null) mods = ModManager.ActiveMods.Select(mod => mod.id).ToArray();

            string[] items = RainMeadow.rainMeadowOptions.ModBlacklist.Value.Split(',');
            if (items.Length > 0)
            {
                foreach (string item in items)
                {
                    // Trim leading/trailing spaces from each substring
                    string trimmedItem = item.Trim();

                    // Add the cleaned substring to the list if it's not empty
                    if (!string.IsNullOrEmpty(trimmedItem))
                    {
                        RainMeadow.Debug(trimmedItem);
                        blacklistMods.Add(trimmedItem);
                    }
                }
                blacklistMods.ToArray();

            }

            return mods.Intersect(blacklistMods).ToArray();



        }
        internal static void CheckMods(string[] lobbyMods, string[] localMods)
        {
            var lobbyCheatMods = GetBlackList(lobbyMods);
            var localCheatMods = GetBlackList(localMods);
            if (Enumerable.SequenceEqual(localMods.Except(localCheatMods), lobbyMods.Except(lobbyCheatMods))
                && !(localCheatMods.Any() && !lobbyCheatMods.Any()))
            {
                RainMeadow.Debug("Same mod set !");
            }
            else
            {
                RainMeadow.Debug("Mismatching mod set");

                var (MissingMods, ExcessiveMods) = CompareModSets(lobbyMods, localMods);

                bool[] mods = ModManager.InstalledMods.ConvertAll(mod => mod.enabled).ToArray();
                List<int> loadOrder = ModManager.InstalledMods.ConvertAll(mod => mod.loadOrder);

                List<string> unknownMods = new();
                List<ModManager.Mod> modsToEnable = new();
                List<ModManager.Mod> modsToDisable = new();

                foreach (var id in MissingMods)
                {
                    int index = ModManager.InstalledMods.FindIndex(_mod => _mod.id == id);

                    if (index >= 0)
                    {
                        mods[index] = true;
                        modsToEnable.Add(ModManager.InstalledMods[index]);
                    }
                    else
                    {
                        RainMeadow.Debug("Unknown mod: " + id);
                        unknownMods.Add(id);
                    }
                }

                foreach (var id in ExcessiveMods)
                {
                    int index = ModManager.InstalledMods.FindIndex(_mod => _mod.id == id);

                    mods[index] = false;

                    modsToDisable.Add(ModManager.InstalledMods[index]);
                }

                ModApplier modApplyer = new(RWCustom.Custom.rainWorld.processManager, mods.ToList(), loadOrder);

                modApplyer.ShowConfirmation(modsToEnable, modsToDisable, unknownMods);

                modApplyer.OnFinish += (ModApplier modApplyer) =>
                {
                    RainMeadow.Debug("Finished applying");

                    //Utils.Restart($"+connect_lobby {MatchmakingManager.instance.GetLobbyID()}"); 

                };
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

        private static (List<string> MissingMods, List<string> ExcessiveMods) CompareModSets(string[] arr1, string[] arr2)
        {
            // Find missing strings in arr2
            var missingStrings = arr1.Except(arr2).ToList();

            // Find excessive strings in arr2
            var excessiveStrings = arr2
                .GroupBy(item => item)
                .Where(group => group.Count() > arr1.Count(item => item == group.Key))
                .Select(group => group.Key)
                .ToList();

            return (missingStrings, excessiveStrings);
        }
    }
}

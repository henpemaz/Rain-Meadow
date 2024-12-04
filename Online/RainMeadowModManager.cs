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
                    || highImpactMods.Contains(mod.id)
                    || cheatMods.Contains(mod.id))
                .Select(mod => mod.id)
                .ToArray();
        }
        
        public static readonly string[] highImpactMods = {
            "devtools",
            "rwremix",
            "moreslugcats",
            "keepthatawayfromme",  // needs extra syncing to work
            "no-damage-rng",
        };

        public static readonly string[] cheatMods = {
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
        };

        public static string[] GetCheatMods(string[] mods = null)
        {
            if (mods is null) mods = ModManager.ActiveMods.Select(mod => mod.id).ToArray();
            return mods.Intersect(cheatMods).ToArray();
        }
        internal static void CheckMods(string[] lobbyMods, string[] localMods)
        {
            if (GetCheatMods(lobbyMods).Any())
            {
                // ignore all cheats  // TODO: handle cheat load order?
                lobbyMods = lobbyMods.Except(cheatMods).ToArray();
                localMods = localMods.Except(cheatMods).ToArray();
            }
            RainMeadow.Debug($"lobbyMods: [ {string.Join(", ", lobbyMods)} ]");
            RainMeadow.Debug($"localMods: [ {string.Join(", ", localMods)} ]");
            if (Enumerable.SequenceEqual(localMods, lobbyMods))
            {
                RainMeadow.Debug("Same mod set");
            }
            else if (localMods.ToHashSet().SetEquals(lobbyMods))
            {
                RainMeadow.Debug("Same mod set, but different order");  // TODO ask user to reorder
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

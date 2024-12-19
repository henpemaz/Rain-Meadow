using RWCustom;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

            return ModManager.ActiveMods
                .Where(mod => highImpactMods.Contains(mod.id)
                    || Directory.Exists(Path.Combine(mod.path, "modify", "world")))
                .Select(mod => mod.id)
                .ToArray();
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
        };

        public static string[] GetBannedMods()
        {
            UpdateFromOrWriteToFile("meadow-highimpactmods.txt", ref highImpactMods);
            UpdateFromOrWriteToFile("meadow-bannedmods.txt", ref bannedMods);

            // (high impact + banned) - enabled
            return highImpactMods.Concat(bannedMods)
                .Except(ModManager.ActiveMods.Select(mod => mod.id))
                .ToArray();
        }

        internal static void CheckMods(string[] requiredMods, string[] bannedMods)
        {
            RainMeadow.Debug($"required: [ {string.Join(", ", requiredMods)} ]");
            RainMeadow.Debug($"banned:   [ {string.Join(", ", bannedMods)} ]");
            var active = ModManager.ActiveMods.Select(mod => mod.id);
            bool reorder = false;
            var disable = GetRequiredMods().Union(bannedMods).Except(requiredMods).Intersect(active);
            var enable = requiredMods.Except(active);

            RainMeadow.Debug($"active:  [ {string.Join(", ", active)} ]");
            RainMeadow.Debug($"enable:  [ {string.Join(", ", enable)} ]");
            RainMeadow.Debug($"disable: [ {string.Join(", ", disable)} ]");

            if (!reorder && !disable.Any() && !enable.Any()) return;

            var lobbyID = MatchmakingManager.instance.GetLobbyID();
            RWCustom.Custom.rainWorld.processManager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
            OnlineManager.LeaveLobby();

            List<bool> pendingEnabled = ModManager.InstalledMods.ConvertAll(mod => mod.enabled);
            List<int> pendingLoadOrder = ModManager.InstalledMods.ConvertAll(mod => mod.loadOrder);

            List<string> missingMods = new();
            List<ModManager.Mod> modsToEnable = new(), modsToDisable = new();

            foreach (var id in enable)
            {
                int index = ModManager.InstalledMods.FindIndex(mod => mod.id == id);
                if (index == -1) missingMods.Add(id);
                else
                {
                    pendingEnabled[index] = true;
                    modsToEnable.Add(ModManager.InstalledMods[index]);
                }
            }

            foreach (var id in disable)
            {
                int index = ModManager.InstalledMods.FindIndex(_mod => _mod.id == id);
                pendingEnabled[index] = false;
                modsToDisable.Add(ModManager.InstalledMods[index]);
            }

            ModApplier modApplier = new(RWCustom.Custom.rainWorld.processManager, pendingEnabled, pendingLoadOrder);

            modApplier.ShowConfirmation(modsToEnable, modsToDisable, missingMods);

            modApplier.OnFinish += (ModApplier modApplyer) =>
            {
                RainMeadow.Debug("Finished applying");

                if (modApplier.requiresRestart)
                {
                    Utils.Restart($"+connect_lobby {lobbyID}");
                }
            };
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RWCustom;

namespace RainMeadow
{
    public static class RainMeadowModManager
    {
        // TODO: possibly rename these
        public static string SyncRequiredModsFileName => "meadow-syncrequiredmods.txt";
        public static string BannedOnlineModsFileName => "meadow-bannedonlinemods.txt";

        /// <summary>
        /// Prefix that indicates the following characters should be ignored in one of the user defined files.
        /// </summary>
        public static string CommentPrefix => "//";

        public static string[] GetRequiredMods()
        {
            var modInfo = RainMeadowModInfoManager.MergedModInfo;

            var requiredMods = modInfo.SyncRequiredMods.Except(modInfo.SyncRequiredModsOverride).ToList();

            requiredMods = UpdateFromOrWriteToFile(SyncRequiredModsFileName, requiredMods);

            return ModManager.ActiveMods
                .Where(mod => requiredMods.Contains(mod.id)
                              || Directory.Exists(Path.Combine(mod.path, "modify", "world"))) // TODO: check if this is still necessary, see GetGeneratedModInfo()
                .Select(mod => mod.id)
                .ToArray();
        }

        public static string[] GetBannedMods()
        {
            var modInfo = RainMeadowModInfoManager.MergedModInfo;

            var requiredMods = modInfo.SyncRequiredMods.Except(modInfo.SyncRequiredModsOverride).ToList();
            var bannedMods = modInfo.BannedOnlineMods.Except(modInfo.BannedOnlineModsOverride).ToList();

            requiredMods = UpdateFromOrWriteToFile(SyncRequiredModsFileName, requiredMods);
            bannedMods = UpdateFromOrWriteToFile(BannedOnlineModsFileName, bannedMods);

            // (required + banned) - enabled
            return requiredMods.Concat(bannedMods)
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

        private static List<string> UpdateFromOrWriteToFile(string path, List<string> newLines)
        {
            path = Path.Combine(Custom.RootFolderDirectory(), path);

            if (!File.Exists(path))
            {
                File.WriteAllLines(path, newLines);
                return newLines;
            }

            var existingLines = File.ReadAllLines(path).Distinct().ToList();
            var linesToWrite = new List<string>();

            // Lines without their comments and whitespaces: disabled have a leading comment, meaning the whole line is commented out
            var trimmedActiveLines = new List<string>();
            var trimmedDisabledLines = new List<string>();

            // Trim non-leading comments (leading comments will be used to exclude mods)
            foreach (var line in existingLines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    linesToWrite.Add(line);
                    continue;
                }

                var trimmedLine = line.Trim();
                var isDisabledLine = false;

                // Leading comment disables the whole line
                if (trimmedLine.StartsWith(CommentPrefix))
                {
                    trimmedLine = trimmedLine.TrimStart(CommentPrefix);
                    isDisabledLine = true;
                }

                var commentStartIndex = trimmedLine.IndexOf(CommentPrefix, StringComparison.InvariantCulture);

                // Trim any additional (non-leading) comments
                if (commentStartIndex != -1)
                {
                    trimmedLine = string.Concat(trimmedLine.TakeFromTo(0, commentStartIndex)).Trim();
                }

                // Discard duplicate active lines
                if (!isDisabledLine && trimmedActiveLines.Contains(trimmedLine))
                {
                    continue;
                }

                if (isDisabledLine)
                {
                    trimmedDisabledLines.Add(trimmedLine);
                }
                else
                {
                    trimmedActiveLines.Add(trimmedLine);
                }

                linesToWrite.Add(line);
            }

            var linesToAdd = newLines.Except(trimmedActiveLines).Except(trimmedDisabledLines);

            linesToWrite.AddDistinctRange(linesToAdd);

            File.WriteAllLines(path, linesToWrite);

            return linesToWrite.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        }
    }
}

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
        public static string SyncRequiredModsFileName => "meadow-highimpactmods.txt";
        public static string BannedOnlineModsFileName => "meadow-bannedmods.txt";

        public static string SyncRequiredModsExplanationComment =>
            """
            // The following is a list of mods that must be synced between client and host:
            // (if the host has these mods enabled/disabled, the client must match)
            // To exclude mods on the list from these requirements, prefix the lines with '//', like this:
            //mod-id-to-exclude

            """;

        public static string BannedOnlineModsExplanationComment =>
            """
            // The following is a list of mods that are banned from online play if the host does not have them enabled:
            // (if any of these mods are enabled on a client, they must be enabled on the host to join)
            // To exclude mods on the list from these requirements, prefix the lines with '//', like this:
            //mod-id-to-exclude

            """;

        /// <summary>
        /// Prefix that indicates the following characters should be ignored in one of the user defined files.
        /// </summary>
        public static string CommentPrefix => "//";

        public static string[] GetRequiredMods()
        {
            var modInfo = RainMeadowModInfoManager.MergedModInfo;

            var requiredMods = modInfo.SyncRequiredMods.Except(modInfo.SyncRequiredModsOverride).ToList();

            requiredMods = UpdateFromOrWriteToFile(SyncRequiredModsFileName, requiredMods, SyncRequiredModsExplanationComment);

            return ModManager.ActiveMods
                .Where(mod => requiredMods.Contains(mod.id))
                .Select(mod => mod.id)
                .ToArray();
        }

        public static string ModIdToName(string id)
        {
            //default case: if the name isn't found, the ID should hopefully be a better replacement than "null" or something
            return ModManager.InstalledMods.FirstOrDefault(mod => mod.id == id)?.name ?? id;
        }

        public static string RequiredModsArrayToString(string[] requiredMods)
        {
            return string.Join("\n", requiredMods);
        }
      
        public static string[] RequiredModsStringToArray(string requiredMods)
        {
            return requiredMods.Split('\n');
        }
      
        public static string[] GetBannedMods()
        {
            var modInfo = RainMeadowModInfoManager.MergedModInfo;

            var syncRequiredMods = modInfo.SyncRequiredMods.Except(modInfo.SyncRequiredModsOverride).ToList();
            var bannedOnlineMods = modInfo.BannedOnlineMods.Except(modInfo.BannedOnlineModsOverride).ToList();

            syncRequiredMods = UpdateFromOrWriteToFile(SyncRequiredModsFileName, syncRequiredMods, SyncRequiredModsExplanationComment);
            bannedOnlineMods = UpdateFromOrWriteToFile(BannedOnlineModsFileName, bannedOnlineMods, BannedOnlineModsExplanationComment);

            // (required + banned) - enabled
            return syncRequiredMods.Concat(bannedOnlineMods)
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

            var lobbyID = MatchmakingManager.currentInstance.GetLobbyID();
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

        private static List<string> UpdateFromOrWriteToFile(string path, List<string> newLines, string startingComment = "")
        {
            path = Path.Combine(Custom.RootFolderDirectory(), path);

            if (!File.Exists(path))
            {
                newLines = ModIdsToIdAndName(newLines);

                if (startingComment != "")
                {
                    newLines.Insert(0, startingComment);
                }

                File.WriteAllLines(path, newLines);
                return newLines;
            }

            var existingLines = File.ReadAllLines(path).Distinct().ToList();
            var linesToWrite = new List<string>();

            if (startingComment != "")
            {
                if (existingLines.Count == 0 || existingLines[0].Trim() != startingComment.Split('\n')[0].Trim())
                {
                    existingLines.Insert(0, startingComment);
                }
            }

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

            var linesToAdd = newLines.Except(trimmedActiveLines).Except(trimmedDisabledLines).ToList();

            linesToWrite.AddDistinctRange(linesToAdd);
            linesToWrite = linesToWrite.Select(x => x.Trim()).ToList();
            linesToWrite = ModIdsToIdAndName(linesToWrite);

            File.WriteAllLines(path, linesToWrite);

            return trimmedActiveLines;
        }

        private static List<string> ModIdsToIdAndName(List<string> modIds)
        {
            return modIds.Select(x => x == ModIdToName(x) ? x : x + " // " + ModIdToName(x)).ToList();
        }
    }
}

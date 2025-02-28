using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace RainMeadow
{
    public static class RainMeadowModManager
    {
        // TODO: possibly rename these
        public static string SyncRequiredModsFileName => "meadow-highimpactmods.txt";
        public static string BannedOnlineModsFileName => "meadow-bannedmods.txt";

        public static string SyncRequiredModsExplanationComment => """
                                                                   // The following is a list of mods that must be synced between client and host:
                                                                   // (if the host has these mods enabled/disabled, the client must match)
                                                                   // To remove mod IDs and allow them to be de-synced, prefix the lines with '//', like this:
                                                                   //mod-id-to-exclude
                                                                   
                                                                   """;

        public static string BannedOnlineModsExplanationComment =>"""
                                                                   // The following is a list of mods that are banned from online play:
                                                                   // (if any of these mods are enabled, they must be disabled before meadow can be entered)
                                                                   // To remove mod IDs and allow them online, prefix the lines with '//', like this:
                                                                   //mod-id-to-exclude

                                                                   """;

        /// <summary>
        /// Prefix that indicates the following characters should be ignored in one of the user defined files.
        /// </summary>
        public static string CommentPrefix => "//";

        /// <summary>
        /// Used to prevent double-checking mods that have already been marked as required.
        /// Does NOT save the load order.
        /// This is NOT a replacement for calling GetRequiredMods().
        /// </summary>
        private static string[] CurrentRequiredMods = { };
        public static string[] GetRequiredMods()
        {

            var modInfo = RainMeadowModInfoManager.MergedModInfo;

            var requiredMods = modInfo.SyncRequiredMods.Except(modInfo.SyncRequiredModsOverride).ToList();

            requiredMods = UpdateFromOrWriteToFile(SyncRequiredModsFileName, requiredMods, SyncRequiredModsExplanationComment);

            //look through old required mod list
            var activeMods = ModManager.ActiveMods.Select(mod => mod.id);
            requiredMods.Concat(CurrentRequiredMods);
            requiredMods.RemoveAll(mod => !activeMods.Contains(mod)); //if no longer active, remove it

            //add dependencies
            foreach (var mod in ModManager.ActiveMods)
            {
                if (requiredMods.Contains(mod.id))
                    requiredMods.AddDistinctRange(mod.requirements);
            }

            //check .dlls for extra required mods
            CurrentRequiredMods = GetExtendedHighImpactMods(requiredMods.ToArray());

            return ModManager.ActiveMods
                .Where(mod => CurrentRequiredMods.Contains(mod.id))
                .OrderBy(mod => mod.loadOrder)
                .Select(mod => mod.id)
                .ToArray();
        }

        public static string ModIdToName(string id)
        {
            foreach (var mod in ModManager.ActiveMods)
            {
                if (mod.id == id)
                    return mod.LocalizedName;
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

        /// <summary>
        /// Used to prevent double-checking mods that have already been marked as banned.
        /// This is NOT a replacement for calling GetBannedMods().
        /// </summary>
        private static string[] CurrentBannedMods = { };
        public static string[] GetBannedMods()
        {
            var modInfo = RainMeadowModInfoManager.MergedModInfo;

            //var requiredMods = modInfo.SyncRequiredMods.Except(modInfo.SyncRequiredModsOverride).ToList();
            var bannedMods = modInfo.BannedOnlineMods.Except(modInfo.BannedOnlineModsOverride).ToList();

            //requiredMods = UpdateFromOrWriteToFile(SyncRequiredModsFileName, requiredMods, SyncRequiredModsExplanationComment);
            bannedMods = UpdateFromOrWriteToFile(BannedOnlineModsFileName, bannedMods, BannedOnlineModsExplanationComment);

            //look through old banned mod list
            var activeMods = ModManager.ActiveMods.Select(mod => mod.id);
            bannedMods.Concat(CurrentBannedMods);
            //updatedBanned.RemoveAll(mod => !activeMods.Contains(mod)); //if no longer active, remove it

            // (required + banned) - enabled
            var initialBanned = GetRequiredMods().Concat(bannedMods)
                .Select(mod => activeMods.Contains(mod) ? CommentPrefix + mod : mod) //add CommentPrefix to active mods
                .ToArray();

            CurrentBannedMods = GetExtendedHighImpactMods(initialBanned)
                .Select(mod => activeMods.Contains(mod) ? CommentPrefix + mod : mod) //add CommentPrefix to active mods AGAIN
                .ToArray();

            return CurrentBannedMods;
        }

        /// <summary>
        /// A list of types that are high-impact (either required or banned).
        /// Mod plugins will be scanned to see if they implement these types;
        /// and if so, they will be marked as high-impact
        /// </summary>
        private static string[] HighImpactBaseTypes = ["Fisob", "Critob", nameof(PhysicalObject), nameof(Creature)];

        /// <summary>
        /// Looks through all active mods (that aren't explicitly allowed)
        /// to determine if they implement any highImpact base types.
        /// Used to extend required or banned mod lists.
        /// </summary>
        /// <param name="highImpactMods">The banned or required mod list</param>
        /// <returns></returns>
        public static string[] GetExtendedHighImpactMods(string[] highImpactMods)
        {
            List<string> extendedBannedMods = highImpactMods.ToList();

            foreach (var mod in ModManager.ActiveMods)
            {
                if (highImpactMods.Contains(mod.id) || highImpactMods.Contains(CommentPrefix + mod.id))
                    continue; //if the mod is allowed or already banned, don't try to (re)ban it

                bool modBanned = false;

                //get mod dlls
                List<string> dlls = new();
                var plugins = Utils.GetDirectoryFilesSafe(Path.Combine(mod.path, "plugins"))
                    .Union(Utils.GetDirectoryFilesSafe(Path.Combine(mod.NewestPath, "plugins"))
                    .Union(Utils.GetDirectoryFilesSafe(Path.Combine(mod.TargetedPath, "plugins"))));
                foreach (var file in plugins)
                    if (Path.GetExtension(file) == ".dll")
                        dlls.Add(file);

                //loop through each dll
                foreach (var file in dlls)
                {
                    try
                    {
                        //load it as an assembly
                        var assembly = Assembly.LoadFrom(file);
                        if (assembly == null)
                        {
                            RainMeadow.Debug($"Invalid assembly: {file}");
                            continue;
                        }

                        foreach (var type in assembly.GetTypesSafely().ToList())
                        {
                            try
                            {
                                for (var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
                                {
                                    if (HighImpactBaseTypes.Contains(baseType.Name))
                                    {
                                        extendedBannedMods.Add(mod.id);
                                        modBanned = true;
                                        RainMeadow.Debug($"Marked {mod.id} as high-impact due to implementing a Fisob, Critob, or PhysicalObject.");
                                        break;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                RainMeadow.Error(assembly.FullName + ":" + type.FullName);
                                RainMeadow.Error(e);
                            }
                            if (modBanned) break;
                        }
                    }
                    catch (Exception ex)
                    {
                        RainMeadow.Error(ex);
                    }
                    if (modBanned) break;
                }
            }

            return extendedBannedMods.Distinct().ToArray();
        }

        /// <summary>
        /// Checks the user's mod list with the lobby, and makes his alter his mod list if necessary.
        /// </summary>
        /// <param name="requiredMods">Mods that the user MUST have in order to join the lobby</param>
        /// <param name="bannedMods">Mods that the user must NOT have, unless the mods are in <paramref name="requiredMods"/></param>
        /// <param name="onFinish">The action to be taken once the mods are successfully applied.</param>
        /// <param name="ignoreReorder">Whether the lobby should accept users with the same mods but in a different order</param>
        /// <param name="restartCode">The code that the restarter will use to attempt to rejoin the lobby after a restart.</param>
        /// <returns>True if the mods were successfully applied (or didn't need to be applied) AND the game does not require a restart.</returns>
        internal static void CheckMods(string[] requiredMods, string[] bannedMods, Action? onFinish, bool ignoreReorder = false, string restartCode = "")
        {
            try
            {
                RainMeadow.Debug($"required: [ {string.Join(", ", requiredMods)} ]");
                RainMeadow.Debug($"banned:   [ {string.Join(", ", bannedMods)} ]");
                var active = ModManager.ActiveMods.Select(mod => mod.id).ToList();

                bool reorder = true; //or change mods whatsoever

                var disable = GetRequiredMods().Union(GetExtendedHighImpactMods(bannedMods)).Except(requiredMods).Intersect(active).ToList();

                var enable = requiredMods.Except(active).ToList();

                //clear phony entries to the mod list
                enable.RemoveAll(mod => mod == null || mod == "" || mod.StartsWith(CommentPrefix)); //ignore commented mods, just in case
                disable.RemoveAll(mod => mod == null || mod == "" || mod.StartsWith(CommentPrefix));

                //determine whether a reorder is necessary
                if (!disable.Any() && !enable.Any())
                {
                    reorder = false;
                    if (!ignoreReorder)
                    {
                        try
                        {
                            int prevIdx = Int32.MinValue;
                            for (int i = 0; i < requiredMods.Length; i++)
                            {
                                if (requiredMods[i] == "henpemaz_rainmeadow")
                                    continue; //ignore Rain Meadow when determining whether mods need to be reordered
                                int modIdx = ModManager.ActiveMods.FindIndex(mod => requiredMods[i] == mod.id);
                                if (modIdx < 0)
                                {
                                    RainMeadow.Debug($"Couldn't find {requiredMods[i]} in ActiveMods");
                                    continue;
                                }
                                int loadOrder = ModManager.ActiveMods[modIdx].loadOrder;
                                if (loadOrder < prevIdx)
                                {
                                    reorder = true;
                                    RainMeadow.Debug($"Reorder necessary. Idx: {i}");
                                    break;
                                }
                                prevIdx = loadOrder;
                            }
                        }
                        catch (Exception ex) { RainMeadow.Error(ex); }
                    }
                }

                RainMeadow.Debug($"active:  [ {string.Join(", ", active)} ]");
                RainMeadow.Debug($"enable:  [ {string.Join(", ", enable)} ]");
                RainMeadow.Debug($"disable: [ {string.Join(", ", disable)} ]");
                RainMeadow.Debug($"reorder: {reorder}");

                if (!reorder) {
                    onFinish?.Invoke();
                    return;
                }

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
                            RainMeadow.Debug($"Restarting game with code {restartCode}");
                            Utils.Restart(restartCode);
                        }
                        else if (modApplier.WasSuccessful())
                        {
                            RainMeadow.Debug("Successfully applied mods");
                            onFinish?.Invoke();
                        }
                        else
                            RainMeadow.Debug("Error in mod applier; unsuccessful");
                    };
                });
            }
            catch (Exception ex)
            {
                RainMeadow.Error(ex);
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

        private static List<string> UpdateFromOrWriteToFile(string path, List<string> newLines, string startingComment = "")
        {
            path = Path.Combine(Custom.RootFolderDirectory(), path);

            if (!File.Exists(path))
            {
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

            var linesToAdd = newLines.Except(trimmedActiveLines).Except(trimmedDisabledLines);

            linesToWrite.AddDistinctRange(linesToAdd);

            File.WriteAllLines(path, linesToWrite);

            return trimmedActiveLines;
        }
    }
}

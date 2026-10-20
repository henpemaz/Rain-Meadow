using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RainMeadow.LevelsScan;

public static partial class ArenaScanner
{
    internal static void ApplyHooks(RainWorld rainWorld)
    {
        ArenaScanner.rainWorld = rainWorld;
        ScanForArenas(ref scannedArenas, rainWorld, true, true);
        RainMeadow.Debug("Arenas found: " + string.Join(" ;; ", scannedArenas.Select(x => x.Key + ": " + string.Join(",", x.Value.Select(x => (x.IsAvailable ? "" : "[X]") + x.levelName)))));

        MatchmakingManager.OnLobbyJoined += MatchmakingManager_OnLobbyJoined_ScanForNewArenas;
        On.ModManager.RefreshModsLists += ModManager_RefreshModsLists_ScanForNewArenas;
        On.AssetManager.ResolveFilePath_string_bool_bool += AssetManager_ResolveFilePath_GetAdditionalLevelsPath;
        On.ArenaSetup.GameTypeSetup.LevelExists += GameTypeSetup_LevelExists_SearchAdditionalLevels;
    }

    private static string AssetManager_ResolveFilePath_GetAdditionalLevelsPath(On.AssetManager.orig_ResolveFilePath_string_bool_bool orig, string path, bool skipMergedMods, bool skipConsoleFiles)
    {
        string[] pathTable = path.Split(Path.DirectorySeparatorChar);
        if (RainMeadow.isArenaMode(out var _) 
            && !skipMergedMods 
            && pathTable[0].ToLowerInvariant() == "Levels".ToLowerInvariant()
            && pathTable.Length == 2) // we should have smt like levels/levelname_smtsmt.extension
        {
            var arenas = availableArenas;
            int index = arenas.FindIndex(x => pathTable[1].ToLowerInvariant().StartsWith(x.levelName.ToLowerInvariant()));
            if (index >= 0)
            {
                string levelExtension = pathTable[1].Substring(arenas[index].levelName.Length);
                string finalPath = (arenas[index].BaldPath + levelExtension).ToLowerInvariant();
                if (File.Exists(finalPath)) return finalPath;
            }
        }
        return orig(path, skipMergedMods, skipConsoleFiles);
    }
    private static bool GameTypeSetup_LevelExists_SearchAdditionalLevels(On.ArenaSetup.GameTypeSetup.orig_LevelExists orig, string levelName)
    {
        return orig(levelName) || (RainMeadow.isArenaMode(out var _) && availableArenasFromDisabledMods.Exists(x => x.levelName == levelName));
    }
    private static void MatchmakingManager_OnLobbyJoined_ScanForNewArenas(bool ok, string error)
    {
        if (ok && RainMeadow.isArenaMode(out var _))
        {
            ScanForArenas(ref scannedArenas, rainWorld, refreshAll:true);
        }
    }
    private static void ModManager_RefreshModsLists_ScanForNewArenas(On.ModManager.orig_RefreshModsLists orig, RainWorld rainWorld)
    {
        orig(rainWorld);
        ScanForArenas(ref scannedArenas, rainWorld, true, true);
    }
}
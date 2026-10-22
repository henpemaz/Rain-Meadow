using System;
using System.IO;
using System.Linq;

namespace RainMeadow.ArenaScan;

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
        On.MultiplayerUnlocks.LevelDisplayName += MultiplayerUnlocks_LevelDisplayName_ShowRealName;
    }

    private static string MultiplayerUnlocks_LevelDisplayName_ShowRealName(On.MultiplayerUnlocks.orig_LevelDisplayName orig, string s)
    {
        if (RainMeadow.isArenaMode(out var _))
        {
            return orig(ArenaInfo.SplitLevelID(s).levelName);
        }
        return orig(s);
    }

    private static string AssetManager_ResolveFilePath_GetAdditionalLevelsPath(On.AssetManager.orig_ResolveFilePath_string_bool_bool orig, string path, bool skipMergedMods, bool skipConsoleFiles)
    {
        
        if (RainMeadow.isArenaMode(out var _))
        {
            string[] pathTable = path.Split(Path.DirectorySeparatorChar);
            if (!skipMergedMods 
                && pathTable[0].ToLowerInvariant() == "Levels".ToLowerInvariant()
                && pathTable.Length == 2) // we should have smt like levels/levelname[\0]modid_smtsmt.extension
            {
                var arenas = availableArenas;
                string finalPath = "";
                if (arenas.Find(x => x.TryFindFileFromFileName(pathTable[1], out finalPath)) is ArenaInfo arenaInfo)
                {
                    RainMeadow.Debug($"Found file {finalPath.Split(Path.DirectorySeparatorChar).Last()} from file name {path}! Arena was {arenaInfo.levelName} from {arenaInfo.ModID}.");
                    return finalPath;
                }
            }
            if (path.Contains(ArenaInfo.modSeparator))
            {
                RainMeadow.Debug($"Path {path} is illegal and supposed to be an arena, making it empty.");
                return "";
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
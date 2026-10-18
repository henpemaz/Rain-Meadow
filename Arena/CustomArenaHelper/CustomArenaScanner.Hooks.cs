using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow.CustomArenaHelper;

public static partial class CustomArenaScanner
{
    internal static void ApplyHooks(RainWorld rainWorld)
    {
        CustomArenaScanner.rainWorld = rainWorld;
        ScanAllUnloadedModsForArenas(rainWorld, true, true);

        MatchmakingManager.OnLobbyJoined += MatchmakingManager_OnLobbyJoined_ScanForNewArenas;
        On.ModManager.RefreshModsLists += ModManager_RefreshModsLists_ScanForNewArenas;
        On.Menu.MultiplayerMenu.FindAllLevels += MultiplayerMenu_FindAllLevels_AddUnlodedLevels;
    }

    private static void MatchmakingManager_OnLobbyJoined_ScanForNewArenas(bool ok, string error)
    {
        if (ok && RainMeadow.isArenaMode(out var _))
        {
            ScanAllUnloadedModsForArenas(rainWorld, refreshAll:true);
        }
    }

    private static List<string> MultiplayerMenu_FindAllLevels_AddUnlodedLevels(On.Menu.MultiplayerMenu.orig_FindAllLevels orig)
    {
        List<string> origList = orig();
        if (RainMeadow.isArenaMode(out var _))
        {
            string[] addedlevels = allLevelsName;
            RainMeadow.Debug($"Added levels [{string.Join(", ", addedlevels)}] to the list !");
            origList.AddRange(addedlevels);
        }
        return origList;
    }

    private static void ModManager_RefreshModsLists_ScanForNewArenas(On.ModManager.orig_RefreshModsLists orig, RainWorld rainWorld)
    {
        orig(rainWorld);
        ScanAllUnloadedModsForArenas(rainWorld, true, true);
    }
}
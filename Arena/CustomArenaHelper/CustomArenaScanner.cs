using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RWCustom;
using Steamworks;
using UnityEngine;

namespace RainMeadow.CustomArenaHelper;

public static partial class CustomArenaScanner
{
    public static Dictionary<string, List<UnloadedArenaInfo>> scannedUnloadedArenas = [];
    public static string[] allLevelsName 
        => unloadedArenas.Select(x => x.levelName).ToArray();
    public static string[] allLevelsPath 
        => unloadedArenas.Select(x => x.levelPath).ToArray();
    public static UnloadedArenaInfo[] unloadedArenas 
        => scannedUnloadedArenas.SelectMany(kp => kp.Value.Where(x => x.IsAvailable && x.mod?.enabled is not true)).ToArray();
    private static RainWorld rainWorld = null!;
    
    public static ModManager.Mod[] GetAllInstalledMods(RainWorld rainWorld) // from ModManager.RefreshModList
    {
        List<ModManager.Mod> mods = [];
        var sw = Stopwatch.StartNew();

        // Scan locally installed mods
        try
        {
            string modMainFolder = Custom.RootFolderDirectory() + Path.DirectorySeparatorChar.ToString() + "mods";
            if (Directory.Exists(modMainFolder))
            {
                string[] modsFolder = Directory.GetDirectories(modMainFolder);
                for (int j = 0; j < modsFolder.Length; j++)
                {
                    string fileName = Path.GetFileName(modsFolder[j]);
                    string path = modsFolder[j];
                    ModManager.Mod? mod = ModManager.InstalledMods.Find(x => x.path == path);
                    if (mod is not null)
                    {
                        mods.Add(mod);
                    }
                    else
                    {
                        mod = ModManager.LoadModFromJson(rainWorld, path, path);
                        if (mod is not null) mods.Add(mod);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            RainMeadow.Debug($"Error while scanning for local mods : " + ex);
        }

        // Scan steam workshop installed mods
        try
        {
            if (rainWorld.processManager?.mySteamManager is not null)
            {
                PublishedFileId_t[] subscribedItems = rainWorld.processManager.mySteamManager.GetSubscribedItems();
                for (int m = 0; m < subscribedItems.Length; m++)
                {
                    if (SteamUGC.GetItemInstallInfo(subscribedItems[m], out var _, out var path, 1024U, out var _))
                    {
                        ModManager.Mod? mod = ModManager.InstalledMods.Find(x => x.path == path);
                        if (mod is not null)
                        {
                            mods.Add(mod);
                        }
                        else
                        {
                            mod = ModManager.LoadModFromJson(rainWorld, path, path);
                            mod.workshopId = subscribedItems[m].m_PublishedFileId;
                            mod.workshopMod = true;
                            if (mod is not null) mods.Add(mod);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            RainMeadow.Debug($"Error while scanning for steam workshop mods : " + ex);
        }

        RainMeadow.Debug($"Found {mods.Count} mods in {sw.ElapsedMilliseconds}ms !");
        sw.Stop();
        return mods.ToArray();
    }
    public static void ScanAllUnloadedModsForArenas(RainWorld rainWorld, bool refreshAll = false, bool checkOnlyRWList = false)
    {
        // Check all mods installed
        ModManager.Mod[] mods = checkOnlyRWList 
            ? ModManager.InstalledMods.ToArray() 
            : GetAllInstalledMods(rainWorld);

        if (refreshAll) scannedUnloadedArenas.Clear();

        var sw = Stopwatch.StartNew();
        int modScanned = 0, levelScanned = 0;
        RainMeadow.Debug($"Scanning {(refreshAll ? "all" : "newly")} installed mod {(checkOnlyRWList ? "(only rain world list)" : "(all directories)")} for arenas");
        try
        {
            for (int i = 0; i < mods.Length; i++)
            {
                if (!mods[i].enabled
                    && (refreshAll || !scannedUnloadedArenas.ContainsKey(mods[i].id))
                    && Directory.Exists(Path.Combine(mods[i].path, "Levels".ToLowerInvariant())))
                {
                    scannedUnloadedArenas[mods[i].id] = UnloadedArenaInfo.ScanUnloadedMod(mods[i]);
                    modScanned++;
                    levelScanned += scannedUnloadedArenas[mods[i].id].Count;
                }
            }
        }
        catch (Exception ex)
        {
            RainMeadow.Debug($"Error while scanning for arenas in mods : " + ex);
        }
        RainMeadow.Debug($"Scanned {levelScanned} arenas across {modScanned} mods in {sw.ElapsedMilliseconds}ms !");
        sw.Stop();
    }
}
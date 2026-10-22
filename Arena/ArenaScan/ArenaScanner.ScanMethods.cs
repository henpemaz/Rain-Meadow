using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using HarmonyLib;
using RWCustom;
using Steamworks;

using ArenaInfoPerModTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<RainMeadow.ArenaScan.ArenaInfo>>;

namespace RainMeadow.ArenaScan;

public static partial class ArenaScanner
{
    public delegate void ArenaScanFunc(RainWorld rainWorld, ArenaInfoPerModTable scannedArenas, bool refreshAll);
    public static event ArenaScanFunc? OnArenaScan;
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
            RainMeadow.Debug($"Error while scanning for local mods: " + ex);
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
            RainMeadow.Debug($"Error while scanning for steam workshop mods: " + ex);
        }

        RainMeadow.Debug($"Found {mods.Count} mods in {sw.ElapsedMilliseconds}ms!");
        sw.Stop();
        return mods.ToArray();
    }
    public static ArenaInfoPerModTable ScanForArenas(ref ArenaInfoPerModTable arenaRefTable, RainWorld rainWorld, bool refreshAll = false, bool checkOnlyRWmodList = false)
    {
        int modScanned = 0, levelScanned = 0, availableLevels = 0, offlineLevels = 0, importedLevels = 0;
        RainMeadow.Debug($"Scanning {(refreshAll ? "all" : "newly")} installed mods {(checkOnlyRWmodList ? "(only rain world mod list)" : "(all directories)")} for arenas");

        if (refreshAll) arenaRefTable.Clear();

        var sw = Stopwatch.StartNew();
        
        ScanVanillaFolderForArenas(ref arenaRefTable, refreshAll);
        ScanImportedFolderForArenas(ref arenaRefTable, refreshAll);
        ScanAllModsForArenas(ref arenaRefTable, rainWorld, refreshAll, checkOnlyRWmodList);

        if (RainMeadow.rainMeadowOptions.CurrentLogLevel.Value <= RainMeadow.LogLevel.Debug)
        {
            arenaRefTable.Do(kp =>
            {
                modScanned++;
                kp.Value.Do(x =>
                {
                    levelScanned++;
                    if (x.IsAvailable)
                    {
                        availableLevels++;
                        if (x.IsFromMeadowImport) importedLevels++;
                        else if (x.mod?.enabled is false) offlineLevels++;
                    }
                });
            });
        }

        RainMeadow.Debug($"Scanned {levelScanned} arenas across {modScanned} mods, vanilla and import in {sw.ElapsedMilliseconds}ms! Found {availableLevels} arenas available, including {offlineLevels} with their mod disabled and {importedLevels} arenas in the import folder!");
        sw.Stop();

        OnArenaScan?.Invoke(rainWorld, arenaRefTable, refreshAll);

        return arenaRefTable;
    }
    public static ArenaInfoPerModTable ScanVanillaFolderForArenas(ref ArenaInfoPerModTable arenas, bool refreshAll = false)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            if (refreshAll || !scannedArenas.ContainsKey(ArenaInfo.vanillaID))
                scannedArenas[ArenaInfo.vanillaID] = ArenaInfo.ScanVanillaLevelDirectory();
        }
        catch (Exception ex)
        {
            RainMeadow.Error($"Error while scanning for vanilla arenas: " + ex);
        }

        RainMeadow.Debug($"Scanned vanilla arenas in {sw.ElapsedMilliseconds}ms");
        sw.Stop();

        return arenas;
    }
    public static ArenaInfoPerModTable ScanImportedFolderForArenas(ref ArenaInfoPerModTable arenas, bool refreshAll = false)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            if (refreshAll || !scannedArenas.ContainsKey(ArenaInfo.importedID))
                scannedArenas[ArenaInfo.importedID] = ArenaInfo.ScanImportedLevelDirectory();
        }
        catch (Exception ex)
        {
            RainMeadow.Error($"Error while scanning for imported arenas: " + ex);
        }

        RainMeadow.Debug($"Scanned imported arenas in {sw.ElapsedMilliseconds}ms");
        sw.Stop();

        return arenas;
    }
    public static ArenaInfoPerModTable ScanAllModsForArenas(ref ArenaInfoPerModTable arenas, RainWorld rainWorld, bool refreshAll = false, bool checkOnlyRWList = false)
    {
        ModManager.Mod[] mods = checkOnlyRWList 
            ? ModManager.InstalledMods.ToArray() 
            : GetAllInstalledMods(rainWorld);

        var sw = Stopwatch.StartNew();

        try
        {
            for (int i = 0; i < mods.Length; i++)
            {
                if ((refreshAll || !scannedArenas.ContainsKey(mods[i].id))
                    && Directory.Exists(Path.Combine(mods[i].path, "Levels".ToLowerInvariant())))
                {
                    var foundArenas = ArenaInfo.ScanUnloadedMod(mods[i]);
                    if (foundArenas.Count > 0) scannedArenas[mods[i].id] = foundArenas;
                }
            }
        }
        catch (Exception ex)
        {
            RainMeadow.Error($"Error while scanning for arenas in mods : " + ex);
        }

        RainMeadow.Debug($"Scanned modded arenas in {sw.ElapsedMilliseconds}ms");
        sw.Stop();

        return arenas;
    }
}
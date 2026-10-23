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
    public const string vanillaID = ArenaInfo.vanillaID; 
    public const string importedID = ArenaInfo.importedID;
    public const string unknownID = ArenaInfo.unknownID;
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
        int modScanned = 0, levelScanned = 0, availableLevels = 0, offlineLevels = 0, importedLevels = 0, overrideLevels = 0;
        RainMeadow.Debug($"Scanning {(refreshAll ? "all" : "newly")} installed mods {(checkOnlyRWmodList ? "(only rain world mod list)" : "(all directories)")} for arenas and palettes");

        if (refreshAll) arenaRefTable.Clear();

        var sw = Stopwatch.StartNew();

        var mods = checkOnlyRWmodList ? null : GetAllInstalledMods(rainWorld);
        ScanVanillaFolderForArenas(ref arenaRefTable, refreshAll);
        ScanImportedFolderForArenas(ref arenaRefTable, true);
        ScanAllModsForArenas(ref arenaRefTable, rainWorld, refreshAll, checkOnlyRWmodList, mods);

        ScanVanillaFolderForPalettes(ref scannedPalettesPaths, true);
        ScanImportedFolderForPalettes(ref scannedPalettesPaths, true);
        ScanAllModsForPalettes(ref scannedPalettesPaths, rainWorld, refreshAll, checkOnlyRWmodList, mods);

        Dictionary<string, List<string>> arenaToRemove = [];
        List<ArenaInfo> otherOverride = [];
        foreach (var kp in arenaRefTable)
        {
            modScanned++;
            foreach (var arenaInfo in kp.Value)
            {
                levelScanned++;

                ArenaSettings.RegisterSettingFromArenaInfo(arenaInfo);

                if (arenaInfo.IsAvailable)
                {
                    if (arenaInfo.IsOverriding)
                    {
                        // check for vanilla override, since MSC and Watcher tend to do it (holy shit O(n^3))
                        if (arenaInfo.LevelSettings?.OverridingMod is null)
                        {
                            if (arenaRefTable[vanillaID].Exists(x => x.levelName == arenaInfo.levelName))
                            {
                                if (!arenaToRemove.TryGetValue(kp.Key, out _)) arenaToRemove[kp.Key] = [];
                                arenaToRemove[kp.Key].Add(arenaInfo.levelName);
                                RainMeadow.Debug($"{arenaInfo.LevelID} is overriding a vanilla level.");
                                continue;
                            }
                        }
                        else
                        {
                            otherOverride.Add(arenaInfo);
                        }
                    }
                    
                    availableLevels++;
                    if (arenaInfo.IsFromMeadowImport) importedLevels++;
                    else if (arenaInfo.mod?.enabled is false) offlineLevels++;
                }
            }
        }

        foreach (var arenaOverride in otherOverride)
        {
            if (arenaOverride.LevelSettings?.OverridingMod is string ModID
                && arenaRefTable.TryGetValue(ModID, out var arenaInfos)
                && arenaInfos.Find(x => x.IsAvailable && x.levelName == arenaOverride.levelName) is ArenaInfo arenaInfo)
            {
                RainMeadow.Debug($"{arenaOverride.LevelID} is overriding {arenaInfo.LevelID}");
                arenaInfo.Overriding.Add(arenaOverride);

                if (!arenaToRemove.TryGetValue(ModID, out _)) arenaToRemove[ModID] = [];
                arenaToRemove[arenaOverride.ModID].Add(arenaOverride.levelName);
            }
        }

        foreach (var kp in arenaToRemove)
        {
            int removed = arenaRefTable[kp.Key].RemoveAll(x => kp.Value?.Contains(x.levelName) is true);
            availableLevels -= removed;
        }

        RainMeadow.Debug($"Scanned {levelScanned} arenas and {scannedPalettesPaths.Count} palettes across {modScanned} mods, vanilla and import in {sw.ElapsedMilliseconds}ms! Found {availableLevels} arenas available, including {offlineLevels} with their mod disabled, {overrideLevels} overrides, and {importedLevels} arenas in the import folder!");
        sw.Stop();

        OnArenaScan?.Invoke(rainWorld, arenaRefTable, refreshAll);

        return arenaRefTable;
    }

    public static Dictionary<int, string> ScanVanillaFolderForPalettes(ref Dictionary<int, string> palettes, bool refreshAll = false)
    {        
        var sw = Stopwatch.StartNew();

        try
        {
            foreach (var kpv in ArenaInfo.ScanVanillaPaletteDirectory())
            {
                if (refreshAll || !palettes.ContainsKey(kpv.Key))
                palettes[kpv.Key] = kpv.Value;
            }
        }
        catch (Exception ex)
        {
            RainMeadow.Error($"Error while scanning for vanilla palettes: " + ex);
        }

        RainMeadow.Debug($"Scanned vanilla palettes in {sw.ElapsedMilliseconds}ms");
        sw.Stop();

        return palettes;
    }
    public static Dictionary<int, string> ScanImportedFolderForPalettes(ref Dictionary<int, string> palettes, bool refreshAll = false)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            foreach (var kpv in ArenaInfo.ScanImportedPaletteDirectory())
            {
                if (refreshAll || !palettes.ContainsKey(kpv.Key))
                palettes[kpv.Key] = kpv.Value;
            }
        }
        catch (Exception ex)
        {
            RainMeadow.Error($"Error while scanning for imported palettes: " + ex);
        }

        RainMeadow.Debug($"Scanned imported palettes in {sw.ElapsedMilliseconds}ms");
        sw.Stop();

        return palettes;
    }
    public static Dictionary<int, string> ScanAllModsForPalettes(ref Dictionary<int, string> palettes, RainWorld rainWorld, bool refreshAll = false, bool checkOnlyRWList = false, ModManager.Mod[]? mods = null)
    {
        mods ??= checkOnlyRWList 
            ? ModManager.InstalledMods.ToArray() 
            : GetAllInstalledMods(rainWorld);
        
        var sw = Stopwatch.StartNew();

        try
        {
            foreach (var mod in mods)
            {
                foreach (var kpv in ArenaInfo.ScanModPaletteDirectory(mod))
                {
                    if (refreshAll || !palettes.ContainsKey(kpv.Key))
                    palettes[kpv.Key] = kpv.Value;
                }
            }
        }
        catch (Exception ex)
        {
            RainMeadow.Error($"Error while scanning for modded palettes: " + ex);
        }

        RainMeadow.Debug($"Scanned modded palettes in {sw.ElapsedMilliseconds}ms");
        sw.Stop();

        return palettes;
    }

    public static ArenaInfoPerModTable ScanVanillaFolderForArenas(ref ArenaInfoPerModTable arenas, bool refreshAll = false)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            if (refreshAll || !scannedArenas.ContainsKey(vanillaID))
                scannedArenas[vanillaID] = ArenaInfo.ScanVanillaLevelDirectory();
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
            if (refreshAll || !scannedArenas.ContainsKey(importedID))
                scannedArenas[importedID] = ArenaInfo.ScanImportedLevelDirectory();
        }
        catch (Exception ex)
        {
            RainMeadow.Error($"Error while scanning for imported arenas: " + ex);
        }

        RainMeadow.Debug($"Scanned imported arenas in {sw.ElapsedMilliseconds}ms");
        sw.Stop();

        return arenas;
    }
    public static ArenaInfoPerModTable ScanAllModsForArenas(ref ArenaInfoPerModTable arenas, RainWorld rainWorld, bool refreshAll = false, bool checkOnlyRWList = false, ModManager.Mod[]? mods = null)
    {
        mods ??= checkOnlyRWList 
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
                    var foundArenas = ArenaInfo.ScanModLevelDirectory(mods[i]);
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
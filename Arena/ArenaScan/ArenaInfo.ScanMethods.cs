using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RWCustom;
using UnityEngine;

namespace RainMeadow.ArenaScan;

public partial class ArenaInfo
{
    public const string ImportedArenaFolderName = "meadow-importedarenas";
    // HOPEFULLY we don't get mods named like this
    public const string vanillaID = "___vanilla"; 
    public const string importedID = "___imported";
    public const string unknownID = "___unknown";
    public const string palettePrefix = "palette";
    public const string palettesFolder = "palettes";
    public static readonly List<string> suffixThatAreNOTAnArena = [
        "_settings.txt", "_arena.txt", "_properties.txt", "_meadowsettings.txt", 
    ];
    public static string ImportedArenaFolder
    {
        get
        {
            string path = Path.Combine(Custom.RootFolderDirectory(), ImportedArenaFolderName);
            
            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception ex)
                {
                    RainMeadow.Error("Failed to create Imported Arena directory!" + ex);
                }
            }

            return path;
        }
    }

    public static List<string> GetPalettePathFromFolder(string folderPath, out List<int> paletteIDs)
    {
        List<string> palettes = [];
        paletteIDs = [];

        if (Directory.Exists(folderPath))
        {
            foreach (string file in Directory.GetFiles(folderPath))
            {
                string fileName = Path.GetFileName(file).ToLowerInvariant();
                if (fileName.Length > 4 + palettePrefix.Length
                    && fileName.Substring(fileName.Length - 4, 4) == ".png" 
                    && fileName.StartsWith(palettePrefix)
                    && int.TryParse(fileName.Substring(palettePrefix.Length, fileName.Length - (4 + palettePrefix.Length)), out var ID))
				{
                    palettes.Add(file);
					paletteIDs.Add(ID);
				}
            }
        } 

        return palettes;
    }
    public static List<string> GetPalettePathFromMod(string modPath, out List<int> paletteIDs)
        => GetPalettePathFromFolder(Path.Combine(modPath, palettesFolder), out paletteIDs);
    public static List<string> GetPalettePathFromMod(ModManager.Mod mod, out List<int> paletteIDs)
        => GetPalettePathFromMod(mod.path, out paletteIDs);

    public static List<string> GetLevelsPathFromFolder(string folderPath, out List<string> levelNames)
    {
        List<string> levels = [];
        levelNames = [];

        if (Directory.Exists(folderPath))
        {
            foreach (string file in Directory.GetFiles(folderPath)) // from MultiplayerMenu.FindAllLevels
            {
                string fileName = Path.GetFileName(file).ToLowerInvariant();
                if (fileName.Length > 4
                    && fileName.Substring(fileName.Length - 4, 4) == ".txt" 
                    && !suffixThatAreNOTAnArena.Exists(x => fileName.EndsWith(x))
                    && !fileName.Contains("unlockall")
                    && !levelNames.Contains(fileName))
				{
                    levels.Add(file);
					levelNames.Add(fileName.Substring(0, fileName.Length - 4));
				}
            }
        } 

        return levels;
    }
    public static List<string> GetLevelsPathFromMod(string modPath, out List<string> levelNames)
        => GetLevelsPathFromFolder(Path.Combine(modPath, "Levels".ToLowerInvariant()), out levelNames);
    public static List<string> GetLevelsPathFromMod(ModManager.Mod mod, out List<string> levelNames)
        => GetLevelsPathFromMod(mod.path, out levelNames);
    
    internal static List<ArenaInfo> ScanModLevelDirectory(ModManager.Mod mod)
    {
        // Check if this mod is high impact
        var mergedModInfo = RainMeadowModInfoManager.MergedModInfo;
        bool highImpact = RainMeadowModInfoManager.GenerateModInfo(mod).SyncRequiredMods.Contains(mod.id)
            || mergedModInfo.SyncRequiredMods.Except(mergedModInfo.SyncRequiredModsOverride).Contains(mod.id);
        
        // Get all levels
        List<string> levelsPath = GetLevelsPathFromMod(mod, out var levelNames);
        List<ArenaInfo> unloadedArenaInfos = [];

        // Generate the unloaded arena info
        for (int i = 0; i < levelsPath.Count; i++)
        {
            unloadedArenaInfos.Add(new(levelNames[i], levelsPath[i], mod, highImpact));
        }

        return unloadedArenaInfos;
    }
    internal static Dictionary<int, string> ScanModPaletteDirectory(ModManager.Mod mod)
    {
        // Get all palettes
        List<string> palettePaths = GetPalettePathFromMod(mod, out var paletteIDs);

        // Make the palette dict
        Dictionary<int, string> palettes = [];
        for (int i = 0; i < paletteIDs.Count; i++)
        {
            palettes[paletteIDs[i]] = palettePaths[i];
        }
        return palettes;
    }

    internal static List<ArenaInfo> ScanVanillaLevelDirectory()
    {
        // Get all levels
        List<string> levelsPath = GetLevelsPathFromMod(Custom.RootFolderDirectory(), out var levelNames);
        List<ArenaInfo> unloadedArenaInfos = [];

        // Generate the unloaded arena info
        for (int i = 0; i < levelsPath.Count; i++)
        {
            unloadedArenaInfos.Add(new(levelNames[i], levelsPath[i], null, false));
        }
        return unloadedArenaInfos;
    }
    internal static Dictionary<int, string> ScanVanillaPaletteDirectory()
    {
        // Get all palettes
        List<string> palettePaths = GetPalettePathFromMod(Custom.RootFolderDirectory(), out var paletteIDs);

        // Make the palette dict
        Dictionary<int, string> palettes = [];
        for (int i = 0; i < paletteIDs.Count; i++)
        {
            palettes[paletteIDs[i]] = palettePaths[i];
        }
        return palettes;
    }

    internal static List<ArenaInfo> ScanImportedLevelDirectory()
    {
        // Get all levels in the folder
        List<string> levelsPath = GetLevelsPathFromFolder(ImportedArenaFolder, out var levelNames);
        
        // Get all levels in folders in the folder
        if (Directory.Exists(ImportedArenaFolder))
        {
            foreach (var folder in Directory.GetDirectories(ImportedArenaFolder))
            {
                levelsPath.AddRange(GetLevelsPathFromFolder(folder, out var additionalLevelNames));
                levelNames.AddRange(additionalLevelNames);
            }
        }
        
        // Generate the unloaded arena info
        List<ArenaInfo> unloadedArenaInfos = [];
        for (int i = 0; i < levelsPath.Count; i++)
        {
            unloadedArenaInfos.Add(new(levelNames[i], levelsPath[i], null, true));
        }
        return unloadedArenaInfos;
    }
    internal static Dictionary<int, string> ScanImportedPaletteDirectory()
    {
        // Get all palette in the folder
        List<string> palettePaths = GetPalettePathFromFolder(ImportedArenaFolder, out var paletteIDs);
        
        // Get all palette in folders in the folder
        if (Directory.Exists(ImportedArenaFolder))
        {
            foreach (var folder in Directory.GetDirectories(ImportedArenaFolder))
            {
                palettePaths.AddRange(GetPalettePathFromFolder(folder, out var additionalpaletteIDs));
                paletteIDs.AddRange(additionalpaletteIDs);
            }
        }
        
        // Make the palette dict
        Dictionary<int, string> palettes = [];
        for (int i = 0; i < paletteIDs.Count; i++)
        {
            palettes[paletteIDs[i]] = palettePaths[i];
        }
        return palettes;
    }
}
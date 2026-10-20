using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RWCustom;
using UnityEngine;

namespace RainMeadow.LevelsScan;

public readonly struct ArenaInfo(string name, string path, ModManager.Mod? mod, bool highImpact = false)
{
    public static List<string> GetLevelsFromMod(string modPath, out List<string> levelNames)
    {
        string levelDirectory = Path.Combine(modPath, "Levels".ToLowerInvariant());
        List<string> levels = [];
        levelNames = [];

        if (Directory.Exists(levelDirectory))
        {
            foreach (string file in Directory.GetFiles(levelDirectory)) // from MultiplayerMenu.FindAllLevels
            {
                string fileName = Path.GetFileName(file).ToLowerInvariant();
                if (fileName.Length > 4
                    && fileName.Substring(fileName.Length - 4, 4) == ".txt" 
                    && !fileName.EndsWith("_settings.txt")
                    && !fileName.EndsWith("_arena.txt")
                    && !fileName.EndsWith("_arena.txt")
                    && !fileName.EndsWith("_properties.txt")
                    && !fileName.Contains("unlockall")
                    && !levelNames.Contains(fileName))
				{
                    levels.Add(file);
					levelNames.Add(fileName.Substring(0, fileName.Length - 4).Split(Path.DirectorySeparatorChar).Last());
				}
            }
        } 

        return levels;
    }
    public static List<string> GetLevelsFromMod(ModManager.Mod mod, out List<string> levelNames)
        => GetLevelsFromMod(mod.path, out levelNames);
    
    internal static List<ArenaInfo> ScanUnloadedMod(ModManager.Mod mod)
    {
        // Check if this mod is high impact
        var mergedModInfo = RainMeadowModInfoManager.MergedModInfo;
        bool highImpact = RainMeadowModInfoManager.GenerateModInfo(mod).SyncRequiredMods.Contains(mod.id)
            || mergedModInfo.SyncRequiredMods.Except(mergedModInfo.SyncRequiredModsOverride).Contains(mod.id);
        
        // Get all levels
        List<string> levelsPath = GetLevelsFromMod(mod, out var levelNames);
        List<ArenaInfo> unloadedArenaInfos = [];

        // Generate the unloaded arena info
        for (int i = 0; i < levelsPath.Count; i++)
        {
            unloadedArenaInfos.Add(new(levelNames[i], levelsPath[i], mod, highImpact));
        }

        return unloadedArenaInfos;
    }
    internal static List<ArenaInfo> ScanVanillaLevelDirectory()
    {
        // Get all levels
        List<string> levelsPath = GetLevelsFromMod(Custom.RootFolderDirectory(), out var levelNames);
        List<ArenaInfo> unloadedArenaInfos = [];

        // Generate the unloaded arena info
        for (int i = 0; i < levelsPath.Count; i++)
        {
            unloadedArenaInfos.Add(new(levelNames[i], levelsPath[i], null, false));
        }
        return unloadedArenaInfos;
    }
    internal static List<ArenaInfo> ScanImportedLevelDirectory()
    {
        return [];
        // // Get all levels
        // List<string> levelsPath = GetLevelsFromMod(Custom.RootFolderDirectory(), out var levelNames);
        // List<ArenaInfo> unloadedArenaInfos = [];

        // // Generate the unloaded arena info
        // for (int i = 0; i < levelsPath.Count; i++)
        // {
        //     unloadedArenaInfos.Add(new(levelNames[i], levelsPath[i], null, true));
        // }
        // return unloadedArenaInfos;

    }

    public readonly string levelName = name;
    public readonly string levelPath = path;

    public readonly ModManager.Mod? mod = mod;
    public readonly bool highImpact = highImpact;
    

    
    public readonly bool IsAvailableWithModDisabled => // either it's from a mod, all its dependencies are loaded and it's not high impact, or it's from the meadow import folder, or it's vanilla
        mod?.requirements.All(id => ModManager.ActiveMods.Exists(mod => mod.id == id)) is not false
        && (mod is null || !highImpact);
        
    public readonly bool IsAvailable => // the mod is enabled or it's available offline
        mod?.enabled is not false || IsAvailableWithModDisabled;
    public readonly bool IsFromSteamWorkshopMod => mod?.workshopMod is true;
    public readonly bool IsFromMeadowImport => mod is null && highImpact;
    public readonly bool IsFromLocalMod => mod?.workshopMod is false;
    public readonly bool IsFromVanilla => mod is null && !highImpact;

    public readonly string baldPath => levelPath.Substring(0, levelPath.Length - 4);
    public readonly string thumbPath => baldPath + "_Thumb.png";
    public readonly string firstCameraPath => baldPath + "_1.png";

    public override bool Equals(object obj)
    {
        return obj is ArenaInfo other && other.levelPath == levelPath;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
    
    public static bool operator== (ArenaInfo left, ArenaInfo right)
    {
        return left.Equals(right);
    }
    public static bool operator!= (ArenaInfo left, ArenaInfo right)
    {
        return !(left == right);
    }
    public static bool operator== (ArenaInfo? left, ArenaInfo? right)
    {
        return left is null ? right is null : right.HasValue && right.Value.Equals(left.Value);
    }
    public static bool operator!= (ArenaInfo? left, ArenaInfo? right)
    {
        return !(left == right);
    }
}
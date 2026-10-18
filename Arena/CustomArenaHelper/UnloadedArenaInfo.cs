using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RainMeadow.CustomArenaHelper;

public readonly struct UnloadedArenaInfo(string name, string path, ModManager.Mod? mod, bool highImpact = false)
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
    
    internal static List<UnloadedArenaInfo> ScanUnloadedMod(ModManager.Mod mod)
    {
        // Check if this mod is high impact
        var mergedModInfo = RainMeadowModInfoManager.MergedModInfo;
        bool highImpact = RainMeadowModInfoManager.GenerateModInfo(mod).SyncRequiredMods.Contains(mod.id)
            || mergedModInfo.SyncRequiredMods.Except(mergedModInfo.SyncRequiredModsOverride).Contains(mod.id);
        
        // Get all levels
        List<string> levelsPath = GetLevelsFromMod(mod, out var levelNames);
        List<UnloadedArenaInfo> unloadedArenaInfos = [];

        // Generate the unloaded arena info
        for (int i = 0; i < levelsPath.Count; i++)
        {
            unloadedArenaInfos.Add(new(levelNames[i], levelsPath[i], mod, highImpact));
        }

        return unloadedArenaInfos;
    }
    internal static List<UnloadedArenaInfo> ScanMeadowLevelDirectory()
    {
        return [];
    }

    public readonly string levelName = name;
    public readonly string levelPath = path;

    public readonly ModManager.Mod? mod = mod;
    public readonly bool highImpact = highImpact;

    public readonly bool IsAvailable => 
        mod?.enabled is not false ||
            (mod?.requirements.All(id => ModManager.ActiveMods.Exists(mod => mod.id == id)) is not false
            && !highImpact);
    public readonly bool IsFromSteamWorkshopMod => mod?.workshopMod is true;
    public readonly bool IsFromMeadowLevel => mod is null;
    public readonly bool IsFromLocalMod => mod?.workshopMod is false;
}
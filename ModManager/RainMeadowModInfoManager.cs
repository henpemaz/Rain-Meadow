using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace RainMeadow;

public static class RainMeadowModInfoManager
{
    private static string ModInfoFileName => "rainmeadow.json";

    /// <summary>
    /// All the loaded Rain Meadow modinfos merged together, so that all information can be accessed at once.
    /// </summary>
    public static RainMeadowModInfo MergedModInfo { get; private set; } = new();

    /// <summary>
    /// Dictionary of mod ID to Rain Meadow modinfo.
    /// </summary>
    public static Dictionary<string, RainMeadowModInfo> ModInfos { get; } = new();

    /// <summary>
    /// Additional rainmeadow.json modinfo file that can be defined in StreamingAssets, allows info to be loaded without needing to add it to any particular mod.
    /// </summary>
    public static RainMeadowModInfo? DebugModInfo { get; private set; }


    internal static void RefreshRainMeadowModInfos()
    {
        ModInfos.Clear();
        DebugModInfo = null;

        foreach (var mod in ModManager.ActiveMods)
        {
            var generatedModInfo = GetGeneratedModInfo(mod);

            var filePath = GetFileFromModOrNull(mod, ModInfoFileName);

            if (filePath is not null)
            {
                var loadedModInfo = GetLoadedModInfoOrNull(filePath);

                if (loadedModInfo is not null)
                {
                    generatedModInfo.MergeInfoFrom(loadedModInfo);
                }
            }

            ModInfos[mod.id] = generatedModInfo;
        }

        LoadDebugModInfo();

        RefreshMergedModInfo();
    }

    internal static void RefreshDebugModInfo()
    {
        DebugModInfo = null;

        LoadDebugModInfo();

        RefreshMergedModInfo();
    }

    private static void LoadDebugModInfo()
    {
        var filePath = Path.Combine(RWCustom.Custom.RootFolderDirectory(), ModInfoFileName);

        var modInfo = GetLoadedModInfoOrNull(filePath);

        if (modInfo is null)
        {
            return;
        }

        DebugModInfo = modInfo;
    }

    private static void RefreshMergedModInfo()
    {
        var mergedModInfo = new RainMeadowModInfo();

        var modInfos = ModInfos.Values.ToList();

        if (DebugModInfo is not null)
        {
            modInfos.Add(DebugModInfo);
        }

        foreach (var modInfo in modInfos)
        {
            mergedModInfo.MergeInfoFrom(modInfo);
        }

        MergedModInfo = mergedModInfo;
    }


    // Generates a new mod info class, automatically adding any mandatory tags (e.g. if it modifies regions, then it needs a mod info with itself added to the required_mods list)
    private static RainMeadowModInfo GetGeneratedModInfo(ModManager.Mod mod)
    {
        var modInfo = new RainMeadowModInfo();

        // TODO: update conditions sometime
        // if (mod.modifiesRegions)
        // {
        //     modInfo.SyncRequiredMods.Add(mod.id);
        // }

        return modInfo;
    }

    private static RainMeadowModInfo? GetLoadedModInfoOrNull(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var contents = File.ReadAllText(filePath);

            var rainMeadowModInfo = JsonConvert.DeserializeObject<RainMeadowModInfo>(contents);

            if (rainMeadowModInfo is null)
            {
                throw new Exception("Deserializer returned null.");
            }

            return rainMeadowModInfo;
        }
        catch (Exception e)
        {
            RainMeadow.Error($"Error loading Meadow mod info:\n{e}\n{e.StackTrace}");
        }

        return null;
    }

    // Returns the most appropriate file from a mod, given a default path (considering the newest and targeted file paths if they exist)
    private static string? GetFileFromModOrNull(ModManager.Mod mod, string filePath)
    {
        if (mod.hasTargetedVersionFolder)
        {
            var targetedPath = Path.Combine(mod.TargetedPath, filePath.ToLowerInvariant());

            if (File.Exists(targetedPath))
            {
                return targetedPath;
            }
        }

        if (mod.hasNewestFolder)
        {
            var newestPath = Path.Combine(mod.NewestPath, filePath.ToLowerInvariant());

            if (File.Exists(newestPath))
            {
                return newestPath;
            }
        }

        var defaultPath = Path.Combine(mod.path, filePath.ToLowerInvariant());

        if (File.Exists(defaultPath))
        {
            return defaultPath;
        }

        return null;
    }
}

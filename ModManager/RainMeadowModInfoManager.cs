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
    /// All the loaded Rain Meadow mod infos merged together, so that all information can be accessed at once.
    /// </summary>
    public static RainMeadowModInfo MergedModInfo { get; private set; } = new();

    /// <summary>
    /// Dictionary of mod id to rain meadow mod info
    /// </summary>
    public static Dictionary<string, RainMeadowModInfo> ModInfos { get; } = new();

    /// <summary>
    /// Additional Rain Meadow mod info file defined in StreamingAssets, allows users to add their own ids to banned_mods for instance.
    /// </summary>
    public static RainMeadowModInfo? UserModInfo { get; set; }


    internal static void RefreshRainMeadowModInfos()
    {
        MergedModInfo = new();
        ModInfos.Clear();
        UserModInfo = null;

        foreach (var mod in ModManager.ActiveMods)
        {
            var filePath = GetFileFromMod(mod, ModInfoFileName);

            if (filePath is null)
            {
                continue;
            }

            var modInfo = LoadModInfo(filePath);

            if (modInfo is null)
            {
                continue;
            }

            ModInfos[mod.id] = modInfo;
        }

        LoadUserModInfo();

        RefreshMergedModInfo();
    }

    private static void LoadUserModInfo()
    {
        var userModInfoFilePath = AssetManager.ResolveFilePath(ModInfoFileName);

        if (!File.Exists(userModInfoFilePath))
        {
            return;
        }

        var userModInfo = LoadModInfo(userModInfoFilePath);

        if (userModInfo is null)
        {
            return;
        }

        UserModInfo = userModInfo;
    }

    private static void RefreshMergedModInfo()
    {
        var mergedModInfo = new RainMeadowModInfo();

        var modInfos = ModInfos.Values.ToList();

        if (UserModInfo is not null)
        {
            modInfos.Add(UserModInfo);
        }

        foreach (var modInfo in modInfos)
        {
            modInfo.AddInfoTo(mergedModInfo);
        }

        MergedModInfo = mergedModInfo;
    }

    private static RainMeadowModInfo? LoadModInfo(string filePath)
    {
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
            RainMeadow.Debug($"Error loading Meadow mod info:\n{e}\n{e.StackTrace}");
        }

        return null;
    }

    private static string? GetFileFromMod(ModManager.Mod mod, string filePath)
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

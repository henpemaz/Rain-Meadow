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
            RainMeadow.Debug(mod.basePath);
            RainMeadow.Debug(mod.path);

            var filePath = Path.Combine(mod.basePath, ModInfoFileName);
            var fileName = Path.GetFileName(filePath);

            if (fileName != ModInfoFileName)
            {
                continue;
            }

            if (!IsNewestModFile(filePath))
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

    private static bool IsNewestModFile(string path)
    {
        var fullPath = Path.GetFullPath(path);

        foreach(var mod in ModManager.ActiveMods)
        {
            var targetedPath = Path.GetFullPath(mod.TargetedPath) + Path.DirectorySeparatorChar;
            var newestPath = Path.GetFullPath(mod.NewestPath) + Path.DirectorySeparatorChar;
            var defaultPath = Path.GetFullPath(mod.path) + Path.DirectorySeparatorChar;

            if (mod.hasTargetedVersionFolder && fullPath.StartsWith(targetedPath, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (mod.hasNewestFolder && fullPath.StartsWith(newestPath, StringComparison.InvariantCultureIgnoreCase))
            {
                var relativePath = fullPath.Substring(newestPath.Length);

                return !mod.hasTargetedVersionFolder || !File.Exists(Path.Combine(mod.TargetedPath, relativePath));
            }

            if (fullPath.StartsWith(defaultPath, StringComparison.InvariantCultureIgnoreCase))
            {
                var relativePath = fullPath.Substring(defaultPath.Length);

                return (!mod.hasTargetedVersionFolder || !File.Exists(Path.Combine(mod.TargetedPath, relativePath))) && (!mod.hasNewestFolder || !File.Exists(Path.Combine(mod.NewestPath, relativePath)));
            }
        }

        return true;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace RainMeadow;

public static class MeadowModInfoManager
{
    private static string MeadowModInfoFileName => "meadow.json";
    private static Dictionary<string, MeadowModInfo> MeadowModInfos { get; } = new();


    public static MeadowModInfo? GetMeadowModInfo(string modId)
    {
        if (TryGetMeadowModInfo(modId, out var meadowModInfo))
        {
            return meadowModInfo;
        }

        return null;
    }

    public static bool TryGetMeadowModInfo(string modId, out MeadowModInfo meadowModInfo)
    {
        return MeadowModInfos.TryGetValue(modId, out meadowModInfo);
    }

    public static Dictionary<string, MeadowModInfo> GetMeadowModInfoModIdMap()
    {
        return MeadowModInfos;
    }

    public static List<MeadowModInfo> GetMeadowModInfos()
    {
        return MeadowModInfos.Values.ToList();
    }


    internal static void RefreshMeadowModInfos()
    {
        MeadowModInfos.Clear();

        foreach (var mod in ModManager.ActiveMods)
        {
            RainMeadow.Debug(mod.basePath);
            RainMeadow.Debug(mod.path);

            var filePath = Path.Combine(mod.basePath, MeadowModInfoFileName);
            var fileName = Path.GetFileName(filePath);

            if (fileName != MeadowModInfoFileName)
            {
                continue;
            }

            if (!IsNewestModFile(filePath))
            {
                continue;
            }

            var meadowModInfo = LoadMeadowModInfo(filePath);

            if (meadowModInfo is null)
            {
                continue;
            }

            MeadowModInfos[mod.id] = meadowModInfo;
        }
    }

    private static MeadowModInfo? LoadMeadowModInfo(string filePath)
    {
        try
        {
            var contents = File.ReadAllText(filePath);

            var meadowModInfo = JsonConvert.DeserializeObject<MeadowModInfo>(contents);

            if (meadowModInfo is null)
            {
                throw new Exception("Deserializer returned null.");
            }

            return meadowModInfo;
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

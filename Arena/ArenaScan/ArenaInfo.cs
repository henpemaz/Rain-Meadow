using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RainMeadow.ArenaScan;

public partial class ArenaInfo(string name, string path, ModManager.Mod? mod, bool highImpact = false)
{
    public const char modSeparator = '\0'; // gonna need an untypable character for this
    public static bool TryGetLevelInfo(List<ArenaInfo> allLevelsInfo, string name, out ArenaInfo arenaInfo)
    {
        arenaInfo = allLevelsInfo.Find(x => x.levelName == name);
        return arenaInfo is not null;
    }
    public static bool TryGetLevelInfoFromID(List<ArenaInfo> allLevelsInfo, string levelID, out ArenaInfo arenaInfo)
    {
        arenaInfo = allLevelsInfo.Find(x => x.LevelID == levelID);
        return arenaInfo is not null;
    }
    public static bool TryGetLevelInfo(List<ArenaInfo> allLevelsInfo, string name, string modID, out ArenaInfo arenaInfo)
    {
        arenaInfo = allLevelsInfo.Find(x => x.levelName == name && x.ModID == modID);
        return arenaInfo is not null;
    }
    public static int LevelListSortNumber(List<string> allModsIDOrdered, ArenaInfo level)
    {
        return allModsIDOrdered.FindIndex(x => x == level.ModID);
    }
    public static string MakeLevelID(string levelName, string modID)
        => levelName + modSeparator + modID;
    public static (string levelName, string modID) SplitLevelID(string levelID)
    {
        string[] ID = levelID.Split(modSeparator);
        if (ID.Length > 1) return (ID[0], ID[1]);
        return (ID[0], "");
    }
    public static ArenaInfo MakeArenaInfoOfUnknownMod(string levelID)
    {
        var info = SplitLevelID(levelID);
        RainMeadow.Debug($"Created unknown level from {levelID} !");
        return new(info.levelName, "", null, true){_unknownLevel = true, _unknownModID = info.modID};
    }

    public static ArenaInfo GetOrCreateArenaInfo(List<ArenaInfo> allLevelsInfo, string nameOrID)
    {
        // if it's a level name
        if (string.IsNullOrEmpty(SplitLevelID(nameOrID).modID) 
            && TryGetLevelInfo(allLevelsInfo, nameOrID, out var arenaInfo))
        {
            return arenaInfo;
        }
        // if it's a level ID
        else if (TryGetLevelInfoFromID(allLevelsInfo, nameOrID, out arenaInfo))
        {
            return arenaInfo;
        }
        // if nothing can be done
        return MakeArenaInfoOfUnknownMod(nameOrID);
    }
    
    public readonly string levelName = name;
    public readonly string levelPath = path;

    public readonly ModManager.Mod? mod = mod;
    private readonly bool highImpact = highImpact;
    private bool _unknownLevel = false; // bool indicator of invalid level
    private string _unknownModID = "";

    public ArenaSettings? LevelSettings {get; internal set;} = null;
    public readonly List<ArenaInfo> Overriding = [];

    
    public int LevelListSortNumber(List<string> allModsIDOrdered)
        => LevelListSortNumber(allModsIDOrdered, this);
    public bool TryFindFileFromFileName(string fileName, out string actualFile)
    {
        // check the overides first
        foreach (var ov in Overriding)
        {
            if (TryFindFileFromFileName(fileName, out actualFile)) return true;
        }

        // we should have smt like levelname[\0]modid_smtsmt.extension in filename
        string[] levelID = fileName.Split(modSeparator); // levelname/modid_smtsmt.extension
        if (levelID.Length > 1) // used levelID instead of levelName
        {
            if (levelID[0].ToLowerInvariant().StartsWith(levelName.ToLowerInvariant()) // level name check
                && levelID[1].StartsWith(ModID)) //modID check
            {
                string levelExtension = levelID[1].Substring(ModID.Length);
                string finalPath = (BaldPath + levelExtension).ToLowerInvariant();
                if (File.Exists(finalPath))
                {
                    actualFile = finalPath;
                    return true;
                }
            }
        }
        else // used levelName
        {
            if (levelID[0].ToLowerInvariant().StartsWith(levelName.ToLowerInvariant()))
            {
                string levelExtension = fileName.Substring(levelName.Length);
                string finalPath = (BaldPath + levelExtension).ToLowerInvariant();
                if (File.Exists(finalPath))
                {
                    actualFile = finalPath;
                    return true;
                }
            }
        }
        actualFile = fileName;
        return false;
    }

    public bool AllDependanciesLoaded 
        => LevelSettings?.DependanciesOverride is not null
            ? LevelSettings.DependanciesOverride.All(id => ModManager.ActiveMods.Exists(mod => mod.id == id))
            : mod?.requirements.All(id => ModManager.ActiveMods.Exists(mod => mod.id == id)) is not false;
    public bool HighImpact => LevelSettings?.HighImpact is not null ? (bool)LevelSettings.HighImpact : highImpact;
    public bool IsModDisabled => mod?.enabled is false;
    public bool IsOverriding => LevelSettings?.IsOverride is not null 
        ? (bool)LevelSettings.IsOverride 
        : (IsFromMSC || IsFromWatcher);


    public bool IsAvailableWithModDisabled => 
        !IsUnknownLevel && AllDependanciesLoaded && (mod is null || !HighImpact);
    public bool IsAvailable => 
        !IsUnknownLevel && (!IsModDisabled || IsAvailableWithModDisabled);

    public bool IsUnknownLevel => _unknownLevel || string.IsNullOrEmpty(levelPath);
    public bool IsFromSteamWorkshopMod => mod?.workshopMod is true;
    public bool IsFromMeadowImport => mod is null && HighImpact && !IsUnknownLevel;
    public bool IsFromLocalMod => mod?.workshopMod is false;
    public bool IsFromVanilla => mod is null && !HighImpact && !IsUnknownLevel;
    public bool IsFromHighImpactMod => !IsFromMeadowImport && !IsUnknownLevel && HighImpact;
    public bool IsFromMSC => ModID == MoreSlugcats.MoreSlugcats.MOD_ID;
    public bool IsFromWatcher => ModID == Watcher.Watcher.MOD_ID;

    public string BaldPath => IsUnknownLevel ? "" : levelPath.Substring(0, levelPath.Length - 4);
    public string ThumbPath => BaldPath + "_Thumb.png";
    public string FirstCameraPath => BaldPath + "_1.png";

    public string ModID => mod is not null 
        ? mod.id
        : IsFromVanilla 
            ? vanillaID
            : IsFromMeadowImport
                ? importedID
                : unknownID;
    public string ModName => mod is not null 
        ? mod.name
        : IsFromVanilla 
            ? "Vanilla"
            : IsFromMeadowImport
                ? "Imported"
                : string.IsNullOrEmpty(_unknownModID) ? "Unknown" : "(?)" + _unknownModID;
    public string LevelID => MakeLevelID(levelName, ModID);

    public override int GetHashCode() => base.GetHashCode();
    public override bool Equals(object obj)
    {
        return obj is ArenaInfo other 
            && other.levelPath == levelPath 
            && !other.IsUnknownLevel 
            && !IsUnknownLevel;
    }
    public static bool operator== (ArenaInfo? left, ArenaInfo? right)
    {
        return left is null ? right is null : right?.Equals(left) is true;
    }
    public static bool operator!= (ArenaInfo? left, ArenaInfo? right)
    {
        return !(left == right);
    }
}
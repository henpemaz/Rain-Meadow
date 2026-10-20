using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RWCustom;
using UnityEngine;

namespace RainMeadow.ArenaScan;

public partial class ArenaInfo(string name, string path, ModManager.Mod? mod, bool highImpact = false)
{
    public readonly string levelName = name;
    public readonly string levelPath = path;

    public readonly ModManager.Mod? mod = mod;
    public readonly bool highImpact = highImpact;
    

    public bool IsAvailableWithModDisabled => // either it's from a mod, all its dependencies are loaded and it's not high impact, or it's from the meadow import folder, or it's vanilla
        mod?.requirements.All(id => ModManager.ActiveMods.Exists(mod => mod.id == id)) is not false
        && (mod is null || !highImpact);
        
    public bool IsAvailable => // the mod is enabled or it's available offline
        mod?.enabled is not false || IsAvailableWithModDisabled;
    public bool IsFromSteamWorkshopMod => mod?.workshopMod is true;
    public bool IsFromMeadowImport => mod is null && highImpact;
    public bool IsFromLocalMod => mod?.workshopMod is false;
    public bool IsFromVanilla => mod is null && !highImpact;

    public string BaldPath => levelPath.Substring(0, levelPath.Length - 4);
    public string ThumbPath => BaldPath + "_Thumb.png";
    public string FirstCameraPath => BaldPath + "_1.png";

    public string ModID => mod is not null 
        ? mod.id
        : IsFromVanilla 
            ? vanillaID
            : importedID;
    public string ModName => mod is not null 
        ? mod.name
        : IsFromVanilla 
            ? "Vanilla"
            : "Imported";

    public override bool Equals(object obj)
    {
        return obj is ArenaInfo other && other.levelPath == levelPath;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
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
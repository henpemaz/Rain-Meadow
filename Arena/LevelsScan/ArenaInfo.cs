using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RWCustom;
using UnityEngine;

namespace RainMeadow.LevelsScan;

public readonly partial struct ArenaInfo(string name, string path, ModManager.Mod? mod, bool highImpact = false)
{
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

    public readonly string modName => mod is not null 
        ? mod.id
        : IsFromVanilla 
            ? vanillaID
            : importedID;

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
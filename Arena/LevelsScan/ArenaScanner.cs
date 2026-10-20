using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonoMod.Utils;
using RWCustom;
using Steamworks;
using UnityEngine;

namespace RainMeadow.LevelsScan;

public static partial class ArenaScanner
{
    public static Dictionary<string, List<ArenaInfo>> scannedArenas = [];
    public const string vanillaID = "_vanilla";
    public const string importedID = "_imported";
    public static string[] allLevelsName 
        => availableArenas.Select(x => x.levelName).ToArray();
    public static string[] allLevelsPath 
        => availableArenas.Select(x => x.levelPath).ToArray();
    public static List<ArenaInfo> availableArenas 
        => scannedArenas.SelectMany(kp => kp.Value.Where(x => x.IsAvailable)).ToList();
    public static List<ArenaInfo> availableArenasFromDisabledMods
        => scannedArenas.SelectMany(kp => kp.Value.Where(x => x.IsAvailable && (x.IsFromMeadowImport || x.mod?.enabled is false))).ToList();
    private static RainWorld rainWorld = null!;
    
    public static bool AreAllArenasAvailable(List<string> playList)
    {
        string[] levels = allLevelsName;
        return playList.All(x => levels.Contains(x));
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RWCustom;
using UnityEngine;

namespace RainMeadow.ArenaScan;

public class ArenaSettings
{
    public const string ArenaFileSuffix = "_Arena.txt";
    public const string ItemSplit = " - ";
    public const char ModSplit = ';';
    public const string MeadowAttributeID = "Meadow";
    public const string DisplayNameID = "DisplayName";
    public const string DependanciesOverrideID = "Dependancies";
    public const string HighImpactOverrideID = "HighImpact";
    public readonly static Dictionary<string, ArenaSettings> settingsByID = [];
    public static void RegisterSettingFromArenaInfo(ArenaInfo arenaInfo)
    {
        string arenaFilePath = arenaInfo.BaldPath + ArenaFileSuffix;
        if (File.Exists(arenaFilePath))
        {
            try
            {
                string[] lines = File.ReadAllLines(arenaFilePath);
                ArenaSettings settings = new();
                bool doSaveSettings = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Length > 2 && lines[i].Substring(0, 2) != "//")
                    {
                        string[] items = Regex.Split(lines[i], ItemSplit);
                        if (items.Length > 2 && items[0] == MeadowAttributeID)
                        {
                            doSaveSettings = true;
                            if (items[1] == DisplayNameID)
                            {
                                settings.DisplayName = items[2];
                            }
                            else if (items[1] == DependanciesOverrideID)
                            {
                                settings.DependanciesOverride = items[2].Split(ModSplit);
                            }
                            else if (items[1] == HighImpactOverrideID)
                            {
                                settings.HighImpact = !items[2].ToLowerInvariant().Contains("false");
                            }
                            else
                            {
                                settings.UnrecognizedAttributes[items[1]] = string.Join(ItemSplit, items.Skip(2));
                            }
                        }
                    }
                }

                if (doSaveSettings)
                {
                    RainMeadow.Debug($"Found and added {arenaInfo.LevelID} meadow settings !");
                    settingsByID[arenaInfo.LevelID] = settings;
                    arenaInfo.LevelSettings = settings;
                }
            }
            catch (Exception ex)
            {
                RainMeadow.Error($"Error while reading {arenaInfo?.LevelID} meadow settings :" + ex);
            }
        }
    }

    private ArenaSettings() {}
    
    public string? DisplayName {get; private set;} = null;
    public bool? HighImpact {get; private set;} = null;
    public string[]? DependanciesOverride {get; private set;} = null;
    public Dictionary<string,string> UnrecognizedAttributes {get; private set;} = [];
}
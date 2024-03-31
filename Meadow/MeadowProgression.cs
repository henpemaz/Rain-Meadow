using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    // Progression is the content unlock system
    // Characters, skins, emotes are listed here
    // Saving loading what's unlocked is handled here
    public static class MeadowProgression
    {

        internal static void InitializeBuiltinTypes()
        {
            // todo load progression
            try
            {
                _ = Character.Slugcat;
                _ = Skin.Slugcat_Survivor;
                currentTestSkin = Skin.Slugcat_Survivor;

                RainMeadow.Debug($"characters loaded: {Character.values.Count}");
                RainMeadow.Debug($"skins loaded: {Skin.values.Count}");
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        public static Dictionary<Character, CharacterData> characterData = new();

        public class CharacterData
        {
            public string displayName;
            public string emotePrefix;
            public string emoteAtlas;
            public Color emoteColor;
            public List<Skin> skins = new();
        }

        public class Character : ExtEnum<Character>
        {
            public Character(string value, bool register = false, CharacterData characterDataEntry = null) : base(value, register)
            {
                if (register)
                {
                    characterData[this] = characterDataEntry;
                }
            }

            public static Character Slugcat = new("Slugcat", true, new()
            {
                displayName = "SLUGCAT",
                emotePrefix = "sc_",
                emoteAtlas = "emotes_slugcat",
                emoteColor = new Color(85f, 120f, 120f, 255f) / 255f,
            });
            public static Character Cicada = new("Cicada", true, new()
            {
                displayName = "CICADA",
                emotePrefix = "squid_",
                emoteAtlas = "emotes_squid",
                emoteColor = new Color(81f, 81f, 81f, 255f) / 255f,
            });
            public static Character Lizard = new("Lizard", true, new()
            {
                displayName = "LIZARD",
                emotePrefix = "liz_",
                emoteAtlas = "emotes_lizard",
                emoteColor = new Color(197, 220, 232, 255f) / 255f,
            });
            public static Character Scavenger = new("Scavenger", true, new()
            {
                displayName = "SCAVENGER",
                emotePrefix = "sc_", // "scav_"
                emoteAtlas = "emotes_slugcat",//"emotes_scav",
                emoteColor = new Color(232, 187, 200, 255f) / 255f,
            });
            public static Character Noodlefly = new("Noodlefly", true, new()
            {
                displayName = "NOODLEFLY",
                emotePrefix = "sc_", // "noot_"
                emoteAtlas = "emotes_slugcat",//"emotes_noot",
                emoteColor = new Color(232, 187, 200, 255f) / 255f, // todo
            });
            public static Character Eggbug = new("Eggbug", true, new()
            {
                displayName = "EGGBUG",
                emotePrefix = "sc_", // "noot_"
                emoteAtlas = "emotes_slugcat",//"emotes_noot",
                emoteColor = new Color(232, 187, 200, 255f) / 255f, // todo
            });
        }

        public static Dictionary<Skin, SkinData> skinData = new();
        internal static Skin currentTestSkin;

        public class SkinData
        {
            public Character character;
            public string displayName;
            public CreatureTemplate.Type creatureType;
            public SlugcatStats.Name statsName; // curently only used for color
            public int randomSeed;
            public Color? baseColor;
            public Color? eyeColor;
            public Color? effectColor;
            public float tintFactor = 0.3f;
            public string emoteAtlasOverride;
            public string emotePrefixOverride;
            public Color? emoteColorOverride;
        }

        public class Skin : ExtEnum<Skin>
        {
            public Skin(string value, bool register = false, SkinData skinDataEntry = null) : base(value, register)
            {
                if (register)
                {
                    skinData[this] = skinDataEntry;
                    characterData[skinDataEntry.character].skins.Add(this);
                }
            }

            public static Skin Slugcat_Survivor = new("Slugcat_Survivor", true, new()
            {
                character = Character.Slugcat,
                displayName = "Survivor",
                creatureType = CreatureTemplate.Type.Slugcat,
                statsName = SlugcatStats.Name.White,
            });
            public static Skin Slugcat_Monk = new("Slugcat_Monk", true, new()
            {
                character = Character.Slugcat,
                displayName = "Monk",
                creatureType = CreatureTemplate.Type.Slugcat,
                statsName = SlugcatStats.Name.Yellow,
            });
            public static Skin Slugcat_Hunter = new("Slugcat_Hunter", true, new()
            {
                character = Character.Slugcat,
                displayName = "Hunter",
                creatureType = CreatureTemplate.Type.Slugcat,
                statsName = SlugcatStats.Name.Red,
            });
            public static Skin Slugcat_Fluffy = new("Slugcat_Fluffy", true, new()
            {
                character = Character.Slugcat,
                displayName = "Fluffy",
                creatureType = CreatureTemplate.Type.Slugcat,
                statsName = SlugcatStats.Name.White,
                baseColor = new Color(111, 216, 255, 255) / 255f
            });

            public static Skin Cicada_White = new("Cicada_White", true, new()
            {
                character = Character.Cicada,
                displayName = "White",
                creatureType = CreatureTemplate.Type.CicadaA,
            });
            public static Skin Cicada_Dark = new("Cicada_Dark", true, new()
            {
                character = Character.Cicada,
                displayName = "Dark",
                creatureType = CreatureTemplate.Type.CicadaB,
            });

            public static Skin Lizard_Pink = new("Lizard_Pink", true, new()
            {
                character = Character.Lizard,
                displayName = "Pink",
                creatureType = CreatureTemplate.Type.PinkLizard,
                tintFactor = 0.5f,
            });
            public static Skin Lizard_Blue = new("Lizard_Blue", true, new()
            {
                character = Character.Lizard,
                displayName = "Blue",
                creatureType = CreatureTemplate.Type.BlueLizard,
                tintFactor = 0.5f,
            });
            public static Skin Lizard_Yellow = new("Lizard_Yellow", true, new()
            {
                character = Character.Lizard,
                displayName = "Yellow",
                creatureType = CreatureTemplate.Type.YellowLizard,
                randomSeed = 1366,
                tintFactor = 0.5f,
            });
            public static Skin Lizard_Cyan = new("Lizard_Cyan", true, new()
            {
                character = Character.Lizard,
                displayName = "Cyan",
                creatureType = CreatureTemplate.Type.CyanLizard,
                randomSeed = 1366,
                tintFactor = 0.5f,
            });

            public static Skin Scavenger_Twigs = new("Scavenger_Twigs", true, new()
            {
                character = Character.Scavenger,
                displayName = "Twigs",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 4481,
            });
            public static Skin Scavenger_Acorn = new("Scavenger_Acorn", true, new()
            {
                character = Character.Scavenger,
                displayName = "Acorn",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 1213,
            });
            public static Skin Scavenger_Oak = new("Scavenger_Oak", true, new()
            {
                character = Character.Scavenger,
                displayName = "Oak",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 9503,
            });
            public static Skin Scavenger_Shrub = new("Scavenger_Shrub", true, new()
            {
                character = Character.Scavenger,
                displayName = "Shrub",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 1139,
            });
            public static Skin Scavenger_Branches = new("Scavenger_Branches", true, new()
            {
                character = Character.Scavenger,
                displayName = "Branches",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 1503,
            });
            public static Skin Scavenger_Sage = new("Scavenger_Sage", true, new()
            {
                character = Character.Scavenger,
                displayName = "Sage",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 1184,
            });
            public static Skin Scavenger_Cherry = new("Scavenger_Cherry", true, new()
            {
                character = Character.Scavenger,
                displayName = "Cherry",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 9464,
            });
            public static Skin Scavenger_Lavender = new("Scavenger_Lavender", true, new()
            {
                character = Character.Scavenger,
                displayName = "Lavender",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 8201,
            });
            public static Skin Scavenger_Peppermint = new("Scavenger_Peppermint", true, new()
            {
                character = Character.Scavenger,
                displayName = "Peppermint",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 8750,
            });
            public static Skin Scavenger_Juniper = new("Scavenger_Juniper", true, new()
            {
                character = Character.Scavenger,
                displayName = "Juniper",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 4566,
            });

            public static Skin Noodlefly_Big = new("Noodlefly_Big", true, new()
            {
                character = Character.Noodlefly,
                displayName = "Big",
                creatureType = CreatureTemplate.Type.BigNeedleWorm,
            });
            public static Skin Noodlefly_Small = new("Noodlefly_Small", true, new()
            {
                character = Character.Noodlefly,
                displayName = "Small",
                creatureType = CreatureTemplate.Type.SmallNeedleWorm,
            });

            public static Skin Eggbug_Blue = new("Eggbug_Blue", true, new()
            {
                character = Character.Eggbug,
                displayName = "Blue",
                creatureType = CreatureTemplate.Type.EggBug,
                randomSeed = 1001,
            });
            public static Skin Eggbug_Teal = new("Eggbug_Teal", true, new()
            {
                character = Character.Eggbug,
                displayName = "Teal",
                creatureType = CreatureTemplate.Type.EggBug,
                randomSeed = 1002,
            });
        }

        public static bool SkinAvailable(Skin skin)
        {
            return true; // todo progression system
        }

        public static List<Skin> AllAvailableSkins(Character character)
        {
            return characterData[character].skins.Where(SkinAvailable).ToList();
        }

        public static List<Character> AllAvailableCharacters()
        {
            return characterData.Keys.ToList();
        }
    }
}

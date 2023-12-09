using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static RainMeadow.MeadowProgression;

namespace RainMeadow
{
    // Progression is the content unlock system
    // Characters, skins, emotes are listed here
    // Saving loading what's unlocked is handled here
    public static class MeadowProgression
    {

        internal static void InitializeBuiltinTypes()
        {
            // todo load
            try
            {
                _ = Character.Slugcat;
                _ = Skin.Slugcat_Survivor;
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
                if(register)
                {
                    characterData[this] = characterDataEntry;
                }
            }

            public static Character Slugcat = new("Slugcat", true, new() {
                displayName = "SLUGCAT",
                emotePrefix = "sc_",
                emoteAtlas = "emotes_slugcat",
                emoteColor = new Color(85f, 120f, 120f, 255f) / 255f,
            });
            public static Character Cicada = new("Cicada", true, new() { 
                displayName = "CICADA", 
                emotePrefix = "sc_", //"cada_",
                emoteAtlas = "emotes_slugcat", //"emotes_cicada",
                emoteColor = new Color(120f, 80f, 120f, 255f) / 255f,
            });
            public static Character Lizard = new("Lizard", true, new() { 
                displayName = "LIZARD", 
                emotePrefix = "liz_",
                emoteAtlas = "emotes_lizard",
                emoteColor = new Color(197, 220, 232, 255f) / 255f,
            });
        }

        public static Dictionary<Skin, SkinData> skinData = new();

        public class SkinData
        {
            public Character character;
            public string displayName;
            public CreatureTemplate.Type creatureType;
            public SlugcatStats.Name statsName; // curently only used for color
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

            public static Skin Slugcat_Survivor = new("Slugcat_Survivor", true, new() {
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
                baseColor = new Color(111, 216, 255, 255)/255f
            });

            public static Skin Cicada_White = new("Cicada_White", true, new()
            {
                character = Character.Cicada,
                displayName = "White",
                creatureType = CreatureTemplate.Type.CicadaA,
            });
            public static Skin Cicada_Dark = new("Cicada_Dark", true,  new()
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
            return Character.values.entries.Select(s => new Character(s)).ToList();
        }
    }
}

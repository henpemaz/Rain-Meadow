using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow
{
    // Progression is the content unlock system
    // Characters, skins, emotes are listed here
    // Saving loading what's unlocked is handled here
    public static class SkinSelection
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
            public float tintFactor = 0.3f;
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

            public static Skin Slugcat_Hunter = new("Slugcat_Hunter", true, new()
            {
                character = Character.Slugcat,
                displayName = "Hunter",
                creatureType = CreatureTemplate.Type.Slugcat,
                statsName = SlugcatStats.Name.Red,
            });
        }


        public static List<Skin> AllAvailableSkins(Character character)
        {
            return characterData[character].skins.ToList();
        }

        public static List<SlugcatStats.Name> AllAvailableCharacters()
        {
            return SlugcatStats.Name.values.entries.Select(s => new SlugcatStats.Name(s)).ToList();
        }
    }
}

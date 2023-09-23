using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMeadow
{
    public class MeadowProgression
    {
        public static Dictionary<Character, List<Skin>> characterSkins = new();
        public static MeadowProgression instance = new MeadowProgression();
        public MeadowProgression()
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

        // do I really need this?
        public class Character : ExtEnum<Character>
        {
            public Character(string value, bool register = false, SlugcatStats.Name statsName = null, string displayName = null) : base(value, register)
            {
                if(register)
                {
                    characterStats[this] = statsName;
                    characterNames[this] = displayName;
                    characterSkins[this] = new();
                }
            }

            public static Dictionary<Character, SlugcatStats.Name> characterStats = new();
            public static Dictionary<Character, string> characterNames = new();

            public static Character Slugcat = new("Slugcat", true, RainMeadow.Ext_SlugcatStatsName.MeadowSlugcat, "SLUGCAT");
            public static Character Cicada = new("Cicada", true, RainMeadow.Ext_SlugcatStatsName.MeadowSquidcicada, "CICADA");
            public static Character Lizard = new("Lizard", true, RainMeadow.Ext_SlugcatStatsName.MeadowLizard, "LIZARD");
        }

        public class Skin : ExtEnum<Skin>
        {
            public static Dictionary<Skin, string> skinNames = new();
            public Skin(string value, bool register = false, Character character = null, string skinName = null, bool isDefaul = false) : base(value, register)
            {
                if (register)
                {
                    characterSkins[character].Add(this);
                    skinNames[this] = skinName;
                }
            }
            public static Skin Slugcat_Survivor = new("Slugcat_Survivor", true, Character.Slugcat, "Survivor");
            public static Skin Slugcat_Monk = new("Slugcat_Monk", true, Character.Slugcat, "Monk");
            public static Skin Slugcat_Hunter = new("Slugcat_Hunter", true, Character.Slugcat, "Hunter");
            public static Skin Slugcat_Fluffy = new("Slugcat_Fluffy", true, Character.Slugcat, "Fluffy");

            public static Skin Cicada_White = new("Cicada_White", true, Character.Cicada, "White");
            public static Skin Cicada_Dark = new("Cicada_Dark", true, Character.Cicada, "Dark");

            public static Skin Lizard_Pink = new("Lizard_Pink", true, Character.Lizard, "Pink");
        }

        public bool SkinAvailable(Skin skin)
        {
            return true;
        }

        public List<Skin> AllAvailableSkins(Character character)
        {
            return characterSkins[character].Where(SkinAvailable).ToList();
        }

        public List<Character> AllAvailableCharacters()
        {
            return Character.values.entries.Select(s => new Character(s)).ToList();
        }
    }
}

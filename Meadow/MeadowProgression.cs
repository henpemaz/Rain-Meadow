using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RWCustom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
            try
            {
                _ = Character.Slugcat;
                _ = Skin.Slugcat_Survivor;
                _ = Emote.emoteHello;
                currentTestSkin = Skin.Lizard_Pink;

                RainMeadow.Debug($"characters loaded: {Character.values.Count}");
                RainMeadow.Debug($"skins loaded: {Skin.values.Count}");
                RainMeadow.Debug($"emotes loaded: {Emote.values.Count}");
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        public static List<Character> allCharacters = new();

        public static Dictionary<Character, CharacterData> characterData = new();

        public class CharacterData
        {
            public string displayName; // in select screen
            public string emotePrefix; // for emote sprites
            public string emoteAtlas; // atlas name
            public Color emoteColor; // emote tile color (pre-tint)
            public SoundID voiceId;
            public List<Skin> skins = new();
            public int[] selectSpriteIndexes; // sprites to darken on character locked in select screen
            public WorldCoordinate startingCoords; // first spawn pos
        }

        [TypeConverter(typeof(ExtEnumTypeConverter<Character>))]
        public class Character : ExtEnum<Character>
        {
            public Character(string value, bool register = false) : base(value, register) { }
            public Character(string value, bool register, CharacterData characterDataEntry) : base(value, register)
            {
                if (register)
                {
                    characterData[this] = characterDataEntry;
                    allCharacters.Add(this);
                }
            }

            public static Character Slugcat = new("Slugcat", true, new()
            {
                displayName = "SLUGCAT",
                emotePrefix = "sc_",
                emoteAtlas = "emotes_slugcat",
                emoteColor = new Color(85f, 120f, 120f, 255f) / 255f,
                voiceId = RainMeadow.Ext_SoundID.RM_Slugcat_Call,
                selectSpriteIndexes = new[] { 2 },
                startingCoords = new WorldCoordinate("SU_C04", 7, 28, -1),
            });
            public static Character Lizard = new("Lizard", true, new()
            {
                displayName = "LIZARD",
                emotePrefix = "liz_",
                emoteAtlas = "emotes_lizard",
                emoteColor = new Color(197, 220, 232, 255f) / 255f,
                voiceId = RainMeadow.Ext_SoundID.RM_Lizard_Call,
                selectSpriteIndexes = new[] { 1, 2 },
                startingCoords = new WorldCoordinate("DS_A06", 12, 16, -1),
            });
            public static Character Cicada = new("Cicada", true, new()
            {
                displayName = "CICADA",
                emotePrefix = "squid_",
                emoteAtlas = "emotes_squid",
                emoteColor = new Color(81f, 81f, 81f, 255f) / 255f,
                voiceId = RainMeadow.Ext_SoundID.RM_Cicada_Call,
                selectSpriteIndexes = new[] { 2 },
                startingCoords = new WorldCoordinate("SI_D05", 32, 18, -1),
            });
            public static Character Scavenger = new("Scavenger", true, new()
            {
                displayName = "SCAVENGER",
                emotePrefix = "scav_",
                emoteAtlas = "emotes_scav",
                emoteColor = new Color(80, 87, 80, 255) / 255f,
                voiceId = RainMeadow.Ext_SoundID.RM_Scav_Call,
                selectSpriteIndexes = new[] { 1 },
                startingCoords = new WorldCoordinate("GW_A11", 26, 22, -1),
            });
            public static Character Noodlefly = new("Noodlefly", true, new()
            {
                displayName = "NOODLEFLY",
                emotePrefix = "noot_",
                emoteAtlas = "emotes_noot",
                emoteColor = Extensions.ColorFromHex(0x7e6f6a),
                voiceId = RainMeadow.Ext_SoundID.RM_Noot_Call,
                selectSpriteIndexes = new int[0],
                startingCoords = new WorldCoordinate("LF_F02", 63, 43, -1),
            });
            public static Character Eggbug = new("Eggbug", true, new()
            {
                displayName = "EGGBUG",
                emotePrefix = "sc_", // "bug_"
                emoteAtlas = "emotes_slugcat",//"emotes_bug",
                emoteColor = new Color(232, 187, 200, 255f) / 255f, // todo
                voiceId = RainMeadow.Ext_SoundID.RM_Eggbug_Call,
                selectSpriteIndexes = new[] { 2 },
                startingCoords = new WorldCoordinate("HI_B04", 32, 18, -1),
            });
            public static Character LanternMouse = new("LanternMouse", true, new()
            {
                displayName = "LANTERN MOUSE",
                emotePrefix = "maus_",
                emoteAtlas = "emotes_maus",
                emoteColor = Extensions.ColorFromHex(0x79635f),
                voiceId = RainMeadow.Ext_SoundID.RM_Mouse_Call,
                selectSpriteIndexes = new int[0],
                startingCoords = new WorldCoordinate("SH_A21", 32, 26, -1),
            });
        }

        public static Dictionary<Skin, SkinData> skinData = new();
        internal static Skin currentTestSkin;

        public class SkinData
        {
            public Character character;
            public string displayName;
            public CreatureTemplate.Type creatureType;
            public int randomSeed;
            public Color? baseColor;
            public Color? eyeColor;
            public Color? previewColor;
            public float tintFactor = 0.3f;
            public string emoteAtlasOverride;
            public string emotePrefixOverride;
            public Color? emoteColorOverride;
            public SoundID? voiceIdOverride;
        }

        [TypeConverter(typeof(ExtEnumTypeConverter<Skin>))]
        public class Skin : ExtEnum<Skin>
        {
            public Skin(string value, bool register = false) : base(value, register) { }
            public Skin(string value, bool register, SkinData skinDataEntry) : base(value, register)
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
                baseColor = PlayerGraphics.SlugcatColor(SlugcatStats.Name.White),
            });
            public static Skin Slugcat_Monk = new("Slugcat_Monk", true, new()
            {
                character = Character.Slugcat,
                displayName = "Monk",
                creatureType = CreatureTemplate.Type.Slugcat,
                baseColor = PlayerGraphics.SlugcatColor(SlugcatStats.Name.Yellow),
            });
            public static Skin Slugcat_Hunter = new("Slugcat_Hunter", true, new()
            {
                character = Character.Slugcat,
                displayName = "Hunter",
                creatureType = CreatureTemplate.Type.Slugcat,
                baseColor = PlayerGraphics.SlugcatColor(SlugcatStats.Name.Red),
            });
            public static Skin Slugcat_Fluffy = new("Slugcat_Fluffy", true, new()
            {
                character = Character.Slugcat,
                displayName = "Fluffy",
                creatureType = CreatureTemplate.Type.Slugcat,
                baseColor = new Color(111, 216, 255, 255) / 255f
            });

            public static Skin Cicada_White = new("Cicada_White", true, new()
            {
                character = Character.Cicada,
                displayName = "White",
                creatureType = CreatureTemplate.Type.CicadaA,
                previewColor = Extensions.ColorFromHex(0xd4dddf),
            });
            public static Skin Cicada_Dark = new("Cicada_Dark", true, new()
            {
                character = Character.Cicada,
                displayName = "Dark",
                creatureType = CreatureTemplate.Type.CicadaB,
                previewColor = Extensions.ColorFromHex(0x0e0d19),
            });

            public static Skin Lizard_Pink = new("Lizard_Pink", true, new()
            {
                character = Character.Lizard,
                displayName = "Pink",
                creatureType = CreatureTemplate.Type.PinkLizard,
                randomSeed = 3401,
                previewColor = Extensions.ColorFromHex(0xff07ee),
                tintFactor = 0.5f,
            });
            public static Skin Lizard_Blue = new("Lizard_Blue", true, new()
            {
                character = Character.Lizard,
                displayName = "Blue",
                creatureType = CreatureTemplate.Type.BlueLizard,
                randomSeed = 9659,
                previewColor = Extensions.ColorFromHex(0x006bed),
                tintFactor = 0.5f,
            });
            public static Skin Lizard_Yellow = new("Lizard_Yellow", true, new()
            {
                character = Character.Lizard,
                displayName = "Yellow",
                creatureType = CreatureTemplate.Type.YellowLizard,
                randomSeed = 6526,
                previewColor = Extensions.ColorFromHex(0xd35503),
                tintFactor = 0.5f,
            });
            public static Skin Lizard_Cyan = new("Lizard_Cyan", true, new()
            {
                character = Character.Lizard,
                displayName = "Cyan",
                creatureType = CreatureTemplate.Type.CyanLizard,
                randomSeed = 5243,
                previewColor = Extensions.ColorFromHex(0x06f5ff),
                tintFactor = 0.5f,
            });
            public static Skin Lizard_Fluffers = new("Lizard_Fluffers", true, new()
            {
                character = Character.Lizard,
                displayName = "Fluffers",
                creatureType = CreatureTemplate.Type.PinkLizard,
                randomSeed = 7713,
                previewColor = Extensions.ColorFromHex(0xff15b3),
                tintFactor = 0.5f,
            });

            public static Skin Scavenger_Twigs = new("Scavenger_Twigs", true, new()
            {
                character = Character.Scavenger,
                displayName = "Twigs",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 4481,
                previewColor = Extensions.ColorFromHex(0x212030),
            });
            public static Skin Scavenger_Acorn = new("Scavenger_Acorn", true, new()
            {
                character = Character.Scavenger,
                displayName = "Acorn",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 1213,
                previewColor = Extensions.ColorFromHex(0x341417),
            });
            public static Skin Scavenger_Oak = new("Scavenger_Oak", true, new()
            {
                character = Character.Scavenger,
                displayName = "Oak",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 9503,
                previewColor = Extensions.ColorFromHex(0xd9bba1),
            });
            public static Skin Scavenger_Shrub = new("Scavenger_Shrub", true, new()
            {
                character = Character.Scavenger,
                displayName = "Shrub",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 1139,
                previewColor = Extensions.ColorFromHex(0x9b765d),
            });
            public static Skin Scavenger_Branches = new("Scavenger_Branches", true, new()
            {
                character = Character.Scavenger,
                displayName = "Branches",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 1503,
                previewColor = Extensions.ColorFromHex(0x912e19),
            });
            public static Skin Scavenger_Sage = new("Scavenger_Sage", true, new()
            {
                character = Character.Scavenger,
                displayName = "Sage",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 1184,
                previewColor = Extensions.ColorFromHex(0x493d3f),
            });
            public static Skin Scavenger_Cherry = new("Scavenger_Cherry", true, new()
            {
                character = Character.Scavenger,
                displayName = "Cherry",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 9464,
                previewColor = Extensions.ColorFromHex(0xa72014),
            });
            public static Skin Scavenger_Lavender = new("Scavenger_Lavender", true, new()
            {
                character = Character.Scavenger,
                displayName = "Lavender",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 8201,
                previewColor = Extensions.ColorFromHex(0x9b81d5),
            });
            public static Skin Scavenger_Peppermint = new("Scavenger_Peppermint", true, new()
            {
                character = Character.Scavenger,
                displayName = "Peppermint",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 8750,
                previewColor = Extensions.ColorFromHex(0xd1ebdb),
            });
            public static Skin Scavenger_Juniper = new("Scavenger_Juniper", true, new()
            {
                character = Character.Scavenger,
                displayName = "Juniper",
                creatureType = CreatureTemplate.Type.Scavenger,
                randomSeed = 4566,
                previewColor = Extensions.ColorFromHex(0xb8c3ec),
            });

            public static Skin Noodlefly_Small_Lily = new("Noodlefly_Small_Lily", true, new()
            {
                character = Character.Noodlefly,
                displayName = "Lily",
                creatureType = CreatureTemplate.Type.SmallNeedleWorm,
                voiceIdOverride = RainMeadow.Ext_SoundID.RM_SmallNoot_Call,
                randomSeed = 10989,
                previewColor = Extensions.ColorFromHex(0xd15460),
            });
            public static Skin Noodlefly_Big_Rose = new("Noodlefly_Big_Rose", true, new()
            {
                character = Character.Noodlefly,
                displayName = "Rose",
                creatureType = CreatureTemplate.Type.BigNeedleWorm,
                randomSeed = 9091,
                previewColor = Extensions.ColorFromHex(0x450814),
            });
            public static Skin Noodlefly_Small_Poppy = new("Noodlefly_Small_Poppy", true, new()
            {
                character = Character.Noodlefly,
                displayName = "Poppy",
                creatureType = CreatureTemplate.Type.SmallNeedleWorm,
                voiceIdOverride = RainMeadow.Ext_SoundID.RM_SmallNoot_Call,
                randomSeed = 11114,
                previewColor = Extensions.ColorFromHex(0x740719),
            });
            public static Skin Noodlefly_Big_Lotus = new("Noodlefly_Big_Lotus", true, new()
            {
                character = Character.Noodlefly,
                displayName = "Lotus",
                creatureType = CreatureTemplate.Type.BigNeedleWorm,
                randomSeed = 9092,
                previewColor = Extensions.ColorFromHex(0x44082f),
            });
            public static Skin Noodlefly_Small_Lilac = new("Noodlefly_Small_Lilac", true, new()
            {
                character = Character.Noodlefly,
                displayName = "Lilac",
                creatureType = CreatureTemplate.Type.SmallNeedleWorm,
                voiceIdOverride = RainMeadow.Ext_SoundID.RM_SmallNoot_Call,
                randomSeed = 10992,
                previewColor = Extensions.ColorFromHex(0xb72264),
            });
            public static Skin Noodlefly_Big_Petunia = new("Noodlefly_Big_Petunia", true, new()
            {
                character = Character.Noodlefly,
                displayName = "Petunia",
                creatureType = CreatureTemplate.Type.BigNeedleWorm,
                randomSeed = 9110,
                previewColor = Extensions.ColorFromHex(0x2a232d),
            });
            public static Skin Noodlefly_Small_Jasmine = new("Noodlefly_Small_Jasmine", true, new()
            {
                character = Character.Noodlefly,
                displayName = "Jasmine",
                creatureType = CreatureTemplate.Type.SmallNeedleWorm,
                voiceIdOverride = RainMeadow.Ext_SoundID.RM_SmallNoot_Call,
                randomSeed = 10986,
                previewColor = Extensions.ColorFromHex(0xa7a8aa),
            });
            public static Skin Noodlefly_Big_Gardenia = new("Noodlefly_Big_Gardenia", true, new()
            {
                character = Character.Noodlefly,
                displayName = "Gardenia",
                creatureType = CreatureTemplate.Type.BigNeedleWorm,
                randomSeed = 9065,
                previewColor = Extensions.ColorFromHex(0xc3c5c7),
            });

            public static Skin Eggbug_Blueberry = new("Eggbug_Blueberry", true, new()
            {
                character = Character.Eggbug,
                displayName = "Blueberry",
                creatureType = CreatureTemplate.Type.EggBug,
                randomSeed = 2340,
                previewColor = Extensions.ColorFromHex(0x007aef),
            });
            public static Skin Eggbug_Kiwi = new("Eggbug_Kiwi", true, new()
            {
                character = Character.Eggbug,
                displayName = "Kiwi",
                creatureType = CreatureTemplate.Type.EggBug,
                randomSeed = 2337,
                previewColor = Extensions.ColorFromHex(0x00ea68),
            });
            public static Skin Eggbug_Citrus = new("Eggbug_Citrus", true, new()
            {
                character = Character.Eggbug,
                displayName = "Citrus",
                creatureType = CreatureTemplate.Type.EggBug,
                randomSeed = 2399,
                previewColor = Extensions.ColorFromHex(0x00e1f5),
            });
            public static Skin Eggbug_Mango = new("Eggbug_Mango", true, new()
            {
                character = Character.Eggbug,
                displayName = "Mango",
                creatureType = CreatureTemplate.Type.EggBug,
                randomSeed = 2337,
                baseColor = Extensions.ColorFromHex(0xff9900),
            });
            public static Skin Eggbug_Dragonfruit = new("Eggbug_Dragonfruit", true, new()
            {
                character = Character.Eggbug,
                displayName = "Dragonfruit",
                creatureType = CreatureTemplate.Type.EggBug,
                randomSeed = 8557,
                baseColor = Extensions.ColorFromHex(0xff0859),
            });

            public static Skin LanternMouse_Blue = new("LanternMouse_Blue", true, new()
            {
                character = Character.LanternMouse,
                displayName = "Blue",
                creatureType = CreatureTemplate.Type.LanternMouse,
                randomSeed = 1001,
            });
            public static Skin LanternMouse_Teal = new("LanternMouse_Teal", true, new()
            {
                character = Character.LanternMouse,
                displayName = "Teal",
                creatureType = CreatureTemplate.Type.LanternMouse,
                randomSeed = 1002,
            });
        }

        [TypeConverter(typeof(ExtEnumTypeConverter<Emote>))]
        public class Emote : ExtEnum<Emote>
        {
            public Emote(string value, bool register = false) : base(value, register)
            {
                if (register)
                {
                    if (value.StartsWith("emote"))
                    {
                        emoteEmotes.Add(this);
                    }
                    if (value.StartsWith("symbol"))
                    {
                        symbolEmotes.Add(this);
                    }
                }
            }
            public static Emote none = new("none", true);

            // emotions
            public static Emote emoteHello = new("emoteHello", true);
            public static Emote emoteHappy = new("emoteHappy", true);
            public static Emote emoteSad = new("emoteSad", true);

            public static Emote emoteConfused = new("emoteConfused", true);
            public static Emote emoteGoofy = new("emoteGoofy", true);
            public static Emote emoteDead = new("emoteDead", true);

            public static Emote emoteAmazed = new("emoteAmazed", true);
            public static Emote emoteShrug = new("emoteShrug", true);
            public static Emote emoteHug = new("emoteHug", true);

            public static Emote emoteAngry = new("emoteAngry", true);
            public static Emote emoteWink = new("emoteWink", true);
            public static Emote emoteMischievous = new("emoteMischievous", true);

            // symbols (displayed 3 per column)
            public static Emote symbolYes = new("symbolYes", true);
            public static Emote symbolNo = new("symbolNo", true);
            public static Emote symbolQuestion = new("symbolQuestion", true);

            public static Emote symbolExclamation = new("symbolExclamation", true);
            public static Emote symbolArrow = new("symbolArrow", true);
            public static Emote symbolTime = new("symbolTime", true);

            public static Emote symbolTravel = new("symbolTravel", true);
            public static Emote symbolGroup = new("symbolGroup", true);
            public static Emote symbolFollow = new("symbolFollow", true);

            public static Emote symbolFriends = new("symbolFriends", true);
            public static Emote symbolCollectible = new("symbolCollectible", true);
            public static Emote symbolEcho = new("symbolEcho", true);

            public static Emote symbolShelter = new("symbolShelter", true);
            public static Emote symbolGate = new("symbolGate", true);
            public static Emote symbolTree = new("symbolTree", true);

            public static Emote symbolSurvivor = new("symbolSurvivor", true);
            public static Emote symbolWanderer = new("symbolWanderer", true);
            public static Emote symbolHigh = new("symbolHigh", true);

            public static Emote symbolIterator = new("symbolIterator", true);
            public static Emote symbolLight = new("symbolLight", true);
            public static Emote symbolPlants = new("symbolPlants", true);

            public static Emote symbolMetal = new("symbolMetal", true);
            public static Emote symbolPipes = new("symbolPipes", true);
            public static Emote symbolWater = new("symbolWater", true);
        }

        public static List<Emote> emoteEmotes = new();
        public static List<Emote> symbolEmotes = new();

        public static List<Emote> AllAvailableEmotes(Character character)
        {
            return emoteEmotes.Intersect(progressionData.characterProgress[character].unlockedEmotes).ToList();
        }

        public static List<Skin> AllAvailableSkins(Character character)
        {
            return characterData[character].skins.Intersect(progressionData.characterProgress[character].unlockedSkins).ToList();
        }

        public static List<Character> AllAvailableCharacters()
        {
            return allCharacters.Intersect(progressionData.characterProgress.Keys).ToList();
        }

        public static Emote NextUnlockableEmote()
        {
            return emoteEmotes.Except(progressionData.currentCharacterProgress.unlockedEmotes).FirstOrDefault();
        }

        public static Skin NextUnlockableSkin()
        {
            return characterData[progressionData.currentlySelectedCharacter].skins.Except(progressionData.currentCharacterProgress.unlockedSkins).FirstOrDefault();
        }

        public static Character NextUnlockableCharacter()
        {
            return allCharacters.Except(progressionData.characterProgress.Keys).FirstOrDefault();
        }

        public static Character CharacterProgress()
        {
            progressionData.characterUnlockProgress++;
            if (progressionData.characterUnlockProgress >= characterProgressTreshold)
            {
                if (NextUnlockableCharacter() is Character character)
                {
                    progressionData.characterProgress.Add(character, new ProgressionData.CharacterProgressionData(character));
                    if (NextUnlockableCharacter() != null) progressionData.characterUnlockProgress -= characterProgressTreshold;
                    SaveProgression();
                    return character;
                }
                else
                {
                    progressionData.characterUnlockProgress = characterProgressTreshold;
                }
            }
            else anythingToSave = true;
            return null;
        }

        public static Skin SkinProgress()
        {
            progressionData.currentCharacterProgress.skinUnlockProgress++;
            if (progressionData.currentCharacterProgress.skinUnlockProgress >= skinProgressTreshold)
            {
                if (NextUnlockableSkin() is Skin skin)
                {
                    progressionData.currentCharacterProgress.unlockedSkins.Add(skin);
                    if (NextUnlockableSkin() != null) progressionData.currentCharacterProgress.skinUnlockProgress -= skinProgressTreshold;
                    SaveProgression();
                    return skin;
                }
                else
                {
                    progressionData.currentCharacterProgress.skinUnlockProgress = skinProgressTreshold;
                }
            }
            else anythingToSave = true;
            return null;
        }

        public static Emote EmoteProgress()
        {
            progressionData.currentCharacterProgress.emoteUnlockProgress++;
            if (progressionData.currentCharacterProgress.emoteUnlockProgress >= emoteProgressTreshold)
            {
                if (NextUnlockableEmote() is Emote emote)
                {
                    progressionData.currentCharacterProgress.unlockedEmotes.Add(emote);
                    if (NextUnlockableEmote() != null) progressionData.currentCharacterProgress.emoteUnlockProgress -= emoteProgressTreshold;
                    SaveProgression();
                    return emote;
                }
                else
                {
                    progressionData.currentCharacterProgress.emoteUnlockProgress = emoteProgressTreshold;
                }
            }
            else anythingToSave = true;
            return null;
        }

        internal static void ItemCollected(AbstractMeadowCollectible abstractMeadowCollectible)
        {
            var meadowHud = (Custom.rainWorld.processManager.currentMainLoop as RainWorldGame).cameras[0].hud.parts.First(p => p is MeadowProgressionHud) as MeadowProgressionHud;
            if (abstractMeadowCollectible.type == RainMeadow.Ext_PhysicalObjectType.MeadowTokenGold)
            {
                meadowHud.AnimateChar();
                if (CharacterProgress() is Character character) meadowHud.NewCharacterUnlocked(character);
            }
            else if (abstractMeadowCollectible.type == RainMeadow.Ext_PhysicalObjectType.MeadowTokenBlue)
            {
                meadowHud.AnimateSkin();
                if (SkinProgress() is Skin skin) meadowHud.NewSkinUnlocked(skin);
            }
            else if (abstractMeadowCollectible.type == RainMeadow.Ext_PhysicalObjectType.MeadowTokenRed)
            {
                meadowHud.AnimateEmote();
                if (EmoteProgress() is Emote emote) meadowHud.NewEmoteUnlocked(emote);
            }
            else if (abstractMeadowCollectible.type == RainMeadow.Ext_PhysicalObjectType.MeadowGhost)
            {
                meadowHud.AnimateChar();
                meadowHud.AnimateSkin();
                meadowHud.AnimateEmote();
                if (CharacterProgress() is Character character) meadowHud.NewCharacterUnlocked(character);
                if (SkinProgress() is Skin skin) meadowHud.NewSkinUnlocked(skin);
                if (EmoteProgress() is Emote emote) meadowHud.NewEmoteUnlocked(emote);
            }

            AutosaveProgression(); // will be skipped if already saved
        }

        public static Color TokenRedColor = new Color(248f / 255f, 89f / 255f, 93f / 255f);
        public static Color TokenBlueColor = RainWorld.AntiGold.rgb;
        public static Color TokenGoldColor = RainWorld.GoldRGB;

        private static string _saveLocation;
        static string SaveLocation
        {
            get
            {
                if (string.IsNullOrEmpty(_saveLocation))
                {
                    _saveLocation = Path.Combine(Path.GetFullPath(Kittehface.Framework20.UserData.GetPersistentDataPath()), "meadow.json");
                }
                return _saveLocation;
            }
        }

        public static void LoadProgression()
        {
            RainMeadow.DebugMe();
            if (progressionData != null) return;
            try
            {
                progressionData = JsonConvert.DeserializeObject<ProgressionData>(File.ReadAllText(SaveLocation));
            }
            catch (Exception ex)
            {
                RainMeadow.Error(ex);
            }
            if (progressionData == null) LoadDefaultProgression();
            lastSaved = UnityEngine.Time.realtimeSinceStartup;
        }

        public static void ReloadProgression()
        {
            RainMeadow.DebugMe();
            progressionData = null;
            LoadProgression();
        }

        public static void LoadDefaultProgression()
        {
            RainMeadow.DebugMe();
            if (progressionData != null) return;

            progressionData = new ProgressionData();

            SaveProgression();
        }

        // force save
        internal static void SaveProgression()
        {
            RainMeadow.DebugMe();
            File.WriteAllText(SaveLocation, JsonConvert.SerializeObject(progressionData));
            anythingToSave = false;
            lastSaved = UnityEngine.Time.realtimeSinceStartup;
        }

        // maybe save
        internal static void AutosaveProgression(bool changes = false)
        {
            anythingToSave |= changes;
            if ((anythingToSave && UnityEngine.Time.realtimeSinceStartup > lastSaved + 120) // no more than once a couple minutes
                || UnityEngine.Time.realtimeSinceStartup > lastSaved + 300) // or 5 minutes for position etc
            {
                SaveProgression();
            }
        }

        public static ProgressionData progressionData;
        internal static int emoteProgressTreshold = 4;
        internal static int skinProgressTreshold = 6;
        internal static int characterProgressTreshold = 8;
        private static float lastSaved;
        private static bool anythingToSave;

        // Will future me regret unversioned serialization? We'll see... If there's an issue try Serialization Callbacks
        [JsonObject(MemberSerialization.OptIn)] // otherwise properties/accessors create duplicated data, could also be opt-out but I don't like implicit
        public class ProgressionData
        {
            [JsonProperty]
            public int characterUnlockProgress;
            [JsonProperty]
            public Character currentlySelectedCharacter { get => _currentlySelectedCharacter; set { _currentlySelectedCharacter = value; if (value != null && !characterProgress.ContainsKey(value)) characterProgress[value] = new CharacterProgressionData(value); } }
            private Character _currentlySelectedCharacter;
            [JsonProperty]
            public Dictionary<Character, CharacterProgressionData> characterProgress;
            public CharacterProgressionData currentCharacterProgress => characterProgress[_currentlySelectedCharacter];

            public ProgressionData()
            {
                characterProgress = new();
                currentlySelectedCharacter = Character.Slugcat;
            }

            [JsonObject(MemberSerialization.OptIn)]
            public class CharacterProgressionData
            {
                [JsonProperty]
                public long timePlayed;
                [JsonProperty]
                public int emoteUnlockProgress;
                [JsonProperty]
                public int skinUnlockProgress;
                [JsonProperty]
                public List<Emote> unlockedEmotes;
                [JsonProperty]
                public List<Skin> unlockedSkins;
                [JsonProperty]
                [JsonConverter(typeof(WorldCoordinateConverter))]
                internal WorldCoordinate saveLocation;
                [JsonProperty]
                internal bool everSeenInMenu;
                [JsonProperty]
                internal Skin selectedSkin;
                [JsonProperty]
                internal float tintAmount;
                [JsonProperty]
                [JsonConverter(typeof(UnityColorConverter))]
                internal Color tintColor;
                [JsonProperty]
                public List<Emote> emoteHotbar;

                [JsonConstructor]
                private CharacterProgressionData() { }
                public CharacterProgressionData(Character character)
                {
                    unlockedEmotes = emoteEmotes.Take(4).ToList();
                    unlockedSkins = characterData[character].skins.Take(1).ToList();
                    selectedSkin = unlockedSkins[0];
                    saveLocation = characterData[character].startingCoords;
                    emoteHotbar = unlockedEmotes.Concat(symbolEmotes.Take(4)).ToList();
                }
            }
        }
    }
}

using UnityEngine;
using static RainMeadow.MeadowCustomization;

namespace RainMeadow
{
    public class EmoteType : ExtEnum<EmoteType>
    {
        public EmoteType(string value, bool register = false) : base(value, register) { }

        // emotions
        public static EmoteType emoteHello = new EmoteType("emoteHello", true);
        public static EmoteType emoteHappy = new EmoteType("emoteHappy", true);
        public static EmoteType emoteSad = new EmoteType("emoteSad", true);
        public static EmoteType emoteConfused = new EmoteType("emoteConfused", true);
        public static EmoteType emoteGoofy = new EmoteType("emoteGoofy", true);
        public static EmoteType emoteDead = new EmoteType("emoteDead", true);
        public static EmoteType emoteAmazed = new EmoteType("emoteAmazed", true);
        public static EmoteType emoteShrug = new EmoteType("emoteShrug", true);
        public static EmoteType emoteHug = new EmoteType("emoteHug", true);
        public static EmoteType emoteAngry = new EmoteType("emoteAngry", true);
        public static EmoteType emoteWink = new EmoteType("emoteWink", true);
        public static EmoteType emoteMischievous = new EmoteType("emoteMischievous", true);

        // ideas
        public static EmoteType symbolYes = new EmoteType("symbolYes", true);
        public static EmoteType symbolNo = new EmoteType("symbolNo", true);
        public static EmoteType symbolQuestion = new EmoteType("symbolQuestion", true);
        public static EmoteType symbolTime = new EmoteType("symbolTime", true);
        public static EmoteType symbolSurvivor = new EmoteType("symbolSurvivor", true);
        public static EmoteType symbolFriends = new EmoteType("symbolFriends", true);
        public static EmoteType symbolGroup = new EmoteType("symbolGroup", true);
        public static EmoteType symbolKnoledge = new EmoteType("symbolKnoledge", true);
        public static EmoteType symbolTravel = new EmoteType("symbolTravel", true);
        public static EmoteType symbolMartyr = new EmoteType("symbolMartyr", true);



        // things
        public static EmoteType symbolCollectible = new EmoteType("symbolCollectible", true);
        public static EmoteType symbolFood = new EmoteType("symbolFood", true);
        public static EmoteType symbolLight = new EmoteType("symbolLight", true);
        public static EmoteType symbolShelter = new EmoteType("symbolShelter", true);
        public static EmoteType symbolGate = new EmoteType("symbolGate", true);
        public static EmoteType symbolEcho = new EmoteType("symbolEcho", true);
        public static EmoteType symbolPointOfInterest = new EmoteType("symbolPointOfInterest", true);
        public static EmoteType symbolTree = new EmoteType("symbolTree", true);
        public static EmoteType symbolIterator = new EmoteType("symbolIterator", true);
        

        // verbs
        // todo
    }

    class EmoteHandler : HUD.HudPart
    {
        public static void InitializeBuiltinTypes()
        {
            _ = EmoteType.emoteHappy;
            RainMeadow.Debug($"{ExtEnum<EmoteType>.values.entries.Count} emotes loaded");
        }

        private InputScheme currentInputScheme;

        static KeyCode[] alphaRow = new[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0, KeyCode.Minus, KeyCode.Equals };
        static string[] keycodeNames = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "="};

        static EmoteType[,] keyboardMappingRows = new EmoteType[,]{
            {
                EmoteType.emoteHello,
                EmoteType.emoteHappy,
                EmoteType.emoteSad,
                EmoteType.emoteConfused,
                EmoteType.emoteGoofy,
                EmoteType.emoteDead,
                EmoteType.emoteAmazed,
                EmoteType.emoteShrug,
                EmoteType.emoteHug,
                EmoteType.emoteAngry,
                EmoteType.emoteWink,
                EmoteType.emoteMischievous,
            },{
                EmoteType.symbolYes,
                EmoteType.symbolNo,
                EmoteType.symbolQuestion,
                EmoteType.symbolTime,
                EmoteType.symbolSurvivor,
                EmoteType.symbolFriends,
                EmoteType.symbolGroup,
                EmoteType.symbolKnoledge,
                EmoteType.symbolTravel,
                EmoteType.symbolMartyr,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
            },{
                EmoteType.symbolCollectible,
                EmoteType.symbolFood,
                EmoteType.symbolLight,
                EmoteType.symbolShelter,
                EmoteType.symbolGate,
                EmoteType.symbolEcho,
                EmoteType.symbolPointOfInterest,
                EmoteType.symbolTree,
                EmoteType.symbolIterator,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
            }
        };

        enum InputScheme
        {
            none,
            keyboard,
            mouse,
            controller
        }

        public static EmoteHandler instance;
        private readonly OnlineCreature avatar;
        private readonly CreatureCustomization customization;
        private readonly FSprite[] emoteDisplayers;
        private FLabel[] inputLabels;
        private readonly FSprite[] emoteSeparators;
        private int currentKeyboardRow;

        public const int emotePreviewSize = 40;
        public const int emotePreviewSpacing = 8;
        public const float emotePreviewOpacity = 0.6f;

        public EmoteHandler(HUD.HUD hud, OnlineCreature avatar, CreatureCustomization customization) : base(hud)
        {
            RainMeadow.Debug($"EmoteHandler created for {avatar}");
            instance = this;
            currentInputScheme = InputScheme.keyboard;
            this.avatar = avatar;
            this.customization = customization;

            if (!Futile.atlasManager.DoesContainAtlas("emotes_common"))
            {
                HeavyTexturesCache.futileAtlasListings.Add(Futile.atlasManager.LoadAtlas("illustrations/emotes/emotes_common").name);
            }
            if (!Futile.atlasManager.DoesContainAtlas(customization.EmoteAtlas))
            {
                HeavyTexturesCache.futileAtlasListings.Add(Futile.atlasManager.LoadAtlas("illustrations/emotes/" + customization.EmoteAtlas).name);
            }

            // this is speficic to keyboard preview
            this.emoteDisplayers = new FSprite[keyboardMappingRows.GetLength(1)];
            this.inputLabels = new FLabel[emoteDisplayers.Length];
            var start = hud.rainWorld.options.ScreenSize.x / 2f - (emotePreviewSize + emotePreviewSpacing) * ((emoteDisplayers.Length - 1) / 2f);
            for (int i = 0; i < emoteDisplayers.Length; i++)
            {
                hud.fContainers[1].AddChild(emoteDisplayers[i] = new FSprite(customization.GetEmote(keyboardMappingRows[0, i]))
                {
                    scale = emotePreviewSize / EmoteDisplayer.emoteSourceSize,
                    x = start + (emotePreviewSize + emotePreviewSpacing) * i,
                    y = emotePreviewSize / 2f + emotePreviewSpacing / 2f,
                    alpha = emotePreviewOpacity
                });
                hud.fContainers[1].AddChild(inputLabels[i] = new FLabel("font", keycodeNames[i])
                {
                    x = start + (emotePreviewSize + emotePreviewSpacing) * i,
                    y = emotePreviewSize + emotePreviewSpacing,
                    alpha = emotePreviewOpacity
                });
            }
            this.emoteSeparators = new FSprite[emoteDisplayers.Length - 1];
            start = hud.rainWorld.options.ScreenSize.x / 2f - (emotePreviewSize + emotePreviewSpacing) * ((emoteSeparators.Length - 1) / 2f);
            for (int i = 0; i < emoteSeparators.Length; i++)
            {
                hud.fContainers[1].AddChild(emoteSeparators[i] = new FSprite("listDivider")
                {
                    scaleX = 40f / 140f,
                    rotation = 90f,
                    x = start + (emotePreviewSize + emotePreviewSpacing) * i,
                    y = emotePreviewSize / 2f + emotePreviewSpacing,
                    alpha = emotePreviewOpacity
                });
            }
        }

        public override void Draw(float timeStacker)
        {
            InputUpdate(); // here because 60fps for key events
            base.Draw(timeStacker);
        }

        public override void Update()
        {
            base.Update();
        }

        public void InputUpdate()
        {
            if (currentInputScheme == InputScheme.keyboard)
            {
                for (int i = 0; i < alphaRow.Length; i++)
                {
                    if (Input.GetKeyDown(alphaRow[i]))
                    {
                        EmotePressed(keyboardMappingRows[currentKeyboardRow, i]);
                    }
                }
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    currentKeyboardRow = (currentKeyboardRow + 1) % keyboardMappingRows.GetLength(0);
                    UpdateDisplayers();
                }
            }
        }

        public void UpdateDisplayers()
        {
            for (int i = 0; i < emoteDisplayers.Length; i++)
            {
                emoteDisplayers[i].element = Futile.atlasManager.GetElementWithName(customization.GetEmote(keyboardMappingRows[currentKeyboardRow, i]));
            }
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            for (int i = 0; i < emoteDisplayers.Length; i++)
            {
                emoteDisplayers[i].RemoveFromContainer();
            }

            for (int i = 0; i < inputLabels.Length; i++)
            {
                inputLabels[i].RemoveFromContainer();
            }

            for (int i = 0; i < emoteSeparators.Length; i++)
            {
                emoteSeparators[i].RemoveFromContainer();
            }
        }

        private void EmotePressed(EmoteType emoteType)
        {
            RainMeadow.Debug(emoteType);
            if (!EmoteDisplayer.map.TryGetValue(avatar.realizedCreature, out var displayer))
            {
                RainMeadow.Debug("holder not found");
                return;
            }
            if (displayer.AddEmoteLocal(emoteType)) {
                RainMeadow.Debug("emote added");
                // todo play local input sound
            }
        }
    }
}

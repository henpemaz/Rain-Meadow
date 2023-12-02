using UnityEngine;

namespace RainMeadow
{
    public class EmoteType : ExtEnum<EmoteType>
    {
        public EmoteType(string value, bool register = false) : base(value, register) { }

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

        public static EmoteType symbolYes = new EmoteType("symbolYes", true);
        public static EmoteType symbolNo = new EmoteType("symbolNo", true);
        public static EmoteType symbolQuestion = new EmoteType("symbolQuestion", true);
        // todo
    }

    class EmoteHandler
    {
        private InputScheme currentInputScheme;

        static KeyCode[] alphaRow = new[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0, KeyCode.Minus, KeyCode.Equals };

        static EmoteType[][] keyboardMappingRows = new[]{
            new EmoteType[12]{
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
            },new EmoteType[12]{
                EmoteType.symbolYes,
                EmoteType.symbolNo,
                EmoteType.symbolQuestion,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
            },new EmoteType[12]{
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
            }
        };
        private int currentKeyboardRow;

        enum InputScheme
        {
            none,
            keyboard,
            mouse,
            controller
        }

        public void RawUpdate()
        {
            if(currentInputScheme == InputScheme.keyboard)
            {
                for (int i = 0; i < alphaRow.Length; i++)
                {
                    if (Input.GetKeyDown(alphaRow[i]))
                    {
                        EmotePressed(keyboardMappingRows[currentKeyboardRow][i]);
                    }
                }
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    currentKeyboardRow = (currentKeyboardRow + 1) % 3;
                }
            }
        }

        public static EmoteHandler instance;
        private readonly OnlineCreature avatar;

        public EmoteHandler(OnlineCreature avatar)
        {
            RainMeadow.Debug($"EmoteHandler created for {avatar}");
            instance = this;
            currentInputScheme = InputScheme.keyboard;
            this.avatar = avatar;
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

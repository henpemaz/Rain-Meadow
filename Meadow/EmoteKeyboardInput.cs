using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public class EmoteKeyboardInput : HUD.HudPart
    {
        static KeyCode[] alphaRow = new[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0, KeyCode.Minus, KeyCode.Equals };
        static string[] keycodeNames = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" };

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

        private readonly EmoteHandler owner;
        private FContainer container;
        private FSprite[] emoteDisplayers;
        private FSprite[] emoteTiles;
        private FSprite[] emoteSeparators;
        private FLabel[] inputLabels;
        private int currentKeyboardRow;
        private Vector2 corner;
        private int nr;
        private int ne;
        private IntVector2 hoverPos;
        private bool lastMouseDown;
        public const int emotePreviewSize = 40;
        public const int emotePreviewSpacing = 8;
        public const float emotePreviewOpacityActive = 0.8f;
        public const float emotePreviewOpacityInactive = 0.5f;

        public EmoteKeyboardInput(HUD.HUD hud, MeadowAvatarCustomization customization, EmoteHandler owner) : base(hud)
        {
            this.container = new FContainer();

            nr = keyboardMappingRows.GetLength(0);
            ne = keyboardMappingRows.GetLength(1);
            this.emoteDisplayers = new FSprite[ne * nr];
            this.emoteTiles = new FSprite[ne * nr];
            this.inputLabels = new FLabel[ne];
            this.emoteSeparators = new FSprite[nr * (ne - 1)];
            var left = hud.rainWorld.options.ScreenSize.x / 2f - (emotePreviewSize + emotePreviewSpacing) * ((ne - 1) / 2f);
            corner = new(left - (emotePreviewSize + emotePreviewSpacing) / 2f, 0);
            for (int j = 0; j < nr; j++)
            {
                var y = emotePreviewSize / 2f + (emotePreviewSize + emotePreviewSpacing) * j;
                var ylabel = emotePreviewSize + (emotePreviewSize + emotePreviewSpacing) * (currentKeyboardRow);
                var alpha = j == currentKeyboardRow ? emotePreviewOpacityActive : emotePreviewOpacityInactive;
                for (int i = 0; i < ne; i++)
                {
                    float x = left + (emotePreviewSize + emotePreviewSpacing) * i;
                    if (i != 0) container.AddChild(emoteSeparators[j * (ne - 1) + i - 1] = new FSprite("listDivider")
                    {
                        scaleX = 40f / 140f,
                        rotation = 90f,
                        x = x - (emotePreviewSize + emotePreviewSpacing) / 2f,
                        y = y,
                        alpha = alpha / 2f
                    });
                    container.AddChild(emoteTiles[j * ne + i] = new FSprite(customization.GetBackground(keyboardMappingRows[j, i]))
                    {
                        scale = emotePreviewSize / EmoteDisplayer.emoteSourceSize,
                        x = x,
                        y = y,
                        alpha = alpha
                    });
                    container.AddChild(emoteDisplayers[j * ne + i] = new FSprite(customization.GetEmote(keyboardMappingRows[j, i]))
                    {
                        scale = emotePreviewSize / EmoteDisplayer.emoteSourceSize,
                        x = x,
                        y = y,
                        alpha = alpha
                    });
                    if (j == currentKeyboardRow)
                    {
                        container.AddChild(inputLabels[i] = new FLabel("font", keycodeNames[i])
                        {
                            x = x,
                            y = ylabel,
                            alpha = alpha
                        });
                    }
                }
            }

            hud.fContainers[1].AddChild(container);
            this.owner = owner;
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            InputUpdate(); // here because 60fps for key events
        }

        public void InputUpdate()
        {
            for (int i = 0; i < alphaRow.Length; i++)
            {
                if (Input.GetKeyDown(alphaRow[i]))
                {
                    owner.EmotePressed(keyboardMappingRows[currentKeyboardRow, i]);
                }
            }
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                currentKeyboardRow = (currentKeyboardRow + 1) % keyboardMappingRows.GetLength(0);
                UpdateDisplayers();
            }
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                owner.ClearEmotes();
            }

            IntVector2 newHover = IntVector2.FromVector2(((Vector2)Futile.mousePosition - this.corner) / (emotePreviewSize + emotePreviewSpacing));
            if (newHover.x < 0 || newHover.x >= ne || newHover.y < 0 || newHover.y >= nr)
            {
                newHover = new(-1, -1);
            }
            if (newHover != hoverPos)
            {
                hoverPos = newHover;
                UpdateDisplayers();
            }
            var mouseDown = Input.GetMouseButtonDown(0);
            if (hoverPos.x != -1 && mouseDown && !lastMouseDown)
            {
                owner.EmotePressed(keyboardMappingRows[hoverPos.y, hoverPos.x]);
            }
            lastMouseDown = mouseDown;
        }

        public void UpdateDisplayers()
        {
            var ylabel = emotePreviewSize + (emotePreviewSize + emotePreviewSpacing) * (currentKeyboardRow);
            for (int i = 0; i < ne; i++)
            {
                inputLabels[i].y = ylabel;
            }

            for (int j = 0; j < nr; j++)
            {
                var emotealpha = j == currentKeyboardRow ? emotePreviewOpacityActive : emotePreviewOpacityInactive * 1.33f;
                var tilealpha = j == currentKeyboardRow ? emotePreviewOpacityActive : emotePreviewOpacityInactive * 0.75f;
                for (int i = 0; i < ne; i++)
                {
                    if (i != 0) emoteSeparators[j * (ne - 1) + i - 1].alpha = emotealpha / 2f;
                    emoteDisplayers[j * ne + i].alpha = (hoverPos.x == i && hoverPos.y == j) ? 1 : emotealpha;
                    emoteTiles[j * ne + i].alpha = (hoverPos.x == i && hoverPos.y == j) ? 1 : tilealpha;
                }
            }
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            container.RemoveFromContainer();
            container.RemoveAllChildren();
            container = null;
            emoteDisplayers = null;
            emoteSeparators = null;
            inputLabels = null;
        }
    }
}

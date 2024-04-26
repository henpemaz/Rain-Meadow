using RWCustom;
using System;
using UnityEngine;
using static RainMeadow.MeadowProgression;

namespace RainMeadow
{
    public class EmoteGridInput : HUD.HudPart
    {
        private readonly MeadowHud owner;
        private FContainer gridContainer;
        private FSprite[] emoteDisplayers;
        private FSprite[] emoteTiles;
        private FSprite[] dots;
        private Vector2 corner;
        Emote[,] emoteGrid;
        private int nr;
        private int nc;
        private IntVector2 hoverPos;
        private bool lastMouseDown;
        private Rect buttonRect;
        public const int emotePreviewSize = 50;
        public const int emotePreviewSpacing = 6;
        public const float emotePreviewOpacityInactive = 0.5f;

        public EmoteGridInput(HUD.HUD hud, MeadowAvatarCustomization customization, MeadowHud owner) : base(hud)
        {
            this.gridContainer = new FContainer();

            var emotes = MeadowProgression.AllAvailableEmotes(customization.character);
            var symbols = MeadowProgression.symbolEmotes;

            int iconsPerColumn = 3;
            var emoteColumns = (emotes.Count - 1) / iconsPerColumn + 1;
            var symbolColumns = (symbols.Count - 1) / iconsPerColumn + 1;

            emoteGrid = new Emote[iconsPerColumn, emoteColumns + symbolColumns];

            for (int i = 0; i < emoteColumns; i++)
            {
                for (int j = 0; j < iconsPerColumn; j++)
                {
                    var n = i * iconsPerColumn + j;
                    if (n < emotes.Count)
                    {
                        emoteGrid[j, i] = emotes[n];
                    }
                    else break;
                }
            }

            for (int i = 0; i < symbolColumns; i++)
            {
                for (int j = 0; j < iconsPerColumn; j++)
                {
                    var n = i * iconsPerColumn + j;
                    if (n < symbols.Count)
                    {
                        emoteGrid[j, emoteColumns + i] = symbols[n];
                    }
                    else break;
                }
            }

            nr = iconsPerColumn;
            nc = emoteColumns + symbolColumns;
            this.emoteDisplayers = new FSprite[nc * nr];
            this.emoteTiles = new FSprite[nc * nr];
            var left = hud.rainWorld.options.ScreenSize.x / 2f - (emotePreviewSize + emotePreviewSpacing) * ((nc - 1) / 2f);
            var top = emotePreviewSpacing / 2f + emotePreviewSize / 2f + (emotePreviewSize + emotePreviewSpacing) * (nr - 1);
            corner = new(left - (emotePreviewSize + emotePreviewSpacing) / 2f, top + (emotePreviewSize + emotePreviewSpacing) / 2f);
            for (int j = 0; j < nr; j++)
            {
                var y = top - (emotePreviewSize + emotePreviewSpacing) * j;
                var alpha = emotePreviewOpacityInactive;
                for (int i = 0; i < nc; i++)
                {
                    if (emoteGrid[j, i] == null) continue;
                    float x = left + (emotePreviewSize + emotePreviewSpacing) * i;
                    gridContainer.AddChild(emoteTiles[j * nc + i] = new FSprite(customization.GetBackground(emoteGrid[j, i]))
                    {
                        scale = emotePreviewSize / EmoteDisplayer.emoteSourceSize,
                        x = x,
                        y = y,
                        alpha = alpha
                    });
                    gridContainer.AddChild(emoteDisplayers[j * nc + i] = new FSprite(customization.GetEmote(emoteGrid[j, i]))
                    {
                        scale = emotePreviewSize / EmoteDisplayer.emoteSourceSize,
                        x = x,
                        y = y,
                        alpha = alpha
                    });
                }
            }

            dots = new FSprite[6];
            var dotsPos = new Vector2(corner.x - 30f, emotePreviewSpacing/2f);
            var dotsSize = new Vector2(30, 20);
            buttonRect = new Rect(dotsPos + new Vector2(-6,-8), dotsSize + new Vector2(12, 16));
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    hud.fContainers[1].AddChild(dots[i * 2 + j] = new FSprite("Futile_White")
                    {
                        shader = Custom.rainWorld.Shaders["FlatLight"],
                        scale = 12f / 16f,
                        x = dotsPos.x + 2f + i * 8f,
                        y = dotsPos.y + 2f + j * 8f,
                    });
                }
            }
            visible = true;
            hud.fContainers[1].AddChild(gridContainer);
            this.owner = owner;
        }

        bool visible;
        bool lastVisible;
        bool EffectiveVisible
        {
            get
            {
                if (owner.game.pauseMenu != null) return false;
                return visible;
            }
        }

        public override void Update()
        {
            base.Update();

            bool newVisible = EffectiveVisible;
            if (newVisible != lastVisible)
            {
                this.gridContainer.isVisible = newVisible;
                for (int i = 0; i < dots.Length; i++)
                {
                    dots[i].alpha = newVisible? 1f : 0.33f;
                }
            }
            lastVisible = newVisible;
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            InputUpdate(); // here because 60fps for key events
            // nothing to move
        }

        public void InputUpdate()
        {
            if (owner.game.pauseMenu != null) return;
            var mouseDown = Input.GetMouseButton(0);
            if (mouseDown && !lastMouseDown && buttonRect.Contains((Vector2)Futile.mousePosition))
            {
                ToggleVisibility();
            }

            if (visible)
            {
                Vector2 offset = ((Vector2)Futile.mousePosition - this.corner) * new Vector2(1, -1) / (emotePreviewSize + emotePreviewSpacing);
                IntVector2 newHover = new IntVector2(Mathf.FloorToInt(offset.x), Mathf.FloorToInt(offset.y));
                if (newHover.x < 0 || newHover.x >= nc || newHover.y < 0 || newHover.y >= nr)
                {
                    newHover = new(-1, -1);
                }
                if (newHover != hoverPos)
                {
                    hoverPos = newHover;
                    UpdateDisplayers();
                }
                if (mouseDown && !lastMouseDown && hoverPos.x != -1)
                {
                    owner.EmotePressed(emoteGrid[hoverPos.y, hoverPos.x]);
                }
            }
            
            lastMouseDown = mouseDown;
        }

        public void UpdateDisplayers()
        {
            for (int j = 0; j < nr; j++)
            {
                for (int i = 0; i < nc; i++)
                {
                    if (emoteDisplayers[j * nc + i] == null) continue;
                    emoteDisplayers[j * nc + i].alpha = (hoverPos.x == i && hoverPos.y == j) ? 1 : emotePreviewOpacityInactive;
                    emoteTiles[j * nc + i].alpha = (hoverPos.x == i && hoverPos.y == j) ? 1 : emotePreviewOpacityInactive;
                }
            }
        }

        private void ToggleVisibility()
        {
            visible = !visible;
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            gridContainer.RemoveFromContainer();
            gridContainer.RemoveAllChildren();
            gridContainer = null;
            emoteDisplayers = null;
            emoteTiles = null;
        }
    }
}

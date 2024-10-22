using RWCustom;
using UnityEngine;
using static RainMeadow.MeadowProgression;

namespace RainMeadow
{
    public class EmoteGridDisplay
    {
        private FContainer container;
        private FSprite[] emoteDisplayers;
        private FSprite[] emoteTiles;

        private int nr;
        private int nc;

        public const int emotePreviewSize = 50;
        public const int emotePreviewSpacing = 6;
        public const float emotePreviewOpacityInactive = 0.5f;

        public bool isVisible
        {
            get { return container.isVisible; }
            set { container.isVisible = value; }
        }

        public Vector2 pos
        {
            get { return container.GetPosition(); }
            set { container.SetPosition(value); }
        }

        public float alpha
        {
            get { return container.alpha; }
            set { container.alpha = value; container.isVisible = (value > 0f); }
        }

        public EmoteGridDisplay(FContainer parent, MeadowAvatarData customization, Emote[,] emotes, Vector2 pos)
        {
            this.container = new();
            nr = emotes.GetLength(0);
            nc = emotes.GetLength(1);

            this.emoteDisplayers = new FSprite[nc * nr];
            this.emoteTiles = new FSprite[nc * nr];

            var alpha = emotePreviewOpacityInactive;
            for (int j = 0; j < nr; j++)
            {
                // top to bottom
                var y = pos.y + (emotePreviewSize + emotePreviewSpacing) * (nr - j - 0.5f);
                for (int i = 0; i < nc; i++)
                {
                    if (emotes[j, i] == null) continue;
                    // left to right
                    float x = pos.x + (emotePreviewSize + emotePreviewSpacing) * (i + 0.5f);
                    container.AddChild(emoteTiles[j * nc + i] = new FSprite(customization.GetBackground(emotes[j, i]))
                    {
                        scale = emotePreviewSize / EmoteDisplayer.emoteSourceSize,
                        x = x,
                        y = y,
                        alpha = alpha,
                        color = customization.EmoteBackgroundColor(emotes[j, i])
                    });
                    container.AddChild(emoteDisplayers[j * nc + i] = new FSprite(customization.GetEmote(emotes[j, i]))
                    {
                        scale = emotePreviewSize / EmoteDisplayer.emoteSourceSize,
                        x = x,
                        y = y,
                        alpha = alpha,
                        color = customization.EmoteColor(emotes[j, i])
                    });
                }
            }
            parent.AddChild(container);
        }

        internal void SetSelected(IntVector2 selected)
        {
            for (int j = 0; j < nr; j++)
            {
                for (int i = 0; i < nc; i++)
                {
                    if (emoteDisplayers[j * nc + i] == null) continue;
                    emoteDisplayers[j * nc + i].alpha = (selected.x == i && selected.y == j) ? 1 : emotePreviewOpacityInactive;
                    emoteTiles[j * nc + i].alpha = (selected.x == i && selected.y == j) ? 1 : emotePreviewOpacityInactive;
                }
            }
        }

        public void ClearSprites()
        {
            container.RemoveFromContainer();
            container.RemoveAllChildren();
            emoteDisplayers = null;
            emoteTiles = null;
            container = null;
        }
    }
}

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
        private Vector2[] emoteInitPositions;

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
            this.emoteInitPositions = new Vector2[nc * nr];

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
                    emoteInitPositions[j * nc + i] = new Vector2(x, y);
                }
            }
            parent.AddChild(container);
        }
        internal void positionssss(float inittime)
        {
            for (int j = 0; j < nr; j++)
            {
                for (int i = 0; i < nc; i++)
                {
                    if (emoteDisplayers[j * nc + i] == null) continue;
                    float thing = inittime + ((j * nc + i) / 100f);
                    float y = (1 - Custom.SCurve(Mathf.InverseLerp(thing, thing + 0.4f, Time.time + 0.16f), 0.65f)) * -20f;
                    Vector2 newpos = emoteInitPositions[j * nc + i] + new Vector2(0, y);
                    emoteDisplayers[j * nc + i].SetPosition(newpos);
                    emoteTiles[j * nc + i].SetPosition(newpos);


                    float thealpha = emoteDisplayers[j * nc + i].alpha;
                    if (Time.time == inittime || (thealpha != emotePreviewOpacityInactive && thealpha != 1))
                    {
                        float alpha = Custom.SCurve(Mathf.InverseLerp(thing, thing + 0.4f, Time.time), 0.65f) * emotePreviewOpacityInactive;
                        emoteDisplayers[j * nc + i].alpha = alpha;
                        emoteTiles[j * nc + i].alpha = alpha;
                    }
                }
            }
        }

        internal void SetSelected(IntVector2 selected)
        {
            for (int j = 0; j < nr; j++)
            {
                for (int i = 0; i < nc; i++)
                {
                    if (emoteDisplayers[j * nc + i] == null) continue;
                    float thealpha = emoteDisplayers[j * nc + i].alpha;
                    bool issellected = (selected.x == i && selected.y == j);
                    if (thealpha == 1 || issellected) { } else { continue; }
                    emoteDisplayers[j * nc + i].alpha = issellected ? 1 : emotePreviewOpacityInactive; //((emoteDisplayers[j * nc + i].alpha != emotePreviewOpacityInactive && emoteDisplayers[j * nc + i].alpha != 1) ? emoteDisplayers[j * nc + i].alpha:emotePreviewOpacityInactive);
                    emoteTiles[j * nc + i].alpha = issellected ? 1 : emotePreviewOpacityInactive;
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

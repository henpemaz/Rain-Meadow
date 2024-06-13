using HarmonyLib;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public class TokenSparkIcon
    {
        private Color TokenColor;
        private Vector2[,] lines;
        public Vector2 pos;
        public float scale;
        public float alpha;
        private float sinCounter2;
        private FSprite[] sprites;
        private Vector2 lastPos;
        private float lastScale;
        private float lastAlpha;
        private FContainer container; // controls position and scaling

        public TokenSparkIcon(FContainer hudContainer, Color color, Vector2 pos, float scale, float alpha)
        {
            TokenColor = color;

            this.lines = new Vector2[4, 4];
            this.lines[0, 2] = new Vector2(-7f, 0f);
            this.lines[1, 2] = new Vector2(0f, 11f);
            this.lines[2, 2] = new Vector2(7f, 0f);
            this.lines[3, 2] = new Vector2(0f, -11f);
            this.sprites = new FSprite[6];
            this.sprites[0] = new FSprite("Futile_White", true);
            this.sprites[0].shader = Custom.rainWorld.Shaders["FlatLight"];
            this.sprites[1] = new FSprite("JetFishEyeA", true);
            this.sprites[1].shader = Custom.rainWorld.Shaders["Hologram"];
            for (int i = 0; i < 4; i++)
            {
                this.sprites[(2 + i)] = new FSprite("pixel", true);
                this.sprites[(2 + i)].anchorY = 0f;
                this.sprites[(2 + i)].shader = Custom.rainWorld.Shaders["Hologram"];
            }

            float num = 0.2f;
            float num2 = 0f;
            float num3 = 1f;
            Color goldColor = this.GoldCol(num);
            this.sprites[1].color = goldColor;
            this.sprites[1].alpha = (1f - num) * Mathf.InverseLerp(0.5f, 0f, num2) * num3 * (1f);
            this.sprites[0].alpha = 0.9f * (1f - num) * Mathf.InverseLerp(0.5f, 0f, num2) * num3;
            this.sprites[0].scale = Mathf.Lerp(20f, 40f, num) / 16f;
            this.sprites[0].color = Color.Lerp(this.TokenColor, goldColor, 0.4f);

            this.pos = pos;
            this.scale = scale;
            this.alpha = alpha;
            this.lastPos = pos;
            this.lastScale = scale;
            this.lastAlpha = alpha;

            this.container = new FContainer();
            container.SetPosition(pos);
            container.scale = scale; // yipee
            container.alpha = alpha;
            container.isVisible = alpha > 0f;

            this.sprites.Do(s => container.AddChild(s));
            hudContainer.AddChild(container);
        }

        public void Update()
        {
            this.lastPos = pos;
            this.lastScale = scale;
            this.lastAlpha = alpha;
            this.sinCounter2 += (1f + Mathf.Lerp(-10f, 10f, UnityEngine.Random.value) * 0.2f);
            float num = Mathf.Sin(this.sinCounter2 / 20f);
            num = Mathf.Pow(Mathf.Abs(num), 0.5f) * Mathf.Sign(num);
            var lenLines = this.lines.GetLength(0);
            for (int i = 0; i < lenLines; i++)
            {
                this.lines[i, 1] = this.lines[i, 0];
            }
            for (int k = 0; k < lenLines; k++)
            {
                if (Mathf.Pow(UnityEngine.Random.value, 0.1f + 0.2f * 5f) > this.lines[k, 3].x)
                {
                    this.lines[k, 0] = Vector2.Lerp(this.lines[k, 0], new Vector2(this.lines[k, 2].x * num, this.lines[k, 2].y), Mathf.Pow(UnityEngine.Random.value, 1f + this.lines[k, 3].x * 17f));
                }
                if (UnityEngine.Random.value < Mathf.Pow(this.lines[k, 3].x, 0.2f) && UnityEngine.Random.value < Mathf.Pow(0.2f, 0.8f - 0.4f * this.lines[k, 3].x))
                {
                    this.lines[k, 0] += Custom.RNV() * 17f * this.lines[k, 3].x;
                    this.lines[k, 3].y = Mathf.Max(this.lines[k, 3].y, 0.2f);
                }
                this.lines[k, 3].x = Custom.LerpAndTick(this.lines[k, 3].x, this.lines[k, 3].y, 0.01f, 0.033333335f);
                this.lines[k, 3].y = Mathf.Max(0f, this.lines[k, 3].y - 0.014285714f);
                if (UnityEngine.Random.value < 1f / Mathf.Lerp(210f, 20f, 0.2f))
                {
                    this.lines[k, 3].y = Mathf.Max(0.2f, (UnityEngine.Random.value < 0.5f) ? 0.2f : UnityEngine.Random.value);
                }
            }
        }

        public Color GoldCol(float g)
        {
            return Color.Lerp(this.TokenColor, new Color(1f, 1f, 1f), 0.4f + 0.4f * Mathf.Max(0, Mathf.Pow(g, 0.5f)));
        }

        public void Draw(float timeStacker)
        {
            var drawPos = Vector2.Lerp(lastPos, pos, timeStacker);
            var drawAlpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
            var drawScale = Mathf.Lerp(lastScale, scale, timeStacker);

            container.SetPosition(drawPos);
            container.alpha = drawAlpha;
            container.scale = drawScale; // yipee
            if (drawAlpha > 0f)
            {
                container.isVisible = true;

                // lines animate
                Color goldColor = this.GoldCol((float)0.2f);
                for (int i = 0; i < 4; i++)
                {
                    Vector2 vector2 = Vector2.Lerp(this.lines[i, 1], this.lines[i, 0], timeStacker);
                    int num4 = (i == 3) ? 0 : (i + 1);
                    Vector2 vector3 = Vector2.Lerp(this.lines[num4, 1], this.lines[num4, 0], timeStacker);
                    float num5 = 1f - (1f - Mathf.Max(this.lines[i, 3].x, this.lines[num4, 3].x)) * 0.8f;
                    num5 = Mathf.Pow(num5, 2f);
                    if (UnityEngine.Random.value < num5)
                    {
                        vector3 = Vector2.Lerp(vector2, vector3, UnityEngine.Random.value);
                    }
                    this.sprites[(2 + i)].x = vector2.x;
                    this.sprites[(2 + i)].y = vector2.y;
                    this.sprites[(2 + i)].scaleY = Vector2.Distance(vector2, vector3);
                    this.sprites[(2 + i)].rotation = Custom.AimFromOneVectorToAnother(vector2, vector3);
                    this.sprites[(2 + i)].alpha = (1f - num5);
                    this.sprites[(2 + i)].color = goldColor;
                }
            }
            else
            {
                container.isVisible = false;
            }
        }

        public void ClearSprites()
        {
            this.sprites.Do(s => s.RemoveFromContainer());
        }
    }
}
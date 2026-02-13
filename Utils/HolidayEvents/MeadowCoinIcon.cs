using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    // If you don't actually need the line-flicker logic, 
    // we can keep the Update simple to save performance.
    public class MeadowCoinIcon : TokenSparkIcon
    {
        private Color TokenColor;
        public Vector2 pos;
        public float scale;
        public float alpha;
        private FSprite[] sprites;
        private Vector2 lastPos;
        private float lastScale;
        private float lastAlpha;
        private FContainer container;

        public MeadowCoinIcon(FContainer hudContainer, Color color, Vector2 pos, float scale, float alpha) : base(hudContainer, color, pos, scale, alpha)
        {
            this.TokenColor = color;
            this.sprites = new FSprite[1];
            
            // Using Circle20 for a smooth round coin
            this.sprites[0] = new FSprite("Circle20", true); 

            // Logic for that "Rain World Gold"
            float goldIntensity = 0.2f;
            Color goldColor = this.GoldCol(goldIntensity);
            
            this.sprites[0].color = Color.Lerp(this.TokenColor, goldColor, 0.6f);
            this.sprites[0].alpha = alpha;

            this.pos = pos;
            this.scale = scale;
            this.alpha = alpha;
            this.lastPos = pos;
            this.lastScale = scale;
            this.lastAlpha = alpha;

            this.container = new FContainer();
            container.SetPosition(pos);
            container.scale = scale; 
            container.alpha = alpha;
            
            container.AddChild(this.sprites[0]);
            hudContainer.AddChild(container);
        }

        public void Update()
        {
            this.lastPos = pos;
            this.lastScale = scale;
            this.lastAlpha = alpha;
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

            float internalScale = 0.05f; 
            this.sprites[0].scale = internalScale; 

            container.SetPosition(drawPos);
            container.alpha = drawAlpha;
            container.scale = drawScale;
            container.isVisible = drawAlpha > 0f;
        }

        public void ClearSprites()
        {
            this.container.RemoveFromContainer();
            this.sprites[0].RemoveFromContainer();
        }
    }
}
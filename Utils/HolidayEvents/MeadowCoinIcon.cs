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

        public MeadowCoinIcon(
            FContainer hudContainer,
            Color color,
            Vector2 pos,
            float scale,
            float alpha
        )
            : base(hudContainer, color, pos, scale, alpha)
        {
            this.TokenColor = color;
            this.sprites = new FSprite[1];
            SpecialEvents.LoadElement("meadowcoin");
            this.sprites[0] = new FSprite("meadowcoin", true);

            this.sprites[0].color = Color.Lerp(this.TokenColor, color, 0.6f);
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

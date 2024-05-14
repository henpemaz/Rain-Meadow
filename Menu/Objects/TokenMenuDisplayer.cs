using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class TokenMenuDisplayer : PositionedMenuObject
    {
        public float alpha;
        public float lastAlpha;
        public TokenSparkIcon token;
        public MenuLabel label;
        internal string text;

        public TokenMenuDisplayer(Menu.Menu menu, MenuObject owner, Vector2 pos, Color color, string text) : base(menu, owner, pos)
        {
            Shader.SetGlobalVector(RainWorld.ShadPropSpriteRect, new Vector4(0, 0, 1, 1)); // only ever set in game smh
            Shader.SetGlobalVector(RainWorld.ShadPropScreenSize, menu.manager.rainWorld.screenSize);
            this.text = text;
            this.token = new TokenSparkIcon(this.Container, color, pos, 1.5f, 1f);
            this.label = new MenuLabel(menu, this, text, new Vector2(0, 24f), Vector2.zero, false);
            this.subObjects.Add(label);
            alpha = 1f;
        }

        public override void Update()
        {
            base.Update();
            token.Update();
            lastAlpha = alpha;
            token.pos = pos;
            token.alpha = alpha;
            label.text = text;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            token.Draw(timeStacker);
            var drawAlpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
            label.label.alpha = drawAlpha;
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            token.ClearSprites();
        }
    }
}

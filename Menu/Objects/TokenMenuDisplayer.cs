using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class TokenMenuDisplayer : PositionedMenuObject
    {
        public float alpha;
        private MeadowHud.TokenSparkIcon token;
        private MenuLabel label;
        internal string text;

        public TokenMenuDisplayer(Menu.Menu menu, MenuObject owner, Vector2 pos, Color color, string text) : base(menu, owner, pos)
        {
            Shader.SetGlobalVector(RainWorld.ShadPropSpriteRect, new Vector4(0, 0, 1, 1)); // only ever set in game smh
            Shader.SetGlobalVector(RainWorld.ShadPropScreenSize, menu.manager.rainWorld.screenSize);
            this.text = text;
            this.token = new MeadowHud.TokenSparkIcon(this.Container, color, pos, 1.5f);
            this.label = new MenuLabel(menu, owner, text, pos + new Vector2(0, 24f), Vector2.zero, false);
            this.subObjects.Add(label);
            alpha = 1f;
        }

        public override void Update()
        {
            base.Update();
            token.Update();
            label.text = text;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            token.Draw(timeStacker);
            token.container.SetPosition(pos);
            token.container.alpha = alpha;
            token.container.isVisible = alpha > 0f;
            label.pos = pos + new Vector2(0, 24f);
            label.label.alpha = alpha;
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            token.ClearSprites();
        }
    }
}

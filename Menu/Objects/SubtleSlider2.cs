using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class SubtleSlider2 : HorizontalSlider
    {
        public SubtleSlider2(Menu.Menu menu, MenuObject owner, string text, Vector2 pos, Vector2 size) : base(menu, owner, text, pos, size, null, true)
        {
            this.menuLabel = new MenuLabel(menu, this, text, new Vector2(this.length + 10f, 0f), size, false, null);
            this.subObjects.Add(this.menuLabel);
            this.subtleSliderNob.pos = new Vector2(-10f, 5f);
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            this.menuLabel.label.x = base.RelativeAnchorPoint.x + 5f + this.length / 2f;
            this.menuLabel.label.y = this.DrawY(timeStacker);

            var vector = this.subtleSliderNob.DrawPos(timeStacker);
            var vector2 = new Vector2(this.subtleSliderNob.DrawSize(timeStacker), this.subtleSliderNob.DrawSize(timeStacker));
            this.lineSprites[2].x = base.RelativeAnchorPoint.x + this.length + 10;
            this.lineSprites[2].scaleX = this.length + 10 - (vector.x + vector2.x - base.RelativeAnchorPoint.x);
        }
    }
}

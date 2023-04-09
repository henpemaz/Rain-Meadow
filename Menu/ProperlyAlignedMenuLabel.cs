using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class ProperlyAlignedMenuLabel : MenuLabel
    {
        public ProperlyAlignedMenuLabel(Menu.Menu menu, MenuObject owner, string text, Vector2 pos, Vector2 size, bool bigText, FTextParams textParams = null) : base(menu, owner, text, pos, size, bigText, textParams)
        {
            label.alignment = FLabelAlignment.Left;
            label.anchorX = 0;
            label.anchorY = 0;
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            this.label.x = this.DrawX(timeStacker);
            this.label.y = this.DrawY(timeStacker);
        }
    }
}

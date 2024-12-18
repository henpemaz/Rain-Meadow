using Menu;
using UnityEngine;

namespace RainMeadow
{
    internal class MenuSprite : PositionedMenuObject
    {
        public FSprite sprite;
        public MenuSprite(Menu.Menu menu, Menu.MenuObject owner, FSprite fSprite, Vector2 pos) : base(menu, owner, pos)
        {
            this.sprite = fSprite;
            Container.AddChild(sprite);
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            sprite.SetPosition(DrawPos(timeStacker));
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            sprite.RemoveFromContainer();
        }
    }
}
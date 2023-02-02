using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class MenuDarkSprite : MenuObject
    {
        private FSprite darkSprite;

        public MenuDarkSprite(Menu.Menu menu, MenuObject owner) : base(menu, owner)
        {
            this.Container.AddChild(this.darkSprite = new FSprite("pixel"));
            this.darkSprite.color = new Color(0f, 0f, 0f);
            this.darkSprite.anchorX = 0f;
            this.darkSprite.anchorY = 0f;
            this.darkSprite.scaleX = 1368f;
            this.darkSprite.scaleY = 770f;
            this.darkSprite.x = -1f;
            this.darkSprite.y = -1f;
            this.darkSprite.alpha = 0.85f;
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            this.darkSprite.RemoveFromContainer();
        }
    }
}

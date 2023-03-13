using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class MenuDarkSprite : MenuObject
    {
        private FSprite darkSprite;

        public MenuDarkSprite(Menu.Menu menu, MenuObject owner) : base(menu, owner)
        {
            this.Container.AddChild(this.darkSprite = new FSprite("pixel")
            {
                color = new Color(0f, 0f, 0f),
                anchorX = 0f,
                anchorY = 0f,
                scaleX = 1368f,
                scaleY = 770f,
                x = -1f,
                y = -1f,
                alpha = 0.85f,
            });
        }

        public override void RemoveSprites()
        {
            this.darkSprite.RemoveFromContainer();
            base.RemoveSprites();
        }
    }
}

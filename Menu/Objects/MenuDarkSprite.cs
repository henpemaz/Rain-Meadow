using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class MenuDarkSprite : MenuObject
    {
        public FSprite darkSprite;

        public MenuDarkSprite(Menu.Menu menu, MenuObject owner) : base(menu, owner)
        {
            Container.AddChild(darkSprite = new("Futile_White")
            {
                color = new Color(0f, 0f, 0f),
                anchorX = 0f,
                anchorY = 0f,
                scaleX = 1000f,
                scaleY = 1000f,
                x = 0f,
                y = 0f,
                alpha = 0.85f,
            });
        }
        public override void RemoveSprites()
        {
            darkSprite.RemoveFromContainer();
            base.RemoveSprites();
        }
    }
}

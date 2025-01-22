using Menu;

namespace RainMeadow
{
    public class FSpriteWrap : MenuObject
    {
        public FSprite sprite;
        public FSpriteWrap(Menu.Menu menu, MenuObject owner, FSprite fSprite) : base(menu, owner) 
        {
            this.sprite = fSprite;
            Container.AddChild(fSprite);
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            sprite.RemoveFromContainer();
        }
    }
}

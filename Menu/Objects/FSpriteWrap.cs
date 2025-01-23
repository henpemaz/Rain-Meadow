using Menu;

namespace RainMeadow
{
    public class FSpriteWrap : MenuObject
    {
        public FSprite sprite;
        public FSpriteWrap(Menu.Menu menu, MenuObject owner, FSprite sprite) : base(menu, owner) 
        {
            this.sprite = sprite;
            Container.AddChild(this.sprite);
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            sprite.RemoveFromContainer();
        }
    }
}

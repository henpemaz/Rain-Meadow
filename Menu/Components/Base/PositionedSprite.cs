using Menu;
using UnityEngine;

namespace RainMeadow.UI.Components.Base;

public class PositionedSprite : PositionedMenuObject
{
    public FSprite Sprite { get; set; }
    public Vector2 Size { get; set; }

    public PositionedSprite(Menu.Menu menu, MenuObject owner, Vector2 pos, FSprite sprite)
        : base(menu, owner, pos)
    {
        Sprite = sprite;
        Size = new Vector2(sprite.width, sprite.height);
        Container.AddChild(sprite);
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        Sprite.x = DrawX(timeStacker) + Sprite.width / 2;
        Sprite.y = DrawY(timeStacker) + Sprite.height / 2;
    }

    public override void RemoveSprites()
    {
        Sprite.RemoveFromContainer();
        base.RemoveSprites();
    }
}

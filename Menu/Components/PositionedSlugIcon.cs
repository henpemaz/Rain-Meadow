using System.Collections.Generic;
using Menu;
using RainMeadow.UI.Components.Base;
using UnityEngine;

namespace RainMeadow.UI.Components;

public class PositionedSlugIcon : PositionedMenuObject
{
    public SlugIcon slugIcon;
    public List<PositionedSprite> PositionedSprites = [];

    public PositionedSlugIcon(
        Menu.Menu menu,
        MenuObject owner,
        Vector2 pos,
        string slugcatName,
        List<Color>? colors = null,
        bool dead = false
    )
        : base(menu, owner, pos)
    {
        slugIcon = new SlugIcon("", colors);
        Container.AddChild(slugIcon.container);
        DrawScugSprites(slugcatName, dead);
    }

    public void ClearSprites()
    {
        slugIcon.ClearSprites();
        for (int i = 0; i < PositionedSprites.Count; i++)
            this.ClearMenuObject(PositionedSprites[i]);
        PositionedSprites.Clear();
    }

    public void DrawScugSprites(string? newSlugcatName = "", bool dead = false)
    {
        slugIcon.DrawScugSprites(newSlugcatName, dead);
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        foreach (FSprite sprite in slugIcon.sprites)
        {
            sprite.x = DrawX(timeStacker) + sprite.width / 2;
            sprite.y = DrawY(timeStacker) + sprite.height / 2;
        }
    }
}

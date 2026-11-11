using RainMeadow.UI.Components;
using UnityEngine;

namespace RainMeadow;

public class PlayerIcon
{
    public FContainer container = new();
    public SlugIcon slugIcon;
    public FSprite icon;

    public string currentElementName = "";

    public Vector2 Pos
    {
        get;
        set
        {
            icon.x = value.x;
            icon.y = value.y;
            slugIcon.sprites.ForEach(sprite =>
            {
                sprite.x = value.x;
                sprite.y = value.y;
            });
            field = value;
        }
    }

    public float Alpha
    {
        get;
        set
        {
            icon.alpha = value;
            slugIcon.sprites.ForEach(sprite => sprite.alpha = value);
            field = value;
        }
    }

    public PlayerIcon(
        FContainer parentContainer,
        SlugcatCustomization customization,
        Color iconColor
    )
    {
        RainMeadow.Debug("current colours:" + customization.currentColors.Count);

        slugIcon = new SlugIcon("", customization.currentColors)
        {
            slugcatName = customization.playingAs.value,
        };
        icon = new FSprite("Kill_Slugcat") { color = iconColor };
        parentContainer.AddChild(slugIcon.container);
        parentContainer.AddChild(icon);
    }

    public void DrawSlugIcon(bool dead)
    {
        if (RainMeadow.rainMeadowOptions.MinimalistSlugIcon.Value)
        {
            if (dead)
                DrawSingleElement("Multiplayer_Death");
            else
                DrawSingleElement("Kill_Slugcat");
            return;
        }

        icon.isVisible = false;
        slugIcon.DrawScugSprites(drawDead: dead);
        slugIcon.container.alpha = dead ? 0.5f : 1;
    }

    public void DrawSingleElement(string elementName)
    {
        if (currentElementName == elementName)
            return;
        currentElementName = elementName;

        slugIcon.ClearSprites();
        icon.isVisible = true;
        icon.SetElementByName(elementName);
        icon.scale = elementName == "meadowcoin" ? 0.08f : 1f;
    }

    public void RemoveFromContainer()
    {
        RainMeadow.Debug("REMOVING PLAYERICON FROM CONTAINER");
        slugIcon.RemoveFromContainer();
        icon.RemoveFromContainer();
        container.RemoveFromContainer();
    }
}

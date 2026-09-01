using Menu;
using Menu.Remix;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RainMeadow.UI.Components.OnlineSlugcatAbilitiesInterface;
using static RainMeadow.UI.Components.TabContainer;
using static Menu.Menu;
using System;
using HarmonyLib;

namespace RainMeadow.UI.Components;

public class OnlineSettingTab : OnlineSettingElement
{
    private const float iconSize = 24;
    private const float elementSpacing = 10;
    public readonly SettingsTabData config;
    public bool folded = false;
    public PositionedMenuObject icon;
    public MenuLabel label;
    public FSprite divider;
    public TabButton tabButton;
    public string name = "";
    public Color tabColor = Color.gray;

    public OnlineSettingTab(Menu.Menu menu, MenuObject owner, SettingsTabData config)
         : base(menu, owner, null)
    {
        this.config = config;
        if (config.slugcatIcon is not null)
        {
            icon = new PositionedSlugIcon(menu, this, Vector2.zero, config.slugcatIcon.value);
        }
        else
        {
            icon = new MenuIllustration(menu, this, "", config.icon, Vector2.zero, true, true);
        }

        if (config.name is not null && config.color is not null)
        {
            name = config.name;
            tabColor = (Color)config.color;
        }
        else if (config.slugcatIcon is not null)
        {
            name = SlugcatStats.getSlugcatName(config.slugcatIcon);
            tabColor = Color.HSVToRGB(
                PlayerGraphics.DefaultSlugcatColor(config.slugcatIcon).ToHSL().hue,
                PlayerGraphics.DefaultSlugcatColor(config.slugcatIcon).ToHSL().saturation < 0.1f ? 0 : 0.75f,
                1f
            );
        }

        label = new(
            menu,
            this,
            menu.Translate(name),
            Vector2.zero,
            new(textSpacing, elementHeight),
        true);

        divider = new("pixel", false){
            anchorX = 0f,
            scaleX = 200f,
            scaleY = 2f,
            color = tabColor,
        };

        tabButton = new(menu, this, elementHeight);

        this.SafeAddSubobjects(icon, label, tabButton);
        Container.AddChild(divider);
    }

    public override void Update()
    {
        base.Update();

        tabButton.open = !folded;

        tabButton.pos = Vector2.left * Mathf.Min(margin - 5, tabButton.size.x);
        icon.pos = tabButton.pos + Vector2.right * tabButton.size.x;
        label.pos = icon.pos + Vector2.right * (iconSize + elementSpacing);
        label.size.x = label.label.textRect.width + elementSpacing;
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        if (icon is MenuIllustration illustration)
        {
            illustration.sprite.isVisible = visible;
            illustration.sprite.alpha = currentAlpha;
        }
        else if (icon is PositionedSlugIcon slugicon)
        {
            slugicon.slugIcon.sprites.Do(x =>
            {
                x.isVisible = visible;
                x.alpha = currentAlpha;
            });
        }

        label.label.alpha = (grayedOut ? 0.5f : 1f) * currentAlpha;

        tabButton.arrowSprite.alpha = (grayedOut ? 0.25f : folded ? 0.5f : 0.75f) * currentAlpha;

        float spacing = label.pos.x + label.size.x + elementSpacing;
        divider.isVisible = visible;
        divider.alpha = (grayedOut ? 0.5f : 1f) * currentAlpha;
        divider.x = DrawX(timeStacker) + spacing;
        divider.y = DrawY(timeStacker) + elementSize.y/2f;
        divider.scaleX = Mathf.Max(0, settingsBoxSize.x - spacing);
        divider.color = tabColor;
    }
    public override void Singal(MenuObject sender, string message)
    {
        if (sender == tabButton)
        {
            folded = !folded;
            menu.PlaySound(folded ? SoundID.MENU_Checkbox_Uncheck : SoundID.MENU_Checkbox_Check);
        }
    }
    public override void RemoveSprites()
    {
        base.RemoveSprites();
        divider?.RemoveFromContainer();
    }

    public class TabButton : BigSimpleButton
    {
        public FSprite arrowSprite;
        public bool open = true;
        public TabButton(Menu.Menu menu, OnlineSettingTab owner, float size)
             : base(menu, owner, "", "", Vector2.zero, new Vector2(size, size), FLabelAlignment.Left, false)
        {
            roundedRect.RemoveSprites();
            selectRect.RemoveSprites();
            arrowSprite = new("Menu_Symbol_Arrow")
            {
                rotation = 180,
                anchorX = 0.5f,
                anchorY = 0.5f,
                scale = 0.5f
            };
            Container.AddChild(arrowSprite);
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            arrowSprite.x = DrawX(timeStacker) + size.x/2f;
            arrowSprite.y = DrawY(timeStacker) + size.y/2f;
            arrowSprite.rotation = open ? 180 : 90;
        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            arrowSprite.RemoveFromContainer();
        }
    }
}
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

public class SlugcatSettingTab : SlugcatSettingElement
{
    private const float iconSize = 24;
    private const float iconSpacing = 10;
    private const float labelSpacing = 10;
    private const float dividerSpacing = iconSpacing;
    public readonly SlugcatSettingsTabData config;
    public bool folded = false;
    public PositionedMenuObject icon;
    public MenuLabel label;
    public FSprite divider;
    public string name = "";
    public Color tabColor = Color.gray;

    public SlugcatSettingTab(Menu.Menu menu, OnlineSlugcatSettingsBase owner, string name, string icon, Color color)
         : this(menu, owner, new SlugcatSettingsTabData(name, icon, color)) {}
    public SlugcatSettingTab(Menu.Menu menu, OnlineSlugcatSettingsBase owner, string name, SlugcatStats.Name slugcatIcon, Color color)
         : this(menu, owner, new SlugcatSettingsTabData(name, slugcatIcon, color)) {}
    public SlugcatSettingTab(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SlugcatStats.Name slugcat)
         : this(menu, owner, new SlugcatSettingsTabData(slugcat)) {}
    public SlugcatSettingTab(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SlugcatSettingsTabData config)
         : base(menu, owner, null)
    {
        this.config = config;
        if (config.slugcatIcon is not null)
        {
            this.icon = new PositionedSlugIcon(menu, this, Vector2.zero, config.slugcatIcon.value);
        }
        else
        {
            this.icon = new MenuIllustration(menu, this, "", config.icon, Vector2.zero, true, true);
        }

        if (config.name is not null && config.color is not null)
        {
            this.name = config.name;
            this.tabColor = (Color)config.color;
        }
        else if (config.slugcatIcon is not null)
        {
            this.name = SlugcatStats.getSlugcatName(config.slugcatIcon);
            this.tabColor = Color.HSVToRGB(
                PlayerGraphics.DefaultSlugcatColor(config.slugcatIcon).ToHSL().hue,
                PlayerGraphics.DefaultSlugcatColor(config.slugcatIcon).ToHSL().saturation < 0.1f ? 0 : 0.75f,
                1f
            );
        }

        this.label = new(
            menu,
            this,
            menu.Translate(name),
            Vector2.zero,
            new(settingPage.textSpacing, elementHeight),
        true);
        this.divider = new("pixel", false){
            anchorX = 0f,
            scaleX = 200f,
            scaleY = 2f,
            color = tabColor,
        };

        this.SafeAddSubobjects(icon, label);
        this.Container.AddChild(divider);
    }

    public override void Update()
    {
        base.Update();
        this.icon.pos = Vector2.zero;
        this.label.pos = Vector2.right * (iconSize + iconSpacing);
        this.label.size.x = label.label.textRect.width + labelSpacing;
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        if (icon is MenuIllustration illustration) illustration.sprite.isVisible = visible;
        else if (icon is PositionedSlugIcon slugicon) slugicon.slugIcon.sprites.Do(x => x.isVisible = visible);
        divider.isVisible = visible;

        float spacing = iconSize + iconSpacing + label.size.x + dividerSpacing;
        divider.x = DrawX(timeStacker) + spacing;
        divider.y = DrawY(timeStacker) + elementSize.y/2f;
        divider.scaleX = Mathf.Max(0, settingPage.settingsBoxSize.x - spacing);
        divider.color = tabColor;
    }
    public override void RemoveSprites()
    {
        base.RemoveSprites();
        this.divider?.RemoveFromContainer();
    }
}
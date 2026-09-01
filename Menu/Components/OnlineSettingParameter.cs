using Menu;
using Menu.Remix;
using UnityEngine;

namespace RainMeadow.UI.Components;

public abstract class OnlineSettingParameter : OnlineSettingElement
{
    public readonly SettingsConfigData config;
    public MenuLabel label;
    public MenuTabWrapper tabWrapper;
    public Color? color;
    public bool isClient;

    public OnlineSettingParameter(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SettingsConfigData config, OnlineSettingTab? tab = null, bool isClient = false)
         : this(menu, owner, owner.tabWrapper, config, tab, isClient) {}
    public OnlineSettingParameter(Menu.Menu menu, MenuObject owner, MenuTabWrapper tabWrapper, SettingsConfigData config, OnlineSettingTab? tab = null, bool isClient = false)
         : base(menu, owner, tab)
    {
        this.config = config;
        this.tabWrapper = tabWrapper;
        this.isClient = isClient;
        label = new(
            menu,
            this,
            menu.Translate(config.name + ":"),
            Vector2.zero,
            new(textSpacing, 30),
            false
        );
        label.label.alignment = FLabelAlignment.Left;

        this.SafeAddSubobjects(label);
    }
    public override void Update()
    {
        base.Update();

        label.pos = Vector2.left * textSpacing/2f;
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        if (color is not null) label.label.color = (Color)color;
        label.label.alpha = currentAlpha;
        label.label.isVisible = visible;
    }
    public abstract void SaveOption(bool clientOption = false);
    public abstract void SyncValueToAttribute();
    public abstract void ResetValueToDefault();
}
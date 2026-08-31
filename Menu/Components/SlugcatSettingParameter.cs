using Menu;
using UnityEngine;

namespace RainMeadow.UI.Components;

public abstract class SlugcatSettingParameter : SlugcatSettingElement
{
    public readonly SlugcatSettingsConfigData config;
    public MenuLabel label;
    public Color? color;
    public bool isClient;

    public SlugcatSettingParameter(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SlugcatSettingsConfigData config, SlugcatSettingTab? tab = null, bool isClient = false)
         : base(menu, owner, tab)
    {
        this.config = config;
        this.isClient = isClient;
        this.label = new(
            menu,
            this,
            menu.Translate(config.name + ":"),
            Vector2.zero,
            new(settingPage.textSpacing, 30),
            false
        );
        this.label.label.alignment = FLabelAlignment.Left;

        this.SafeAddSubobjects(label);
    }
    public override void Update()
    {
        base.Update();
        this.label.pos = Vector2.left * settingPage.textSpacing/2f;
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        if (color is not null) label.label.color = (Color)color;
    }
    public abstract void SaveOption(bool clientOption = false);
    public abstract void SyncValueToAttribute();
}
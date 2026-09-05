using Menu;
using Menu.Remix;
using UnityEngine;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components.Patched;

namespace RainMeadow.UI.Components;
public abstract class OnlineSettingUIconfig : OnlineSettingConfigurable
{
    public override MenuObject selectable => uiConfig.wrapper;
    public override object Value => uiConfig.value;
    public readonly UIconfig uiConfig;
    public virtual float BoxSize
    {
        get => uiConfig.size.x;
        set => uiConfig.size = new Vector2(value, uiConfig.size.y);
    }
    public OnlineSettingUIconfig(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SettingsConfigData config, UIconfig uiConfig, OnlineSettingTab? tab = null)
         : this(menu, owner, owner.tabWrapper, config, uiConfig, tab) {}
    public OnlineSettingUIconfig(Menu.Menu menu, MenuObject owner, MenuTabWrapper tabWrapper, SettingsConfigData config, UIconfig uiConfig, OnlineSettingTab? tab = null)
         : base(menu, owner, tabWrapper, config, tab)
    {
        this.uiConfig = uiConfig;
        if (!string.IsNullOrWhiteSpace(data.description))
        {
            uiConfig.description = menu.Translate(data.description);
        }
        uiConfig.OnValueUpdate += (uiConfig, value, lastValue) => SyncValueToAttribute();
        new PatchedUIelementWrapper(tabWrapper, uiConfig);

        uiConfig.Change();
    }

    protected abstract void ShowSyncInUIConfig(bool grayedOut, object value);
    protected void HandleRectAlpha(DyeableRect? dyeableRect)
    {
        if (dyeableRect is not null)
        {
            int[] hiddenSides = dyeableRect.SideSprites();
            for (int i = 0; i < dyeableRect.sprites.Length; i++)
            {
                if (dyeableRect._filled && i < 9)
                {
                    dyeableRect.sprites[i].alpha *= currentAlpha;
                }
                else
                {
                    dyeableRect.sprites[i].alpha = currentAlpha;
                }
                dyeableRect.sprites[i].isVisible = visible && !dyeableRect.isHidden;
            }
            for (int i = 0; i < hiddenSides.Length; i++)
            {
                dyeableRect.sprites[hiddenSides[i]].isVisible = false;
            }
        }
    }
    public override void Update()
    {
        base.Update();

        uiConfig.pos = pos
            + Vector2.right * (elementSize.x - uiConfig.size.x - BoxMargin)
            + Vector2.up * (elementSize.y - uiConfig.size.y)/2f;
        if (data.AttributeValue is not object value) return;
        if (!visible) return;
        if (isClient) SyncValueToAttribute();
        ShowSyncInUIConfig(grayedOut, value);
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        if (!visible && uiConfig.held) uiConfig.held = false;
        uiConfig.Hidden = !visible;
    }
    public override void ResetValueToDefault()
    {
        uiConfig.value = DefaultValue;
    }

    public override void SaveOption(bool clientOption = false)
    {
        if (!clientOption || isClient)
        {
            data.configurable.BoxedValue = uiConfig.value;
        }
    }

    public override void SyncValueToAttribute()
    {
        data.AttributeValue = uiConfig.value;
    }
}

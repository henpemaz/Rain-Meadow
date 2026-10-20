using Menu;
using Menu.Remix;
using UnityEngine;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components.Patched;
using Menu.Remix.MixedUI.ValueTypes;

namespace RainMeadow.UI.Components.Configurables;
public class OnlineSettingUIconfig : OnlineSettingConfigurable
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
         : this(menu, owner.scroller, owner.tabWrapper, config, uiConfig, tab) {}
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

    protected virtual void ShowSyncInUIConfig(bool grayedOut, object value)
    {
        uiConfig.greyedOut = grayedOut;

        if (uiConfig is OpTextBox textBox)
            textBox.held = textBox._KeyboardOn;

        if (!uiConfig.held)
        {
            if (uiConfig is OpCheckBox checkBox && value is bool b)
            {
                checkBox.SetValueBool(b);
            }
            else
            {
                uiConfig.value = value.ToString();
            }
        }
    }
    public override void Update()
    {
        base.Update();

        uiConfig.pos = pos
            + Vector2.right * (size.x - uiConfig.size.x - BoxMargin)
            + Vector2.up * (size.y - uiConfig.size.y)/2f;
        if (data.AttributeValue is not object value) return;
        if (!visible) return;
        // if (isClient) SyncValueToAttribute();
        ShowSyncInUIConfig(grayedOut, value);
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        if (!visible && uiConfig.held) uiConfig.held = false;
        uiConfig.Hidden = !visible;

        uiConfig.myContainer.alpha = currentAlpha;
        uiConfig.myContainer.isVisible = visible;

        if (color is Color c) label.label.color = c;
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

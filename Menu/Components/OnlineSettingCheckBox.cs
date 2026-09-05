using Menu;
using Menu.Remix;
using UnityEngine;
using static RainMeadow.UI.Components.OnlineSlugcatAbilitiesInterface;
using HarmonyLib;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;

namespace RainMeadow.UI.Components;
public class OnlineSettingCheckBox : OnlineSettingUIconfig
{
    public readonly bool defaultValue;
    public bool valueBool => checkBox.GetValueBool();
    public OpCheckBox checkBox => (OpCheckBox)uiConfig;
    public override float BoxSize { get => base.BoxSize; set{} } // no setting the box size
    public string? altDescription;
    public OnlineSettingCheckBox(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SettingsConfigData config, OnlineSettingTab? tab = null)
         : this(menu, owner, owner.tabWrapper, config, tab) {}
    public OnlineSettingCheckBox(Menu.Menu menu, MenuObject owner, MenuTabWrapper tabWrapper, SettingsConfigData config, OnlineSettingTab? tab = null)
         : base(menu, owner, tabWrapper, config, new OpCheckBox(new Configurable<bool>((bool)config.configurable.BoxedValue), Vector2.zero), tab)
    {
        defaultValue = ValueConverter.ConvertToValue<bool>(config.configurable.defaultValue);
        checkBox.OnChange += () =>
        {
            if (!string.IsNullOrWhiteSpace(config.description) && !string.IsNullOrWhiteSpace(altDescription))
            {
                checkBox.description = menu.Translate(
                    defaultValue == checkBox.GetValueBool()
                        ? config.description
                        : altDescription);
            }
        };
    }

    protected override void ShowSyncInUIConfig(bool grayedOut, object value)
        => ShowSyncInRemixCheckbox(checkBox, grayedOut, (bool)value);

    public override void GrafUpdate(float timeStacker)
    {
        if (color is not null) checkBox.colorEdge = (Color)color;
        base.GrafUpdate(timeStacker);
        label.label.color = checkBox.rect.colorEdge;

        checkBox.symbolSprite.isVisible = visible;
        checkBox.symbolSprite.alpha *= currentAlpha;
        HandleRectAlpha(checkBox.rect);
    }
    public override void SyncValueToAttribute()
    {
        data.AttributeValue = valueBool;
    }
}

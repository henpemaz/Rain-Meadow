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
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components.Patched;
using Menu.Remix.MixedUI.ValueTypes;

namespace RainMeadow.UI.Components;
public class OnlineSettingCheckBox : OnlineSettingConfigurable
{
    public const float checkBoxMargin = 5;
    public readonly bool defaultValue;
    public override MenuObject selectable => checkBox.wrapper;
    public override object Value => valueBool;
    public bool valueBool => checkBox.GetValueBool();
    public OpCheckBox checkBox;
    public string? altDescription;
    public OnlineSettingCheckBox(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SettingsConfigData config, OnlineSettingTab? tab = null)
         : this(menu, owner, owner.tabWrapper, config, tab) {}
    public OnlineSettingCheckBox(Menu.Menu menu, MenuObject owner, MenuTabWrapper tabWrapper, SettingsConfigData config, OnlineSettingTab? tab = null)
         : base(menu, owner, tabWrapper, config, tab)
    {
        defaultValue = ValueConverter.ConvertToValue<bool>(config.configurable.defaultValue);
        checkBox = new(new Configurable<bool>((bool)config.configurable.BoxedValue), Vector2.zero);
        if (!string.IsNullOrWhiteSpace(config.description))
        {
            checkBox.description = menu.Translate(config.description);
        }
        checkBox.OnChange += () =>
        {
            SyncValueToAttribute();
            if (!string.IsNullOrWhiteSpace(config.description) && !string.IsNullOrWhiteSpace(altDescription))
            {
                checkBox.description = menu.Translate(
                    defaultValue == checkBox.GetValueBool()
                        ? config.description
                        : altDescription);
            }
        };
        new PatchedUIelementWrapper(tabWrapper, checkBox);

        checkBox.Change(); // update desc
    }

    public override void Update()
    {
        base.Update();
        checkBox.pos = pos
            + Vector2.right * (elementSize.x - checkBox.size.x - checkBoxMargin)
            + Vector2.up * (elementSize.y - checkBox.size.y)/2f;

        if (!visible) return;
        if (data.AttributeValue is not bool value) return;

        if (isClient) SyncValueToAttribute();
        ShowSyncInRemixCheckbox(checkBox, grayedOut, value);
    }
    public override void GrafUpdate(float timeStacker)
    {
        if (color is not null) checkBox.colorEdge = (Color)color;
        base.GrafUpdate(timeStacker);
        label.label.color = checkBox.rect.colorEdge;

        checkBox.Hidden = !visible;
        checkBox.symbolSprite.isVisible = visible;
        checkBox.symbolSprite.alpha *= currentAlpha;
        checkBox.rect.sprites.Do(x =>
        {
            x.isVisible = visible;
            x.alpha = currentAlpha;
        });
    }
    public override void ResetValueToDefault()
    {
        checkBox.value = data.configurable.defaultValue;
    }

    public override void SaveOption(bool clientOption = false)
    {
        if (!clientOption || isClient)
        {
            data.configurable.BoxedValue = checkBox.GetValueBool();
        }
    }

    public override void SyncValueToAttribute()
    {
        data.AttributeValue = checkBox.GetValueBool();
    }
}

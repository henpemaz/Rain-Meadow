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

namespace RainMeadow.UI.Components;
public abstract class OnlineSettingTextBoxValue : OnlineSettingConfigurable
{
    public const float textBoxMargin = 5;
    public override MenuObject selectable => textBox.wrapper;
    public OpTextBox textBox;
    public float TextBoxSize
    {
        get => textBox.size.x;
        set => textBox.size = new Vector2(value, textBox.size.y);
    }
    public OnlineSettingTextBoxValue(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SettingsConfigData config, OnlineSettingTab? tab = null)
         : this(menu, owner, owner.tabWrapper, config, tab) {}
    public OnlineSettingTextBoxValue(Menu.Menu menu, MenuObject owner, MenuTabWrapper tabWrapper, SettingsConfigData config, OnlineSettingTab? tab = null)
         : base(menu, owner, tabWrapper, config, tab)
    {
        textBox = new(new Configurable<int>(40), Vector2.zero, 40);
    }
    protected void InitTextBox()
    {
        if (!string.IsNullOrWhiteSpace(data.description))
        {
            textBox.description = menu.Translate(data.description);
        }
        textBox.OnValueUpdate += (uiConfig, value, lastValue) => SyncValueToAttribute();
        new PatchedUIelementWrapper(tabWrapper, textBox);
    }
    public override void Update()
    {
        base.Update();

        textBox.pos = pos
            + Vector2.right * (elementSize.x - textBox.size.x - textBoxMargin)
            + Vector2.up * (elementSize.y - textBox.size.y)/2f;
        if (data.AttributeValue is not object value) return;

        if (!visible) return;
        if (isClient) SyncValueToAttribute();
        ShowSyncInTextbox(textBox, grayedOut, value);
    }
    public override void GrafUpdate(float timeStacker)
    {
        if (color is not null) textBox.colorEdge = (Color)color;

        base.GrafUpdate(timeStacker);

        label.label.color = textBox.rect.colorEdge;

        textBox.Hidden = !visible;
        textBox.label.isVisible = visible;
        textBox.label.alpha = currentAlpha;
        textBox._cursor.isVisible = visible;
        textBox._cursor.alpha *= currentAlpha;
        textBox.rect.sprites.Do(x =>
        {
            x.isVisible = visible;
            x.alpha = currentAlpha;
        });
    }
    public override void ResetValueToDefault()
    {
        textBox.value = data.configurable.defaultValue;
    }
}

public class OnlineSettingIntValue : OnlineSettingTextBoxValue
{
    public override object Value => valueInt;
    public int valueInt => textBox.valueInt;
    public OnlineSettingIntValue(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SettingsConfigData config, OnlineSettingTab? tab = null)
         : this(menu, owner, owner.tabWrapper, config, tab) {}
    public OnlineSettingIntValue(Menu.Menu menu, MenuObject owner, MenuTabWrapper tabWrapper, SettingsConfigData config, OnlineSettingTab? tab = null)
         : base(menu, owner, tabWrapper, config, tab)
    {
        textBox = new(new Configurable<int>((int)config.configurable.BoxedValue), Vector2.zero, 40)
        {
            alignment = FLabelAlignment.Center,
            accept = OpTextBox.Accept.Int
        };
        InitTextBox();
    }
    public override void SyncValueToAttribute()
    {
        data.AttributeValue = textBox.valueInt;
    }
    public override void SaveOption(bool clientOption = false)
    {
        if (!clientOption || isClient)
        {
            data.configurable.BoxedValue = textBox.valueInt;
        }
    }
}

public class OnlineSettingFloatValue : OnlineSettingTextBoxValue
{
    public override object Value => valueFloat;
    public float valueFloat => textBox.valueFloat;
    public OnlineSettingFloatValue(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SettingsConfigData config, OnlineSettingTab? tab = null)
         : this(menu, owner, owner.tabWrapper, config, tab) {}
    public OnlineSettingFloatValue(Menu.Menu menu, MenuObject owner, MenuTabWrapper tabWrapper, SettingsConfigData config, OnlineSettingTab? tab = null)
         : base(menu, owner, tabWrapper, config, tab)
    {
        textBox = new(new Configurable<float>((float)config.configurable.BoxedValue), Vector2.zero, 60)
        {
            alignment = FLabelAlignment.Center,
            accept = OpTextBox.Accept.Float
        };
        InitTextBox();
    }
    public override void SyncValueToAttribute()
    {
        data.AttributeValue = textBox.valueFloat;
    }
    public override void SaveOption(bool clientOption = false)
    {
        if (!clientOption || isClient)
        {
            data.configurable.BoxedValue = textBox.valueFloat;
        }
    }
}

public class OnlineSettingStringValue : OnlineSettingTextBoxValue
{
    public override object Value => valueString;
    public string valueString => textBox.value;
    public OnlineSettingStringValue(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SettingsConfigData config, OnlineSettingTab? tab = null)
         : this(menu, owner, owner.tabWrapper, config, tab) {}
    public OnlineSettingStringValue(Menu.Menu menu, MenuObject owner, MenuTabWrapper tabWrapper, SettingsConfigData config, OnlineSettingTab? tab = null)
         : base(menu, owner, tabWrapper, config, tab)
    {
        textBox = new(new Configurable<string>((string)config.configurable.BoxedValue), Vector2.zero, 120)
        {
            alignment = FLabelAlignment.Center,
            accept = OpTextBox.Accept.StringEng
        };
        InitTextBox();
    }
    public override void SyncValueToAttribute()
    {
        data.AttributeValue = textBox.value;
    }
    public override void SaveOption(bool clientOption = false)
    {
        if (!clientOption || isClient)
        {
            data.configurable.BoxedValue = textBox.value;
        }
    }
}
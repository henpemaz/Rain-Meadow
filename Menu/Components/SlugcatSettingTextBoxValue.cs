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
public abstract class SlugcatSettingTextBoxValue : SlugcatSettingParameter
{
    public const float textBoxMargin = 5;
    public OpTextBox textBox;
    public float TextBoxSize
    {
        get => textBox.size.x;
        set => textBox.size = new Vector2(value, textBox.size.y);
    }
    public SlugcatSettingTextBoxValue(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SlugcatSettingsConfigData config, SlugcatSettingTab? tab = null)
         : base(menu, owner, config, tab)
    {
        textBox = new(new Configurable<int>(40), Vector2.zero, 40);
    }
    protected void InitTextBox()
    {
        if (!string.IsNullOrWhiteSpace(config.description))
        {
            textBox.description = menu.Translate(config.description);
        }
        textBox.OnValueUpdate += (uiConfig, value, lastValue) => SyncValueToAttribute();
        new PatchedUIelementWrapper(settingPage.tabWrapper, textBox);
    }
    public override void Update()
    {
        base.Update();
        this.textBox.pos = pos
            + Vector2.right * (elementSize.x - textBox.size.x - textBoxMargin)
            + Vector2.up * (elementSize.y - textBox.size.y)/2f;

        if (settingPage.IsActuallyHidden) return;
        if (config.AttributeValue is not object value) return;

        if (isClient) SyncValueToAttribute();
        ShowSyncInTextbox(textBox, settingPage.SettingsDisabled, value);
    }
    public override void GrafUpdate(float timeStacker)
    {
        if (color is not null) textBox.colorEdge = (Color)color;
        base.GrafUpdate(timeStacker);
        label.label.color = textBox.rect.colorEdge;
    }
}

public class SlugcatSettingIntValue : SlugcatSettingTextBoxValue
{
    public SlugcatSettingIntValue(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SlugcatSettingsConfigData config, SlugcatSettingTab? tab = null)
         : base(menu, owner, config, tab)
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
        config.AttributeValue = textBox.valueInt;
    }
    public override void SaveOption(bool clientOption = false)
    {
        if (!clientOption || isClient)
        {
            config.configurable.BoxedValue = textBox.valueInt;
        }
    }
}

public class SlugcatSettingFloatValue : SlugcatSettingTextBoxValue
{
    public SlugcatSettingFloatValue(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SlugcatSettingsConfigData config, SlugcatSettingTab? tab = null)
         : base(menu, owner, config, tab)
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
        config.AttributeValue = textBox.valueFloat;
    }
    public override void SaveOption(bool clientOption = false)
    {
        if (!clientOption || isClient)
        {
            config.configurable.BoxedValue = textBox.valueFloat;
        }
    }
}

public class SlugcatSettingStringValue : SlugcatSettingTextBoxValue
{
    public SlugcatSettingStringValue(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SlugcatSettingsConfigData config, SlugcatSettingTab? tab = null)
         : base(menu, owner, config, tab)
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
        config.AttributeValue = textBox.value;
    }
    public override void SaveOption(bool clientOption = false)
    {
        if (!clientOption || isClient)
        {
            config.configurable.BoxedValue = textBox.value;
        }
    }
}
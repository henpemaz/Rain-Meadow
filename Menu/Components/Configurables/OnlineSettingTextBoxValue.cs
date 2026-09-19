using Menu;
using Menu.Remix;
using UnityEngine;
using static RainMeadow.UI.Components.OnlineSlugcatAbilitiesInterface;
using Menu.Remix.MixedUI;
using System;

namespace RainMeadow.UI.Components.Configurables;
public abstract class OnlineSettingTextBoxValue : OnlineSettingUIconfig
{
    public OpTextBox textBox => (OpTextBox)uiConfig;
    public OnlineSettingTextBoxValue(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SettingsConfigData config, OpTextBox textBox, OnlineSettingTab? tab = null)
         : this(menu, owner, owner.tabWrapper, config, textBox, tab) {}
    public OnlineSettingTextBoxValue(Menu.Menu menu, MenuObject owner, MenuTabWrapper tabWrapper, SettingsConfigData config, OpTextBox textBox, OnlineSettingTab? tab = null)
         : base(menu, owner, tabWrapper, config, textBox, tab) {}

    public override void GrafUpdate(float timeStacker)
    {
        if (color is not null) textBox.colorEdge = (Color)color;
        base.GrafUpdate(timeStacker);
        label.label.color = textBox.rect.colorEdge;
    }
}

public class OnlineSettingIntValue : OnlineSettingTextBoxValue
{
    public int valueInt => textBox.valueInt;
    public OnlineSettingIntValue(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SettingsConfigData config, OnlineSettingTab? tab = null)
         : this(menu, owner.scroller, owner.tabWrapper, config, tab) {}
    public OnlineSettingIntValue(Menu.Menu menu, MenuObject owner, MenuTabWrapper tabWrapper, SettingsConfigData config, OnlineSettingTab? tab = null)
         : base(
            menu,
            owner,
            tabWrapper,
            config,
            new(new Configurable<int>((int)config.configurable.BoxedValue), Vector2.zero, 40)
            {
                alignment = FLabelAlignment.Center,
                accept = OpTextBox.Accept.Int
            },
            tab) {}
}

public class OnlineSettingFloatValue : OnlineSettingTextBoxValue
{
    public int? roundoffDecimals;
    public bool needsRoundOff = false;
    public float valueFloat => textBox.valueFloat;
    public OnlineSettingFloatValue(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SettingsConfigData config, OnlineSettingTab? tab = null)
         : this(menu, owner.scroller, owner.tabWrapper, config, tab) {}
    public OnlineSettingFloatValue(Menu.Menu menu, MenuObject owner, MenuTabWrapper tabWrapper, SettingsConfigData config, OnlineSettingTab? tab = null)
         : base(
            menu,
            owner,
            tabWrapper,
            config,
            new(new Configurable<float>((float)config.configurable.BoxedValue), Vector2.zero, 60)
            {
                alignment = FLabelAlignment.Center,
                accept = OpTextBox.Accept.Float
            },
            tab) {}

    public void RoundOff()
    {
        // decimal roundoff
        if (roundoffDecimals is int r)
        {
            textBox.valueFloat = (float)Math.Round(textBox.valueFloat, r);
        }
    }

    protected override void ShowSyncInUIConfig(bool grayedOut, object value)
    {
        base.ShowSyncInUIConfig(grayedOut, value);
        if (!uiConfig.held && needsRoundOff)
        {
            needsRoundOff = false;
            RoundOff();
        }
    }
    public override void SyncValueToAttribute()
    {
        if (roundoffDecimals is not null)
        {
            // don't override someone typing
            if (textBox.held) needsRoundOff = true;
            else RoundOff();
        }

        base.SyncValueToAttribute();
    }
}

public class OnlineSettingStringValue : OnlineSettingTextBoxValue
{
    public string valueString => textBox.value;
    public OnlineSettingStringValue(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SettingsConfigData config, OnlineSettingTab? tab = null)
         : this(menu, owner.scroller, owner.tabWrapper, config, tab) {}
    public OnlineSettingStringValue(Menu.Menu menu, MenuObject owner, MenuTabWrapper tabWrapper, SettingsConfigData config, OnlineSettingTab? tab = null)
         : base(
            menu,
            owner,
            tabWrapper,
            config,
            new(new Configurable<string>((string)config.configurable.BoxedValue), Vector2.zero, 100)
            {
                alignment = FLabelAlignment.Center,
                accept = OpTextBox.Accept.StringEng
            },
            tab) {}
}
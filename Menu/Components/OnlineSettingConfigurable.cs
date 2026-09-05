using System;
using System.Collections.Generic;
using Menu;
using Menu.Remix;
using UnityEngine;

namespace RainMeadow.UI.Components;
public readonly struct SettingsConfigData
{
    internal static Dictionary<Type, Func<object?>> GetAttributeOwnerDict = [];
    public readonly string name;
    public readonly string attributeName;
    public readonly Type attributeOwnerType;
    public readonly ConfigurableBase configurable;
    public readonly string? tabName;
    public readonly SlugcatStats.Name? slugcatTab;
    public readonly string description;
    public readonly bool isClient;

    public static void AddNewGetAttributeOwnerFunction<T>(Func<T?> getAttributeOwnerFunc) where T : class
    {
        GetAttributeOwnerDict[typeof(T)] = getAttributeOwnerFunc;
    }
    static SettingsConfigData()
    {
        AddNewGetAttributeOwnerFunction(() => OnlineManager.lobby?.gameMode as ArenaOnlineGameMode);
        AddNewGetAttributeOwnerFunction(() => (OnlineManager.lobby?.gameMode as ArenaOnlineGameMode)?.arenaClientSettings);
    }

    public SettingsConfigData(string name, ConfigurableBase configurable, Type attributeOwnerType, string arenaOnlineAttributeName, string description = "", bool isClient = false)
    {
        this.name = name;
        attributeName = arenaOnlineAttributeName;
        this.configurable = configurable;
        this.description = description;
        this.attributeOwnerType = attributeOwnerType;
        this.isClient = isClient;
    }
    public SettingsConfigData(string name, ConfigurableBase configurable, string arenaOnlineAttributeName, string description = "", bool isClient = false)
         : this(name, configurable, typeof(ArenaOnlineGameMode), arenaOnlineAttributeName, description, isClient) {}


    public SettingsConfigData(string name, SlugcatStats.Name slugcat, ConfigurableBase configurable, Type attributeOwnerType, string arenaOnlineAttributeName, string description = "", bool isClient = false)
         : this(name, configurable, attributeOwnerType, arenaOnlineAttributeName, description, isClient)
    {
        slugcatTab = slugcat;
    }
    public SettingsConfigData(string name, SlugcatStats.Name slugcat, ConfigurableBase configurable, string arenaOnlineAttributeName, string description = "", bool isClient = false)
         : this(name, slugcat, configurable, typeof(ArenaOnlineGameMode), arenaOnlineAttributeName, description, isClient) {}

    public SettingsConfigData(string name, string tabName, ConfigurableBase configurable, Type attributeOwnerType, string arenaOnlineAttributeName, string description = "", bool isClient = false)
         : this(name, configurable, attributeOwnerType, arenaOnlineAttributeName, description, isClient)
    {
        this.tabName = tabName;
    }
    public SettingsConfigData(string name, string tabName, ConfigurableBase configurable, string arenaOnlineAttributeName, string description = "", bool isClient = false)
         : this(name, tabName, configurable, typeof(ArenaOnlineGameMode), arenaOnlineAttributeName, description, isClient) {}

    public readonly object? AttributeValue
    {
        get
        {
            if (string.IsNullOrWhiteSpace(attributeName)) return null;
            return GetAttributeOwnerDict[attributeOwnerType]() is object data
                ? attributeOwnerType.GetField(attributeName)?.GetValue(data)
                : null;
        }
        set
        {
            if (string.IsNullOrWhiteSpace(attributeName)) return;
            if (GetAttributeOwnerDict[attributeOwnerType]() is object data)
            {
                try
                {
                    attributeOwnerType.GetField(attributeName).SetValue(data, value is string strVal ? ValueConverter.ConvertToValue(strVal, AttributeType) : value);
                }
                catch (Exception ex)
                {
                    RainMeadow.Error($"Could not convert {value} into {attributeOwnerType.GetField(attributeName)?.FieldType} : {attributeOwnerType.Name}.{attributeOwnerType.GetField(attributeName)?.Name} \n" + ex);
                }
            }
        }
    }
    public readonly Type AttributeType => configurable.settingType;

    public static bool operator== (SettingsConfigData left, SettingsConfigData right)
    {
        return left.name == right.name
            && left.attributeName == right.attributeName
            && left.attributeOwnerType == right.attributeOwnerType
            && left.configurable == right.configurable;
    }
    public static bool operator!= (SettingsConfigData left, SettingsConfigData right)
    {
        return !(left == right);
    }
    public override bool Equals(object obj)
    {
        return obj is SettingsConfigData config && config == this;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
public abstract class OnlineSettingConfigurable : OnlineSettingElement
{
    public const float BoxMargin = 5;
    public readonly SettingsConfigData data;
    public MenuLabel label;
    public MenuTabWrapper tabWrapper;
    public Color? color;
    public string DefaultValue => data.configurable.defaultValue;
    public abstract object Value {get;}

    public OnlineSettingConfigurable(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SettingsConfigData data, OnlineSettingTab? tab = null)
         : this(menu, owner, owner.tabWrapper, data, tab) {}
    public OnlineSettingConfigurable(Menu.Menu menu, MenuObject owner, MenuTabWrapper tabWrapper, SettingsConfigData data, OnlineSettingTab? tab = null)
         : base(menu, owner, tab)
    {
        this.data = data;
        this.tabWrapper = tabWrapper;
        isClient = data.isClient;

        label = new(
            menu,
            this,
            menu.Translate(data.name + ":"),
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
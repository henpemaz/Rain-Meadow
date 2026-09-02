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

    public static void AddNewGetAttributeOwnerFunction<T>(Func<T?> getAttributeOwnerFunc) where T : class
    {
        GetAttributeOwnerDict[typeof(T)] = getAttributeOwnerFunc;
    }
    static SettingsConfigData()
    {
        AddNewGetAttributeOwnerFunction(() => OnlineManager.lobby?.gameMode as ArenaOnlineGameMode);
        AddNewGetAttributeOwnerFunction(() => (OnlineManager.lobby?.gameMode as ArenaOnlineGameMode)?.arenaClientSettings);
    }

    public SettingsConfigData(string name, ConfigurableBase configurable, Type attributeOwnerType, string arenaOnlineAttributeName, string description = "")
    {
        this.name = name;
        attributeName = arenaOnlineAttributeName;
        this.configurable = configurable;
        this.description = description;
        this.attributeOwnerType = attributeOwnerType;
    }
    public SettingsConfigData(string name, ConfigurableBase configurable, string arenaOnlineAttributeName, string description = "")
         : this(name, configurable, typeof(ArenaOnlineGameMode), arenaOnlineAttributeName, description) {}

    public SettingsConfigData(string name, ConfigurableBase configurable, Type attributeOwnerType, string arenaOnlineAttributeName, SlugcatStats.Name slugcat, string description = "")
         : this(name, configurable, attributeOwnerType, arenaOnlineAttributeName, description)
    {
        slugcatTab = slugcat;
    }
    public SettingsConfigData(string name, ConfigurableBase configurable, string arenaOnlineAttributeName, SlugcatStats.Name slugcat, string description = "")
         : this(name, configurable, typeof(ArenaOnlineGameMode), arenaOnlineAttributeName, slugcat, description) {}

    public SettingsConfigData(string name, ConfigurableBase configurable, Type attributeOwnerType, string arenaOnlineAttributeName, string tabName, string description = "")
         : this(name, configurable, attributeOwnerType, arenaOnlineAttributeName, description)
    {
        this.tabName = tabName;
    }
    public SettingsConfigData(string name, ConfigurableBase configurable, string arenaOnlineAttributeName, string tabName, string description = "")
         : this(name, configurable, typeof(ArenaOnlineGameMode), arenaOnlineAttributeName, tabName, description) {}

    public readonly object? AttributeValue
    {
        get
        {
            return GetAttributeOwnerDict[attributeOwnerType]() is object data
                ? attributeOwnerType.GetField(attributeName)?.GetValue(data)
                : null;
        }
        set
        {
            if (GetAttributeOwnerDict[attributeOwnerType]() is object data)
            {
                try
                {
                    attributeOwnerType.GetField(attributeName).SetValue(data, value);
                }
                catch (Exception ex)
                {
                    RainMeadow.Error($"Could not convert [{value}] into {attributeOwnerType.GetField(attributeName)?.FieldType} : {attributeOwnerType.Name}.{attributeOwnerType.GetField(attributeName)?.Name} \n" + ex);
                }
            }
        }
    }

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
    public readonly SettingsConfigData config;
    public MenuLabel label;
    public MenuTabWrapper tabWrapper;
    public Color? color;
    public bool isClient
    {
        get;
        set
        {
            field = value;
            if (value) tabIndependant = true;
        }
    }

    public OnlineSettingConfigurable(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SettingsConfigData config, OnlineSettingTab? tab = null, bool isClient = false)
         : this(menu, owner, owner.tabWrapper, config, tab, isClient) {}
    public OnlineSettingConfigurable(Menu.Menu menu, MenuObject owner, MenuTabWrapper tabWrapper, SettingsConfigData config, OnlineSettingTab? tab = null, bool isClient = false)
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
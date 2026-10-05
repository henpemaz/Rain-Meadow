using System;
using System.Collections.Generic;
using System.Reflection;
using Menu;
using Menu.Remix;
using UnityEngine;

namespace RainMeadow.UI.Components.Configurables;
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
         : this(menu, owner.scroller, owner.tabWrapper, data, tab) {}
    public OnlineSettingConfigurable(Menu.Menu menu, MenuObject owner, MenuTabWrapper tabWrapper, SettingsConfigData data, OnlineSettingTab? tab = null)
         : base(menu, owner, tab)
    {
        this.data = data;
        this.tabWrapper = tabWrapper;
        isClient = data.isClient;

        // try translating with and without the ":"
        if (!menu.TryTranslate(data.name + ":", out string trad)
            && menu.CurrLang != InGameTranslator.LanguageID.English)
        {
            trad = menu.Translate(data.name) + ":";
        }

        label = new(
            menu,
            this,
            trad,
            Vector2.zero,
            new(textSpacing, elementHeight),
            false
        );
        label.label.alignment = FLabelAlignment.Left;

        this.SafeAddSubobjects(label);
    }
    public override void Update()
    {
        base.Update();

        label.pos = Vector2.left * textSpacing / 2f;
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

    public readonly struct SettingsConfigData
    {
        private static readonly Dictionary<Type, Func<object?>> GetAttributeOwnerDict = [];
        public readonly string name;
        public readonly string attributeName;
        public readonly Type attributeOwnerType;
        public readonly ConfigurableBase configurable;
        public readonly string? tabName;
        public readonly SlugcatStats.Name? slugcatTab;
        public readonly string description;
        public readonly bool isClient;

        private readonly FieldInfo? attributeField;
        private readonly Func<object?>? getAttributeOwner;

        public static bool HasASupportedGetFunction(Type type) => GetAttributeOwnerDict.ContainsKey(type);
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

            FieldInfo? field = attributeOwnerType.GetField(arenaOnlineAttributeName);
            if (field is not null && field.FieldType == configurable.settingType)
            {
                attributeField = field;
            }
            else
            {
                if (field is null)
                    RainMeadow.Error($"Field {arenaOnlineAttributeName} of type {attributeOwnerType.Name} couldn't be found.");
                else
                    RainMeadow.Error($"Field {arenaOnlineAttributeName} of type {attributeOwnerType.Name} does not have the same type as the configurable ! Configurable has type {configurable.settingType} while field has type {field.FieldType}");
                RainMeadow.Error($"This SettingsConfigData of {attributeOwnerType.Name}.{arenaOnlineAttributeName} won't save the attribute !");
                attributeField = null;
            }

            if (GetAttributeOwnerDict.TryGetValue(attributeOwnerType, out var selector))
            {
                getAttributeOwner = selector;
            }
            else
            {
                RainMeadow.Error($"Type {attributeOwnerType.Name} does not have a Get function ! Please add a Get function with SettingsConfigData.AddNewGetAttributeOwnerFunction.");
                RainMeadow.Error($"This SettingsConfigData of {attributeOwnerType.Name}.{arenaOnlineAttributeName} won't save the attribute !");
                getAttributeOwner = null;
            }
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
                if (attributeField is null || getAttributeOwner is null) return null;
                return getAttributeOwner() is object data
                    ? attributeField.GetValue(data)
                    : null;
            }
            set
            {
                if (attributeField is null || getAttributeOwner is null) return;
                if (getAttributeOwner() is object data)
                {
                    object? converted;
                    if (value is string strVal)
                    {
                        if (AttributeType != typeof(string) && string.IsNullOrWhiteSpace(strVal)) return;
                        try
                        {
                            converted = ValueConverter.ConvertToValue(strVal, AttributeType);
                        }
                        catch (FormatException ex)
                        {
                            RainMeadow.Error($"Exception while formatting the value : {ex}");
                            return;
                        }
                    }
                    else
                    {
                        converted = value;
                    }

                    try
                    {
                        attributeOwnerType.GetField(attributeName).SetValue(data, converted);
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
}
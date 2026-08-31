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

namespace RainMeadow.UI.Components;

public readonly struct SlugcatSettingsConfigData
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
    static SlugcatSettingsConfigData()
    {
        AddNewGetAttributeOwnerFunction(() => OnlineManager.lobby?.gameMode as ArenaOnlineGameMode);
    }

    public SlugcatSettingsConfigData(string name, ConfigurableBase configurable, string arenaOnlineAttributeName, Type attributeOwnerType, string description = "")
    {
        this.name = name;
        this.attributeName = arenaOnlineAttributeName;
        this.configurable = configurable;
        this.description = description;
        this.attributeOwnerType = attributeOwnerType;
    }
    public SlugcatSettingsConfigData(string name, ConfigurableBase configurable, string arenaOnlineAttributeName, string description = "")
         : this(name, configurable, arenaOnlineAttributeName, typeof(ArenaOnlineGameMode), description) {}

    public SlugcatSettingsConfigData(string name, ConfigurableBase configurable, string arenaOnlineAttributeName, Type attributeOwnerType, SlugcatStats.Name slugcat, string description = "")
         : this(name, configurable, arenaOnlineAttributeName, attributeOwnerType, description)
    {
        this.slugcatTab = slugcat;
    }
    public SlugcatSettingsConfigData(string name, ConfigurableBase configurable, string arenaOnlineAttributeName, SlugcatStats.Name slugcat, string description = "")
         : this(name, configurable, arenaOnlineAttributeName, typeof(ArenaOnlineGameMode), slugcat, description) {}

    public SlugcatSettingsConfigData(string name, ConfigurableBase configurable, string arenaOnlineAttributeName, Type attributeOwnerType, string tabName, string description = "")
         : this(name, configurable, arenaOnlineAttributeName, attributeOwnerType, description)
    {
        this.tabName = tabName;
    }
    public SlugcatSettingsConfigData(string name, ConfigurableBase configurable, string arenaOnlineAttributeName, string tabName, string description = "")
         : this(name, configurable, arenaOnlineAttributeName, typeof(ArenaOnlineGameMode), tabName, description) {}

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

    public static bool operator== (SlugcatSettingsConfigData left, SlugcatSettingsConfigData right)
    {
        return left.name == right.name
            && left.attributeName == right.attributeName
            && left.attributeOwnerType == right.attributeOwnerType
            && left.configurable == right.configurable;
    }
    public static bool operator!= (SlugcatSettingsConfigData left, SlugcatSettingsConfigData right)
    {
        return !(left == right);
    }
    public override bool Equals(object obj)
    {
        return obj is SlugcatSettingsConfigData config && config == this;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
public readonly struct SlugcatSettingsTabData
{
    public readonly SlugcatStats.Name? slugcatIcon;
    public readonly string? name;
    public readonly string? icon;
    public readonly Color? color;
    public SlugcatSettingsTabData(SlugcatStats.Name slugcat)
    {
        this.slugcatIcon = slugcat;
    }
    public SlugcatSettingsTabData(string name, SlugcatStats.Name slugcatIcon, Color color)
    {
        this.slugcatIcon = slugcatIcon;
        this.name = name;
        this.color = color;
    }
    public SlugcatSettingsTabData(string name, string icon, Color color)
    {
        this.icon = icon;
        this.name = name;
        this.color = color;
    }

    public static bool operator== (SlugcatSettingsTabData left, SlugcatSettingsTabData right)
    {
        return left.name == right.name && left.slugcatIcon == right.slugcatIcon;
    }
    public static bool operator!= (SlugcatSettingsTabData left, SlugcatSettingsTabData right)
    {
        return !(left == right);
    }
    public override bool Equals(object obj)
    {
        return obj is SlugcatSettingsTabData tab && tab == this;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

public abstract class OnlineSlugcatSettingsBase : SettingsPage
{
    public const string RESETTODEFAULT = "RESETTODEFAULT";
    public static Vector2 defaultBoxSize = new(450, 430);
    public Vector2 settingsBoxSize;
    public float margin;
    public SimpleButton? backButton;
    public SimpleButton? resetButton;
    public MenuTabWrapper tabWrapper;
    protected List<SlugcatSettingElement> elements;
    public float spacing;
    public float textSpacing;

    public SlugcatSettingTab? GetSlugcatSettingTab(SlugcatStats.Name slugcatTab)
    {
        return elements.Find(x =>
            x is SlugcatSettingTab tab
            && tab.config.name is null
            && tab.config.slugcatIcon == slugcatTab)
        as SlugcatSettingTab;
    }
    public SlugcatSettingTab? GetSlugcatSettingTab(string tabName)
    {
        return elements.Find(x =>
            x is SlugcatSettingTab tab
            && tab.config.name == tabName)
        as SlugcatSettingTab;
    }
    public SlugcatSettingParameter? GetSlugcatSettingParameter(string paramName)
    {
        return elements.Find(x =>
            x is SlugcatSettingParameter param
            && param.config.name == paramName)
        as SlugcatSettingParameter;
    }
    public SlugcatSettingParameter? GetSlugcatSettingParameter(ConfigurableBase configurable)
    {
        return elements.Find(x =>
            x is SlugcatSettingParameter param
            && param.config.configurable == configurable)
        as SlugcatSettingParameter;
    }
    public SlugcatSettingParameter? GetSlugcatSettingParameter(string attributeName, Type attributeOwnerType)
    {
        return elements.Find(x =>
            x is SlugcatSettingParameter param
            && param.config.attributeName == attributeName
            && param.config.attributeOwnerType == attributeOwnerType)
        as SlugcatSettingParameter;
    }

    protected OnlineSlugcatSettingsBase(Menu.Menu menu, MenuObject owner, float spacing = 5f, float margin = 30f, float textSpacing = 300) : base(menu, owner)
    {
        tabWrapper = new(menu, this);
        elements = [];
        this.spacing = spacing;
        this.textSpacing = textSpacing;
        this.margin = margin;

        this.settingsBoxSize = defaultBoxSize - Vector2.right * margin * 2;
        this.SafeAddSubobjects(tabWrapper);
    }
    public override void SelectAndCreateBackButtons(SettingsPage? previousSettingPage, bool forceSelectedObject)
    {
        if (backButton is null)
        {
            backButton = new(menu, this, menu.Translate("BACK"), BACKTOSELECT, new(margin, 30), new(80, 30));
            AddObjects(backButton);
        }
        if (resetButton is null)
        {
            resetButton = new(menu, this, menu.Translate("RESET"), RESETTODEFAULT, new(settingsBoxSize.x - 40, 30), new(80, 30));
            AddObjects(resetButton);
        }
    }
    public override void Update()
    {
        base.Update();

        if (IsActuallyHidden) return;

        bool greyoutAll = SettingsDisabled;
        foreach (MenuObject obj in subObjects)
        {
            if (obj != backButton && obj is ButtonTemplate btn)
                btn.buttonBehav.greyedOut = greyoutAll;
        }
        UpdateElements();
    }

    public void UpdateElements()
    {
        int position = 0;
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i].visible) elements[i].position = position++;
            if (elements[i] is SlugcatSettingTab tab)
            {
                int j = i + 1;
                while (j < elements.Count && elements[i].tab == tab)
                {
                    if (elements[i].grayedOut && !elements[j].tabIndependant)
                        elements[j].grayedOut = true;
                    elements[j].visible = elements[i].visible && !tab.folded;
                    j++;
                }
            }
        }
    }

    public override void SaveInterfaceOptions()
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] is SlugcatSettingParameter param)
            {
                param.SaveOption();
            }
        }
    }
    public override void SaveInterfaceClientOptions()
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] is SlugcatSettingParameter param)
            {
                param.SaveOption(true);
            }
        }
    }
    public override void CallForSync()
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] is SlugcatSettingParameter param)
            {
                param.SyncValueToAttribute();
            }
        }
    }
}
public abstract class OnlineSlugcatSettings<T> : OnlineSlugcatSettingsBase where T : class
{
    protected static List<SlugcatSettingsConfigData> onlineConfigurables = [];
    protected static List<SlugcatSettingsTabData> onlineConfigurableTabs = [];

    public static void AddSlugcatSettingsTab(SlugcatSettingsTabData tab)
    {
        if (onlineConfigurableTabs.Exists(x => x == tab))
        {
            RainMeadow.Error($"Could not add online configurable tab {tab.name ?? tab.slugcatIcon?.value} : {tab.name ?? tab.slugcatIcon?.value} is already in the page !");
            return;
        }
        onlineConfigurableTabs.Add(tab);
    }
    public static void AddSlugcatSettingsConfigurable(SlugcatSettingsConfigData config)
    {
        if (onlineConfigurables.Exists(x => x.attributeName == config.attributeName && x.attributeOwnerType == config.attributeOwnerType))
        {
            RainMeadow.Error($"Could not add online configurable {config.name} : {config.attributeOwnerType.Name}.{config.attributeName} is already in the page !");
            return;
        }
        if (!SlugcatSettingsConfigData.GetAttributeOwnerDict.ContainsKey(config.attributeOwnerType))
        {
            RainMeadow.Error($"Could not add online configurable {config.name} : {config.attributeOwnerType.Name} is not registered and has no GET function !");
            return;
        }
        if (config.attributeOwnerType.GetField(config.attributeName) is null)
        {
            RainMeadow.Error($"Could not add online configurable {config.name} : {config.attributeOwnerType.Name}.{config.attributeName} doesn't exist or is not an attribute !");
            return;
        }

        if (config.slugcatTab is not null
            && !onlineConfigurableTabs.Exists(x => x.name is null && x.slugcatIcon == config.slugcatTab))
        {
            AddSlugcatSettingsTab(new(config.slugcatTab));
        }

        if (config.tabName is not null && !onlineConfigurableTabs.Exists(x => x.name == config.tabName))
        {
            AddSlugcatSettingsTab(new(config.tabName, "Futile_White", Color.gray));
        }

        onlineConfigurables.Add(config);
    }

    private static List<SlugcatSettingsConfigData> GetAllConfigurablesFromTab(SlugcatSettingsTabData? tab = null)
    {
        if (tab is SlugcatSettingsTabData onlineConfigurableTab)
        {
            if (onlineConfigurableTab.name is null)
            {
                return onlineConfigurables.FindAll(x => x.slugcatTab == onlineConfigurableTab.slugcatIcon);
            }
            else
            {
                return onlineConfigurables.FindAll(x => x.name == onlineConfigurableTab.name);
            }
        }
        return onlineConfigurables.FindAll(x => x.tabName is null && x.slugcatTab is null);
    }
    private SlugcatSettingTab GetElementFromConfig(SlugcatSettingsTabData tab)
    {
        return new SlugcatSettingTab(menu, this, tab);
    }
    private SlugcatSettingParameter? GetElementFromConfig(SlugcatSettingsConfigData configurable, SlugcatSettingTab? tab = null)
    {
        if (configurable.configurable.settingType == typeof(int))
        {
            return new SlugcatSettingIntValue(menu, this, configurable, tab);
        }
        else if (configurable.configurable.settingType == typeof(float))
        {
            return new SlugcatSettingFloatValue(menu, this, configurable, tab);
        }
        else if (configurable.configurable.settingType == typeof(string))
        {
            return new SlugcatSettingStringValue(menu, this, configurable, tab);
        }
        RainMeadow.Error($"Error trying to find UI element for [{configurable.name} : {configurable.attributeOwnerType}.{configurable.attributeName}] : type {configurable.configurable.settingType} is not handled !");
        return null;
    }

    protected OnlineSlugcatSettings(Menu.Menu menu, MenuObject owner, float spacing = 5f, float margin = 30f, float textSpacing = 300)
         : base(menu, owner, spacing, margin, textSpacing)
    {
        foreach (var tab in onlineConfigurableTabs)
        {
            SlugcatSettingTab tabElement = GetElementFromConfig(tab);
            elements.Add(tabElement);
            GetAllConfigurablesFromTab(tab).Do(config =>
            {
                if (GetElementFromConfig(config, tabElement) is SlugcatSettingParameter param)
                {
                    elements.Add(param);
                }
                else
                {
                    RainMeadow.Error($"Error trying to create UI element for [{config.name} : {config.attributeOwnerType}.{config.attributeName}], it will not be added !");
                }
            });
        }
        GetAllConfigurablesFromTab().Do(config =>
        {
            if (GetElementFromConfig(config) is SlugcatSettingParameter param)
            {
                elements.Add(param);
            }
            else
            {
                RainMeadow.Error($"Error trying to create UI element for [{config.name} : {config.attributeOwnerType}.{config.attributeName}], it will not be added !");
            }
        });

        UpdateElements();
        this.SafeAddSubobjects([.. elements]);
    }
}

public class TestSetting : OnlineSlugcatSettings<TestSetting>
{
    public override string Name => "Test";
    static TestSetting()
    {
        AddSlugcatSettingsConfigurable(new(
            "Test (it's ascend)",
            RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer,
            nameof(ArenaOnlineGameMode.arenaSaintAscendanceTimer),
            MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint,
            "How long Saint's ascendance ability lasts for. Default: 3s")
        );
        AddSlugcatSettingsConfigurable(new(
            "Test (it's stun)",
            RainMeadow.rainMeadowOptions.ArtificerStunDistanceMult,
            nameof(ArenaOnlineGameMode.artiStunDistanceMult),
            MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer,
            "wawa")
        );
        AddSlugcatSettingsTab(new(SlugcatStats.Name.Red));
    }
    public TestSetting(Menu.Menu menu, MenuObject owner) : base(menu, owner)
    {
        // GetSlugcatSettingParameter(RainMeadow.rainMeadowOptions.ArtificerStunDistanceMult)?.color = Color.red + Color.white * 0.25f;
    }
}
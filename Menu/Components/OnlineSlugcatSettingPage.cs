using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using RainMeadow.UI.Components.Patched;
using RWCustom;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RainMeadow.UI.Components.OnlineSlugcatAbilitiesInterface;
using static RainMeadow.UI.Components.TabContainer;
using static Menu.Menu;
using System;
using HarmonyLib;

namespace RainMeadow.UI.Components;

public abstract class SettingPageElement : PositionedMenuObject
{
    public int position = 0;
    public bool grayedOut = false;
    public bool visible = true;
    public readonly SettingPageTab? tab;
    public OnlineSlugcatSettingPageBase settingPage => (owner as OnlineSlugcatSettingPageBase)!;

    public SettingPageElement(Menu.Menu menu, OnlineSlugcatSettingPageBase owner, SettingPageTab? tab = null)
         : base(menu, owner, Vector2.zero)
    {
        this.tab = tab;
    }

    public override void Update()
    {
        base.Update();
        this.pos = settingPage.anchorPosition
            - Vector2.up * position * settingPage.spacing
            - Vector2.right * (tab is null ? 0 : 30f);
    }
}
public abstract class SettingPageParameter : SettingPageElement
{
    public readonly OnlineConfigurable config;

    public SettingPageParameter(Menu.Menu menu, OnlineSlugcatSettingPageBase owner, OnlineConfigurable config, SettingPageTab? tab = null)
         : base(menu, owner, tab)
    {
        this.config = config;
    }
}
public class SettingPageTab : SettingPageElement
{
    private const float iconSpacing = 60;
    private const float dividerSpacing = 20;
    public readonly OnlineConfigurableTab config;
    public bool folded = false;
    public PositionedMenuObject icon;
    public MenuLabel label;
    public FSprite divider;
    public string name = "";
    public Color tabColor = Color.gray;

    public SettingPageTab(Menu.Menu menu, OnlineSlugcatSettingPageBase owner, OnlineConfigurableTab config)
         : base(menu, owner, null)
    {
        this.config = config;
        if (config.slugcatIcon is not null)
        {
            this.icon = new PositionedSlugIcon(menu, this, Vector2.zero, config.slugcatIcon.value);
        }
        else
        {
            this.icon = new MenuIllustration(menu, this, "", config.icon, Vector2.zero, true, true);
        }

        if (config.name is not null && config.color is not null)
        {
            this.name = config.name;
            this.tabColor = (Color)config.color;
        }
        else if (config.slugcatIcon is not null)
        {
            this.name = config.slugcatIcon.value;
            this.tabColor = Color.HSVToRGB(
                PlayerGraphics.DefaultSlugcatColor(config.slugcatIcon).ToHSL().hue,
                PlayerGraphics.DefaultSlugcatColor(config.slugcatIcon).ToHSL().saturation < 0.1f ? 0 : 0.75f,
                1f
            );
        }

        this.label = new(menu, this, menu.Translate(name), pos, new(300, 20), true);
        this.divider = new("pixel", false){
            anchorX = 0f,
            scaleX = 200f,
            scaleY = 2f,
            color = tabColor,
        };

        this.subObjects.AddRange([icon, label]);
        this.Container.AddChild(divider);
    }

    public override void Update()
    {
        base.Update();
        this.icon.pos = pos;
        this.label.pos = this.icon.pos - Vector2.right * iconSpacing - Vector2.right * settingPage.textSpacing;
        this.divider.x = pos.x + Mathf.Abs(this.label.label.textRect.x);
        this.divider.y = pos.y;
        this.divider.scaleX = Mathf.Max(0, OnlineSlugcatSettingPageBase.topPosition.x - this.divider.x - dividerSpacing);
        RainMeadow.Debug($"Pos at <{position}>, {this.icon.pos}, {this.label.pos}, {this.divider.GetPosition()}");
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        if (icon is MenuIllustration illustration) illustration.sprite.isVisible = visible;
        else if (icon is PositionedSlugIcon slugicon) slugicon.slugIcon.sprites.Do(x => x.isVisible = visible);
        divider.isVisible = visible;
    }
    public override void RemoveSprites()
    {
        base.RemoveSprites();
        this.divider?.RemoveFromContainer();
    }
}
public readonly struct OnlineConfigurable
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
    static OnlineConfigurable()
    {
        AddNewGetAttributeOwnerFunction(() => OnlineManager.lobby?.gameMode as ArenaOnlineGameMode);
    }

    public OnlineConfigurable(string name, ConfigurableBase configurable, string arenaOnlineAttributeName, Type attributeOwnerType, string description = "")
    {
        this.name = name;
        this.attributeName = arenaOnlineAttributeName;
        this.configurable = configurable;
        this.description = description;
        this.attributeOwnerType = attributeOwnerType;
    }
    public OnlineConfigurable(string name, ConfigurableBase configurable, string arenaOnlineAttributeName, string description = "")
         : this(name, configurable, arenaOnlineAttributeName, typeof(ArenaOnlineGameMode), description) {}

    public OnlineConfigurable(string name, ConfigurableBase configurable, string arenaOnlineAttributeName, Type attributeOwnerType, SlugcatStats.Name slugcat, string description = "")
         : this(name, configurable, arenaOnlineAttributeName, attributeOwnerType, description)
    {
        this.slugcatTab = slugcat;
    }
    public OnlineConfigurable(string name, ConfigurableBase configurable, string arenaOnlineAttributeName, SlugcatStats.Name slugcat, string description = "")
         : this(name, configurable, arenaOnlineAttributeName, typeof(ArenaOnlineGameMode), slugcat, description) {}

    public OnlineConfigurable(string name, ConfigurableBase configurable, string arenaOnlineAttributeName, Type attributeOwnerType, string tabName, string description = "")
         : this(name, configurable, arenaOnlineAttributeName, attributeOwnerType, description)
    {
        this.tabName = tabName;
    }
    public OnlineConfigurable(string name, ConfigurableBase configurable, string arenaOnlineAttributeName, string tabName, string description = "")
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
    }

    public static bool operator== (OnlineConfigurable left, OnlineConfigurable right)
    {
        return left.name == right.name
            && left.attributeName == right.attributeName
            && left.attributeOwnerType == right.attributeOwnerType
            && left.configurable == right.configurable;
    }
    public static bool operator!= (OnlineConfigurable left, OnlineConfigurable right)
    {
        return !(left == right);
    }
    public override bool Equals(object obj)
    {
        return obj is OnlineConfigurable config && config == this;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
public readonly struct OnlineConfigurableTab
{
    public readonly SlugcatStats.Name? slugcatIcon;
    public readonly string? name;
    public readonly string? icon;
    public readonly Color? color;
    public OnlineConfigurableTab(SlugcatStats.Name slugcat)
    {
        this.slugcatIcon = slugcat;
    }
    public OnlineConfigurableTab(string name, SlugcatStats.Name slugcatIcon, Color color)
    {
        this.slugcatIcon = slugcatIcon;
        this.name = name;
        this.color = color;
    }
    public OnlineConfigurableTab(string name, string icon, Color color)
    {
        this.icon = icon;
        this.name = name;
        this.color = color;
    }

    public static bool operator== (OnlineConfigurableTab left, OnlineConfigurableTab right)
    {
        return left.name == right.name && left.slugcatIcon == right.slugcatIcon;
    }
    public static bool operator!= (OnlineConfigurableTab left, OnlineConfigurableTab right)
    {
        return !(left == right);
    }
    public override bool Equals(object obj)
    {
        return obj is OnlineConfigurableTab tab && tab == this;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
public abstract class OnlineSlugcatSettingPageBase : SettingsPage, CheckBox.IOwnCheckBox
{
    public const string RESETTODEFAULT = "RESETTODEFAULT";
    public static Vector2 topPosition = new(360, 420);
    public Vector2 anchorPosition = topPosition;
    public SimpleButton? backButton;
    public SimpleButton? resetButton;
    public MenuTabWrapper tabWrapper;
    protected List<SettingPageElement> elements;
    public float spacing;
    public float textSpacing;

    protected OnlineSlugcatSettingPageBase(Menu.Menu menu, MenuObject owner, float spacing = 30f, float textSpacing = 300) : base(menu, owner)
    {
        tabWrapper = new(menu, this);
        elements = [];
        this.spacing = spacing;
        this.textSpacing = textSpacing;
    }
    public override void SelectAndCreateBackButtons(SettingsPage? previousSettingPage, bool forceSelectedObject)
    {
        if (backButton is null)
        {
            backButton = new(menu, this, menu.Translate("BACK"), BACKTOSELECT, new(30, 30), new(80, 30));
            AddObjects(backButton);
        }
        if (resetButton is null)
        {
            resetButton = new(menu, this, menu.Translate("RESET"), RESETTODEFAULT, new(330, 30), new(80, 30));
            AddObjects(resetButton);
        }
    }
    public override void Update()
    {
        base.Update();

        if (IsActuallyHidden) return; //lets not update this when hidden
        bool greyoutAll = SettingsDisabled;
        foreach (MenuObject obj in subObjects)
        {
            if (obj != backButton && obj is ButtonTemplate btn)
                btn.buttonBehav.greyedOut = greyoutAll;
        }

    }

    public bool GetChecked(CheckBox box)
    {
        return false;
    }

    public void SetChecked(CheckBox box, bool c)
    {

    }
}
public abstract class OnlineSlugcatSettingPage<T> : OnlineSlugcatSettingPageBase where T : class
{
    protected static List<OnlineConfigurable> onlineConfigurables = [];
    protected static List<OnlineConfigurableTab> onlineConfigurableTabs = [];

    public static void AddConfigurableTab(OnlineConfigurableTab tab)
    {
        if (onlineConfigurableTabs.Exists(x => x == tab))
        {
            RainMeadow.Error($"Could not add online configurable tab {tab.name ?? tab.slugcatIcon?.value} : {tab.name ?? tab.slugcatIcon?.value} is already in the page !");
            return;
        }
        onlineConfigurableTabs.Add(tab);
    }
    public static void AddConfigurable(OnlineConfigurable config)
    {
        if (onlineConfigurables.Exists(x => x.attributeName == config.attributeName))
        {
            RainMeadow.Error($"Could not add online configurable {config.name} : {config.attributeOwnerType.Name}.{config.attributeName} is already in the page !");
            return;
        }
        if (!OnlineConfigurable.GetAttributeOwnerDict.ContainsKey(config.attributeOwnerType))
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
            AddConfigurableTab(new(config.slugcatTab));
        }

        if (config.tabName is not null && !onlineConfigurableTabs.Exists(x => x.name == config.tabName))
        {
            AddConfigurableTab(new(config.tabName, "Futile_White", Color.gray));
        }

        onlineConfigurables.Add(config);
    }

    private static List<OnlineConfigurable> GetAllConfigurablesFromTab(OnlineConfigurableTab? tab = null)
    {
        if (tab is OnlineConfigurableTab onlineConfigurableTab)
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
    private SettingPageElement GetElementFromConfig(OnlineConfigurableTab tab)
    {
        return new SettingPageTab(menu, this, tab);
    }
    // private SettingPageElement GetElementFromConfig(OnlineConfigurable setting)
    // {
    //     return null;
    // }

    protected OnlineSlugcatSettingPage(Menu.Menu menu, MenuObject owner, float spacing = 30, float textSpacing = 300)
         : base(menu, owner, spacing, textSpacing)
    {
        onlineConfigurableTabs.Do(x => elements.Add(GetElementFromConfig(x)));
        UpdateElements();
        this.SafeAddSubobjects([.. elements]);
    }

    public override void Update()
    {
        base.Update();
        UpdateElements();
    }

    public void UpdateElements()
    {
        // SURELY there's a better way to do it...
        int position = 0;
        for (int i = 0; i < onlineConfigurableTabs.Count; i++)
        {
            SettingPageTab? tabPage = elements.Find(x =>
                x is SettingPageTab tab
                && tab.config == onlineConfigurableTabs[i]
            ) as SettingPageTab;

            SettingPageParameter[] elementsInTab = GetAllConfigurablesFromTab(onlineConfigurableTabs[i])
                .Select(config =>
                    elements.Find(x =>
                        x is SettingPageParameter param
                        && param.config == config
                    )).Cast<SettingPageParameter>().ToArray();

            if (tabPage?.visible is true) tabPage.position = position++;
            for (int j = 0; j < elementsInTab.Length; j++)
            {
                elementsInTab[j].visible = (tabPage?.visible ?? false) && (!tabPage?.folded ?? false);
                elementsInTab[j].grayedOut = tabPage?.grayedOut ?? true;

                if (elementsInTab[j].visible) elementsInTab[j].position = position++;
            }
        }
        GetAllConfigurablesFromTab(null)
            .Do(config =>
                elements.Find(x =>
                    x is SettingPageParameter param
                    && param.config == config
                    && param.visible
                )?.position = position++
            );
    }
}

public class TestSettingPage : OnlineSlugcatSettingPage<TestSettingPage>
{
    static TestSettingPage()
    {
        AddConfigurableTab(new(SlugcatStats.Name.Night));
    }
    public TestSettingPage(Menu.Menu menu, MenuObject owner, float spacing = 30, float textSpacing = 300) : base(menu, owner, spacing, textSpacing)
    {
    }

    public override string Name => "Test";
}
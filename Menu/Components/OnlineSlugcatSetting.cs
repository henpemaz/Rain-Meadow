using Menu;
using Menu.Remix;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RainMeadow.UI.Components.OnlineSlugcatAbilitiesInterface;
using System;
using HarmonyLib;

namespace RainMeadow.UI.Components;

public abstract class OnlineSlugcatSettingsBase : SettingsPage
{
    public static Vector2 defaultBoxSize = new(450, 440);
    public Vector2 settingsBoxSize;
    public float margin;
    public SimpleButton? backButton;
    public SimplerButton? resetButton;
    public MenuTabWrapper tabWrapper;
    protected List<OnlineSettingElement> elements;
    public float spacing;
    public float textSpacing;
    public bool wasHidden = true;
    public int lastVisibleElementCount = 0;

    public OnlineSettingTab? GetSettingTab(SlugcatStats.Name slugcatTab)
    {
        return elements.Find(x =>
            x is OnlineSettingTab tab
            && tab.data.name is null
            && tab.data.slugcatIcon == slugcatTab)
        as OnlineSettingTab;
    }
    public OnlineSettingTab? GetSettingTab(string tabName)
    {
        return elements.Find(x =>
            x is OnlineSettingTab tab
            && tab.data.name == tabName)
        as OnlineSettingTab;
    }
    public OnlineSettingConfigurable? GetSettingParameter(string paramName)
    {
        return elements.Find(x =>
            x is OnlineSettingConfigurable param
            && param.data.name == paramName)
        as OnlineSettingConfigurable;
    }
    public OnlineSettingConfigurable? GetSettingParameter(ConfigurableBase configurable)
    {
        return elements.Find(x =>
            x is OnlineSettingConfigurable param
            && param.data.configurable == configurable)
        as OnlineSettingConfigurable;
    }
    public OnlineSettingConfigurable? GetSettingParameter(string attributeName, Type attributeOwnerType)
    {
        return elements.Find(x =>
            x is OnlineSettingConfigurable param
            && param.data.attributeName == attributeName
            && param.data.attributeOwnerType == attributeOwnerType)
        as OnlineSettingConfigurable;
    }

    protected OnlineSlugcatSettingsBase(Menu.Menu menu, MenuObject owner, float spacing = 5f, float margin = 30f, float textSpacing = 300) : base(menu, owner)
    {
        tabWrapper = new(menu, this);
        elements = [];
        this.spacing = spacing;
        this.textSpacing = textSpacing;
        this.margin = margin;

        settingsBoxSize = defaultBoxSize - Vector2.right * margin * 2;
        this.SafeAddSubobjects(tabWrapper);
    }

    public void UpdateElementsVisibility()
    {
        int visibleElementCount = 0;
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i].tab is OnlineSettingTab tab)
            {
                if (tab.grayedOut && !elements[i].tabIndependant && !elements[i].isClient)
                    elements[i].grayedOut = true;
                if (!tab.visible || tab.folded)
                    elements[i].visible = false;
            }

            if (elements[i].visible)
            {
                elements[i].alpha = 1;
                visibleElementCount++;
            }
            else
            {
                elements[i].HardSetAlpha(0);
            }
        }
        if (lastVisibleElementCount != visibleElementCount)
        {
            lastVisibleElementCount = visibleElementCount;
            BindSettingsButtons(IsActuallyHidden);
        }
    }
    public void UpdateElementsPosition()
    {
        int position = 0;
        for (int i = 0; i < elements.Count; i++)
        {
            elements[i].position = position;
            if (elements[i].visible)
            {
                position++;
                if (elements[i].additionalPositionsTaken > 0)
                    position += elements[i].additionalPositionsTaken;
            }
        }
    }
    public void ResetSettings()
    {
        menu.PlaySound(SoundID.MENU_Button_Successfully_Assigned);

        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] is OnlineSettingConfigurable param)
            {
                param.ResetValueToDefault();
            }
        }
    }
    public void BindSettingsButtons(bool isHidden)
    {
        if (isHidden)
        {
            elements.Select(el => el.selectable).Do(sel =>
            {
                sel.RemoveBind(bottom:true, top:true);
            });
            backButton.RemoveBind(right:true, top:true, bottom:true);
            resetButton.RemoveBind(left:true, top:true, bottom:true);
        }
        else
        {
            List<MenuObject> visibleElements = elements.FindAll(x => x.visible).Select(el => el.selectable).ToList();

            if (backButton is not null)
                visibleElements.Insert(0, backButton);

            menu.TrySequentialMutualBind(visibleElements, bottomTop: true, loopLastIndex: true, reverseList:true);

            if (backButton is not null && resetButton is not null)
                menu.MutualHorizontalButtonBind(backButton, resetButton);

            menu.TryMutualBind(resetButton, visibleElements.FirstOrDefault(), bottomTop:false);
            menu.TryMutualBind(visibleElements.LastOrDefault(), resetButton, bottomTop:false);
        }
    }

    public override void SelectAndCreateBackButtons(SettingsPage? previousSettingPage, bool forceSelectedObject)
    {
        if (backButton is null)
        {
            backButton = new(menu, this, menu.Translate("BACK"), BACKTOSELECT, new(margin, 20), new(80, 30));
            AddObjects(backButton);
        }
        if (resetButton is null)
        {
            resetButton = new(menu, this, menu.Translate("RESET"), new(settingsBoxSize.x - 40, 20), new(80, 30));
            resetButton.OnClick += (b) => ResetSettings();
            AddObjects(resetButton);
        }

        BindSettingsButtons(IsActuallyHidden);
        if (forceSelectedObject) menu.selectedObject = elements.FirstOrDefault()?.selectable ?? backButton;
    }
    public override void Update()
    {
        base.Update();

        if (wasHidden != IsActuallyHidden)
        {
            wasHidden = IsActuallyHidden;
            BindSettingsButtons(IsActuallyHidden);
        }

        if (IsActuallyHidden) return;

        bool greyoutNonClient = SettingsDisabled;
        bool greyoutAll = (OnlineManager.lobby?.gameMode as ArenaOnlineGameMode)?.initiateLobbyCountdown ?? true;

        foreach (MenuObject obj in subObjects)
        {
            if (obj != backButton && obj is ButtonTemplate btn)
                btn.buttonBehav.greyedOut = greyoutNonClient;
        }
        for (int i = 0; i < elements.Count; i++)
        {
            elements[i].visible = !IsActuallyHidden;
            elements[i].grayedOut = elements[i].isClient
                ? greyoutAll
                : greyoutNonClient;
        }
        UpdateElementsVisibility();
        UpdateElementsPosition();
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        if (IsActuallyHidden)
        {
            for (int i = 0; i < elements.Count; i++)
            {
                elements[i].pos = elements[i].targetPos + Vector2.up * 5f;
                elements[i].HardSetAlpha(0);
            }
        }
    }

    public override void SaveInterfaceOptions()
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] is OnlineSettingConfigurable param)
            {
                param.SaveOption();
            }
        }
    }
    public override void SaveInterfaceClientOptions()
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] is OnlineSettingConfigurable param)
            {
                param.SaveOption(true);
            }
        }
    }
    public override void CallForSync()
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] is OnlineSettingConfigurable param)
            {
                param.SyncValueToAttribute();
            }
        }
    }
}
public abstract class OnlineSlugcatSettings<TSelf> : OnlineSlugcatSettingsBase where TSelf : class
{
    protected static List<SettingsConfigData> onlineConfigurables = [];
    protected static List<SettingsTabData> onlineConfigurableTabs = [];

    public static void AddSlugcatSettingsTab(SettingsTabData tab)
    {
        if (onlineConfigurableTabs.Exists(x => x == tab))
        {
            RainMeadow.Error($"Could not add online configurable tab {tab.name ?? tab.slugcatIcon?.value} : {tab.name ?? tab.slugcatIcon?.value} is already in the page !");
            return;
        }
        onlineConfigurableTabs.Add(tab);
    }
    public static void AddSlugcatSettingsConfigurable(SettingsConfigData config)
    {
        if (string.IsNullOrWhiteSpace(config.attributeName))
        {
            RainMeadow.Warn($"Adding configurable {config.name} with nowhere to save the value for the lobby !");
        }
        else
        {
            if (onlineConfigurables.Exists(x => x.attributeName == config.attributeName && x.attributeOwnerType == config.attributeOwnerType))
            {
                RainMeadow.Error($"Could not add online configurable {config.name} : {config.attributeOwnerType.Name}.{config.attributeName} is already in the page !");
                return;
            }
            if (!SettingsConfigData.GetAttributeOwnerDict.ContainsKey(config.attributeOwnerType))
            {
                RainMeadow.Error($"Could not add online configurable {config.name} : {config.attributeOwnerType.Name} is not registered and has no GET function !");
                return;
            }
            if (config.attributeOwnerType.GetField(config.attributeName) is null)
            {
                RainMeadow.Error($"Could not add online configurable {config.name} : {config.attributeOwnerType.Name}.{config.attributeName} doesn't exist or is not an attribute !");
                return;
            }
        }

        if (config.slugcatTab is not null
            && !onlineConfigurableTabs.Exists(x => x.name is null && x.slugcatIcon == config.slugcatTab))
        {
            AddSlugcatSettingsTab(new(config.slugcatTab));
        }

        if (config.tabName is not null && !onlineConfigurableTabs.Exists(x => x.name == config.tabName))
        {
            AddSlugcatSettingsTab(new(config.tabName, Color.gray));
        }

        onlineConfigurables.Add(config);
    }

    private static List<SettingsConfigData> GetAllConfigurablesFromTab(SettingsTabData? tab = null)
    {
        if (tab is SettingsTabData onlineConfigurableTab)
        {
            if (onlineConfigurableTab.name is null)
            {
                return onlineConfigurables.FindAll(x => x.slugcatTab == onlineConfigurableTab.slugcatIcon);
            }
            else
            {
                return onlineConfigurables.FindAll(x => x.tabName == onlineConfigurableTab.name);
            }
        }
        return onlineConfigurables.FindAll(x => x.tabName is null && x.slugcatTab is null);
    }
    private OnlineSettingTab GetElementFromConfig(SettingsTabData tab)
    {
        return new OnlineSettingTab(menu, this, tab);
    }
    private OnlineSettingConfigurable? GetElementFromConfig(SettingsConfigData configurable, OnlineSettingTab? tab = null)
    {
        if (configurable.AttributeType == typeof(int))
        {
            return new OnlineSettingIntValue(menu, this, configurable, tab);
        }
        else if (configurable.AttributeType == typeof(float))
        {
            return new OnlineSettingFloatValue(menu, this, configurable, tab);
        }
        else if (configurable.AttributeType == typeof(string))
        {
            return new OnlineSettingStringValue(menu, this, configurable, tab);
        }
        else if (configurable.AttributeType == typeof(bool))
        {
            return new OnlineSettingCheckBox(menu, this, configurable, tab);
        }
        else if (configurable.AttributeType == typeof(KeyCode))
        {
            return new OnlineSettingKeycode(menu, this, configurable, tab);
        }
        else if (configurable.AttributeType.IsEnum || configurable.AttributeType.IsExtEnum())
        {
            return new OnlineSettingEnumList(menu, this, configurable, tab);
        }
        RainMeadow.Error($"Error trying to find UI element for [{configurable.name} : {configurable.attributeOwnerType}.{configurable.attributeName}] : type {configurable.configurable.settingType} is not handled !");
        return null;
    }

    protected OnlineSlugcatSettings(Menu.Menu menu, MenuObject owner, float spacing = 5f, float margin = 30f, float textSpacing = 300)
         : base(menu, owner, spacing, margin, textSpacing)
    {
        foreach (var tab in onlineConfigurableTabs)
        {
            OnlineSettingTab tabElement = GetElementFromConfig(tab);
            elements.Add(tabElement);
            GetAllConfigurablesFromTab(tab).Do(config =>
            {
                if (GetElementFromConfig(config, tabElement) is OnlineSettingConfigurable param)
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
            if (GetElementFromConfig(config) is OnlineSettingConfigurable param)
            {
                elements.Add(param);
            }
            else
            {
                RainMeadow.Error($"Error trying to create UI element for [{config.name} : {config.attributeOwnerType}.{config.attributeName}], it will not be added !");
            }
        });

        UpdateElementsPosition();
        for (int i = 0; i < elements.Count; i++)
        {
            elements[i].HardSetPosition(elements[i].WantedPosition);
        }
        this.SafeAddSubobjects([.. elements]);
    }
}

public class TestMSCSetting : OnlineSlugcatSettings<TestMSCSetting>
{
    public override string Name => "Test MSC";
    private OnlineSettingCheckBox? sainotSetting;
    private OnlineSettingIntValue? ascendSetting;
    static TestMSCSetting()
    {
        AddSlugcatSettingsConfigurable(new(
            "Artificer Explosion Capacity",
            MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer,
            MoreSlugcats.MoreSlugcats.cfgArtificerExplosionCapacity,
            nameof(ArenaOnlineGameMode.artiExplosionCount),
            "How many explosions Artificer can use before cooldown")
        );
        AddSlugcatSettingsConfigurable(new(
            "Artificer Stun Range Multiplier",
            MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer,
            RainMeadow.rainMeadowOptions.ArtificerStunDistanceMult,
            nameof(ArenaOnlineGameMode.artiStunDistanceMult),
            "Multiplier on how far Artificer can stun other players compared to vanilla range. Default: 0.5")
        );
        AddSlugcatSettingsConfigurable(new(
            "Artificer Parry Range Multiplier",
            MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer,
            RainMeadow.rainMeadowOptions.ArtificerParryDistanceMult,
            nameof(ArenaOnlineGameMode.artiParryDistanceMult),
            "How far Artificer can parry from compared to vanilla range. Default: 0.3")
        );
        AddSlugcatSettingsConfigurable(new(
            "Artificer Parry Leniency",
            MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer,
            RainMeadow.rainMeadowOptions.ArtificerParryLeniency,
            nameof(ArenaOnlineGameMode.artiParryLeniency),
            "Gives Artificer more leniency frames in the concussive blast's parry")
        );
        AddSlugcatSettingsConfigurable(new(
            "Disable Mauling",
            MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer,
            RainMeadow.rainMeadowOptions.BlockMaul,
            nameof(ArenaOnlineGameMode.disableMaul),
            "Prevent Artificer and <PAINCATNAME> from mauling")
        );

        AddSlugcatSettingsConfigurable(new(
            "Sain't",
            MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint,
            RainMeadow.rainMeadowOptions.ArenaSAINOT,
            nameof(ArenaOnlineGameMode.sainot),
            "Disable Saint ascendance ability, but allow it to throw spears")
        );
        AddSlugcatSettingsConfigurable(new(
            "Saint Ascendance Duration",
            MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint,
            RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer,
            nameof(ArenaOnlineGameMode.arenaSaintAscendanceTimer),
            "How long Saint's ascendance ability lasts for. Default: 3s")
        );

        AddSlugcatSettingsConfigurable(new(
            "<PAINCATNAME> gets egg at 0 throw skill",
            MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel,
            RainMeadow.rainMeadowOptions.PainCatEgg,
            nameof(ArenaOnlineGameMode.painCatEgg),
            "If <PAINCATNAME> spawns with 0 throw skill, also spawn with Eggzer0")
        );
        AddSlugcatSettingsConfigurable(new(
            "<PAINCATNAME> can always throw spears",
            MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel,
            RainMeadow.rainMeadowOptions.PainCatThrows,
            nameof(ArenaOnlineGameMode.painCatThrows),
            "Always allow <PAINCATNAME> to throw spears, even if throw skill is 0")
        );
        AddSlugcatSettingsConfigurable(new(
            "<PAINCATNAME> sometimes gets a friend",
            MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel,
            RainMeadow.rainMeadowOptions.PainCatLizard,
            nameof(ArenaOnlineGameMode.painCatLizard),
            "Allow <PAINCATNAME> to rarely spawn with a little friend")
        );
    }
    public TestMSCSetting(Menu.Menu menu, MenuObject owner, string painCatName) : base(menu, owner)
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] is OnlineSettingConfigurable param)
            {
                param.label.text = param.label.text.Replace("<PAINCATNAME>", painCatName);
            }
        }

        sainotSetting = GetSettingParameter(RainMeadow.rainMeadowOptions.ArenaSAINOT) as OnlineSettingCheckBox;
        ascendSetting = GetSettingParameter(RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer) as OnlineSettingIntValue;
    }
    public override void Update()
    {
        base.Update();

        sainotSetting?.tab?.label.text = menu.Translate(sainotSetting.valueBool ? "Sain't" : "Saint");
        if (sainotSetting?.valueBool is true) ascendSetting?.grayedOut = true;
    }
}

public class TestWatcherSetting : OnlineSlugcatSettings<TestWatcherSetting>
{
    public const string WATCHERCAMO = "Watcher Camo",
        WATCHERWEAVER = "Watcher Weaver",
        WATCHERVOIDMASTER = "Watcher Voidmaster";
    public override string Name => "Test Watcher";
    private OnlineSettingIntValue? rippleLevelSetting;
    private OnlineSettingCheckBox? invisSetting;
    private OnlineSettingCheckBox? voidMasterSetting;
    static TestWatcherSetting()
    {
        AddSlugcatSettingsTab(new(
            WATCHERCAMO,
            Watcher.WatcherEnums.SlugcatStatsName.Watcher,
            PlayerGraphics.DefaultSlugcatColor(Watcher.WatcherEnums.SlugcatStatsName.Watcher) * 1.5f
        ));
        AddSlugcatSettingsTab(new(
            WATCHERWEAVER,
            Watcher.WatcherEnums.SlugcatStatsName.Watcher,
            RainWorld.GoldRGB * 1.5f,
            true
        ));
        AddSlugcatSettingsTab(new(
            WATCHERVOIDMASTER,
            Watcher.WatcherEnums.SlugcatStatsName.Watcher,
            RainWorld.RippleColor * 1.5f
        ));

        AddSlugcatSettingsConfigurable(new(
            "Watcher Camo Duration",
            WATCHERCAMO,
            RainMeadow.rainMeadowOptions.ArenaWatcherCamoTimer,
            nameof(ArenaOnlineGameMode.watcherCamoTimer),
            "How long Watcher's abilities last for. Default: 12s")
        );
        AddSlugcatSettingsConfigurable(new(
            "Watcher Ripple Level",
            WATCHERCAMO,
            RainMeadow.rainMeadowOptions.ArenaWatcherRippleLevel,
            nameof(ArenaOnlineGameMode.watcherRippleLevel),
            "Updates Watcher's ripple level. Ranges from 1 to 9. Default: 1")
        );
        AddSlugcatSettingsConfigurable(new(
            "Full Invisibility In Ripple Space",
            WATCHERCAMO,
            RainMeadow.rainMeadowOptions.ArenaWatcherFullInvisibleInRippleSpace,
            nameof(ArenaOnlineGameMode.fullInvisInRippleSpace),
            "Watcher will leave a faint glow at their position when in ripple space. Other Watchers will also be able to see their eyes.")
        );

        AddSlugcatSettingsConfigurable(new(
            "Weaver Watcher",
            WATCHERWEAVER,
            RainMeadow.rainMeadowOptions.WeaverWatcher,
            typeof(ArenaClientSettings),
            nameof(ArenaClientSettings.weaverTail),
            "Your watcher has synced normal cosmetics",
            true)
        );

        AddSlugcatSettingsConfigurable(new(
            "Voidkeeper",
            WATCHERVOIDMASTER,
            RainMeadow.rainMeadowOptions.VoidMaster,
            nameof(ArenaOnlineGameMode.voidMasterEnabled),
            "Amoeba summoning is disabled lobby-wide")
        );
        AddSlugcatSettingsConfigurable(new(
            "Voidkeeper Amoeba Duration",
            WATCHERVOIDMASTER,
            RainMeadow.rainMeadowOptions.AmoebaDuration,
            nameof(ArenaOnlineGameMode.amoebaDuration),
            "Amoeba duration time in seconds")
        );
        AddSlugcatSettingsConfigurable(new(
            "Amoeba Lethality Factor",
            WATCHERVOIDMASTER,
            RainMeadow.rainMeadowOptions.VoidSpawnLethalityFactor,
            nameof(ArenaOnlineGameMode.voidSpawnLethalityFactor),
            "Multiplier for amoeba lethality")
        );
        AddSlugcatSettingsConfigurable(new(
            "Void's Vengeance",
            WATCHERVOIDMASTER,
            RainMeadow.rainMeadowOptions.AmoebaControl,
            nameof(ArenaOnlineGameMode.amoebaControl),
            "Amoebas chase targets at-will")
        );
    }
    public TestWatcherSetting(Menu.Menu menu, MenuObject owner) : base(menu, owner)
    {
        rippleLevelSetting = GetSettingParameter(RainMeadow.rainMeadowOptions.ArenaWatcherRippleLevel) as OnlineSettingIntValue;

        invisSetting = GetSettingParameter(RainMeadow.rainMeadowOptions.ArenaWatcherFullInvisibleInRippleSpace) as OnlineSettingCheckBox;
        invisSetting?.altDescription = "Watcher will be fully invisible to everyone when in ripple space";

        OnlineSettingConfigurable? weaverGraphics = GetSettingParameter(RainMeadow.rainMeadowOptions.WeaverWatcher);
        weaverGraphics?.color = RainWorld.GoldRGB * 1.5f;
        (weaverGraphics as OnlineSettingCheckBox)?.altDescription = "Your watcher has synced weaver cosmetics";

        voidMasterSetting = GetSettingParameter(RainMeadow.rainMeadowOptions.VoidMaster) as OnlineSettingCheckBox;
        voidMasterSetting?.color = RainWorld.RippleColor * 1.5f;
        voidMasterSetting?.tabIndependant = true;
        voidMasterSetting?.altDescription = "Summon amoebas at the cost of your camo timer";

        (GetSettingParameter(RainMeadow.rainMeadowOptions.AmoebaControl) as OnlineSettingCheckBox)?
            .altDescription = "Amoeba's direction is influenced by pointing";
    }

    public override void Update()
    {
        base.Update();

        if (rippleLevelSetting?.valueInt < 9) invisSetting?.grayedOut = true;

        if (voidMasterSetting?.valueBool is false)
        {
            GetSettingTab(WATCHERVOIDMASTER)?.grayedOut = true;
            UpdateElementsVisibility(); // update the whole tab
        }
    }
}

public class TestSetting : OnlineSlugcatSettings<TestSetting>
{
    public const string TESTTAB = "Tab With Icon";
    public override string Name => "Test Others";
    static TestSetting()
    {
        AddSlugcatSettingsTab(new(
            TESTTAB,
            "Symbol_HellSpear",
            RainWorld.RippleGold
        ));

        AddSlugcatSettingsConfigurable(new(
            "Test str",
            "Auto-Gerenated Tab",
            new Configurable<string>("wawa"),
            "")
        );
        AddSlugcatSettingsConfigurable(new(
            "Test key",
            TESTTAB,
            new Configurable<KeyCode>(KeyCode.Escape),
            "")
        );
        AddSlugcatSettingsConfigurable(new(
            "Test enum",
            TESTTAB,
            new Configurable<RainMeadowOptions.ChatClear>(RainMeadowOptions.ChatClear.None),
            "")
        );
    }
    public TestSetting(Menu.Menu menu, MenuObject owner) : base(menu, owner)
    {

    }
}
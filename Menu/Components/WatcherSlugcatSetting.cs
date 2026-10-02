using Menu;
using RainMeadow.UI.Components.Configurables;

namespace RainMeadow.UI.Components;
public class WatcherSlugcatSetting : OnlineSlugcatSettings<WatcherSlugcatSetting>
{
    public const string WATCHERCAMO = "Camo",
        WATCHERWEAVER = "Weaver",
        WATCHERVOIDMASTER = "Voidkeeper";
    public override string Name => "Watcher Settings";
    private readonly OnlineSettingIntValue? rippleLevelSetting;
    private readonly OnlineSettingCheckBox? invisSetting;
    private readonly OnlineSettingCheckBox? voidMasterSetting;
    private readonly OnlineSettingTab? voidmasterTab;
    static WatcherSlugcatSetting()
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
    public WatcherSlugcatSetting(Menu.Menu menu, MenuObject owner) : base(menu, owner)
    {
        rippleLevelSetting = GetSettingParameter(RainMeadow.rainMeadowOptions.ArenaWatcherRippleLevel) as OnlineSettingIntValue;

        invisSetting = GetSettingParameter(RainMeadow.rainMeadowOptions.ArenaWatcherFullInvisibleInRippleSpace) as OnlineSettingCheckBox;
        invisSetting?.altDescription = "Watcher will be fully invisible to everyone when in ripple space";

        OnlineSettingConfigurable? weaverGraphics = GetSettingParameter(RainMeadow.rainMeadowOptions.WeaverWatcher);
        weaverGraphics?.color = RainWorld.GoldRGB * 1.5f;
        (weaverGraphics as OnlineSettingCheckBox)?.altDescription = "Your watcher has synced weaver cosmetics";

        voidmasterTab = GetSettingTab(WATCHERVOIDMASTER);

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
            voidmasterTab?.grayedOut = true;
            UpdateElementsVisibility(); // update the whole tab
        }
    }
}
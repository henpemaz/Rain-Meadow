using Menu;
using RainMeadow.UI.Components.Configurables;

namespace RainMeadow.UI.Components;
public class MSCSlugcatSettings : OnlineSlugcatSettings<MSCSlugcatSettings>
{
    public override string Name => "MSC Settings";
    private readonly OnlineSettingCheckBox? sainotSetting;
    private readonly OnlineSettingIntValue? ascendSetting;
    private readonly string saint, sainot;
    static MSCSlugcatSettings()
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
    public MSCSlugcatSettings(Menu.Menu menu, MenuObject owner, string painCatName) : base(menu, owner)
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] is OnlineSettingConfigurable param)
            {
                param.label.text = param.label.text.Replace("<PAINCATNAME>", menu.Translate(painCatName));
            }
        }

        GetSettingTab(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)?.label?.text = menu.Translate(painCatName);

        sainotSetting = GetSettingParameter(RainMeadow.rainMeadowOptions.ArenaSAINOT) as OnlineSettingCheckBox;
        ascendSetting = GetSettingParameter(RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer) as OnlineSettingIntValue;

        saint = menu.Translate(SlugcatStats.getSlugcatName(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint));
        sainot = menu.Translate("Sain't:").TrimEnd(':').TrimEnd();
    }
    public override void Update()
    {
        base.Update();

        sainotSetting?.tab?.label.text = sainotSetting.valueBool ? sainot : saint;
        if (sainotSetting?.valueBool is true) ascendSetting?.grayedOut = true;
    }
}
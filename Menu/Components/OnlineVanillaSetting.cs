using Menu;
using RainMeadow.UI.Components.Configurables;

namespace RainMeadow.UI.Components;

public class VanillaSetting : OnlineSlugcatSettings<VanillaSetting>
{
    public const string MonkShieldDescription = "When activated, Monk will spawn with a dangle fruit,"
        + OnlineSettingDescription.LINEBREAK + "which can blocks incoming spears."
        + OnlineSettingDescription.LINEBREAK + "When throwing, Monk will prioritize throwing"
        + OnlineSettingDescription.LINEBREAK + "weapons over the fruit."
        + OnlineSettingDescription.LINEBREAK + "Monk will also make the dangle fruit automatically face"
        + OnlineSettingDescription.LINEBREAK + "where you're heading.";
    public override string Name => "Vanilla Settings";
    static VanillaSetting()
    {
        AddSlugcatSettingsConfigurable(new(
            "Monk Spawns With Fruit",
            SlugcatStats.Name.Yellow,
            RainMeadow.rainMeadowOptions.ArenaMonkShield,
            nameof(ArenaOnlineGameMode.arenaMonkShield),
            "Monk starts each arena round holding a dangle fruit")
        );
    }
    public VanillaSetting(Menu.Menu menu, MenuObject owner) : base(menu, owner)
    {
        AddDescriptionToElement(MonkShieldDescription, GetSettingParameter(RainMeadow.rainMeadowOptions.ArenaMonkShield));
    }
}

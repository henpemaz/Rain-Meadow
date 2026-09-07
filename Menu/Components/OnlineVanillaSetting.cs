using Menu;

namespace RainMeadow.UI.Components;

public class VanillaSetting : OnlineSlugcatSettings<VanillaSetting>
{
    public override string Name => "Vanilla Settings";
    static VanillaSetting()
    {
        AddSlugcatSettingsConfigurable(new(
            "Monk Spawns With Fruit",
            SlugcatStats.Name.Yellow,
            RainMeadow.rainMeadowOptions.ArenaMonkFruitSpawn,
            nameof(ArenaOnlineGameMode.monkFruitSpawn),
            "Monk starts each arena round holding a dangle fruit")
        );
    }
    public VanillaSetting(Menu.Menu menu, MenuObject owner) : base(menu, owner)
    {
    }
}

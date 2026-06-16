> Last Updated: **May 22nd, 2026**, <small>(v0.1.14.0)</small>

> Warning: This documentation is still WIP. It is mostly complete but will have mistakes and unfinished sections.<br>
> All known-to-be incomplete elements are marked with `TODO` or `(unfinished)` and a short description. If you have any suggestions or find any mistakes, please ping @OneLetterShor.
# Arena API

## Background
Your cool new game mode must be added to `ArenaOnlineGameMode.registeredGameModes` via `ArenaOnlineGameMode.AddExternalGameModes`.<br>
You can hook it pretty much anywhere as long as it comes before `ArenaOnlineLobbyMenu`'s constructor.

TODO: Add configurables used here for a quick reference.

## Registering Your Game Mode

1. Preferably, hook Rain Meadow's `ArenaOnlineGameMode` constructor and call the method post-orig.
```csharp
// RainMeadowHooks.cs
public static class RainMeadowHooks
{
    internal static void Apply()
    {
        _ = new Hook(
            typeof(ArenaOnlineGameMode).GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null,
                [ typeof(Lobby) ],
                null
            ),
            On_RainMeadow_ArenaOnlineGameMode_ctor
        );
    }
    
    private static void On_RainMeadow_ArenaOnlineGameMode_ctor(
        Action<ArenaOnlineGameMode, Lobby> orig,
        ArenaOnlineGameMode self,
        Lobby lobby)
    {
        orig(self, lobby);
        self.AddExternalGameModes(SuperArenaMode.Id, new SuperArenaMode());
    }
}
```

2. Create a class that inherits `ExternalArenaGameMode`. You'll want a static reference to your `ArenaSetup.GameTypeID`, and a method that checks if the active game mode is your game mode.
```csharp
// SuperArenaMode.cs
public sealed class SuperArenaMode : ExternalArenaGameMode
{
    public static ArenaSetup.GameTypeID Id { get; } = new(Plugin.Name); // Or whatever you want the game mode name to be.
    
    /// <exception cref="InvalidOperationException">Thrown if game mode is not registered.</exception>
    public static bool IsSuperArenaMode(ArenaOnlineGameMode arenaOnline, out SuperArenaMode superArena)
    {
        string name = Id.value;
        if (!arenaOnline.registeredGameModes.TryGetValue(name, out ExternalArenaGameMode registeredMode))
            throw new InvalidOperationException($"Could not find game mode. registered: [ {string.Join(", ", arenaOnline.registeredGameModes.Keys)} ]");
        
        superArena = null!;
        if (arenaOnline.currentGameMode == name)
        {
            superArena = (SuperArenaMode)registeredMode;
            return true;
        }
    }
}
```
> Note: Do not register your extenum unless you intend to make it playable outside Rain Meadow.

3. Add properties to get the custom data for your mode. These classes will be defined later, so don't worry about errors yet.
```csharp
// SuperArenaMode.cs

/// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered yet.</exception>
public SuperArenaLobbyData LobbyData => OnlineManager.lobby!.GetData<SuperArenaLobbyData>();

/// <exception cref="KeyNotFoundException">Thrown if the client data is not registered yet.</exception>
public SuperArenaClientData MyClientData => OnlineManager.lobby!
                                                         .clientSettings[OnlineManager.mePlayer]
                                                         .GetData<SuperArenaClientData>();
```

4. Override the `GetGameModeId` property and other required members.
```csharp
// SuperArenaMode.cs
public override ArenaSetup.GameTypeID GetGameModeId => Id;

public override bool SpawnBatflies(FliesWorldAI self, int spawnRoom) => throw new NotImplementedException();

public override bool IsExitsOpen(
    ArenaOnlineGameMode arena,
    ExitManager.orig_ExitsOpen orig,
    ArenaBehaviors.ExitManager self)
{
    throw new NotImplementedException();
}

```
<br>

Your new game mode will now be accessible in the online Arena menu! 🥳

<br>

# State Data
If you want to leverage the state-system for syncing data look no further!<br>
`OnlineField` and its subtypes are the main way to sync data.

Quick Notes:
- Using `OnlineField`'s `group` parameter can reduce network usage. (explained later in this section)
- Rain Meadow only supports the most common types by default.
  In some cases, such as enums, you may want to cast the type to a serializable type (most primitives work).
  If that is not possible, you can define custom serialization via Rain Meadow's API.
- `OnlineFieldHalf` halves the precision of floats to save network usage.
  You will almost always want to use this for floats.

## Lobby Data

1. Hook `ArenaOnlineGameMode.ResourceAvailable` and add your data post-orig.
```csharp
// RainMeadowHooks.cs
internal static void Apply()
{
    _ = new Hook(
        typeof(Lobby).GetMethod(
            nameof(ArenaOnlineGameMode.ResourceAvailable),
            BindingFlags.NonPublic | BindingFlags.Instance
        ),
        On_RainMeadow_ArenaOnlineGameMode_ResourceAvailable
    );
}

private static void On_RainMeadow_ArenaOnlineGameMode_ResourceAvailable(
    Action<ArenaOnlineGameMode, OnlineResource> orig,
    ArenaOnlineGameMode self,
    OnlineResource onlineResource)
{
    orig(self, onlineResource);
    
    if (onlineResource is Lobby lobby && lobby.isOwner) 
        lobby.AddData(new SuperArenaLobbyData());
}
```

2. Create a class that inherits from `OnlineResource.ResourceData`.
```csharp
// SuperArenaLobbyData.cs
public sealed class SuperArenaLobbyData : OnlineResource.ResourceData
{
    
}
```

3. Add setting properties as needed and the `ApplySetting` method to save to configurables.
```csharp
// SuperArenaLobbyData.cs

/// <remarks>
/// Settings are automatically saved unless
/// <see cref="CanApplySettings"/> is <see langword="false"/>.
/// </remarks>
public sealed class SuperArenaLobbyData : OnlineResource.ResourceData
{
    public static bool CanApplySettings { get; set; } = true;
    
    public bool CanPlayersFly { get; set => ApplySetting(value, out field, Plugin.Options.CfgCanPlayersFly); } = Plugin.Options.CanPlayersFly;
    
    /// <summary>
    /// Clamps the value based on <paramref name="configurable"/>
    /// and writes to it if both the me player is the host and
    /// <see cref="CanApplySettings"/> is <see langword="true"/>.
    /// </summary>
    private void ApplySetting<T>(T value, out T field, Configurable<T> configurable)
    {
        value = configurable.ClampValue(value);
        
        if (CanApplySettings && OnlineManager.lobby!.isOwner)
            configurable.Value = value;
        
        field = value;
    }
}
```

4. Use `OnlineField`, `SuperArenaLobbyData.State.ctor`, and `SuperArenaLobbyData.State.ReadTo` to store, write, and read data repectively.
```csharp
// SuperArenaLobbyData.cs

/// <remarks>
/// Settings are automatically saved unless
/// <see cref="CanApplySettings"/> is <see langword="false"/>.
/// </remarks>
public sealed class SuperArenaLobbyData : OnlineResource.ResourceData
{
    public static bool CanApplySettings { get; set; } = true;
    
    public bool CanPlayersFly { get; set => ApplySetting(value, out field, Plugin.Options.CfgCanPlayersFly); } = Plugin.Options.CanPlayersFly;
    
    /// <summary>
    /// Clamps the value based on <paramref name="configurable"/>
    /// and writes to it if both the me player is the host and
    /// <see cref="CanApplySettings"/> is <see langword="true"/>.
    /// </summary>
    /// <exception cref="NullReferenceException">Thrown if there is no <see cref="Lobby"/>.</exception>
    private void ApplySetting<T>(T value, out T field, Configurable<T> configurable)
    {
        value = configurable.ClampValue(value);
        
        if (CanApplySettings && OnlineManager.lobby!.isOwner)
            configurable.Value = value;
        
        field = value;
    }
    
    public override ResourceDataState MakeState(OnlineResource resource) => new State(this, (Lobby)resource);
    
    public sealed class State : ResourceDataState
    {
        private const string _settings = nameof(_settings);
        
        [OnlineField(group = _settings)]
        public bool CanPlayersFly;
        
        /// <remarks>Rain Meadow requires a constructor with no parameters.</remarks>
        public State() { }
        
        public State(SuperArenaLobbyData data, Lobby lobby)
        {
            ArenaOnlineGameMode arenaOnline = (ArenaOnlineGameMode)lobby.gameMode;
            
            if (!SuperArenaMode.IsSuperArenaMode(arenaOnline, out _)) return;
            
            CanPlayersFly = data.CanPlayersFly;
        }
        
        public override void ReadTo(OnlineResource.ResourceData data_, Lobby lobby)
        {
            SuperArenaLobbyData data = (SuperArenaLobbyData)data_;
            Lobby lobby = (Lobby)onlineResource;
            ArenaOnlineGameMode arenaOnline = (ArenaOnlineGameMode)lobby.gameMode;
            
            if (!SuperArenaMode.IsSuperArenaMode(arenaOnline, out _)) return;
            
            data.CanPlayersFly = CanPlayersFly;
        }
        
        public override Type GetDataType() => typeof(SuperArenaLobbyData);
    }
}
```

## Client Data

Creating client data is very similar to creating lobby data.
Just note that `SuperArenaClientData.MakeState` and `SuperArenaClientData.State.ReadTo` are not called for the me player. TODO: Is this true?

1. Hook `ArenaOnlineGameMode.AddClientData` and add your data post-orig.
```csharp
// RainMeadowHooks.cs
internal static void Apply()
{
    _ = new Hook(
        typeof(ArenaOnlineGameMode).GetMethod(
            nameof(ArenaOnlineGameMode.AddClientData),
            BindingFlags.Public | BindingFlags.Instance
        ),
        On_RainMeadow_ArenaOnlineGameMode_AddClientData
    );
}

private static void On_RainMeadow_ArenaOnlineGameMode_AddClientData(Action<ArenaOnlineGameMode> orig, ArenaOnlineGameMode self)
{
    orig(self);
    self.clientSettings.AddData(new SuperArenaClientData());
}
```

2. Create a class that inherits `OnlineEntity.EntityData`.
```csharp
public sealed class SuperArenaClientData : OnlineEntity.EntityData
{
    
}
```

3. Add setting properties as needed and the `ApplySetting` method to save to configurables. 
   Note that only settings that should persist after the lobby is left should save to configurables (e.g., use `ApplySettings`).
```csharp
// SuperArenaLobbyData.cs

/// <remarks>
/// Settings are automatically saved unless
/// <see cref="CanApplySettings"/> is <see langword="false"/>.
/// </remarks>
public sealed class SuperArenaClientData : OnlineEntity.EntityData
{
    public static bool CanApplySettings { get; set; } = true;
    
    public bool IsInjured { get; set; } = false; // This should not be remembered because it is a temperary effect that doesn't persist between rounds (and by extension, lobbies).
    public bool CanGamble { get; set => ApplySetting(value, out field, Plugin.Options.CfgCanGamble); } = Plugin.Options.CanGamble; // This should be remembered because it is chosen by the player in the menu.
    
    /// <summary>
    /// Clamps the value based on <paramref name="configurable"/>
    /// and writes to it if both the client data is for the me player and
    /// <see cref="CanApplySettings"/> is <see langword="true"/>.
    /// </summary>
    private void ApplySetting<T>(T value, out T field, Configurable<T> configurable)
    {
        value = configurable.ClampValue(value);
        
        // TODO: Only save if the client data is for the me player. 
        if (CanApplySettings)
            configurable.Value = value;
        
        field = value;
    }
}
```

4. Use `OnlineFieldAttribute`, `SuperArenaClientData.State.ctor`, and `SuperArenaClientData.State.ReadTo` to store, write, and read data respectively.
```csharp
// SuperArenaClientsData.cs

/// <remarks>
/// Settings are automatically saved unless
/// <see cref="CanApplySettings"/> is <see langword="false"/>.
/// </remarks>
public sealed class SuperArenaClientData : OnlineEntity.EntityData
{
    public static bool CanApplySettings { get; set; } = true;
    
    public bool IsInjured { get; set; } = false; // This should not be remembered because it is a temperary effect that doesn't persist between rounds (and by extension, lobbies).
    public bool CanGamble { get; set => ApplySetting(value, out field, Plugin.Options.CfgCanGamble); } = Plugin.Options.CanGamble; // This should be remembered because it is chosen by the player in the menu.
    
    /// <summary>
    /// Clamps the value based on <paramref name="configurable"/>
    /// and writes to it if both the client data is for the me player and
    /// <see cref="CanApplySettings"/> is <see langword="true"/>.
    /// </summary>
    private void ApplySetting<T>(T value, out T field, Configurable<T> configurable)
    {
        value = configurable.ClampValue(value);
        
        // TODO: Only save if the client data is for the me player. 
        if (CanApplySettings)
            configurable.Value = value;
        
        field = value;
    }
    
    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource resource)
    {
        return new State(this);
    }
    
    public sealed class State : EntityDataState
    {
        private const string _settings = nameof(_settings);
        
        [OnlineField(group = _settings)]
        public bool IsInjured;
        
        [OnlineField(group = _settings)]
        public bool CanGamble;
        
        /// <remarks>Rain Meadow requires a constructor with no parameters.</remarks>
        public State() { }
        
        public State(SuperArenaClientData data)
        {
            IsInjured = data.IsInjured;
            CanGamble = data.CanGamble;
        }
        
        public override void ReadTo(OnlineEntity.EntityData data_, OnlineEntity onlineEntity)
        {
            SuperArenaClientData data = (SuperArenaClientData)data_;
            data.IsInjured = IsInjured;
            data.CanGamble = CanGamble;
        }
        
        public override Type GetDataType() => typeof(SuperArenaClientData);
    }
}
```
<br>

### What's with the "group = _settings"?
Field-groups are a way to organize fields in groups that each have that boolean flag for sent-or-skipped.

Each group means an extra bool that is continuously sent on every message about that resource/entity.

If any field in the field-group has changed, that entire field-group will be re-sent (because that's more efficient that just supporting every single field be optional).

<br>

# UI
## In-Lobby: Adding Custom Tabs
1. Create a class that inherits `MenuObject`. Typically `TabContainer.Tab` is the most convenient class.
```csharp
// SuperArenaSettingsTab.cs
public sealed class SuperArenaSettingsTab : TabContainer.Tab
{
    
}
```

2. Create some properties to access common objects which are passed into the constructor.
```csharp
// SuperArenaSettingsTab.cs
public sealed class SuperArenaSettingsTab : TabContainer.Tab
{
    private ArenaOnlineGameMode ArenaOnline { get; }
    private ArenaOnlineLobbyMenu ArenaOnlineMenu { get; }
    private SuperArenaMode SuperArena { get; }
    
    public SuperArenaSettingsTab(
        ArenaOnlineLobbyMenu menu,
        ArenaOnlineGameMode arenaOnline,
        SuperArenaMode superArena) : base(arenaOnlineMenu, arenaOnlineMenu.arenaMainLobbyPage.tabContainer)
    {
        ArenaOnline = arenaOnline;
        SuperArena = superArena;
        ArenaOnlineMenu = arenaOnlineMenu;
    }
}
```

3. Now you just need to create some UI elements.
```csharp
// SuperArenaSettingsTab.cs
public sealed class SuperArenaSettingsTab : TabContainer.Tab
{
    private ArenaOnlineGameMode ArenaOnline { get; }
    private ArenaOnlineLobbyMenu ArenaOnlineMenu { get; }
    private SuperArenaMode SuperArena { get; }

    public ProperlyAlignedMenuLabel ExampleLabel { get; }
    public OpCheckBox CanPlayersFlyCheckBox { get; }
    public OpCheckBox CanGambleCheckBox { get; }
    
    public SuperArenaSettingsTab(
        ArenaOnlineLobbyMenu arenaOnlineMenu,
        ArenaOnlineGameMode arenaOnline,
        SuperArenaMode superArena) : base(arenaOnlineMenu, arenaOnlineMenu.arenaMainLobbyPage.tabContainer)
    {
        ArenaOnline = arenaOnline;
        SuperArena = superArena;
        ArenaOnlineMenu = arenaOnlineMenu;
        
        ExampleLabel = new ProperlyAlignedMenuLabel(
            ArenaOnlineMenu,
            this,
            "This is an example of a label.",
            new Vector2(50f, 300f),
            new Vector2(100f, 25f),
            false
        );
        
        CanPlayersFlyCheckBox = new OpCheckBox(
            new Configurable<bool>( // Clone configurable (with a default of the current value)
                Plugin.Options.CanPlayersFly,
                Plugin.Options.CfgCanPlayersFly.info
            ),
            new Vector2(50f, 50f)
        );
        CanPlayersFlyCheckBox.OnValueChanged += (_, _, _) =>
        {
            if (OnlineManager.lobby!.isOwner)
                SuperArena.LobbyData.CanPlayersFly = CanPlayersFlyCheckBox.GetValueBool();
        };
        
        CanGambleCheckBox = new OpCheckBox(
            new Configurable<bool>( // Clone configurable (with a default of the current value)
                Plugin.Options.CanGamble,
                Plugin.Options.CfgCanGamble.info
            ),
            new Vector2(100f, 50f)
        );
        CanGambleCheckBox.OnValueChanged += (_, _, _) =>
        {
            SuperArena.MyClientData.CanGamble = CanGambleCheckBox.GetValueBool();
        };
        
        AddUIElements(CanPlayersFlyCheckBox, CanGambleCheckBox);
        this.SafeAddSubobjects(ExampleLabel);
        
        return;
        
        void AddUIElements(params UIelement[] uiElements)
        {
            foreach (UIelement uiElement in uiElements)
            {
                _ = new PatchedUIelementWrapper(
                    myTabWrapper,
                    uiElement
                );
            }
        }
    }
}
```
> Note: It may be wise to create a helper method to make cloning easier to read. See Hide and Seek's helper method [here](https://github.com/One-Letter-Shor/HideAndSeek/blob/develop/src/HideAndSeek/Utils/ConfigurableHelper.cs).

4. Finally, update your `UIelement`s and `MenuObject`s as needed.
```csharp
// SuperArenaSettingsTab.cs
public override void Update()
{
    MyLobbySettingCheckBox.greyedOut = SettingDisabled;
    MyClientSettingCheckBox.greyedOut = ArenaOnline.initiateLobbyCountdown;
    
    base.Update();

    SeekerSelectionSelector.value = SuperArena.EnabledSeekerSelection.ToString();
    TagResultSelector.value = SuperArena.EnabledTagResult.ToString();
}
```
> Note: If you find yourself adding many `UIfocusable`s you may want to use collections for lobby and client settings. See Hide and Seek's usage of arrays (in the update method) [here](https://github.com/One-Letter-Shor/HideAndSeek/blob/develop/src/HideAndSeek/Arena/HideAndSeekSettingsTab.cs).

<br>

### Adding and Removing Tabs

> Note: Because you are in the `SuperArenaMode` class but working on the UI, it is standard to make it a partial class and put UI-related members in `SuperArenaMode.UI.cs`.

Now that you have a full tab class you simply need to add it in the `SuperArenaMode.OnUIEnabled` method.
```csharp
// SuperArenaMode.UI.cs into a
public SuperArenaSettingsTab? SettingsTab { get; }

public override void OnUIEnabled(ArenaOnlineLobbyMenu menu)
{
    ArenaOnlineGameMode arenaOnline = (ArenaOnlineGameMode)OnlineManager.lobby!.gameMode;
        
    base.OnUIEnabled(menu);
    
    SettingsTab = new SuperArenaSettingsTab(
        menu,
        arenaOnline,
        this
    );
    
    menu.arenaMainLobbyPage.tabContainer.AddTab(SettingsTab, Plugin.Name);
}
```

Remember to remove it in `OnUIDisabled` and that `OnUIShutdown` calls `OnUIDisabled`.
```csharp
// SuperArenaMode.UI.cs
public override void OnUIDisabled(ArenaOnlineLobbyMenu menu)
{
    if (SettingsTab is null) return;
    
    SettingsTab.RemoveSprites();
    menu.arenaMainLobbyPage.tabContainer.RemoveTab(SettingsTab);
    
    SettingsTab = null;
    base.OnUIDisabled(menu);
}

public override void OnUIShutDown(ArenaOnlineLobbyMenu menu) => OnUIDisabled(menu);
```
> Note: Because Rain Meadow will sometimes call `OnUIDisabled` and `OnUIShutDown` without calling `OnUIEnabled` inbetween, you must ensure you do not dereference null members in `OnUIDisabled`.<br>
> The shutting down methods are only called back to back when <>.

TODO: When are the methods called back to back? Is it when exiting a round early?

<br>

### In-Game: Adding and Updating Custom Icons
Leverage `ExternalArenaGameMode`'s virtual functions to do things such as changing the sprite displayed above the players' head and the sprite's color.
```csharp
// SuperArenaMode.UI.cs
public override string AddIcon(
    ArenaOnlineGameMode arenaOnline,
    OnlinePlayerDisplay display,
    PlayerSpecificOnlineHud onlineHud,
    SlugcatCustomization customization,
    OnlinePlayer oPlayer)
{
    if (onlineHud.clientSettings.owner == OnlineManager.lobby!.owner)
        return "ChieftainA";
    
    return base.AddIcon(arenaOnline, display, onlineHud, customization, oPlayer);
}

public override Color IconColor(
    ArenaOnlineGameMode arenaOnline,
    OnlinePlayerDisplay display,
    PlayerSpecificOnlineHud onlineHud,
    SlugcatCustomization customization,
    OnlinePlayer oPlayer)
{
    if (onlineHud.PlayerConsideredDead)
        return Color.grey;
    if (arenaOnline.reigningChamps?.list?.Contains(oPlayer.id) == true)
        return Color.yellow;
    
    return base.IconColor(arenaOnline, display, onlineHud, customization, oPlayer);
}
```
<br>

### Adding Slugcat Settings (TODO: Unfinished, view old docs.)
Slugcat abilities tab has support to add your custom settings for slugcats.

1. Create a class that inherits `SettingsPage`. (unfinished)
```csharp
public sealed class SuperArenaSettingsPage : SettingsPage
{
    private ArenaOnlineGameMode ArenaOnline { get; }
    private SuperArenaMode SuperArena { get; }
    private ArenaOnlineLobbyMenu ArenaOnlineMenu { get; }
    
    public override string Name => Plugin.Name; // This will appear on Select Settings Page.
    
    public OpUpdown FlightDurationSecondsUpdown { get; };
    public SimpleButton BackButton { get; private set; };
    
    public SuperArenaSettingsPage(Menu.Menu menu, MenuObject owner) : base(menu, owner)
    {
        FlightDurationSeconds = new(
            new Configurable<int>( // Clone configurable (with a default of the current value)
                Plugin.Options.FlightDurationSeconds,
                Plugin.Options.CfgFlightDurationSeconds.info,
            ),
            new Vector2(0f, 0f),
            40f
        );
        
        FlightDurationSecondsUpdown.OnValueChanged += (_, _, _) =>
        {
            Plugin.Options.FlightDurationSeconds = FlightDurationSecondsUpdown.GetValueInt();
        };
    }

    public override void SelectAndCreateBackButtons(SettingsPage previousSettingPage, bool shouldSelectButton)
    {
        if (BackButton is null) // TODO: Test when this is null. Update nullable annotations accordingly.
        {
            BackButton = new SimpleButton(
                menu,
                this,
                menu.Translate("BACK"),
                OnlineSlugcatAbilitiesInterface.BACKTOSELECT, // Must OnlineSlugcatAbilitiesInterface.BACKTOSELECT to return to Select Settings Page.
                new Vector2(30f, 30f),
                new Vector2(80f, 30f)
            );

            AddObjects(BackButton);
        }

        if (shouldSelectButton)
            menu.selectedObject = BackButton;
    }
    
    public override void Update()
    {
        FlightDurationSecondsUpdown.greyedOut = SettingsDisabled
        
        if (!FlightDurationSecondsUpdown.held)
            FlightDurationSecondsUpdown.SetValueInt(SuperArena.FlightDurationSeconds);
    }
}
```
You can check both `OnlineSlugcatAbilitiesInterface.MSCSettings` and `OnlineSlugcatAbilitiesInterface.WatcherSettings` [here](https://github.com/henpemaz/Rain-Meadow/blob/main/Menu/Components/OnlineSlugcatAbilitiesInterface.cs) as an example.

Once you made the class, you can start hooking. (unfinished)
```csharp
// RainMeadowHooks.cs
internal static void Apply()
{
    _ = new Hook(
        typeof(OnlineSlugcatAbilitiesInterface).GetMethod(
            nameof(OnlineSlugcatAbilitiesInterface.AddAllSettings),
            BindingFlags.Public | BindingFlags.Instance
        ),
        On_RainMeadow_UI_Components_OnlineSlugcatAbilitiesInterface_AddAllSettings
    );
    
    _ = new Hook(
        typeof(ArenaMainLobbyPage).GetMethod(
            nameof(ArenaMainLobbyPage.ShouldOpenSlugcatAbilitiesTab),
            BindingFlags.Public | BindingFlags.Instance
        ),
        On_RainMeadow_UI_Pages_ArenaMainLobbyPage_ShouldOpenSlugcatAbilitiesTab
    );
}

private static void On_RainMeadow_UI_Components_OnlineSlugcatAbilitiesInterface_AddAllSettings(
    Action<OnlineSlugcatAbilitiesInterface, string> orig,
    OnlineSlugcatAbilitiesInterface self,
    string painCatName)
{
    orig(self, painCatName);
    self.AddSettingsTab(new SuperArenaSettingsPage(self.menu, self));
}

// This hook is optional. Slugcat Abilities Tab only appears when MSC or Watcher is on.
// Add this if you want your settings to appear without MSC AND Watcher. TODO: Is this true?
private static bool On_RainMeadow_UI_Pages_ArenaMainLobbyPage_ShouldOpenSlugcatAbilitiesTab(
    Func<ArenaMainLobbyPage, bool> orig,
    ArenaMainLobbyPage self)
{
    return true;
}
```
<br>

# References

- https://github.com/One-Letter-Shor/HideAndSeek — Hide 'N Seek<br>
TODO: Link to a Rain Meadow arena template here.

<br>

# Beyond
If you've found any syntax errors, logic errors, or just find anything hard to understand, please ping @OneLetterShor about the issue.<br>

There are a number of virtual functions available for you in `ExternalArenaGameMode` to leverage for arena gameplay.
Check out [BaseGameMode.cs](https://github.com/henpemaz/Rain-Meadow/blob/main/Arena/ArenaOnlineGameModes/BaseGameMode.cs) for a full list. They are added as a convenience. If you don't want to use them, hook your own.

Best of luck, and ping @UO when you've made a new game mode!

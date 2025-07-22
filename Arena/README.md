# Arena API

## Background
Your cool new gamemode must be added to the `arena.registeredGameModes`. You can do this using the method, `arena.AddExternalGameModes(string, ExternalOnlineGameMode)`. You can hook it pretty much anywhere as long as it's coming before the ArenaOnlineLobbyMenu ctor. Preferably, hook Meadow's  `ArenaOnlineGameMode` ctor and call the method at the end.

## Registering a new game mode

1. Make a new file that includes your hooks and plugin information:
2. 
```csharp
// MyCoolNewModPlugin.cs
using BepInEx;
using IL;
using RainMeadow;
using System;
using System.Security.Permissions;
using UnityEngine;

//#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace MyNamespace
{
    [BepInPlugin("YOUR_USERNAME.YOUR_PLUGIN_NAME", "FRIENDLY_PLUGIN_NAME", "0.1.0")]
    public partial class MyMod : BaseUnityPlugin
    {
        public static MyMod instance;
        private bool init;
        private bool fullyInit;
        private bool addedMod = false;
        public void OnEnable()
        {
            instance = this;

            On.RainWorld.OnModsInit += RainWorld_OnModsInit;

        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            if (init) return;
            init = true;

            try
            {

                On.Menu.Menu.ctor += Menu_ctor;
                fullyInit = true;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                fullyInit = false;
            }
        }

        private void Menu_ctor(On.Menu.Menu.orig_ctor orig, Menu.Menu self, ProcessManager manager, ProcessManager.ProcessID ID)
        {
            orig(self, manager, ID);
            if (self is ArenaOnlineLobbyMenu)
            {
                AddNewMode();
            }
        }

        private void AddNewMode()
        {
            
            if (RainMeadow.RainMeadow.isArenaMode(out var arena))
            {
                arena.AddExternalGameModes(MyNewExternalArenaGameMode.MyGameModeName, new myNewGamemode());
            }

        }
    }
}
```
3. Make a file that includes your mod's inheritance from Arena's ExternalGameMode:
4. 
```csharp
// ExternalCoolGame.cs
using RainMeadow;
using System.Text.RegularExpressions;
using Menu;

namespace MyNamespace
{
    public class MyCoolNewGameMode : ExternalArenaGameMode
    {

        public static ArenaSetup.GameTypeID MyGameModeName = new ArenaSetup.GameTypeID("MyGameModeName", register: false);
        }
    }
}
```
5. Override the arena.externalGameMode's `GetGameModeId`
(**NOTE**: Must match enum's value you set in `arena.registeredGameModes`)
6. 
```csharp
public override ArenaSetup.GameTypeID GetGameModeId
{
    get
    {
        return MyGameModeName; // Set to YOUR cool game mode
    }
    set { GetGameModeId = value; }
}
```
7. Your new game mode will now be accessible in the online Arena menu!

## GameMode Check
```csharp
public static bool isMyCoolGameMode(ArenaOnlineGameMode arena, out MyCoolNewGameMode tb)
{
    tb = null;
    if (arena.currentGameMode == MyGameModeName.value)
    {
        tb = (arena.registeredGameModes.FirstOrDefault(x => x.Key == MyGameModeName.value).Value as MyCoolNewGameMode);
        return true;
    }
    return false;
}
```

# State Data
For when you want to leverage the state-system for syncing variables. Consider adding "group=<someGroup>" to your state variable.
## LobbyData

```csharp
internal class MyCoolLobbyData : OnlineResource.ResourceData
    {
        public MyCoolLobbyData() { }

        public override ResourceDataState MakeState(OnlineResource resource)
        {
            return new State(this, resource);

        }

        internal class State : ResourceDataState
        {
            [OnlineField group="myGroup"]
            public bool isInGame;
            {
                ArenaOnlineGameMode arena = (onlineResource as Lobby).gameMode as ArenaOnlineGameMode;
                bool myMode = MyCoolNewGameMode.isMyCoolGameMode(arena, out var coolMode);
                if (myMode) {
                            
                isInGame = coolMode.isInGame;
                }   

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                var lobby = (resource as Lobby);
                  bool myMode = MyCoolNewGameMode.isMyCoolGameMode(arena, out var coolMode);
                  if (myMode) {
                coolMode.isInGame = isInGame;
                  }
            }

            public override Type GetDataType() => typeof(ArenaLobbyData);
        }
    }
```
### What's with the "group='myGroup"?
```
Field-groups are a way to organize fields in groups that each have that boolean flag for sent-or-skipped.

Each group means an extra bool that is continuously sent on every message about that resource/entity.

If any field in the field-group has changed, that entire field-group will be re-sent (because that's more efficient that just supporting every single field be optional).
```

```csharp
 public override void ResourceAvailable(OnlineResource onlineResource)
 {
     base.ResourceAvailable(onlineResource);

     if (onlineResource is Lobby lobby)
     {
         lobby.AddData(new MyCoolLobbyData()); 
     }
 }
```
Check [ArenaLobbyData](https://github.com/henpemaz/Rain-Meadow/blob/main/Arena/ArenaLobbyData.cs) for example utilization. 
## ClientData

```csharp
 public class MyClientSettings : OnlineEntity.EntityData
 {
     public int someonesNumber;

     public MyClientSettings() { }

     public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
     {
         return new State(this);
     }

     public class State : EntityDataState
     {
         [OnlineField]
         public int someonesNumber;
         public State() { }

         public State(MyClientSettings onlineEntity) : base()
         {
             someonesNumber = onlineEntity.someonesNumber;
         }

         public override void ReadTo(OnlineEntity.EntityData entityData, OnlineEntity onlineEntity)
         {
             var avatarSettings = (MyClientSettings)entityData;
             avatarSettings.someonesNumber = someonesNumber;
         }

         public override Type GetDataType() => typeof(MyClientSettings);
     }
 }
```

```
        public override void AddClientData()
        {
            clientSettings.AddData( new MyClientSettings());

        }
```
# UI

## Menu
### Adding Menu Objects
ExternalArenaGameMode provides access to the ArenaOnlineLobbyMenu with the following methods:
```
OnUIEnabled, OnUIDisabled, OnUIUpdate, OnUIShutdowdn

# NOTE: OnUIDisabled can be called from the menu's ctor, check for null references if used
```

### Adding Tabs
```csharp
base.OnUIEnabled(menu);
myTab = menu.arenaMainLobbyPage.tabContainer.AddTab("My Tab");
myTab.AddObjects(myInterface = new MyCoolNewInterface((ArenaMode)OnlineManager.lobby.gameMode, this, myTab.menu, myTab, new(0, 0), menu.arenaMainLobbyPage.tabContainer.size));
```

### Remove Tabs
```csharp
base.OnUIDisabled(menu);
myCoolNewInterface?.OnShutdown();
if (myTab != null) menu.arenaMainLobbyPage.tabContainer.RemoveTab(myTab);
myTab = null;
foreach (ArenaPlayerBox playerBox in menu.arenaMainLobbyPage.playerDisplayer?.GetSpecificButtons<ArenaPlayerBox>() ?? [])
{
    if (!playerBoxes.TryGetValue(playerBox, out IfIMadeCoolObjectsInPlayerBoxes customBoxStuff)) continue;
    playerBox.ClearMenuObject(customBoxStuff);
    playerBoxes.Remove(playerBox);
}
```

### UI - In-Game: Adding or Updating Custom Icons
Leverage ExternalArenaGameMode's virtual functions
```csharp
    public override string AddIcon(ArenaOnlineGameMode arena, PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player)
        {

            if (owner.clientSettings.owner == OnlineManager.lobby.owner)
            {
                return "ChieftainA";
            }
            return base.AddIcon(arena, owner, customization, player);

        }

    public override Color IconColor(ArenaOnlineGameMode arena, PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player)
        {
            if (owner.PlayerConsideredDead)
            {
                return Color.grey;
            }
            if (arena.reigningChamps != null && arena.reigningChamps.list != null && arena.reigningChamps.list.Contains(player.id))
            {
                return Color.yellow;
            }

            return base.IconColor(arena, owner, customization, player);
        }
```

## Beyond
There are a number of virtual functions available for you  in ExternalArenaGameMode to leverage for Arena gameplay. Check the [BaseGameMode.cs](https://github.com/henpemaz/Rain-Meadow/blob/main/Arena/ArenaOnlineGameModes/BaseGameMode.cs) for a full list. They are added as a convenience. If you don't want to use them, hook your own. Best of luck, and ping @UO when you've made a new game mode! 

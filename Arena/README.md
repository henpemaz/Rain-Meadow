# Arena API


## Registering a new game mode

1. Make a new file that includes your hooks and plugin information:
2. 
```
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
                arena.AddExternalGameModes(new myNewGamemode(),  MyNewExternalArenaGameMode.MyGameModeName);
            }

        }
    }
}
```
3. Make a file that includes your mod's inheritance from Arena's ExternalGameMode:
4. 
```
// ExternalCoolGame.cs
using RainMeadow;
using System.Text.RegularExpressions;
using Menu;

namespace MyNamespace
{
    public class MyNewExternalArenaGameMode : ExternalArenaGameMode
    {

        public static ArenaSetup.GameTypeID MyGameModeName = new ArenaSetup.GameTypeID("MyGameModeName", register: false);
        }
    }
}
```
5. Your new game mode will now be accessible in the online Arena menu! Use the large arrows at the top of the menu to switch modes.



## Using ExternalGameMode's Hooks

You can review all the hooks provided to you for your game mode in [BaseGameMode.cs](https://github.com/henpemaz/Rain-Meadow/blob/main/Arena/ArenaOnlineGameModes/BaseGameMode.cs).

### Hook Types:
If the function is virtual, you will inherit its contents unless you override it. Double check your hook if you are seeing unanticipated behavior. Some virtual functions are left blank since I had no need for them but decided it'd be nicer for others to have than performing an IL hook.
#### 1. Base + Custom:
```
public override void HUD_InitMultiplayerHud(ArenaOnlineGameMode arena, HUD.HUD self, ArenaGameSession session)
        {
            base.HUD_InitMultiplayerHud(arena, self, session); // typing, player icons, spectator
            self.AddPart(new StoreHUD(self, session.game.cameras[0], this)); // new HUD element for our game mode
        }
```
#### 2. Custom Mod hook outside ExternalGameMode

If you are going to use a custom hook not provided by ExternalGameMode, you MUST wrap it in an online arena check + your game mode:
```
if (RainMeadow.isArenaMode(out var arena) && arena.onlineArenaGameMode = arena.registeredGameModes.FirstOrDefault(kvp => kvp.Value == MyNewExternalArenaGameMode.MyGameModeName.value).Key) { // code here }
```

In this example, we don't want the default online arena HUD elements and we don't want to add our own at the normal hook point. Maybe we want to block all HUD elements in-game:
```
// ExternalCoolGame.cs

public override void HUD_InitMultiplayerHud(ArenaOnlineGameMode arena, HUD.HUD self, ArenaGameSession session)
        {
            // don't call base
        }
```
```
// in your relevant hook file

        private void HUD_InitMultiplayerHud(On.HUD.HUD.orig_InitMultiplayerHud orig, HUD.HUD self, ArenaGameSession session)
        {

            if (isArenaMode(out var arena) && arena.onlineArenaGameMode = arena.registeredGameModes.FirstOrDefault(kvp => kvp.Value == MyNewExternalArenaGameMode.MyGameModeName.value).Key)
            {
                
                // block orig
            }
            else
            {
                orig(self, session);
            }

        }
```


## Creating New Properties to Sync
WIP

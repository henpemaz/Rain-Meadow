# Contributing

If you would like to contribute to the project, please take the time to learn more about it:

Rainworld has a fairly unique codebase, it's not your average unity-game and it's more akin to a custom-engine built inside of unity. If you're not familiar with the game's codebase there will be a learning curve that might be easier to overcome with working on a simpler project first.

Rain Meadow is a multiplayer engine made specifically for Rain World. It's not as fancy and fully-featured as other engines out there but it fits Rainworld perfectly. Please watch [this video](https://www.youtube.com/watch?v=-_WsvZAkFZI) for an explanation of the architecture of the engine.

[![Rain Meadow Engine Overview](https://img.youtube.com/vi/-_WsvZAkFZI/0.jpg)](https://www.youtube.com/watch?v=-_WsvZAkFZI)

# Project Installation

Rain Meadow is set up to build into the `\Mod` directory. The best way to get that into your game during development is to symlink/junction that folder into the manually-installed-mods folder `rainworld_data\streamingassets\mods`.

# Common catches
## Locally installed mods don't load but workshop ones do:

Delete doorstop_config.ini from the game folder and have steam redownload it.

## Steam Deck / Linux mods don't load

Proton/Wine will only load dlls built in to its package by default, which breaks BepInEx modloading. To resolve this you must override the load order for `winhttp.dll` so that the game dll takes precedence:
1. In steam, right click Rain World
2. Navigate to properties
3. In the launch options window, enter `WINEDLLOVERRIDES="winhttp=n,b" %command%`
4. Launch the game and enable Rain Meadow

## Setup your workstation

### Prerequisites

Copied from https://rainworldmodding.miraheze.org/wiki/BepInPlugins for a basic code mod setup

Before you will be able to make a mod, you need the following:

1. Beginner knowledge of C#, such that you are comfortable with your current level of skill.
2. If you understand the concept of object oriented programming as well as main C# elements such as methods, fields, properties, events, and delegates, you should be set to go.
3. Unity knowledge is optional, but useful for advanced modding. Unlike other BepInEx-modded games, Rain World's ties into Unity are sparse due to the use of the Futile engine wrapper, thus the knowledge is not required.
4. Entry-level skills in reverse engineering, using tools like dnSpy (or any other .NET decompiler of your choosing).
5. A .NET programming environment. You can view feasible options here: https://dotnet.microsoft.com/en-us/platform/tools
6. Windows users will want Visual Studio (not Visual Studio Code, they are similar but not the same!)
6. NET Framework 4.8 Developer Pack (not runtime!) which you can download here: https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48
	
> Create a folder named 'plugins' for the build output to work correctly

### Assemblies

Dependencies aren't included in the repo for several reasons. You will need to add the following assemblies to the `lib/` folder:

```
BepInEx/core/0Harmony.dll
BepInEx/core/BepInEx.Harmony.dll
BepInEx/core/BepInEx.dll
BepInEx/core/HarmonyXInterop.dll
BepInEx/core/Mono.Cecil.dll
BepInEx/core/MonoMod.RuntimeDetour.dll
BepInEx/core/MonoMod.Utils.dll
BepInEx/core/MonoMod.dll
BepInEx/plugins/HOOKS-Assembly-CSharp.dll
BepInEx/utils/PUBLIC-Assembly-CSharp.dll
RainWorld_Data/Managed/Assembly-CSharp-firstpass.dll
RainWorld_Data/Managed/Newtonsoft.Json.dll
RainWorld_Data/Managed/Rewired.Runtime.dll
RainWorld_Data/Managed/Rewired_Core.dll
RainWorld_Data/Managed/Unity.Mathematics.dll
RainWorld_Data/Managed/UnityEngine.AssetBundleModule.dll
RainWorld_Data/Managed/UnityEngine.AudioModule.dll
RainWorld_Data/Managed/UnityEngine.CoreModule.dll
RainWorld_Data/Managed/UnityEngine.InputLegacyModule.dll
RainWorld_Data/Managed/UnityEngine.JSONSerializeModule.dll
RainWorld_Data/Managed/UnityEngine.dll
RainWorld_Data/Managed/com.rlabrecque.steamworks.net.dll
```

## Guidelines
###### Advice for development

### 0. This is a fan project by several people
Be nice, have fun, don't stress, talk to people, listen to people. Avoid writing code that only you will understand. Avoid needless rewrites that will force people to re-familizarize with the codebase.

### 1. Be mindful of the modding architecture
Modding typically uses hooks to expand on the game's behaviors. Hooks to the same can be used by several mods at once and they'll all run as long as each hook calls the original code as well. This is peak modding, players can have hundreds of small mods on and they mostly just work:tm:.

Sometimes though it might make more sense to instead use inheritance and overrides for more specialized objects if there's lots of behavior changes for an object. Be mindful that this type of change isn't stackable with other mods (can't automatically combine those inheritances, and if an override doesn't call base(), hooks to that base method won't run!), so this will only make sense for new, unique stuff. History lessons: the first re-skin mod out for 1.5 used inheritance for a more specialized playergraphics class and it was PAIN to interact/add compatibility with.

You might be tempted to copy-paste-and-adapt inside of a hook or override, this should be your last approach since it's not compatible with other mods at all. Before you resort to that, check if it wouldn't be a matter of doing a small IL-Hook to modify the code in question. Check also if it wouldn't be viable to let the game roll its changes, then unroll-and-redo-different afterwards, as a more compatible approach that lets other mods' hooks run as well.

### 2. Where appropriate, use primitives
Keeping your method or function's parameters set to primitives can greatly boost its re-usability across the project space. But don't make it too complicated either, if you're wasting too much time trying to make it fit more cases than it needs to, cut it. This is not a code-clean competition.

### 3. We won't teach you programming, but we can teach about the mod
Please be mindful of other people's time with the kind of questions you ask. General programming questions go into your favorite search engine first. The best use of everybody's time is that you talk about the unique aspects of modding or the unique aspects of the mod.

### 4. Test your sh\*t
A PR means it'll go into main and might go into the next build. Make sure to test your stuff and that it's working before you mark your PR as ready-for-review. Close/draft prs that shouldn't be merged.

### 5. Don't go rogue, don't overbloat the mod
Rain Meadow is a cool mod. We want to keep it that way. Conform to the vision the team has for its game modes, not use them as an excuse to push your own content. Ask other's opinions before you go into long tangents. Ask yourself "would this be better as external mod?".

At this stage in development, new big features or new gamemodes are likely more suitable as external mods, let them be an exercise on how to use and improve the API that we have exposed for external mods.

### 5.1 Don't go rogue, respect the DLC checks
Rain Meadow does not require any DLCs. This means the mod shouldn't have any DLC-exclusive features exposed to lobbies without the DLC.

It's ok to add compatibility for when the DLC is enabled, or to interact with DLC code to fix it, but stealing these features into non-DLC lobbies is not acceptable.

If you want some nice-to-have features such as playable creatures or quests or piggyback without the DLC, implement your own and DO NOT copypaste them, it's not hard to do better than the original.

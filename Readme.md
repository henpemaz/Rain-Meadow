# Rain Meadow
## This is a work in progress

![title banner](/Docs/banner.png)

## What is this

An online multiplayer experience built on top of Rain World. It's currently scoped to support Arena, Story, and a custom game-mode called Meadow.

Do you know [Meadow from Might&Delight](https://store.steampowered.com/app/486310/Meadow/), the Shelter series guys? If you don't, maybe you should! This custom game-mode is a reinterpretation of that multiplayer experience, but in the Rain World universe.

## How will it work

Rain Meadow is a mod that can be loaded as just any mod. Each player launches their game and joins a lobby. Lobbies can be created with different game-modes selected, and the lobby owner can tweak settings for a lobby. Hot-join is supported and configurable.

## What is the current state of this project

We're currently still refining the engine. It's a totally custom engine, since RW doesn't use Unity stuff much. We've started fronts on customizations for each game-mode as well.

![roadmap](/Docs/roadmap_w.png)

## Can I play it / Where can I find more

You can find playtesting builds and the full devlog over at [our Discord server](https://discord.gg/Ze3qaYq49j).

## How can we support this project?

Fuss about it, spread the word. You can also donate to the devs over at [my ko-fi](https://ko-fi.com/henpemaz). If you're an experienced c# dev or you make rainworld mods, you can join the team to try and help with development!

## Why not fix Monkland

When I started this project, my initial thought was that I'm not experienced enough to fully understand and fix someone else's mod, by making my own I'll learn all I need to learn along the way. Also there was a massive update to the game, and they've update the Steamworks library that is used, it's the perfect time to start over.

This seems to be true since now I can fully understand the code for Monkland and all of its limitations.

## Do you have any prior experiences with multiplayer?

I'm an Automation engineer with experience in devices communicating with industrial protocols over lossy, laggy networks, as well as server API design. While I haven't properly done game netcode before, I can design protocols and write serialization with some confidence.

# Project Installation

Rain Meadow builds it's mod files into the `\Mod` directory. 

While this project is still early in development, it is not recommended you install it directly from the repository, not all commits will contain a stable or up-to-date build.

Instead you should join the discord server and check pins in the #playtesting channel.

Instructions on how to install the mod into your game:

1. Open Steam
2. Right click Rain World
3. Manage > Browse Local Files
4. Navigate down to RainWorld_Data\StreamingAssets\mods
5. Follow next steps depending on where you downloaded the mod:
    - GitHub Repository (latest)
        1. Copy `\Mod` folder into game mods folder
        2. Rename directory to `rainmeadow`
    - #playtesting channel (stable)
        1. Extract downloaded zip file
        2. If you see a single folder named 'rainmeadow', copy the folder into your game mods folder


# Contributions

If you would like to contribute to the project, 

This is a mod which implements a custom made engine for multiplayer support.
Please watch this video with an explanation of the engine: 

[![Rain Meadow Engine Overview](https://img.youtube.com/vi/-_WsvZAkFZI/0.jpg)](https://www.youtube.com/watch?v=-_WsvZAkFZI)

[Direct Link to Video](https://www.youtube.com/watch?v=-_WsvZAkFZI)

## Dependencies

There is a `lib/` folder, this is where the assemblies will go, as a masochist rain world modder you likely know what to put in here.

## Restrictions

You should ensure all clients have the same mods enabled, it's probably just recommended you disable all your mods (including MoreSlugcats and Expedition), Remix should be fine.

## Speed Building (Optional)

You can choose to copy the build into your game manually, or go through some setup to speed up development productivity.

Just like the project installation section, the project builds to the `\Mod` directory, you can use this to create a symbolic link from the game files of your choosing to redirect to the folder:

- Open cmd.exe as Administrator
- Type (or copy) ```mklink /D "[GameDirectory]\RainWorld_Data\StreamingAssets\mods\rainmeadow" "[ProjectDirectory]\Mod"```

Now whenever you build the project, the changes should automatically reflect in-game.

## Launch Profiles (Optional)

You can choose to manually close and launch the game (via steam/opening exe) whenever you make a new change, *or* you can create a launch profile which enables use of the Start button in Visual Studio.

This is useful for two reasons:
- You can click "Launch Rain World", which starts the 'debugger', you can use `Ctrl + Shift + F5` or click the restart button to quickly restart the game, handy for testing while programming menus, etc.
- You can click "Start Without Debugging" or press `Ctrl + F5` to simply start an instance, allowing you to do so 2-5+ times, which is a fast and convenient way to test multiplayer with multiple games open.

Create a file in `Properties\launchSettings.json` with the contents:
```
{
  "profiles": {
    "Launch Rain World": {
      "commandName": "Executable",
      "executablePath": "C:\Program Files (x86)\Steam\steamapps\common\Rain World\RainWorld.exe"
    }
  }
}
```
Change the path to point at your game's executable. This is less easily achieved via mklink unless you want to create a symbolic link to the game's directory (targeting file alone doesn't contain game files).

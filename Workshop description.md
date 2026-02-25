A multiplayer engine, custom game mode, and online arena / story experience for Rain World!

## Table of Contents
* [What is it?](#what-is-it)
* [Compatibility](#compatibility)
* [Game Modes](#game-modes)
    * [Meadow](#meadow)
    * [Story](#story)
    * [Arena](#arena)
* [Communication and Moderation](#communication-and-moderation-story-and-arena-only)
* [Modding](#modding)
* [Credits](#credits)

---

# What is it?
Rain Meadow is a true online multiplayer engine for Rain World! It includes both Story and Arena gamemodes, as well as a new "Meadow" gamemode based on the titular game "Meadow" by Might and Delight. Simply enable the mod, and click the new "Meadow" button on the game's title screen to get started. Matchmaking is handled through Steam automatically with no additional setup required. Connections through LAN are also possible for those that own Rain World on GOG.

---

## Compatibility
* Meadow works on Windows, and Linux/MacOS through Wine/Proton, though for Wine/Proton you'll need to use the following launch argument: `WINEDLLOVERRIDES="winhttp=n,b" %command%`
* For the best possible experience, **disable all non-essential mods.** If you're experiencing crashes, desyncs, or similar issues, try trimming your mod list down to the absolute necessities. Even some theoretically client-side or meadow-compatible mods may cause problems.
* Anyone connecting to a host using "content" mods (that add regions, campaigns, objects, gameplay mechanics, etc) must use the same list of content mods, in the same order. Meadow has a mod-syncing tool that will prompt you to enable/disable/reorder mismatched mods when joining a lobby, but its detection isn't perfect.
* Remix, More Slugcats, and Watcher are considered content mods, though their load order in particular doesn't matter. Dev Tools and Jolly Coop both work client-side; Jolly lets you connect multiple scugs to a Meadow lobby from a single instance of Rain World. Expedition doesn't conflict with Meadow, but currently does *not* integrate in any way. All other mods may or may not work depending on how they are implemented; for a baseline you can check [this community-maintained compatibility spreadsheet.](https://docs.google.com/spreadsheets/d/1QG1xYPLECkVSMc2vopO-Rw2rSdnn7_fsdlMajhAUOW0/edit?gid=0#gid=0)

---

## Game Modes

### Meadow
* Explore a peaceful version of Rain World, free from rain or predators! Encounter other players, roam together gathering tokens to unlock new emotes, skins and even other creatures to play as.
* Communicate with gestures, vocalizations, and a full set of custom emotes for each creature, designed by @Wolfycat!
* Features an hour-long original soundtrack by @Intikus!
* Playable creatures' controls and navigation have been completely rebuilt to offer a smoother experience than Safari mode.

### Story
* Uses its own set of save slots, so it won't mess with your regular story saves.
* All vanilla campaigns are available by default, and all DLC campaigns are available with their respective DLCs enabled. Other campaigns can be accessed through the "Experimental Features" option in Meadow's remix settings.
* The Survivor, Monk, Hunter, and Gourmand campaigns are all fully supported and should be stable. All other Downpour campaigns should mostly work, but you may run into quirks or the occasional desync. Most of Watcher is playable but ***not*** yet very stable; expect bugs. Any other campaigns are not explicitly supported and will likely desync or crash on any "special events".
* All players can play as any slugcat, though the host has an option to restrict everyone to the current campaign's slugcat. The host can also restart the cycle at any time from the pause menu.
* Piggybacking is supported; jump next to a player's head and hold exclusively grab to climb onto their back. The piggybacker can press jump to dismount, or the piggybackee can drop them like an item.
* Food is shared between all players, and stomach size is based on the selected campaign. Diet and food nourishment are determined by each individual player's selected slugcat. For instance, playing on the Gourmand campaign will always use a 7|4 food bar, and playing as Hunter will always let *your* slugcat eat meat, but only get 1/4th pip from danglefruit.
* All players must either be unmoving in shelter(s), or dead for shelters to close. All players, dead or alive, must be physically present and unmoving at a karma gate for it to activate. In either case, players can exit to the character select to avoid being considered, and let others progress. If the session has gone through a gate, players waiting in the character select cannot rejoin until the next cycle.

### Arena
* Currently offers both Free-For-All, and Teams PvP formats. Dens open when there's only one scug/team left standing, or when the rain comes.
* All arenas from installed mods are always available for selection, including those normally exclusive to Downpour's challenge mode.
* A countdown timer blocks all methods of attack at the start of each round, ensuring all players have a chance to grab weapons and spread out.
* In addition to "standard" arena configuration, the host can change things like how long the countdown timer should last, or whether Saint should use ascension or spears.
* The host can ban any number of slugcats from being selected, by selecting their portrait in the character select and pressing grab.

---

## Communication and moderation (Story and Arena only)
* Press **Enter** to open the chatbar, and **Comma** to toggle the chatlog's visibility.
* Press **J** to toggle usernames and player icons, and **P** to toggle a ping display next to player names.
* Press **Tab** to open the spectator menu; you can click on other (live) players' usernames to switch your camera to them. The button next to each player will let non-hosts mute or unmute them, and let hosts permanently kick them from the lobby instance.
* Lobby hosts can tweak required and disallowed mods by opening Meadow's settings in the remix menu; the "General" tab has buttons that will open the configuration files for each in your default text editor.

---

## Modding
Meadow supports other mods adding custom gamemodes, as well as custom variations for arena mode. Here are some examples of both:
* [Example of a custom "Tag" gamemode.](https://github.com/henpemaz/RemixMods/tree/master/Tag)
* [Examples and documentation for custom arena variations.](https://github.com/6fears7/Arena-Online/tree/main)

---

## Credits

| Name | Role / Contribution |
| :--- | :--- |
| **Henpemaz** | Lead Developer |
| **Wolfycatt(Ana)** | Lead Artist |
| **Intikus** | Lead Audio Designer |
| **Noircatto** | Programming, Engine |
| **HelloThere** | Programming, Sound |
| **A1iex** | UI Design |
| **FranklyGD** | Programming, Engine |
| **MC41Games** | Programming, Menus |
| **Silvyger** | Programming, Arena |
| **Vigaro** | Programming, Menus |
| **BitiLope** | Programming, Story |
| **Pudgy Turtle** | Programming, Story |
| **ddemile** | Programming, Modsync |
| **UO** | Programming, Story, Arena |
| **Saddest** | Programming, UI, Chat |
| **notchoc** | Programming, Story |
| **phanie_** | Illustration |
| **Timbits** | Programming, UI, Menus |
| **Zedreus** | Programming, UI, Story |
| **Persondotexe** | Programming, Modsync |
| **invalidunits** | Programming, UI, LAN |
| **forthfora** | Programming, Modsync |
| **WillowWisp** | Programming, Story |
| **@None** | Programming, Gameplay |
| **DustyLemmy** | Arena, Story, UI |
| **Elizabeth** | Programming, Story, UI |
| **niacdoial** | LAN |
| **Capt. Redstone** | Meadow, Story |

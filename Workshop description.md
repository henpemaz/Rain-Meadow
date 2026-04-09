A multiplayer engine, custom game mode, and online arena / story experience for Rain World!

---

## What is Rain Meadow?
Rain Meadow is a true online multiplayer engine for Rain World! It includes both Story and Arena gamemodes, as well as a new "Meadow" gamemode based on the titular game "Meadow" by Might and Delight. Matchmaking is handled through Steam automatically. Connections through LAN are also possible for those that own Rain World on GOG.

---

## Compatibility
* Meadow works on Windows, and Linux/MacOS through Wine/Proton, though for Wine/Proton you'll need to use the following launch argument: `WINEDLLOVERRIDES="winhttp=n,b" %command%`
* For the best possible experience, **disable all non-essential mods.** If you're experiencing crashes, desyncs, or similar issues, trim your mod list down to the bare necessities. Even some theoretically client-side or meadow-compatible mods may cause problems.
* Anyone connecting to a host using "content" mods (that add regions, campaigns, objects, etc) must use the same list of mods, in the same order. Meadow has a mod-syncer that will prompt you to enable/disable/reorder mismatched mods when joining a lobby, but it's not perfect.
* Remix, More Slugcats, and Watcher are considered content mods, though their load order in particular doesn't matter.
* Jolly-Coop is supported, but everyone in the lobby will need it active. It lets you connect multiple scugs to a Meadow lobby from a single instance of Rain World
* Dev Tools is client-side
* Expedition doesn't conflict with Meadow, but is *not* supported at this time. All other mods may or may not work depending on how they are implemented; for a baseline you can check [this community-maintained compatibility spreadsheet.](https://docs.google.com/spreadsheets/d/1QG1xYPLECkVSMc2vopO-Rw2rSdnn7_fsdlMajhAUOW0/edit?gid=0#gid=0)

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
* The Survivor, Monk, Hunter, and Gourmand campaigns are all fully supported and should be stable. All other Downpour campaigns should mostly work, but you may run into quirks or the occasional desync. Most of Watcher is playable but expect bugs. Any other campaigns are not explicitly supported and will likely desync or crash on any "special events".
* All players can play as any slugcat, though the host has an option to restrict everyone to the current campaign's slugcat. The host can also restart the cycle at any time from the pause menu.
* Piggybacking is supported; jump next to a player's head and hold exclusively grab to climb onto their back. The piggybacker can press jump to dismount, or the piggybackee can drop them like an item.
* Food is shared between all players, and stomach size is based on the selected campaign. Diet and food nourishment are determined by each individual player's selected slugcat.
* All players must either be unmoving in shelter(s), or dead for shelters to close. All players, dead or alive, must be physically present and unmoving at a karma gate for it to activate. If there's a network issue or a player is dead and missing, players can exit to the character select and let others progress. If the session has gone through a gate, players waiting in the character select cannot rejoin until the next cycle.

### Arena
* Currently offers Free-For-All Teams PvP, and MSC's Challenge Mode.
* FFA / Teams: Dens open when there's only one scug/team left standing, or when the rain comes.
* All arenas from installed mods are always available for selection, including those normally exclusive to Downpour's challenge mode.
* A countdown timer blocks all methods of attack at the start of each round.
* In addition to "standard" arena configuration, the host can configure countdown timer, or whether Saint should use ascension or spears.
* The host can ban any number of slugcats from being selected, by selecting their portrait in the character select and pressing grab.

---

## Communication and moderation (Story and Arena only)
* Press **Enter** to open the chatbar, and **Comma** to toggle the chatlog's visibility.
* Press **J** to toggle usernames and player icons, and **P** to toggle a ping display next to player names.
* Press **Tab** to open the spectator menu; you can click on other (live) players' usernames to switch your camera to them. The button next to each player will let non-hosts mute or unmute them, and let hosts kick them from the lobby instance.
* Lobby hosts can tweak required and disallowed mods by opening Meadow's settings in the remix menu; the "General" tab has buttons that will open the configuration files your default text editor.

---

## Modding
Meadow supports other mods adding custom gamemodes, as well as custom variations for arena mode. Here are some examples of both:
* [Example of a custom "Tag" gamemode.](https://github.com/henpemaz/RemixMods/tree/master/Tag)
* [Arena External Gamemode API.](https://github.com/6fears7/Arena-Online/tree/main)

---

## Credits
```
Henpemaz.......Lead Developer   Persondotexe.......Modsync
Wolfycatt......Lead Artist      invalidunits.......UI, LAN
Intikus........Audio Designer   forthfora..........Modsync
Noircatto......Engine           WillowWisp.........Story
HelloThere.....Sound            @None..............Gameplay
A1iex..........UI Design        DustyLemmy.........Arena, Story
FranklyGD......Engine           Elizabeth..........Story, UI
MC41Games......Menus            niacdoial..........LAN
Silvyger.......Arena            Capt. Redstone.....Meadow, Story
Vigaro.........Menus            BitiLope...........Story
Pudgy Turtle...Story            ddemile............Modsync
UO.............Story, Arena     Saddest............UI, Chat
notchoc........Story            phanie_............Illustration
Timbits........UI, Menus        Zedreus............UI, Story
```

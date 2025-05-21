A multiplayer engine, custom game mode, and online arena / story experience for Rain World!

The Watcher campaign is currently NOT supported.

## How it works
- For the best possible experience, **disable all non-essential content mods**. If you're experiencing crashes, try with less mods on.
- Click the Meadow button to get to the lobby list. Join a lobby or host your own!
- By default Rain Meadow uses Steam to find and create lobbies. Lan matchmaking is also possible.

## Game Modes
### Meadow
- Explore a peaceful version of Rain World, encounter other players, roam together gathering unlocks to unlock new emotes, skins and creatures.
- Custom emoting to communicate by @Wolfycat!
- Sound effects to communicate
- Hour-long original soundtrack by @Intikus!
- Hand-built creature navigation

### Story
- Vanilla campaigns available by default
- Access other campaigns through the "Experimental Features" remix option
- Currently-supported campaigns: Survivor, Monk, Hunter, Artificer, Gourmand
- Campaigns not listed above are NOT supported and will likely desync or crash on any special events
- Host can only play as the campaign slugcat, clients can select slugcats other than the current campaign
- Food requirement is determined by the current campaign. Food nourishment is determined by the player's slugcat regardless of current campaign
  - e.g. Hunter in Survivor's campaign can eat meat
- All ingame players, dead or alive, MUST be present at a karma gate to continue
  - Dead players can exit to lobby to allow others to continue
- Do not attempt to grab or piggyback players as it WILL break the game
- You may den in separate shelters within the same region

### Arena
- Currently offers Competitive (PvP) mode
- Dens open when the rain comes or when there's only one scug left standing
- Countdown timer ensures all players have a chance to grab weapons before attacking; Prevents some slugcat abilties
- Saint by default uses ascendance as the mode of attack. Switch to **Sain't** abilities via Remix
- All levels are unlocked. Some levels are "multi_stage". Some levels do not spawn items due to that levels' pre-baked code.

### Communication (Story and Arena only)
- Press "Enter" to open chat
- Press "J" to show usernames and player icons
- Press "Tab" to open the spectator menu

### Moderation (Story and Arena only)
- Host can kick players via the spectator menu; Clients can mute players
- Kicking players prevents them from rejoining your current lobby
  - The ban does NOT carry between lobbies

## Mod Compatibility
- Mods that add regions/campaigns/creatures/items need to be synced between all players, including load-order.
- There's a modsync tool that will prompt you to enable/disable/install missmatched mods, but it's not completely foolproof.
- You can tweak what mods are subject to modsync as the host by adding them to the `meadow-highimpactmods` textfile in StreamingAssets.
- The community-maintained mod compatibility spreadsheet can be found [here](https://docs.google.com/spreadsheets/d/1QG1xYPLECkVSMc2vopO-Rw2rSdnn7_fsdlMajhAUOW0/edit?gid=0#gid=0)

## Modding
Some examples of custom gamemodes using Rain Meadow's multiplayer engine:
- [Tag](https://github.com/henpemaz/RemixMods/tree/master/Tag)
- [Arena custom game modes documentation & examples](https://github.com/6fears7/Arena-Online/tree/main)

## Credits
```
Henpemaz - Lead Developer		Wolfycatt(Ana) - Lead Artist		Intikus - Lead Audio Designer
Noircatto - Programming, engine		HelloThere - Programming, sound		A1iex - UI Design
FranklyGD - Programming, engine		MC41Games - Programming, menus		Silvyger - Programming, arena
Vigaro - Programming, menus		BitiLope - Programming, story		Pudgy Turtle - Programming, story
ddemile - Programming, modsync		UO - Programming, story, arena		Saddest - Programming, UI, chat
notchoc - Programming, story		phanie_ - Illustration			Timbits - Programming, UI, menus
Zedreus - Programming, UI, story	Persondotexe - Programming, modsync	 invalidunits - Programming, UI, LAN
forthfora - Programming, modsync,	WillowWisp - Programming, story
```

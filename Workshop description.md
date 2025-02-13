A multiplayer engine, custom game mode, and online arena / story experience for Rain World!

## How it works
- It uses Steam (**you must own the game on Steam**) to connect you with others players to play Rain World online! It uses peer-to-peer (P2P) networking topology.
- For the best possible experience, **disable all non-essential content mods**

## Mod support
- Varies by gamemode
- Story & Arena support MSC (Downpour) characters, some content is still work-in-progress
- Story does not fully support all MSC campaigns (yet)
- Content mods (regions, creatures, slugcats) MUST be the same as the host's and MUST be applied in the same order
  - The mod-checker is not foolproof, you can require certain mods by adding them to the `meadow-highimpactmods` textfile in StreamingAssets
The community-maintained mod compatibility spreadsheet can be found [here](https://docs.google.com/spreadsheets/d/1QG1xYPLECkVSMc2vopO-Rw2rSdnn7_fsdlMajhAUOW0/edit?gid=0#gid=0)

## Game Mode Overview

### Meadow
- Exploratory, peaceful gamemode focused on encountering other players and working together to find collectibles and unlock new skins & characters.
- Hand-built creature navigations
- Custom music by @Intikus!
- Custom emoting to communicate by @Wolfycat!
- Sound effects to communicate


### Story

- Survivor campaign is available by default
- Access other campaigns through the "Experimental Features" remix option
  - Host can only play as the campaign slugcat
  - Clients can select slugcats other than the current campaign
- Currently-supported campaigns: Survivor, Monk, Hunter, Artificer, Gourmand

- Food requirement is determined by the current campaign
- Food nourishment is determined by the player's slugcat regardless of current campaign
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
forthfora - Programming, modsync
```

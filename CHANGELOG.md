# Release 1.12.0
## Arena
- Added `arena.session` to access the current ArenaGameSession
- Moved ` ArenaGameSession_Update`  fully inside of ` ExternalGameMode.ArenaGameSession_Update` to enable overrides
- Restored custom Team Names
- Disabled Watcher glow from ripple level in online arena sessions
- Blocked next level call until chatbar is closed
- Blocked exiting to lobby if host already initiated next level loading to prevent crash
- A new character approaches: The Overseer! Select to spectate games
### Team Battle 
- Added friendly fire toggle

## Story
- Fixed Moon dying if the room transferred owners 

### Watcher
- Fixed end-game ability not working
- Fixed mind control happening post-warp

## Meadow
- Disabled Outer Expanse, Spearmaster and Artificer endings in Meadow mode.
  - These endings would cause a crash if done in Meadow mode.

## General
- Fixed Dev Tools trying to teleport remote players when holding V
- Updated documentation in codebase.
- Added OnlineGameMode.ResetOverworld() to release the overworld at the discretion of the gamemode

## Engine
Synced the following
- Waterflux
- Gourmand exhaustion and player lung exhaustion
- Vulture demasking
- Box Worms
- Sand Grubs
Improved sync for the following
- Big Moths
  - Moths will flap their wings correctly and creature interactions are synced..
 - Vultures
   - Vultures should be noticably less jittery and sync more accurately. King Vultures should also work a lot better.
- Fixed Sand Grubs causing crashes and graphical glitches.
- Added coroutine for world loading; blocked entity states during world transitions

### Chat:
- Auto-fill usernames when using "@" in chat. 
- Fixed chat filters not applying to messages above player heads.
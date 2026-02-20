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

# Release 1.11.1
## Engine 
- Fixed an issue where transitioning regions led to disappearing players

# Release 1.11.0
## Arena
- Added flash to tab arrow to assist users in locating game mode tab settings
- Publicized arena.blockList for developers
- Fixed bees/bombs spawning client-side
- Fixed pipe eating during the first few frames of the game if moving into den
- Fixed Saints ascending teammates. Stop that.
## General
- Added Chinese translation (thanks @havenoideawhatismyname!)
- Fixed custom background thumbnails not disappearing when scrolling background pages
- Fixed large lobbies interrupting ping cycle key inputs 
- Fixed spectating never abstracting previous rooms
### Chat
-  Chat Opacity (Makes chat semi transparent when a player is behind it)
-  Chat Inactivity (Makes chat semi transparent after a short period of inactivity (no new messages and no typing))
-  Enforce max message length for receiving messages.
-  Copy/Paste support
-  Recently Sent Messages (Up/Down arrows)
-  Sound when mentioned by name in chat.
-  Host icon in chat
-  Deprecate ChatTextBox2 and use just ChatTextBox
## Story
- Fixes creature duplication occurring the next cycle  after a creature enters a den
- Gracefully handle when the an online game mode menu is loaded but the online lobby hasn't 
- Updated "Match Save" to "Sync Save" for clarity
- Updated the Text Prompt on death to dismiss after 5 seconds instead of requiring input
- Fixed forced den re-sheltering when a client has a valid den
## Meadow
- Added configurable timelines
## Engine
- Fixed a sizing issue with Custom Packets

# Release 1.10.0 (Anniversary Edition)

## Arena:
- Fixes Amoeba controls not listening to your pointed direction
- Piggyback toggle now also controls your ability to piggyback dead / stunned slugcats
- Add scavenger bomb, bee hive, & corpse grab toggles
- Fixed watcher cosmetic option not saving for clients
- Fixed Saint's ascendance timer not updating its default value  
## Story:
- Fixed an issue where custom karma gates wouldn't open
## General
- Fixes infinite tinnitus
- Adds copy/paste functionality to password & IP text boxes
- Fixed Gourmand's damage collision to not linearly increase by player count
- Refined Russian translation
- Added custom backgrounds for the anniversary! Check them out under Options->Backgrounds

# Release 1.9.0
## General:
- Added Streamer Mode, it allows you to replace just your name or everyone's name with a randomized one to deter stream sniping. (Client-side Only)
- Added an optional profanity filter for chat, it will filter chat messages based on your Steam chat settings. (Disabled by default)
- Added Gameplay remix tab
- Synced Scavengers animations and grasps.
- Synced grasp stealing. Works in both Story and Arena gamemodes.
- Fixed weapon phasing
- Fixed sleep-crawling bug
## Story:
- Allow echo warps to be two-way to prevent 3rd ending to be potentially softlocked
- Host is now way less likely to be puppetted by a client after warping from echo
- Fixed going to ripple karma screen whenever going near/to an echo room when 1st ending is already achieved
- Prevent players being able to join after warping through ripple warps
- Added more stability for clients warping
- Fix sand grubs softlocking the game
- Fixed friendly fire affecting scavenger kill behavior. Scavs are not your friends.
  ⚠️ Developers: InputOverrides class is now GameplayOverrides
- Stabilized backpacking through gates & portals 
## Arena:
- Added beehives 
- Synced trophies
- Updated menu to better support controller navigation between UI elements in and outside tabs
- Fixed MSC Settings' back button being greyed out
- Added round reset button
- Added custom map settings for MSC multi-screen arenas to add objects. See levels/ folder for more details. Suffix settings file with "_meadowsettings" to import them for use in Meadow Arena
- Saved backpack toggle selction between game restarts
- Fixed clients being unable to pick up items when host enters ripple space
- Incressed time until singularity bomb vortex activates
- Added additional watcher cosmetic option
- Updated winning logic to consider number of kills and deaths in the event of a tie.
- Added variable watcher description
## Engine
- Added Overworld resource:
  1. WorldSessions will only be created for regions specific to the playercharacters timeline.
  2. Allows players to join a lobby without immidietly loading all regions.
  3. Allows non-host players to manage WorldSessions. (Meadow mode)
  4. Adds potential for downloading custom regions while inside the lobby.
- Synced the following:
	RainWorldGame.clock
	Geysers (via RainWorldGame.clock)
	WaterLevelCycle
	WindRect (via RainWorldGame.clock)
	Big Moths (Drinking Chunk & Legs)
	Death Rain Mode
	FlameJets
	Pomegranates
	Creature Hypothermia
  
# Release 1.8.0
## General:
- Updated to game version 1.11.3
- Japanese translation by MisodeN [ミソデン]. どうもありがとう
## Arena:
- The slugcat select and slugcat coloration menus now show which subpage they're on, if there are multiple.
- Fixes Saint's karma activation to mirror game's original behavior.
- Added a toggle for Watcher's ripple level
- Separated Watcher Settings from MSC Settings in Slugcat Abilities tab

## Story:
- Fixed glow always being enabled in non-watcher campaigns if watcher is enabled
- Fixed one-way warps not teleporting players in Watcher. (Example: bad warps)
- Fixed an ending 3 crash if playing local with Meadow installed
- Prevented clients in lobby being allowed to join after warping
- Added a remix options to gain achievements online. Default is FALSE.
- Fixed ripple space being shared between players


# Release 1.7.1
## Story:
- First pass at stabilizing Watcher warps
- Fixed missing cursor 
- Fixed lobby chat to not select back button on throw input
- Fixes Passages crashing the game
## General
- Fixed chat "..." from persisting unexpectedly
- ModManager now requires a game restart when it detects a change in DLC activation when joining lobbies
- Fixed an issue where backpacked slugcats were not dropped on carrier's death
- Added Profiler to Dev Tools. Click "=" to open, "[" and "]" to change views. Use with discretion
- Prevented achievements from being unlocked in online multiplayer


# Release 1.7.0

## Arena:
- Adds Slugcat banning 
- Adds piggyback toggle
- Fixed a *specific* edge case where a player might not be protected from a parry.
- Fixed a crash in Teams mode when another user suddenly disconnected 
- If you had choppy frame rate in Teams UI, please resubscribe to  “Extended Color Config” mod. 
- Fixed dupe trophy awards in results screen 
- Fixed timer not showing when running some langugages
- Fixed an issue where Inv's friend would cause you to duplicate your spawn
- Fixed Arena's menu chatbot interactions duplicating in Team name display
- Fixed Team names not allowing spaces
- Fixed some water reflection issues related to Watcher 1.5 maps.
- Added multipage slugcat select to support modded cats
- Fixed kicking someone else crashing you

## Engine:
- Added additional security measures 
- Fixed an issue with shortcut loading 
- Added custom packet capabilities 


## General
- Updated pointing logic to prefer *primary* hand, Slups can now poke eyes
- Fixed Gourm stomp not hurting NPCs
- Desynced 5P neurons to marginally improve latency. 
- Synced Vulture grub & hazers  
- Synced Player "special" input 
- Fixed the AFK sleeping animation not cancelling when stunned/killed (thanks <@380127561621176323> for AFK fixes)
- Fixed Spearmaster not closing their eyes during afk sleep.
- Fixed afk sleep rarely triggering when piggybacked onto someone else. 
- Fixed the "Don't fall back asleep when waking up" check not working.
- Fixed an oddity where shelters closing would cause afk sleep to stop.
- Cleared ping label when a user leaves instead of burning it into the foreground in memorium
- Fixed a crash while pointing if the lobby no longer existed <:rmconfused:1177681041822072892>
- Added chat typing notification


## Meadow:
- Fixed Slugcat timelines (Meadow mode)
- Fixed Emote grid animations not playing 
- Updated autohide for emote grid


## Story:
- Fixed Slugcat selection not allowing a specific slug to be chosen 
- Fixed a crash when selecting slugcat colors
- Fixed Saint’s ending 
- Fixed cases where single room warp code  was not running 
- Fixed players inability to progress past the end-of-game statistics screen
- Updated client story menu to match host's
- Updated to support Watcher 1.5
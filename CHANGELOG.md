# Release 1.15.1

## General
- Removed nightcat's stylish hat in SlugIcon (now uses watcher sprites)

## Arena
- Fixed dens not opening when someone left

# Release 1.15.0

## Engine
- Fixed a serialization issue that would break lobbies when Watcher was enabled
- Added more failsafes to lobby joining, including a configurable timeout
- Improved some sources of desync (enums)
- Improved Noodle Fly sync
- Improved Stowaway sync
- The Debug Overlay can now track non-physical object entities
- Added an Ownership view to the Debug Overlay in Dev Tools
  - Pressing '-' shows a list of players along with which objects they own and how many
- Mods containing changes to the levels/ directory are now considered high-impact

### Modders
- Added `MatchmakingManager.OnLobbyLeaving` event
- ⚠️ `MatchmakingManager.JoinLobby` has a new argument, `failReason`, to handle more joining errors

## General
- Fixed irrelevant rooms not being unloaded while spectating other players, which was causing higher network throughput
- Prevented the abstraction of rooms that contain player avatars or other non-transferable objects
- Added a new `OnlinePearlString` entity and synchronized pearl creation
- Fixed objects marked as destroyOnAbstraction not being destroyed
- Added the ability to enable or disable cheats when creating a lobby
  - When cheats are disabled, most Dev Tool cheats that affect gameplay are disabled
  - The host can still use cheats, but no other players can
  - A select few functions, such as viewing logs (K), reloading rooms (Q), and debug/profiler information, continue to work
  - This update completely disables cycle restarting via Dev Tools (R) while online
  - This update also restricts Dev Tools item spawning to the player who spawned the item, and restricts teleporting to exits so it only teleports the local player instead of everyone
- Replaced the Cape Colors dropdown menu with a textbox that accepts a hex code for any color
- Added a checkbox for Rainbow Cape, active only during events
- Cape fetching now runs asynchronously, improving startup times on slow internet connections
- Added scarves
- Fixed the exit button on the main menu getting pushed into the second column when Expedition is enabled
- Added French translation; updated Japanese, Spanish, and Russian translations
- Added a redesigned lobby select menu
  - Lobby metadata is now shown directly in the menu, including the data previously shown on cards plus:
    - Active lobby timeline (always shown in Meadow mode; Story mode requires the game to be in progress)
    - Mods needed to join the selected lobby
  - Lobby cards now show icons for the active lobby timeline and required DLCs
- The slugcat icon in the player HUD now reflects the target player's chosen slugcat and colors in use
  - An additional remix option lets players use the old icon if they find the new icons too distracting
- Added Korean translation by Karnellon

### Modders
- Added `SlugIcon`. Modded slugcats may provide their own assets and colors by adding entries to the static properties in `SlugIcon`; see `Menu/Components/SlugIcon.cs` for details
  - Lobby cards and in-game slugcat icons automatically use the provided sprites and layering, falling back to a default icon if none are set
- `activeTimeline` is now part of `LobbyInfo`
- Added `ScrollableConfirmDialog` menu objects

## Arena
- Added a new gamemode, Drown: kill and survive to buy your escape, available in Cooperative and Competitive modes
- Arena stats update:
  - Empty deaths and friendly fire now subtract from the dying player/teammate's killer instead of adding points to everyone else
  - In Team Battle, teams now have per-player stats
  - Improved round and session end result text
  - Fixed bugs where leaving to the lobby early and restarting wouldn't clear all stats
  - Fixed various other minor bugs
- Fixed winning conditions for a hidden minigame
- Added the ability to import/export game settings as code, with support for FFA, Team Battle, and Drown
- Added scoring in-game UI for Challenge, Drown, or when scoring logic is enabled; the default keybind is "T" and can be changed in the Rain Meadow Remix Menu
- Added an Arena Remix tab toggle for disabling Meadow Cosmetics in the lobby
- Changed the input for banning slugcats from pickup to Shift+Click when using the mouse
- Made Artificer's and Gourmand's parries defender-sided
- Added a configurable setting to change Artificer's parry range
- Changed the configurable setting for Artificer's stun from an on/off checkbox to a multiplier
- Added a configurable setting to give Artificer's parry some leniency
- Added a sound cue for Artificer's parry
- Fixed parried spears not being deflected on late parries
- Fixed explosive spears still damaging the player when parried
- Fixed inconsistent sound/visual cues when parrying
- Fixed Gourmand not being shown as exhausted when throwing a spear
- Slightly improved timer accuracy
- Fixed the result box bump sound effect playing per player on the final results screen instead of once
- Synced players' ready state on the overlay results screen
- Fixed the "TO LOBBY" button on the final results screen drawing behind result boxes
- Players now wait in the starting pipe until everyone has joined; the maximum wait time is configurable in the Remix menu

### Watcher
- Slightly improved Void Amoeba's swim speed
- Added configurable Void Amoeba lethality; the default multiplier is now 3x
- Added a debuff icon for players affected by an Amoeba's visual distortion
- Watcher now has normal camo transition tick time at ripple 5+, enabling levitation tech
- Disabled Ripple Trail in online arena
- Gave summoned Amoebas the Watcher's body color
- Fixed summoned Amoebas being destroyed under various unintended conditions
- Fixed summoned Amoebas spawning on the wrong layer
- Summoned Amoebas now have idle behaviors
- Adjusted summoned Amoebas' friendly fire to account for teams and spear hits
- Summoned Amoebas can now stun non-player creatures
- Amoebas no longer slow down for dead players or teammates
- The distortion effect now fades on player death
- Watcher now shows the same effect as a failed portal opening when attempting to summon an Amoeba during countdown or without enough charge
- Ripple 9 now makes Watcher fully invisible, leaving only a faint glow behind
- Watcher now always has white eyes while camouflaged
- Watcher now sees only the eyes of other Watchers in ripple space, instead of their full bodies
- The dev skin no longer shows in ripple space
- Hands and mud no longer show in ripple space
- Reduced Watcher's camo VFX aura at all ripple levels
- Player nametags now interact as expected with ripple layer changes
- Added an arena option to make ripple 9 Watcher fully invisible to everyone

### Modders
- ⚠️ Changed `ArenaOnlineGameMode.session` from a field to a getter property, renamed to `ArenaOnlineGameMode.ArenaSession`
  - `ArenaSession` no longer references `ArenaGameSession`s that aren't active
- ⚠️ Simplified active external arena mode checks by removing the `ArenaOnlineGameMode` parameter and renaming them to PascalCase
  - Example: `isTeamBattleMode(ArenaOnlineGameMode, out TeamBattleMode)` → `IsTeamBattleMode(out TeamBattleMode)`
- ⚠️ Significantly refactored and fixed Arena stats; `ExternalArenaGameMode`'s API has changed
  - High-level documentation for Arena stats will be added soon
- Added `ExportLocalSettings` and `ImportLocalSettings` virtual functions to `ExternalGameMode` for managing Arena settings

## Meadow
- Fixed creatures being able to get injured

## Story
- Players can now spectate their own corpses as long as they still exist
- Fixed players readied on the sleep screen hearing the continue sound repeatedly after the host continues

### Watcher
- Fixed Watcher warp not working when warping from a world for the second time in a cycle
- Fixed the Prince's duplication
- Synced the Prince's position and look point
- Synced lightning storms

## Chat
- Moved Chat into its own overlay so it is available at all times
- Chat now retains history of what was typed in and out of the lobby menu
- Moved all chat-related Remix options into their own tab
- Added Remix options for different system messages in Arena and Story mode
- Added username color customization as a Remix option
- Added text downscroll as a Remix option

## New Contributors
Thank you to all of our first-time contributors, and welcome EdEnStonne to the Meadow Dev team!
- Wonky
- Ombekende
- ConfiG
- r3nsen
- OneLetterShor
- iiul
- solo snuggles

# Release 1.14.1

## Engine

- Improved parry netcode.
- Fixed ghost spear poles after a spear was pulled out by a remote slugcat.

## Chat

- Fixed events not displaying while Global Mute was toggled
- Fixed username color not updating between Arena games

## Arena

- Fixed Watcher Amoebas not appearing under certain conditions
- Fixed score being granted for killing teammates

# Release 1.14.0

## Engine

- Fixed a scenario where transferring entities could lead to deadlocks
- Fixed Joke Rifle bullets duping and consuming high bandwith.
- Improved piggyback netcode
- Added documentation to the GitHub wiki on OnlinEntity Locks() to manage state race conditions

## Story

- Fixed shelters not closing if there's an untamed Slugpup / SlugNPC in the world. Slugpups should also respond to commands more reliably.
- Fixed duplication of some save records when loading on clients. This should fix save bloating that caused load slowdown over time.

### Watcher

- Fixed some scenarios where watcher warps were not working correctly
- Fixed clients not being sucked into warps

### Saint

- Fixed some critical errors in Saint's ending

## General

- Added Global Mute toggle to Meadow Remix's "Gameplay" tab
- Fixed most cases of pearl strings duplicating. This should noticeably decrease latency
- Fixed Vultures not properly killing players when taken off-screen
- Fixed Saint ascensions not working reliably outside of Arena

## Arena

- Fixed scoring across all modes
- Added import/export map playlists from clipboard
- Added configurable Artificer explosion capacity
- Added Spear Hit score config
- Prevented Overseer from earning score
- Synced more attributes in Challenge 70
- Added additional flair for winning a secret minigame. Check Rain Meadow's Arena Remix page
- Post-game stats now persist for duration of lobby life or reset as desired

### Team Battle

- Teammates will now see the location of Watcher while invisible

### Modders

- ⚠️ Updated `arena.ExternalGameMode.GetGamemodeId` to become a getter
- Added `FinalResultBox_ctor` to `arena.ExternalGameMode`

# Release 1.13.2

- Fixed Meadow crashing in single-player
- Fixed Meadow initialization logs being lost
- Fixed some arena menus running at twice the tickrate
- Fixed an issue with the password resetting when a lobby's host changes

# Release 1.13.1

## General

- Fixed Hunter_Illness IL Hook
- Updated target game version
- Fixed empty Remix menu when playing with non-English translation settings
- Fixed persistent purchases from being greyed out unless you met the store's value

## Arena

- Fixed a bug where you might spawn multiple times during special events

## Meadow

- Granted more event progress when meeting a Meadow Echo

# Release 1.13.0

## General

- The Dev Tools debug UI now shows the local instance's client flags, and arena now shows its "[L]obbied" client flag.
- The Dev Tools debug UI now groups creature/item symbols together, and should lag less.
- Fixed meadow abyss respawns and arena/story abyss death messages failing if the player entered WallCling between -250y and -500y.
- Fixed neuron glow not using players' selected body color.
- Added customizable logging level to Remix in General tab
- Infinite deafness, and _most_ causes of infinite tinnitus should be fixed.
- Inv eggs no longer duplicate per player per Inv.
- Fixed your spectated scug resetting every time you opened the spectate menu.
- Updated Chinease Translation (Thanks HapiFive)
- Fixed translation code for mod applier (Thanks HapiFive)

## Arena

- If MSC is enabled, closed dens will mirror challenge mode by eventually forcing players out, and completely blocking reentry attempts.
- Added More Slugcat's "Challenges" to Arena!
- Synced round kills
- Added configurable scoring
- Added KillList & ScoreCounter HUDs from vanilla Arena
- Altered client Arena overlay loading logic to wait for host to construct first to ensure accurate scoring
- Granted Saint a kill credit if using ascension to ascend others
- Fixed Spears Hit toggle not actually working during that current game session
- Added "Loading x%" message in Arena Overlay to notify of remaining users waiting to leave active resource

### Modders

- ⚠️ BREAKING: Updated `GetPlayerTrophies ` to `GetAllPlayerTrophies ` and `GetRoundPlayerTrophies`
- ⚠️ BREAKING: Moved all arena `arena.Killing` to reside inside of `arena.ExternalGameMode` and removed the `playerIndex` param
- ⚠️ BREAKING: Updated `ExternalGameMode.AddIcon` to include OnlinePlayerDisplay to access all UI elements used in the in-game overhead UI

## Story

- Fixed the "Wait for others to rescue you" death prompt blocking pause inputs.

### Watcher

- Impossibly high ripple levels (6+) no longer crash the game when viewed.
- Fixed a crash where clients would load into the wrong regions

## Meadow

- Slugcats can now enter the lower depths regardless of remix's "Vanilla Exploits".
- MS_CORE and Saint's intro rooms should no longer break the rain timer and/or game.
- The pounce tutorial barrier, the guaranteed jetfish in SL, and the three guaranteed scav corpses in Artificer's GW no longer load in.
- Many different room-specific tooltips across all campaigns are now disabled.
- Added configurable eye color

# Release 1.12.0

## Arena

- Added `arena.session` to access the current ArenaGameSession
- Moved ` ArenaGameSession_Update` fully inside of ` ExternalGameMode.ArenaGameSession_Update` to enable overrides
- Restored custom Team Names
- Disabled Watcher glow from ripple level in online arena sessions
- Blocked next level call until chatbar is closed
- Blocked exiting to lobby if host already initiated next level loading to prevent crash
- A new character approaches: The Overseer! Select to spectate games

### Team Battle

- Added friendly fire toggle

## Story

- Fixed Moon dying if the room transferred owners
- Enabled Sync Save option for clients regardless of save state status

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
- Added support for 4:3 resolution for Spectate overlay
- Fixed proto-rot showing up in unexpected campaigns
- Fixed issues with port assignment resulting in meadow failing to start.

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

- Chat Opacity (Makes chat semi transparent when a player is behind it)
- Chat Inactivity (Makes chat semi transparent after a short period of inactivity (no new messages and no typing))
- Enforce max message length for receiving messages.
- Copy/Paste support
- Recently Sent Messages (Up/Down arrows)
- Sound when mentioned by name in chat.
- Host icon in chat
- Deprecate ChatTextBox2 and use just ChatTextBox

## Story

- Fixes creature duplication occurring the next cycle after a creature enters a den
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
- Added custom map settings for MSC multi-screen arenas to add objects. See levels/ folder for more details. Suffix settings file with "\_meadowsettings" to import them for use in Meadow Arena
- Saved backpack toggle selction between game restarts
- Fixed clients being unable to pick up items when host enters ripple space
- Incressed time until singularity bomb vortex activates
- Added additional watcher cosmetic option
- Updated winning logic to consider number of kills and deaths in the event of a tie.
- Added variable watcher description

## Engine

- Added Overworld resource:
  1. WorldSessions will only be created for regions specific to the playercharacters timeline.
  1. Allows players to join a lobby without immidietly loading all regions.
  1. Allows non-host players to manage WorldSessions. (Meadow mode)
  1. Adds potential for downloading custom regions while inside the lobby.
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
- If you had choppy frame rate in Teams UI, please resubscribe to “Extended Color Config” mod.
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
- Fixed the AFK sleeping animation not cancelling when stunned/killed (thanks \<@380127561621176323> for AFK fixes)
- Fixed Spearmaster not closing their eyes during afk sleep.
- Fixed afk sleep rarely triggering when piggybacked onto someone else.
- Fixed the "Don't fall back asleep when waking up" check not working.
- Fixed an oddity where shelters closing would cause afk sleep to stop.
- Cleared ping label when a user leaves instead of burning it into the foreground in memorium
- Fixed a crash while pointing if the lobby no longer existed \<:rmconfused:1177681041822072892>
- Added chat typing notification

## Meadow:

- Fixed Slugcat timelines (Meadow mode)
- Fixed Emote grid animations not playing
- Updated autohide for emote grid

## Story:

- Fixed Slugcat selection not allowing a specific slug to be chosen
- Fixed a crash when selecting slugcat colors
- Fixed Saint’s ending
- Fixed cases where single room warp code was not running
- Fixed players inability to progress past the end-of-game statistics screen
- Updated client story menu to match host's
- Updated to support Watcher 1.5

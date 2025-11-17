# Release 1.9.0
## General:
- Fixed weapon phasing
## Story:
- Allow echo warps to be two-way to prevent 3rd ending to be potentially softlocked
- Host is now way less likely to be puppetted by a client after warping from echo
- Fixed going to ripple karma screen whenever going near/to an echo room when 1st ending is already achieved
- Prevent players being able to join after warping through ripple warps
- Fix sand grubs softlocking the game
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
  
# Release 1.8.0
## General:
- Updated to game version 1.11.3
- Japanese translation by MisodeN [ミソデン]. どうもありがとう
## Arena:
- The slugcat select and slugcat coloration menus now show which subpage they're on, if there are multiple.
- Fixes Saint's karma activation to mirror game's original behavior.
- Added a toggle for Watcher's ripple level
- Separated Watcher Settings from MSC Settings in Slugcat Abilities tab
- Fixed Beehives not being created in online space leading to Gourmand players being able to "lag-switch".
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

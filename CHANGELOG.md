# Release 1.8.0
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

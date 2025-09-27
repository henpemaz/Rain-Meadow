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

- ## Engine:
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

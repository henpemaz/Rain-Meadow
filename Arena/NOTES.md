# Arena Development Notes

## Executive Summary
* **Coding Style:** The codebase favors index-based iterations and manual null checking over LINQ cuz I was more familiar with that.
* **Player Identification:** Player numbering and identity are strictly determined by the `InLobbyId` index position.
* **Scoring Logic:** Current technical debt requires an IL hook for negative integers to properly handle suicide deductions without awarding points to opponents.
* **Killing Mechanism:** The `Killing` hook acts as the central source of truth for combat violence and uses specific logic to revert to base game scoring when needed.
* **NextLevel Flow:** A host-initiated handshake and an 8-second deadlock timer in `WorldSession` protect the lobby from crashing due to failed resource exits.
* **Arena Helpers:** This class manages critical player-finding functions and contains static logic, some of which should eventually be moved into external modes.
* **External Game Modes:** The stable API must be updated sparingly to preserve compatibility for other modders relying on the provided documentation.
* **Contributor Legacy:** Arena represents an iterative learning process where design patterns may shift depending on when specific logic was implemented.

---

## Scoring
One of the biggest pain points was redundant logic in `ArenaOverlay` and `Player.specificMultiplayerHud` that clamped points to 0 if they dropped below that threshold. 

> [!WARNING]
> **Technical Debt Alert:** When addressing the suicide point deduction request for tournaments, points were granted to *everyone else* instead of deducting from the victim. This caused a bug in Team Battle where members gain points when an opponent suicides. 

**The Fix:**
* **Do not** just update the `teamsScore` dictionary. 
* **Use an IL hook** to support negative integers.
* Update `DistributeEmptyKillScore` [here](https://github.com/henpemaz/Rain-Meadow/blob/main/Arena/ArenaRPCs.cs#L38) to remove score from the target player directly.
* Clean up the legacy "clamp to 0" code once the hook is in place.
* Right now, `UpdatePlayerScore` only checks if current score is less than incoming score. [Drown PR](https://github.com/henpemaz/Rain-Meadow/pull/1448/files) renames it to `IncreasePlayerScore` and adds `UpdatePlayerScore` without this check. 
* It was too close to tournament to make any changes to this, so thats why two RPCs will exist.

## Killing
The `BaseGameMode.Killing` hook has been rewritten about four times and is currently stable. **Change it sparingly.**

* **Logic:** A PR for `1.15.0` to add Drown mode [here](https://github.com/henpemaz/Rain-Meadow/blob/0503-drown/Arena/ArenaOnlineGameModes/BaseGameMode.cs#L222) checks if `arena.killingScore` is `0`. If so, it reverts to base game logic for score assignment.
* **Configuration:** Score configs in the menu do not auto-reset to `0` when the mode changes; this is currently left to the user’s discretion.

## NextLevel Flow
The transition logic is designed to isolate crashes and prevent "lobby tanking."

* **Deadlock Timer:** `WorldSession` has an **8s timer** if a player fails to leave a world resource. This isolates problematic players so the rest of the lobby can proceed.
* **Handshake:** The Host initiates the level change in `ArenaOverlay_Update` [here](https://github.com/henpemaz/Rain-Meadow/blob/main/Arena/ArenaHooks.cs#L1091), then pulls the other players along once the host has loaded.
* **Known Issues:**
    * **Lobbied Players:** Some players report being stuck in the lobby menu on start without errors. Occurrences are rare.
    * **Visual Bug:** Mid-game joins cause duplicate score entries in the result overlay. Harmless, but needs a fix eventually.

---

## Arena Helpers
This is a highly used class. Currently, `TeamBattle` logic is static here, but it should eventually be moved into the external mode itself.

**Primary Functions:**
1.  `FindOnlinePlayerFromFakePlayerNumber`: Finds the owner from the `ArenaSitting.ArenaPlayer` player number.
2.  `FineOnlinePlayerNumber`: Finds the index position of `InLobbyId`.

---

## External Game Modes
The API is live and used by other modders. **Update the API sparingly** to avoid breaking external dependencies.

* **Documentation:** Detailed docs can be found in [Arena/README.md](https://github.com/henpemaz/Rain-Meadow/blob/main/Arena/README.md).

---

## Contributor Summary
This project represents 2.5 years of iterative learning. You may encounter:
* **Inconsistent Designs:** Later implementations reflect a better understanding of the engine than earlier ones.
* **Manual Implementations:** Some helper functions might be missing in certain spots simply due to the sheer scale of the project.

## Conclusion
It's been one hell of a ride. I hope you enjoy it like I did.

**Need help?** DM **@UO** with questions. Fight hard, Slugcat
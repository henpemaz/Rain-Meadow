# Arena Development Notes

## Scoring
One of the biggest pain points was redundant logic in `ArenaOverlay` and `Player.specificMultiplayerHud` that clamped points to 0 if they dropped below that threshold. 

> [!WARNING]
> **Technical Debt Alert:** When addressing the suicide point deduction request for tournaments, points were granted to *everyone else* instead of deducting from the victim. This caused a bug in Team Battle where members gain points when an opponent suicides. 

**The Fix:**
* **Do not** just update the `teamsScore` dictionary. 
* **Use an IL hook** to support negative integers.
* Update `UpdatePlayerScore` (TODO: [Link]) to remove score from the target player directly.
* Clean up the legacy "clamp to 0" code once the hook is in place.

## Killing
The `BaseGameMode.Killing` hook has been rewritten about four times and is currently stable. **Change it sparingly.**

* **Logic:** A PR for `1.15.0` (TODO: [Link]) checks if `arena.killingScore` is `0`. If so, it reverts to base game logic for score assignment.
* **Configuration:** Score configs in the menu do not auto-reset to `0` when the mode changes; this is currently left to the user’s discretion.

## NextLevel Flow
The transition logic is designed to isolate crashes and prevent "lobby tanking."

* **Deadlock Timer:** `WorldSession` has an **8s timer** if a player fails to leave a world resource. This isolates problematic players so the rest of the lobby can proceed.
* **Handshake:** The Host initiates the level change in `ArenaOverlay_Update`, then pulls the other players along once the host has loaded.
* **Known Issues:**
    * **Ghost Players:** Some players report being stuck in the lobby menu on start without errors. Occurrences are rare.
    * **Visual Bug:** Mid-game joins cause duplicate score entries in the result overlay. Harmless, but needs a fix eventually.

## General Design & Patterns
The codebase favors Golang-style iterations (index-based loops and manual null checking) over `Linq`. 

* **Player Identification:** Determined by the `InLobbyId` index position.
* **Logic Management:** `arena.winByScore` handles the scoring logic for `ArenaOverlay`.
* **The Crux:** The `Killing` hook is the "source of truth" for all arena violence, including scores and trophy assignments.

---

## Arena Helpers
This is a high-traffic class. Currently, `TeamBattle` logic is static here, but it should eventually be moved into the external mode itself.

**Primary Functions:**
1.  `FindOnlinePlayerFromFakePlayerNumber`: Finds the owner from the `ArenaSitting.ArenaPlayer` player number.
2.  `FineOnlinePlayerNumber`: Finds the index position of `InLobbyId`.

---

## External Game Modes
The API is live and used by other modders. **Update the API sparingly** to avoid breaking external dependencies.

* **Documentation:** Detailed docs can be found in `Arena/README.me`.

---

## Contributor Summary
This project represents 2.5 years of iterative learning. You may encounter:
* **Inconsistent Designs:** Later implementations reflect a better understanding of the engine than earlier ones.
* **Manual Implementations:** Some helper functions might be missing in certain spots simply due to the sheer scale of the project.

**Need help?** DM **@UO** with questions. Good luck—the Arena awaits!
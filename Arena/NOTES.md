# Arena Development Notes

## Summary
* **Coding Style:** The codebase favors index-based iterations and manual null checking over LINQ cuz I was more familiar with that.
* **Player Identification:** Player numbering and identity are determined by a combo of the `InLobbyId` index position and `ArenaSitting` setup.
* **Killing Mechanism:** The `Killing` hook acts as the central source of truth for combat violence and uses specific logic to revert to base game scoring when needed.
* **NextLevel Flow:** A host-initiated handshake and an 8-second deadlock timer in `WorldSession` protect the lobby from crashing due to failed resource exits.
* **Arena Helpers:** This class manages critical player-finding functions and contains static logic, some of which should eventually be moved into external modes.
* **External Game Modes:** The stable API must be updated sparingly to preserve compatibility for other modders relying on the provided documentation.
* **Contributor Legacy:** Arena represents an iterative learning process where design patterns may shift depending on when specific logic was implemented.

---

## Scoring
Newest logic (1.14.1) now checks if users have "WinByScore" set to true (which is a getter for if any scores > 0) to determine how scoring behavior should work across game modes. Ex: If you're playing drown and you have killScore set to 2, you'll net 2 points per kill regardless of creature's kill value. 

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
2.  `FindOnlinePlayerNumber`: Finds the index position of `InLobbyId`.

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

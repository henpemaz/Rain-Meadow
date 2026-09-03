# Infinite tinnitus in Arena — why 5 fixes have not worked

Scope: **Arena only** (per Preston, 2026-09-03). Story/Meadow paths noted only where they share code.

Verified against `ikdasm` disassembly of
`~/.local/share/Steam/steamapps/common/Rain World/BepInEx/utils/PUBLIC-Assembly-CSharp.dll`
(dumped to `/tmp/game.il`) and against the current tree at `main` (abde2c12).

## The mistake every attempt has made

`DeafLoopHolder.Deaf` does **not** read `holder.player.deaf`:

```
// DeafLoopHolder::get_Deaf  (game.il:1142777)
return room.game.cameras[0].virtualMicrophone.deafContribution;
```

The tinnitus loop's volume, its creation, and its teardown are all driven by the *camera's*
`VirtualMicrophone.deafContribution`. `Player.deaf` only decides whether `Player.Update` bothers
to allocate a holder. So:

- `self.deaf = 0` in `Player_Destroy` (`Game/RainMeadow.PlayerHooks.cs:2126`) cannot silence a
  running loop. It only removes that player from the arena max (below), and only when `Destroy()`
  actually runs — abstractization / room unload does not call it.
- The `On.DeafLoopHolder.Update` guard (removed in `433818b1` "Fix, take 9") only killed *remote*
  holders. The loop you hear is your own **local** holder.
- The IL patch in `Player_Update` (`25101a4d`, attempt #5, `RainMeadow.PlayerHooks.cs:1229-1242`)
  only stops remote players from allocating a holder. Same story.

None of the five attempts ever touched `deafContribution`. The mod has no
`IL.VirtualMicrophone.Update` / `On.VirtualMicrophone.Update` hook at all
(only `NewRoom` in `Meadow/MeadowMusic.cs` and `DrawUpdate` in `Meadow/PlopMachine.cs`).

## Root cause

`VirtualMicrophone::Update` (game.il:611086):

```csharp
deafContribution = 0f;
if (camera.followAbstractCreature?.realizedCreature != null)
{
    if (room.game.IsArenaSession && camera.followAbstractCreature.realizedCreature is Player)
    {
        float max = 0f;
        for (int i = 0; i < room.game.Players.Count; i++)          // <-- ALL players
            if (room.game.Players[i].realizedCreature != null &&
                room.game.Players[i].realizedCreature.Deaf > max)
                max = room.game.Players[i].realizedCreature.Deaf;
        deafContribution = max;
    }
    else deafContribution = camera.followAbstractCreature.realizedCreature.Deaf;
}
deaf = LerpAndTick(deaf, Mathf.Pow(deafContribution, 1.2f), 0.06f, 1f/30f);
```

Vanilla does this because local co-op arena shares one screen. Rain Meadow arena reuses
`ArenaGameSession` (so `IsArenaSession` is true) and pushes **every online avatar** into
`session.Players` (`Arena/ArenaOnlineGameModes/BaseGameMode.cs:818` and `:1271`).

So your microphone's deafness is `max(Deaf)` across every player in the lobby.

`Creature.deaf` is **not synchronized** — a repo-wide grep finds exactly one write in the whole mod
(`PlayerHooks.cs:2126`). Remote players' `deaf` is whatever the local simulation happened to set,
and it only decays inside `Creature.Update` (game.il:335818, `if (deaf > 0) deaf--;` — top-level,
not gated on aliveness, so corpses do decay *while they are still being updated*). Any remote
player whose `Creature.Update` stops running keeps a frozen `deaf` forever:

- body abstractized / room unloaded (no `Destroy()`, so the `PlayerHooks.cs:2126` band-aid misses it)
- `inShortcut` (removed from the room update list)
- disconnected player still left in `session.Players`
- dead body removed from the room before decay finished

`Creature.Deaf` = `Mathf.Pow(Mathf.InverseLerp(0, 120, deaf), 0.2f)` — the 0.2 exponent means even
`deaf == 1` yields `Deaf ≈ 0.38`, i.e. a clearly audible sine loop. One stuck tick is enough.

## Why it is permanent once it starts

Teardown in `Player.Update` (game.il:377906) requires `Player.Deaf == 0` **and**
`deafLoopHolder.deafLoop == null`. `deafLoop` is only nulled inside `DeafLoopHolder.Update`
(game.il:1142914) when `Deaf == 0f` — i.e. when `deafContribution` is 0. With a frozen remote
player pinning the max above zero, neither condition is ever met. Your own `deaf` reaching 0
changes nothing.

`DeafLoopHolder.Update` also re-creates itself into the camera's room whenever
`Deaf > 0 && game.cameras[0].room != this.room` (game.il:1142828), bypassing `Player.Update`
entirely — so the attempt-#5 IL patch cannot prevent a holder from existing, it can only prevent
the *first* one.

## Arena spectating rules out the cheap fix

Arena installs `SpectatorHud` (`Arena/ArenaOnlineGameModes/BaseGameMode.cs:615`), and it points the
camera at a **remote** creature (`OnlineUIComponents/SpectatorHud.cs:219`, `:141`). So simply
disabling the `IsArenaSession` branch while in a lobby is *not* enough — it would fall through to
`camera.followAbstractCreature.realizedCreature.Deaf`, which while spectating is a remote player's
unsynced, possibly-frozen `deaf`.

The correct rule for Meadow arena is: **`deafContribution` is the local player's `Deaf`, and 0
whenever the local player creature is not currently being updated.** That last clause is what
`6df64bbe` ("handle cases where a player died and the tinnitus loop is stuck") was groping for —
a local corpse pulled out of the room freezes its own counter just as a remote one does.

Overseer spectators fall out correctly for free: `BaseGameMode.cs:1267` excludes
`CreatureTemplate.Type.Overseer` from `session.Players`, so there is no local entry and the
contribution is 0.

## Secondary defects introduced by attempt #5

`Game/RainMeadow.PlayerHooks.cs:1229-1242` inserts `if (!IsLocal()) goto skipTinnitus` immediately
after `brtrue` on `Player.get_AI` (game.il IL_1607). That label is `IL_16bc` — past the **whole**
block, which contains:

1. `AdrenalineEffect` creation *and* its `slatedForDeletetion` cleanup (IL_160c-IL_1654).
   Remote players now never get adrenaline visuals, and a pre-existing `adrenalineEffect`
   reference is never cleared.
2. Both the creation *and* the destruction branch of `deafLoopHolder` (IL_1654-IL_16bc).
   If a creature is locally owned when the holder is created and ownership later moves away,
   nothing destroys the holder — and since `433818b1` removed the `On.DeafLoopHolder.Update`
   guard, nothing slates it for deletion either. Attempt #5 and "Fix, take 9" together removed
   the last cleanup path.

## Keep one-holder-per-lobby — do not just revert #5

Every `DeafLoopHolder` in the camera's room plays its own `Deaf_Sine_LOOP` at
`LerpAndTick(volume, Deaf, ...)`, and `Deaf` is the shared mic value. With N players in the room
that is N stacked copies of the same sine — the tinnitus gets louder with lobby size. So the
"remote players must not own a holder" intent of attempt #5 is correct; only its implementation
(an IL branch that overshoots) is wrong. Restore that intent with the `On.DeafLoopHolder.Update`
guard instead, which also catches the holder that `DeafLoopHolder.Update` re-creates for itself.

## Fix, in short

1. IL-patch `VirtualMicrophone.Update` so `deafContribution` = local player's `Deaf`, 0 if that
   creature has no room. Must be an IL patch, not a post-`orig` write: `orig` consumes
   `deafContribution` in the same call to compute `VirtualMicrophone.deaf` (global sound muffling).
2. Revert attempt #5's IL patch in `Player_Update`.
3. Restore the `On.DeafLoopHolder.Update` non-local guard removed in `433818b1`.
4. Revert `self.deaf = 0` in `Player_Destroy`.

## Verification plan

- Arena, 2 clients. A flashbangs itself, then immediately leaves via a shortcut / dies and despawns.
  B must hear nothing.
- Arena, both flashbanged. Each client's tinnitus fades on its own `deaf` timeline, independently.
- Arena, die and spectate a remote player who is deafened. No tinnitus while spectating; none after
  returning.
- Arena, 4 players all flashbanged. Tinnitus is no louder than with 2 (holder-stacking check).
- Confirm remote players still show `AdrenalineEffect` (regression check for step 2).

## Note for the next session

Read `01-implementation.md` next. Re-dump the IL before editing:
`ikdasm "$HOME/.local/share/Steam/steamapps/common/Rain World/BepInEx/utils/PUBLIC-Assembly-CSharp.dll" > /tmp/game.il`
`VirtualMicrophone::Update` starts at game.il:611086, `DeafLoopHolder` at 1142770,
the `Player::Update` deaf block at 377884.

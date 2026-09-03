# Implementation — infinite tinnitus (Arena)

Prereq: read `00-findings.md`. Execute as Sonnet. Do **not** add code comments.

Four steps, in order. Stop after each so the build can run. Ask before `dotnet build`.

---

## Step 1 — make `deafContribution` local-only

File: `Game/RainMeadow.GameplayHooks.cs`

**1a.** Register next to the other gameplay hooks (the block around line 65-75, alongside
`IL.Player.TerrainImpact += Player_TerrainImpact;`):

```csharp
IL.VirtualMicrophone.Update += VirtualMicrophone_Update;
```

**1b.** Add the handler near `Player_TerrainImpact` (line ~416), matching that file's try/catch style:

```csharp
private void VirtualMicrophone_Update(ILContext il)
{
    try
    {
        var c = new ILCursor(il);
        var skipVanilla = il.DefineLabel();

        c.GotoNext(MoveType.After,
            i => i.MatchStfld<VirtualMicrophone>(nameof(VirtualMicrophone.deafContribution))
            );
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate((VirtualMicrophone self) =>
        {
            if (OnlineManager.lobby == null) return false;
            var me = self.room?.game?.Players?
                .FirstOrDefault(x => x != null && x.IsLocal())?.realizedCreature;
            self.deafContribution = (me != null && me.room != null && !me.slatedForDeletetion)
                ? me.Deaf
                : 0f;
            return true;
        });
        c.Emit(OpCodes.Brtrue, skipVanilla);

        c.GotoNext(i => i.MatchLdfld<VirtualMicrophone>(nameof(VirtualMicrophone.deaf)));
        c.GotoPrev(i => i.MatchLdarg(0));
        c.MarkLabel(skipVanilla);
    }
    catch (Exception e)
    {
        Logger.LogError(e);
    }
}
```

Anchors, from the IL dump:

- First anchor is the `stfld VirtualMicrophone::deafContribution` at `IL_0006` — the
  `deafContribution = 0f;` on the method's first line. `MoveType.After` puts the cursor at
  `IL_000b`, before the vanilla `followAbstractCreature` null check.
- Branch target is `IL_010a` (`ldarg.0` / `ldarg.0` / `ldfld VirtualMicrophone::deaf`), the first
  instruction of the `deaf = LerpAndTick(...)` line. Every branch in the block above converges
  there, so it is safe to jump over the lot. **Verify the label actually lands on `IL_010a`** by
  dumping the patched method — do not eyeball it.
- `get_IsArenaSession` occurs exactly once in `VirtualMicrophone::Update`, if you need a
  cross-check anchor.

Why the guards:

- `me.room != null && !me.slatedForDeletetion` — an out-of-room creature's `deaf` never decays
  (`Creature.Update` is what decrements it), so a frozen counter must read as 0, not as its last
  value. This is the case `6df64bbe` was chasing.
- Local player missing entirely (overseer spectator: excluded from `session.Players` at
  `BaseGameMode.cs:1267`) → 0. Correct.
- `OnlineManager.lobby == null` → returns false, vanilla path runs untouched. Offline is unaffected.

Do **not** implement this as `On.VirtualMicrophone.Update` writing `deafContribution` after `orig`.
`orig` uses `deafContribution` to compute `VirtualMicrophone.deaf` (global sound muffling) in the
same call, so a post-hoc write fixes the tinnitus loop and leaves muffling driven by the remote max.

Check that `System.Linq` is in scope in `RainMeadow.GameplayHooks.cs` for `FirstOrDefault`.

---

## Step 2 — revert attempt #5

File: `Game/RainMeadow.PlayerHooks.cs`, lines ~1229-1242 in `Player_Update`.

Delete the whole `// infinite tinnitus fix` block:

```csharp
// infinite tinnitus fix
c.Index = 0;
ILLabel skipTinnitus = il.DefineLabel();
c.GotoNext(MoveType.After,
    i => i.MatchStfld<Player>(nameof(Player.mushroomEffect)),
    i => i.MatchLdarg(0),
    i => i.MatchCall<Player>("get_AI"),
    i => i.MatchBrtrue(out skipTinnitus)
    );
c.Emit(OpCodes.Ldarg_0);
c.EmitDelegate((Player self) => self.abstractPhysicalObject.IsLocal());
c.Emit(OpCodes.Brfalse, skipTinnitus);
```

Its branch target overshoots past the `AdrenalineEffect` block as well as the `deafLoopHolder`
block, so it suppresses remote adrenaline effects and blocks all `deafLoopHolder` teardown.
Step 3 replaces its intent. Leave the `c.Index = 0;` that opens the *next* block
(`// don't try teleporting remote players`) in place.

---

## Step 3 — restore the non-local holder guard (required, not optional)

Without this, remote players allocate their own `DeafLoopHolder`s in the camera's room, each
playing `Deaf_Sine_LOOP` at the same shared volume — the tinnitus stacks with lobby size.

File: `Game/RainMeadow.GameplayHooks.cs`. This is verbatim the hook removed in `433818b1`, plus a
null guard:

```csharp
On.DeafLoopHolder.Update += DeafLoopHolder_Update;
```

```csharp
private void DeafLoopHolder_Update(On.DeafLoopHolder.orig_Update orig, DeafLoopHolder self, bool eu)
{
    if (OnlineManager.lobby != null && self.player != null && !self.player.IsLocal())
    {
        self.slatedForDeletetion = true;
        return;
    }
    orig(self, eu);
}
```

Preferred over re-aiming the `Player.Update` IL patch because it also catches the holder that
`DeafLoopHolder.Update` re-creates for itself when the camera changes room (game.il:1142828),
which `Player.Update` never sees.

---

## Step 4 — revert the `Player_Destroy` band-aid

File: `Game/RainMeadow.PlayerHooks.cs:2126`

Delete `self.deaf = 0; //Doctors HATE this one simple trick!`. It never affected the loop volume,
and step 1's `room != null` guard covers the death case properly.

---

## Step 5 — CHANGELOG

`CHANGELOG.md` already carries "Fixed infinite tinnitus for real this time." from the March 2026
attempt. Replace that line rather than adding a sixth one.

---

## Testing

See the verification plan in `00-findings.md`. All five checks are arena scenarios.

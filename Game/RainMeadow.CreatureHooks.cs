using System;
using System.Collections.Generic;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        // customize creature behavior for online sync
        private void CreatureHooks()
        {
            On.OverseerAI.UpdateTempHoverPosition += OverseerAI_UpdateTempHoverPosition; // no teleporting
            On.OverseerAI.Update += OverseerAI_Update; // please look at what I tell you to

            On.TentaclePlantAI.Update += TentaclePlantAI_Update;

            On.AbstractPhysicalObject.Update += AbstractPhysicalObject_Update; // Don't think
            On.AbstractCreature.Update += AbstractCreature_Update; // Don't think
            On.AbstractCreature.OpportunityToEnterDen += AbstractCreature_OpportunityToEnterDen; // Don't think
            On.AbstractCreature.InDenUpdate += AbstractCreature_InDenUpdate; // Don't think
            IL.AbstractCreature.IsEnteringDen += AbstractCreature_IsEnteringDen;

            On.ScavengerAbstractAI.InitGearUp += ScavengerAbstractAI_InitGearUp;

            IL.GarbageWorm.NewHole += GarbageWorm_NewHole;
            On.GarbageWormAI.Update += GarbageWormAI_Update;

            On.DropBugAI.CeilingSitModule.Dislodge += DropBugAI_CeilingSitModule_Dislodge;
            On.DropBugAI.CeilingSitModule.JumpFromCeiling += DropBugAI_CeilingSitModule_JumpFromCeiling;

            On.EggBugGraphics.Update += EggBugGraphics_Update;
            On.BigSpiderGraphics.Update += BigSpiderGraphics_Update;

            On.EggBug.DropEggs += EggBug_DropEggs;
            On.Vulture.DropMask += Vulture_DropMask;
            On.BigSpider.BabyPuff += BigSpider_BabyPuff;
        }

        private void EggBugGraphics_Update(On.EggBugGraphics.orig_Update orig, EggBugGraphics self)
        {
            if (self.bug.bodyChunks[0].pos == self.bug.bodyChunks[1].pos)
            {
                // eggbug graphics does some line calcs that break if pos0 == pos1
                // doesn't happen offline but when receiving pos from remove, can happen
                // pos are equal the frame it's sucked into shortcut
                // pos are set to different when sput out
                // but due to the suckedintoshortcut not removing client-sided when ran by the creature (it waits for the RPC)
                // then the bad values do happen
                self.bug.bodyChunks[1].pos += Vector2.down;
            }
            orig(self);
        }

        private static void BigSpiderGraphics_Update(On.BigSpiderGraphics.orig_Update orig, BigSpiderGraphics self)
        {
            if (self.bug.bodyChunks[0].pos == self.bug.bodyChunks[1].pos)
            {
                // spiders do this too
                self.bug.bodyChunks[1].pos += Vector2.down;
            }
            orig(self);
        }

        private void ScavengerAbstractAI_InitGearUp(On.ScavengerAbstractAI.orig_InitGearUp orig, ScavengerAbstractAI self)
        {
            if (OnlineManager.lobby != null)
            {
                if (self.world.GetResource() is WorldSession ws && !ws.isOwner)
                {
                    return;
                }
            }
            orig(self);
        }

        // Don't think
        private void AbstractCreature_InDenUpdate(On.AbstractCreature.orig_InDenUpdate orig, AbstractCreature self, int time)
        {
            if (OnlineManager.lobby != null && !self.CanMove(quiet: true)) return;
            orig(self, time);
        }

        // Don't think
        private void AbstractCreature_OpportunityToEnterDen(On.AbstractCreature.orig_OpportunityToEnterDen orig, AbstractCreature self, WorldCoordinate den)
        {
            if (OnlineManager.lobby != null && !self.CanMove()) return;
            orig(self, den);
        }

        // Don't think
        private void AbstractCreature_Update(On.AbstractCreature.orig_Update orig, AbstractCreature self, int time)
        {
            if (OnlineManager.lobby != null && !self.CanMove(quiet: true)) return;
            orig(self, time);
        }

        // Don't think
        private void AbstractPhysicalObject_Update(On.AbstractPhysicalObject.orig_Update orig, AbstractPhysicalObject self, int time)
        {
            if (OnlineManager.lobby != null && !self.CanMove(quiet: true)) return;
            orig(self, time);
        }

        // overseers determine what they look at based on:
        // Random.range/value calls, a ton of state that would be a waste to sync,
        // who player 1 is (i think), and the location of stars in the sky.
        // so lets not let them choose for themselves.
        private void OverseerAI_Update(On.OverseerAI.orig_Update orig, OverseerAI self)
        {
            if (!self.overseer.IsLocal())
            {
                Vector2 tempLookAt = self.lookAt;
                orig(self);
                self.lookAt = tempLookAt;
                return;
            }
            orig(self);
        }

        // remote overseers have gotten their zipping permissions revoked.
        // we might also need to block ziptoposition, but i havent been able to test if thats an issue.
        private void OverseerAI_UpdateTempHoverPosition(On.OverseerAI.orig_UpdateTempHoverPosition orig, OverseerAI self)
        {
            if (!self.overseer.IsLocal()) return;
            orig(self);
        }

        private void GarbageWorm_NewHole(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.GotoNext(moveType: MoveType.AfterLabel,
                    i => i.MatchNewobj<List<int>>(),
                    i => i.MatchStloc(0)
                    );
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate((GarbageWorm self, bool burrowed) => !burrowed || self.IsLocal());  // HACK: not burrowed on NewRoom => spawn normally
                c.Emit(OpCodes.Brfalse, skip);
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchStfld<GarbageWorm>("hole")
                    );
                c.MarkLabel(skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void GarbageWormAI_Update(On.GarbageWormAI.orig_Update orig, GarbageWormAI self)
        {
            var origAngry = self.showAsAngry;
            var origLookPoint = self.worm.lookPoint;
            orig(self);
            if (!self.creature.IsLocal())
            {
                self.worm.lookPoint = origLookPoint;
                self.showAsAngry = origAngry;
            }
        }

        private void DropBugAI_CeilingSitModule_Dislodge(On.DropBugAI.CeilingSitModule.orig_Dislodge orig, DropBugAI.CeilingSitModule self)
        {
            if (!self.AI.creature.IsLocal()) return;
            orig(self);
        }

        private void DropBugAI_CeilingSitModule_JumpFromCeiling(On.DropBugAI.CeilingSitModule.orig_JumpFromCeiling orig, DropBugAI.CeilingSitModule self, BodyChunk targetChunk, Vector2 attackDir)
        {
            if (!self.AI.creature.IsLocal()) return;
            orig(self, targetChunk, attackDir);
        }

        private void AbstractCreature_IsEnteringDen(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.GotoNext(moveType: MoveType.AfterLabel,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<AbstractWorldEntity>("world"),
                    i => i.MatchLdfld<World>("fliesWorldAI"),
                    i => i.MatchCallOrCallvirt<FliesWorldAI>("RespawnOneFly")
                    );
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((AbstractCreature self) => self.IsLocal());
                c.Emit(OpCodes.Brfalse, skip);
                c.Index += 4;
                c.MarkLabel(skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void TentaclePlantAI_Update(On.TentaclePlantAI.orig_Update orig, TentaclePlantAI self)
        {
            var mostInterestingItem = self.mostInterestingItem;
            orig(self);
            if (!self.creature.IsLocal()) self.mostInterestingItem = mostInterestingItem;
        }

        // HACK: doesn't play sounds, we should IL hook to disable just the eggs
        private void EggBug_DropEggs(On.EggBug.orig_DropEggs orig, EggBug self)
        {
            if (!self.IsLocal())
            {
                self.dropEggs = false;
                return;
            }
            orig(self);
        }

        private void Vulture_DropMask(On.Vulture.orig_DropMask orig, Vulture self, Vector2 violenceDir)
        {
            if (!self.IsLocal())
            {
                (self.State as Vulture.VultureState).mask = false;
                return;
            }
            orig(self, violenceDir);
        }

        private void BigSpider_BabyPuff(On.BigSpider.orig_BabyPuff orig, BigSpider self)
        {
            if (!self.IsLocal())
            {
                self.spewBabies = true;
                return;
            }
            orig(self);
        }
    }
}

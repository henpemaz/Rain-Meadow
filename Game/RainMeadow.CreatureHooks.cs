using System;
using System.Collections.Generic;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine;
using Watcher;
using System.Linq;

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

            ScavengerHooks();

            IL.GarbageWorm.NewHole += GarbageWorm_NewHole;
            On.GarbageWormAI.Update += GarbageWormAI_Update;

            On.DropBugAI.CeilingSitModule.Dislodge += DropBugAI_CeilingSitModule_Dislodge;
            On.DropBugAI.CeilingSitModule.JumpFromCeiling += DropBugAI_CeilingSitModule_JumpFromCeiling;

            On.EggBugGraphics.Update += EggBugGraphics_Update;
            On.BigSpiderGraphics.Update += BigSpiderGraphics_Update;

            On.EggBug.DropEggs += EggBug_DropEggs;
            On.Vulture.DropMask += Vulture_DropMask;
            On.BigSpider.BabyPuff += BigSpider_BabyPuff;
            On.VultureGrub.AttemptCallVulture += VultureGrub_AttemptCallVulture;

            On.Watcher.BoxWorm.RecieveHelp += BoxWorm_RecieveHelp;
            IL.Watcher.BoxWorm.LarvaHolder.Update += LarvaHolder_Update;

            IL.Hazer.Update += Hazer_HasSprayed;
            IL.Hazer.Die += Hazer_HasSprayed;
            
            On.Creature.Grab += Creature_Grab;
            On.Creature.SwitchGrasps += Creature_SwitchGrasps;
        }

        private void LarvaHolder_Update(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<Watcher.BoxWorm.LarvaHolder>(nameof(Watcher.BoxWorm.LarvaHolder.abstractLarva)),
                i => i.MatchBrtrue(out _));

            c.GotoNext(i => i.MatchRet());

            var ret = c.MarkLabel();

            //c.GotoPrev(MoveType.After,
            //    i => i.MatchLdarg(0),
            //    i => i.MatchLdfld<Watcher.BoxWorm.LarvaHolder>(nameof(Watcher.BoxWorm.LarvaHolder.abstractLarva)),
            //    i => i.MatchBrtrue(out _));

            //c.Emit(OpCodes.Ldarg_0);
            //c.EmitDelegate((BoxWorm.LarvaHolder self) =>
            //{
            //    var boxWorm = self.bodyChunk.owner as BoxWorm;
            //    if (!boxWorm.IsLocal())
            //    {
            //        //self.abstractLarva = new(self.room.world, null, self.room.GetWorldCoordinate(self.position), self.room.game.GetNewID());
            //        //self.room.abstractRoom.AddEntity(self.abstractLarva);

            //        //self.abstractLarva.RealizeInRoom();

            //        //self.room.world.GetResource().ApoEnteringWorld(self.abstractLarva);
            //        //self.room.abstractRoom.GetResource()?.ApoEnteringRoom(self.abstractLarva, self.abstractLarva.pos);
            //        return false; // Don't spawn larva for remotes
            //    }
            //    return true;
            //});
            //c.Emit(OpCodes.Brfalse, ret); // Return if our abstractLarva doesn't yet exist for a remote BoxWorm

            c.GotoPrev(MoveType.Before,
                i => i.MatchLdarg(0),
                i => i.MatchCallOrCallvirt<BoxWorm.LarvaHolder>(nameof(BoxWorm.LarvaHolder.ManageLarvaDetachment)));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((BoxWorm.LarvaHolder self) =>
            {
                if (!RealizedFireSpriteLarva.themoddershavebeenlefttostarve.TryGetValue((BoxWorm.Larva)self.abstractLarva.realizedObject, out _))
                {
                    RealizedFireSpriteLarva.themoddershavebeenlefttostarve.Add((BoxWorm.Larva)self.abstractLarva.realizedObject, self);
                }
            });
        }

        private void BoxWorm_RecieveHelp(On.Watcher.BoxWorm.orig_RecieveHelp orig, Watcher.BoxWorm self)
        {
            if (OnlineManager.lobby != null && self.abstractPhysicalObject.GetOnlineObject(out var opo) && opo.isMine)
            {
                orig(self);
                opo.BroadcastRPCInRoomExceptOwners(opo.RecieveHelp);
                return;
            }
            orig(self);
        }

        private void Hazer_HasSprayed(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdcI4(1),
                    i => i.MatchStfld<Hazer>(nameof(Hazer.hasSprayed))
                    );
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Hazer hazer) =>
                {
                    if (OnlineManager.lobby != null && hazer.abstractPhysicalObject.GetOnlineObject(out var opo) && opo.isMine)
                    {
                        opo.BroadcastRPCInRoomExceptOwners(opo.HazerSpraySync, hazer.spraying, hazer.inkLeft);
                    }
                });
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void VultureGrub_AttemptCallVulture(On.VultureGrub.orig_AttemptCallVulture orig, VultureGrub self)
        {
            if (OnlineManager.lobby != null && self.abstractPhysicalObject.GetOnlineObject(out var opo) && opo.isMine)
            {
                orig(self);
                opo.BroadcastRPCInRoomExceptOwners(opo.GrubResultSync, (byte)self.callingMode);
                return;
            }
            orig(self);
        }


        private bool Creature_Grab(On.Creature.orig_Grab orig, Creature self, PhysicalObject obj, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool overrideEquallyDominant, bool pacifying)
        {
            var ret = orig(self, obj, graspUsed, chunkGrabbed, shareability, dominance, overrideEquallyDominant, pacifying);
            if (ret && obj.abstractPhysicalObject.GetOnlineObject() is OnlinePhysicalObject grabbingOnline && !grabbingOnline.isMine && self.IsLocal())
            {
                OnlineCreature? oc = self.abstractCreature.GetOnlineCreature();
                if (oc is null)
                {
                    RainMeadow.Error($"grabbing entity does not exist in online space {obj.abstractPhysicalObject}");
                    return ret;
                }

                GraspRef grasp = GraspRef.FromGrasp(self.grasps[graspUsed]);
                RPCEvent graspRPC = grabbingOnline.owner.InvokeRPC(CreatureGrabRPC, oc.id, grasp);
                grabbingOnline.graspLocked.Add(graspRPC);
                graspRPC.Then((result) => grabbingOnline.graspLocked.Remove(graspRPC));

                if (!grabbingOnline.isPending && grabbingOnline.isTransferable)
                {
                    grabbingOnline.Request();
                } 
            }
            
            return ret;
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
                //orig(self, violenceDir);
                var opo = self.abstractCreature.GetOnlineObject();
                if (opo is null) return;
                opo.RunRPC(opo.Demask, violenceDir);
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

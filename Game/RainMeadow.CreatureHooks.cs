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

            On.Watcher.SandGrubAI.PickNewBurrow += SandGrubAI_PickNewBurrow;

            On.Watcher.BoxWorm.RecieveHelp += BoxWorm_RecieveHelp;
            IL.Watcher.BoxWorm.LarvaHolder.Update += LarvaHolder_Update;

            IL.Hazer.Update += Hazer_HasSprayed;
            IL.Hazer.Die += Hazer_HasSprayed;
            
            On.Creature.Grab += Creature_Grab;
            On.Creature.SwitchGrasps += Creature_SwitchGrasps;

            On.Watcher.Rattler.ValidSpawnPos += Rattler_ValidSpawnPos;

            On.MoreSlugcats.StowawayBugAI.Update += StowawayBugAI_Update; // non owners will not change behavior on their own
            IL.MoreSlugcats.StowawayBug.Update += StowawayBug_Update; // non owners will not bite on their own
            IL.MoreSlugcats.StowawayBug.bodySetup += StowawayBug_bodySetup; // calling homepos instead of bodyChunk.pos because bodyChunk.pos will be different for non owners due to sync
            IL.MoreSlugcats.StowawayBug.Act += StowawayBug_Act; // non owners will not attack on their own 
        }

        private void StowawayBug_Act(ILContext il)
        {
            var c = new ILCursor(il);

            // else if (this.AI.behavior == global::MoreSlugcats.StowawayBugAI.Behavior.Attacking || this.placedDirection.y > 0.3f)

            c.GotoNext(MoveType.Before,
                x => x.MatchLdarg(0),            
            x => x.MatchLdfld<MoreSlugcats.StowawayBug>(nameof(MoreSlugcats.StowawayBug.AI)),            
                x => x.MatchLdfld<MoreSlugcats.StowawayBugAI>(nameof(MoreSlugcats.StowawayBugAI.behavior)),            
                x => x.MatchLdsfld<MoreSlugcats.StowawayBugAI.Behavior>(nameof(MoreSlugcats.StowawayBugAI.Behavior.Attacking)),            
                x => x.MatchCall(typeof(ExtEnum<MoreSlugcats.StowawayBugAI.Behavior>).GetMethod("op_Equality")),            
                x => x.MatchBrtrue(out _)
                );

            c.GotoNext(MoveType.Before, x => x.MatchBrtrue(out _));
            c.Emit(OpCodes.Ldarg_0);

            c.EmitDelegate((bool isAttacking, MoreSlugcats.StowawayBug self) => {
                if (OnlineManager.lobby is null) return isAttacking;
                return isAttacking && self.IsLocal();
            });

            // if (!this.headFired[k] && this.spitCooldown < 0 && base.grasps[k] == null)

            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<MoreSlugcats.StowawayBug>(nameof(MoreSlugcats.StowawayBug.spitCooldown)),
                x => x.MatchLdcI4(0),
                x => x.MatchBge(out _),

                x => x.MatchLdarg(0),
                x => x.MatchCall<Creature>("get_grasps"),
                x => x.MatchLdloc(5),
                x => x.MatchLdelemRef(),
                x => x.MatchBrtrue(out _)
                );

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 5);
            
            c.EmitDelegate((MoreSlugcats.StowawayBug self, int index) => {
                if (OnlineManager.lobby is null) return;

                if (self.abstractPhysicalObject.GetOnlineObject() is OnlinePhysicalObject opo)
                    opo.BroadcastRPCInRoom(StowawayHeadAttackRPC, opo, (byte)index);
            });

        }

        private void StowawayBug_bodySetup(ILContext il)
        {
            var c = new ILCursor(il);

            // base.abstractCreature.Room.realizedRoom.RayTraceTilesList(base.abstractCreature.pos.x, base.abstractCreature.pos.y, base.abstractCreature.pos.x, 0, ref list);

            Func<Instruction, bool>[] predicates = {
                x => x.MatchLdarg(0),
                x => x.MatchCall(typeof(Creature).GetMethod("get_abstractCreature")),
                x => x.MatchCallvirt(typeof(AbstractWorldEntity).GetMethod("get_Room")),
                x => x.MatchLdfld<AbstractRoom>(nameof(AbstractRoom.realizedRoom)),

                x => x.MatchLdarg(0),
                x => x.MatchCall(typeof(Creature).GetMethod("get_abstractCreature")),
                x => x.MatchLdflda<AbstractWorldEntity>(nameof(AbstractWorldEntity.pos)),
                x => x.MatchLdfld<WorldCoordinate>(nameof(WorldCoordinate.x)),

                x => x.MatchLdarg(0),
                x => x.MatchCall(typeof(Creature).GetMethod("get_abstractCreature")),
                x => x.MatchLdflda<AbstractWorldEntity>(nameof(AbstractWorldEntity.pos)),
                x => x.MatchLdfld<WorldCoordinate>(nameof(WorldCoordinate.y)),

                x => x.MatchLdarg(0),
                x => x.MatchCall(typeof(Creature).GetMethod("get_abstractCreature")),
                x => x.MatchLdflda<AbstractWorldEntity>(nameof(AbstractWorldEntity.pos)),
                x => x.MatchLdfld<WorldCoordinate>(nameof(WorldCoordinate.x)),

                x => x.MatchLdcI4(0),
                x => x.MatchLdloca(0),
                x => x.MatchCallvirt(typeof(Room).GetMethod("RayTraceTilesList")),
                x => x.MatchPop()
            };

            var skip = c.DefineLabel();
            c.GotoNext(MoveType.After, predicates);
            skip = c.MarkLabel();

            c = new ILCursor(il);

            c.GotoNext(MoveType.Before, predicates);

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloca, 0);

            c.EmitDelegate((MoreSlugcats.StowawayBug self, ref List<RWCustom.IntVector2> list) => {
                if (OnlineManager.lobby is null) return false;

                UnityEngine.Vector2 homePos = ((MoreSlugcats.StowawayBugState)self.State).HomePos;
                RWCustom.IntVector2 intHomePos = Room.StaticGetTilePosition(homePos);
                // RayTraceTilesList(base.abstractCreature.pos.x, base.abstractCreature.pos.y, base.abstractCreature.pos.x, 0, ref list) will become
                self.abstractCreature.Room.realizedRoom.RayTraceTilesList(intHomePos.x, intHomePos.y, intHomePos.x, 0, ref list);

                return true;
            });
            c.Emit(OpCodes.Brtrue, skip);
        }

        private void StowawayBug_Update(ILContext il)
        {
            var c = new ILCursor(il);
            var skip = c.DefineLabel();

            // if (base.graphicsModule != null && (global::UnityEngine.Random.value < 0.02f || flag2) && flag)

            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchCall(typeof(PhysicalObject).GetMethod("get_graphicsModule")),
                x => x.MatchBrfalse(out _),

                x => x.MatchCall(typeof(UnityEngine.Random).GetMethod("get_value")),
                x => x.MatchLdcR4(.02f),
                x => x.MatchClt(),
                x => x.MatchLdloc(2),
                x => x.MatchOr(),
                x => x.MatchBr(out _),

                x => x.MatchLdcI4(0),

                x => x.MatchLdloc(1),
                x => x.MatchAnd(),
                x => x.MatchBrfalse(out skip)
                );

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((MoreSlugcats.StowawayBug stowaway) => {
                if (OnlineManager.lobby is null) return false;
                if (stowaway.IsLocal()) return false;
                return true;
            });
            c.Emit(OpCodes.Brtrue, skip);

            // flag3 = (base.graphicsModule as global::MoreSlugcats.StowawayBugGraphics).Bite();

            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchCall(typeof(PhysicalObject).GetMethod("get_graphicsModule")),
                x => x.MatchIsinst<MoreSlugcats.StowawayBugGraphics>(),
                x => x.MatchCallvirt(typeof(MoreSlugcats.StowawayBugGraphics).GetMethod(nameof(MoreSlugcats.StowawayBugGraphics.Bite))),
                x => x.MatchStloc(15)
                );

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((MoreSlugcats.StowawayBug self) => {
                if (OnlineManager.lobby is null) return;
                
                byte stowawayNormalByte = 0;
                if (self.abstractPhysicalObject.GetOnlineObject() is OnlinePhysicalObject opo)
                    opo.BroadcastRPCInRoom(StowawayBiteRPC, opo, stowawayNormalByte);
            });

            // if ((base.grasps[num4].grabbed as global::Creature).dead)

            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchCall<Creature>("get_grasps"),
                x => x.MatchLdloc(17),
                x => x.MatchLdelemRef(),
                x => x.MatchLdfld<Creature.Grasp>(nameof(Creature.Grasp.grabbed)),
                x => x.MatchIsinst<Creature>(),
                x => x.MatchCallvirt(typeof(Creature).GetMethod("get_dead"))
                );

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((bool isDead, MoreSlugcats.StowawayBug self) => {
                if (OnlineManager.lobby is null) return isDead;

                byte stowawayKillerByteNotDead = 1;
                byte stowawayKillerByteDead = 2;
                
            if (self.abstractPhysicalObject.GetOnlineObject() is OnlinePhysicalObject opo)
                    opo.BroadcastRPCInRoom(StowawayBiteRPC, opo, isDead ? stowawayKillerByteDead : stowawayKillerByteNotDead);

                return isDead;
            });
        }

        private void StowawayBugAI_Update(On.MoreSlugcats.StowawayBugAI.orig_Update orig, MoreSlugcats.StowawayBugAI self)
        {
            var behavior = self.behavior;
            orig(self);
            if (OnlineManager.lobby != null && !self.creature.GetOnlineCreature().isMine)
            {
                self.behavior = behavior;
            }
        }

        private bool Rattler_ValidSpawnPos(On.Watcher.Rattler.orig_ValidSpawnPos orig, Room room, RWCustom.IntVector2 pos, List<Vector2> rattlerSpawnLocsSoFar)
        {
            // Only allow room owner to spawn rattlers
            if (OnlineManager.lobby != null)
            {
                var roomSession = room.abstractRoom.GetResource();
                if (roomSession != null && roomSession.isOwner)
                {
                    return orig(room, pos, rattlerSpawnLocsSoFar);
                }
                return false;
            }
            return orig(room, pos, rattlerSpawnLocsSoFar);
        }

        private Watcher.SandGrubBurrow SandGrubAI_PickNewBurrow(On.Watcher.SandGrubAI.orig_PickNewBurrow orig, Watcher.SandGrubAI self)
        {
            if (!self.Grub.IsLocal()) return null; // Don't try switching burrows if we are a remote, only my owner is allowed to do that.
            return orig(self);
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
                grabbingOnline.Lock("grasp", grabbingOnline.owner.InvokeRPC(CreatureGrabRPC, oc.id, grasp));
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

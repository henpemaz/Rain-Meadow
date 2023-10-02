using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using RWCustom;
using MonoMod.Cil;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace RainMeadow
{
    public partial class MeadowCustomization
    {
        public static void EnableCicada()
        {
            On.Cicada.Update += Cicada_Update; // input, sync, player things
            On.Cicada.Act += Cicada_Act; // movement
            On.Cicada.Swim += Cicada_Swim; // prevent loss of control
            On.Cicada.GrabbedByPlayer += Cicada_GrabbedByPlayer; // prevent loss of control
            On.Cicada.CarryObject += Cicada_CarryObject; // more realistic grab pos
            On.CicadaAI.Update += CicadaAI_Update; // dont let AI interfere on squiddy

            IL.Cicada.Act += Cicada_Act1; // cicada pather gets confused about entering shortcuts, let our code handle that instead
                                          // also fix zerog
        }

        class CicadaController : CreatureController
        {
            public CicadaController(Cicada creature, int playerNumber) : base(creature, playerNumber)
            {
            }

            public override bool GrabImpl(PhysicalObject pickUpCandidate)
            {
                return (creature as Cicada).TryToGrabPrey(pickUpCandidate);
            }
        }

        private static void Cicada_Update(On.Cicada.orig_Update orig, Cicada self, bool eu)
        {
            if (creatureController.TryGetValue(self.abstractCreature, out var p))
            {
                p.Update(eu);
            }

            orig(self, eu);
        }

        // inputs and stuff
        // player consious update
        private static void Cicada_Act(On.Cicada.orig_Act orig, Cicada self)
        {
            if (creatureController.TryGetValue(self.abstractCreature, out var p))
            {

                p.ConsciousUpdate();

                var room = self.room;
                var chunks = self.bodyChunks;
                var nc = chunks.Length;

                //// shroom things
                //if (p.Adrenaline > 0f)
                //{
                //    if (self.waitToFlyCounter < 30) self.waitToFlyCounter = 30;
                //    if (self.flying)
                //    {
                //        self.flyingPower = Mathf.Lerp(self.flyingPower, 1.4f, 0.03f * p.Adrenaline);
                //    }
                //}
                //var stuccker = self.AI.stuckTracker; // used while in climbing/pipe mode
                //stuccker.stuckCounter = (int)Mathf.Lerp(stuccker.minStuckCounter, stuccker.maxStuckCounter, p.Adrenaline);

                // faster takeoff
                if (self.waitToFlyCounter <= 15)
                    self.waitToFlyCounter = 15;

                bool preventStaminaRegen = false;
                if (p.input[0].thrw && !p.input[1].thrw) p.wantToJump = 5;
                if (p.wantToJump > 0) // dash charge
                {
                    if (self.flying && !self.Charging && self.chargeCounter == 0 && self.stamina > 0.2f)
                    {
                        self.Charge(self.mainBodyChunk.pos + (p.inputDir == Vector2.zero ? (chunks[0].pos - chunks[1].pos) : p.inputDir) * 100f);
                        p.wantToJump = 0;
                    }
                }

                if (self.chargeCounter > 0) // charge windup or midcharge
                {
                    self.stamina -= 0.008f;
                    preventStaminaRegen = true;
                    if (self.chargeCounter < 20)
                    {
                        if (self.stamina <= 0.2f || !p.input[0].thrw) // cancel out if unable to complete
                        {
                            self.chargeCounter = 0;
                        }
                    }
                    else
                    {
                        if (self.stamina <= 0f) // cancel out mid charge if out of stamina (happens in long bouncy charges)
                        {
                            self.chargeCounter = 0;
                        }
                    }
                    self.chargeDir = (self.chargeDir
                                                + 0.15f * p.inputDir
                                                + 0.03f * Custom.DirVec(self.bodyChunks[1].pos, self.mainBodyChunk.pos)).normalized;

                    if (self.Charging && self.grasps[0] != null && self.grasps[0].grabbed is Weapon w)
                    {
                        SharedPhysics.CollisionResult result = SharedPhysics.TraceProjectileAgainstBodyChunks(null, self.room, w.firstChunk.lastPos, ref w.firstChunk.pos, w.firstChunk.rad, 1, self, true);
                        if (result.hitSomething)
                        {
                            var dir = (self.bodyChunks[0].pos - self.bodyChunks[1].pos).normalized;
                            var throwndir = new IntVector2(Mathf.Abs(dir.x) > 0.38 ? (int)Mathf.Sign(dir.x) : 0, Mathf.Abs(dir.y) > 0.38 ? (int)Mathf.Sign(dir.y) : 0);
                            w.Thrown(self, w.firstChunk.pos, w.firstChunk.lastPos, throwndir, 1f, self.evenUpdate);
                            if (w is Spear sp && !(result.obj is Player))
                            {
                                sp.spearDamageBonus *= 0.6f;
                                sp.setRotation = dir;
                            }
                            w.Forbid();
                            self.ReleaseGrasp(0);
                        }
                    }
                }

                // scoooot
                self.AI.swooshToPos = null;
                if (p.input[0].jmp)
                {
                    if (self.room.aimap.getAItile(self.mainBodyChunk.pos).terrainProximity > 1 && self.stamina > 0.5f) // cada.flying && 
                    {
                        self.AI.swooshToPos = self.mainBodyChunk.pos + p.inputDir * 40f + new Vector2(0, 4f);
                        self.flyingPower = Mathf.Lerp(self.flyingPower, 1f, 0.05f);
                        preventStaminaRegen = true;
                        self.stamina -= 0.6f * self.stamina * p.inputDir.magnitude / ((!self.gender) ? 120f : 190f);
                    }
                    else // easier takeoff
                    {
                        if (self.waitToFlyCounter < 30) self.waitToFlyCounter = 30;
                    }
                }

                // move
                var basepos = 0.5f * (self.firstChunk.pos + room.MiddleOfTile(self.abstractCreature.pos.Tile));
                if (p.inputDir != Vector2.zero || self.Charging)
                {
                    self.AI.pathFinder.AbortCurrentGenerationPathFinding(); // ignore previous dest
                    self.AI.behavior = CicadaAI.Behavior.GetUnstuck; // helps with sitting behavior
                    var dest = basepos + p.inputDir * 20f;
                    if (self.flying) dest.y -= 12f; // nose up goes funny
                    if (Mathf.Abs(p.inputDir.y) < 0.1f) // trying to move horizontally, compensate for momentum a bit
                    {
                        dest.y -= self.mainBodyChunk.vel.y * 1.3f;
                    }
                    self.abstractCreature.abstractAI.SetDestination(self.room.GetWorldCoordinate(dest));
                }
                else
                {
                    self.AI.behavior = CicadaAI.Behavior.Idle;
                    if (p.inputDir == Vector2.zero && p.inputLastDir != Vector2.zero) // let go
                    {
                        self.abstractCreature.abstractAI.SetDestination(self.room.GetWorldCoordinate(basepos));
                    }
                }

                if (preventStaminaRegen) // opposite of what happens in orig
                {
                    if (self.grabbedBy.Count == 0 && self.stickyCling == null)
                    {
                        self.stamina -= 0.014285714f;
                    }
                }
                self.stamina = Mathf.Clamp01(self.stamina);
            }

            orig(self);
        }

        // cicada brain normally shut down when it touches water huh
        private static void Cicada_Swim(On.Cicada.orig_Swim orig, Cicada self)
        {
            if (creatureController.TryGetValue(self.abstractCreature, out var p))
            {
                if (self.Consious)
                {
                    self.Act();
                    if (self.Submersion == 1f)
                    {
                        self.flying = false;
                        if (self.graphicsModule is CicadaGraphics cg)
                        {
                            cg.wingDeploymentGetTo = 0.2f;
                        }
                        self.waitToFlyCounter = 0; // so graphics uses wingdeployment
                    }
                }
            }
            orig(self);
        }

        private static void Cicada_CarryObject(On.Cicada.orig_CarryObject orig, Cicada self)
        {
            if (creatureController.TryGetValue(self.abstractCreature, out var p))
            {
                // more realistic grab pos plz
                var oldpos = self.mainBodyChunk.pos;
                var owndir = (self.bodyChunks[0].pos - self.bodyChunks[1].pos).normalized;
                self.mainBodyChunk.pos += 5f * owndir;
                orig(self); // this thing drops creatures cada doesn't eat. it's a bit weird but its ok I guess
                self.mainBodyChunk.pos = oldpos;
                return;
            }
            orig(self);
        }

        // dont let AI interfere on squiddy
        private static void CicadaAI_Update(On.CicadaAI.orig_Update orig, CicadaAI self)
        {
            if (creatureController.TryGetValue(self.creature, out var p))
            {
                p.AIUpdate(self);
            }
            else
            {
                orig(self);
            }
        }

        private static void Cicada_GrabbedByPlayer(On.Cicada.orig_GrabbedByPlayer orig, Cicada self)
        {
            if (creatureController.TryGetValue(self.abstractCreature, out var p) && self.Consious)
            {
                var oldflypower = self.flyingPower;
                self.flyingPower *= 0.6f;
                self.Act();
                orig(self);
                self.flyingPower = oldflypower;
            }
            else
            {
                orig(self);
            }
        }

        // cicada pather gets confused about entering shortcuts, let our code handle that instead, also patchup zerog
        private static void Cicada_Act1(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel dorun = null;
            ILLabel dontrun = null;
            try
            {
                c.GotoNext(MoveType.AfterLabel,
                i => i.MatchLdloc(0),
                i => i.MatchLdfld<MovementConnection>("type"),
                i => i.MatchLdcI4(13),
                i => i.MatchBeq(out dorun),
                i => i.MatchLdloc(0),
                i => i.MatchLdfld<MovementConnection>("type"),
                i => i.MatchLdcI4(14),
                i => i.MatchBneUn(out dontrun)
                );
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<Cicada, bool>>((self) => // squiddy don't
                {
                    if (creatureController.TryGetValue(self.abstractCreature, out var p))
                    {
                        return true;
                    }
                    return false;
                });
                c.Emit(OpCodes.Brtrue, dontrun); // dont run if squiddy
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
            // patchup zerog
            c.Index = 0;
            while (c.TryGotoNext(MoveType.After,
                i => (i.MatchMul() && i.Previous.MatchLdcR4(out _)) || i.MatchLdcR4(out _),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<Cicada>("flyingPower"),
                i => i.MatchMul(),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<Cicada>("stamina"),
                i => i.MatchMul(),
                i => i.MatchAdd(),
                i => i.MatchStfld<Vector2>("y")
                ))
            {
                c.Index -= 2;
                c.Emit(OpCodes.Ldarg_0);
                c.Emit<PhysicalObject>(OpCodes.Callvirt, "get_gravity");
                c.Emit(OpCodes.Mul);
                c.Emit(OpCodes.Ldc_R4, (float)(1d / 0.9d));
                c.Emit(OpCodes.Mul);
            }
        }
    }
}

using System;
using UnityEngine;
using RWCustom;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace RainMeadow
{
    class CicadaController : AirCreatureController
    {
        public CicadaController(Cicada creature, OnlineCreature oc, int playerNumber, MeadowAvatarCustomization customization) : base(creature, oc, playerNumber, customization) { }

        public Cicada cicada => creature as Cicada;

        /*
        public override bool GrabImpl(PhysicalObject pickUpCandidate)
        {
            return cicada.TryToGrabPrey(pickUpCandidate);
        }
        */

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

            // colors
            IL.CicadaGraphics.ApplyPalette += CicadaGraphics_ApplyPalette;
        }

        private static void CicadaGraphics_ApplyPalette(ILContext il)
        {
            var c = new ILCursor(il);
            try
            {
                c.Index = c.Instrs.Count - 1;
                c.GotoPrev(MoveType.After,
                i => i.MatchStloc(0)
                );
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloca, 0);
                c.EmitDelegate((CicadaGraphics self, ref Color origColor) =>
                {
                    if (RainMeadow.creatureCustomizations.TryGetValue(self.cicada, out var c))
                    {
                        c.ModifyBodyColor(ref origColor);
                        // todo eyecolor? does it matter if not customizable?
                    }
                });
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        private static void Cicada_Update(On.Cicada.orig_Update orig, Cicada self, bool eu)
        {
            if (creatureControllers.TryGetValue(self, out var p))
            {
                p.Update(eu);
            }
            orig(self, eu);
        }

        // inputs and stuff
        // player consious update
        private static void Cicada_Act(On.Cicada.orig_Act orig, Cicada self)
        {
            if (creatureControllers.TryGetValue(self, out var p))
            {
                p.ConsciousUpdate();
            }

            orig(self);
        }

        // cicada brain normally shut down when it touches water huh
        private static void Cicada_Swim(On.Cicada.orig_Swim orig, Cicada self)
        {
            if (creatureControllers.TryGetValue(self, out var p))
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
            if (creatureControllers.TryGetValue(self, out var p))
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
            if (creatureControllers.TryGetValue(self.creature.realizedCreature, out var p))
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
            if (creatureControllers.TryGetValue(self, out var p) && self.Consious)
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
            try
            {
                ILLabel dorun = null;
                ILLabel dontrun = null;
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
                    if (creatureControllers.TryGetValue(self, out var p))
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

            // patch out invalid terrain proximity calcs
            try
            {
                int num7 = 0; // the var that stores it
                c.Index = 0;
                c.GotoNext(MoveType.After,
                i => i.MatchCallvirt<AImap>("getTerrainProximity"),
                i => i.MatchConvR4(),
                i => i.MatchLdcR4(1),
                i => i.MatchCall<UnityEngine.Mathf>("Max"),
                i => i.MatchDiv(),
                i => i.MatchStloc(out num7)
                );
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, num7);
                c.EmitDelegate<Func<Cicada, float, float>>((self, num7) => // num7 is negative if oob, shouldn't
                {
                    if (creatureControllers.TryGetValue(self, out var p))
                    {
                        if (num7 < 0) num7 *= -1;
                    }
                    return num7;
                });
                c.Emit(OpCodes.Stloc, num7); // patched value
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }


            // land on horiz poles wtf game
            try
            {
                ILLabel skip = null;
                ILLabel done = null;
                c.Index = 0;
                c.GotoNext(MoveType.After,
                i => i.MatchLdfld<Room.Tile>("verticalBeam"), // line 134
                i => i.MatchBrfalse(out skip),
                i => i.MatchLdarg(0),
                i => i.MatchCall<Cicada>("Land"),
                i => i.MatchBr(out done)
                );
                c.GotoLabel(skip);
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<Cicada, bool>>((self) => // if( hascontroller && room...tile.horizontalBeam)
                {
                    if (creatureControllers.TryGetValue(self, out var p))
                    {
                        if (self.room.GetTile(self.mainBodyChunk.pos).horizontalBeam)
                        {
                            self.Land();
                            return true;
                        }
                    }
                    return false;
                });
                c.Emit(OpCodes.Brtrue, done);
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        internal override void ConsciousUpdate()
        {
            base.ConsciousUpdate();

            var room = cicada.room;
            var chunks = cicada.bodyChunks;
            var nc = chunks.Length;

            bool localTrace = UnityEngine.Input.GetKey(KeyCode.L);

            //// shroom things
            //if (this.Adrenaline > 0f)
            //{
            //    if (cicada.waitToFlyCounter < 30) cicada.waitToFlyCounter = 30;
            //    if (cicada.flying)
            //    {
            //        cicada.flyingPower = Mathf.Lerp(cicada.flyingPower, 1.4f, 0.03f * this.Adrenaline);
            //    }
            //}
            //var stuccker = cicada.AI.stuckTracker; // used while in climbing/pipe mode
            //stuccker.stuckCounter = (int)Mathf.Lerp(stuccker.minStuckCounter, stuccker.maxStuckCounter, this.Adrenaline);

            // faster takeoff
            if (cicada.waitToFlyCounter <= 30)
                cicada.waitToFlyCounter = 30;

            bool preventStaminaRegen = false;
            if (this.wantToThrow > 0) // dash charge
            {
                if (cicada.flying && !cicada.Charging && cicada.chargeCounter == 0 && cicada.stamina > 0.2f)
                {
                    if (localTrace) RainMeadow.Debug("Dash charge!");
                    cicada.Charge(cicada.mainBodyChunk.pos + (this.inputDir == Vector2.zero ? (chunks[0].pos - chunks[1].pos) : this.inputDir) * 100f);
                    this.wantToThrow = 0;
                }
            }

            if (cicada.chargeCounter > 0) // charge windup or midcharge
            {
                if (localTrace) RainMeadow.Debug("charging");
                cicada.stamina -= 0.008f;
                preventStaminaRegen = true;
                if (cicada.chargeCounter < 20)
                {
                    if (cicada.stamina <= 0.2f || !this.input[0].thrw) // cancel out if unable to complete
                    {
                        cicada.chargeCounter = 0;
                    }
                }
                else
                {
                    if (cicada.stamina <= 0f) // cancel out mid charge if out of stamina (happens in long bouncy charges)
                    {
                        cicada.chargeCounter = 0;
                    }
                }
                cicada.chargeDir = (cicada.chargeDir
                                            + 0.15f * this.inputDir
                                            + 0.03f * Custom.DirVec(cicada.bodyChunks[1].pos, cicada.mainBodyChunk.pos)).normalized;

                // hopefully won't be needing that in the meadows...
                //if (cicada.Charging && cicada.grasps[0] != null && cicada.grasps[0].grabbed is Weapon w)
                //{
                //    SharedPhysics.CollisionResult result = SharedPhysics.TraceProjectileAgainstBodyChunks(null, cicada.room, w.firstChunk.lastPos, ref w.firstChunk.pos, w.firstChunk.rad, 1, cicada, true);
                //    if (result.hitSomething)
                //    {
                //        var dir = (cicada.bodyChunks[0].pos - cicada.bodyChunks[1].pos).normalized;
                //        var throwndir = new IntVector2(Mathf.Abs(dir.x) > 0.38 ? (int)Mathf.Sign(dir.x) : 0, Mathf.Abs(dir.y) > 0.38 ? (int)Mathf.Sign(dir.y) : 0);
                //        w.Thrown(cicada, w.firstChunk.pos, w.firstChunk.lastPos, throwndir, 1f, cicada.evenUpdate);
                //        if (w is Spear sp && !(result.obj is Player))
                //        {
                //            sp.spearDamageBonus *= 0.6f;
                //            sp.setRotation = dir;
                //        }
                //        w.Forbid();
                //        cicada.ReleaseGrasp(0);
                //    }
                //}
            }

            // scoooot
            cicada.AI.swooshToPos = null;
            if (this.input[0].jmp)
            {
                if (localTrace) RainMeadow.Debug("jump input");
                if (cicada.room.aimap.getTerrainProximity(cicada.mainBodyChunk.pos) > 1 && cicada.stamina > 0.5f) // cada.flying && 
                {
                    if (localTrace) RainMeadow.Debug("flight");
                    cicada.AI.swooshToPos = cicada.mainBodyChunk.pos + this.inputDir * 40f + new Vector2(0, 4f);
                    cicada.flyingPower = Mathf.Lerp(cicada.flyingPower, 1f, 0.05f);
                    preventStaminaRegen = true;
                    cicada.stamina -= 0.6f * cicada.stamina * this.inputDir.magnitude / ((!cicada.gender) ? 120f : 190f);
                }
                else // easier takeoff
                {
                    if (cicada.waitToFlyCounter < 30) cicada.waitToFlyCounter = 30;
                }
            }

            if (preventStaminaRegen) // opposite of what happens in orig
            {
                if (cicada.grabbedBy.Count == 0 && cicada.stickyCling == null)
                {
                    cicada.stamina -= 0.014285714f;
                }
            }
            cicada.stamina = Mathf.Clamp01(cicada.stamina);
        }

        protected override void LookImpl(Vector2 pos)
        {
            if (cicada.graphicsModule == null) return;
            var dir = (pos - cicada.DangerPos) / 500f;
            var mag = dir.magnitude;

            (cicada.graphicsModule as CicadaGraphics).lookDir = dir.normalized * Mathf.Pow(mag, 0.5f) * 1.5f;
            (cicada.graphicsModule as CicadaGraphics).lookRotation = - RWCustom.Custom.VecToDeg(dir);
        }

        public override WorldCoordinate CurrentPathfindingPosition
        {
            get
            {
                if (cicada.AtSitDestination)
                {
                    return cicada.AI.pathFinder.destination;
                }
                else if (cicada.flying && cicada.Climbable(creature.coord.Tile))
                {
                    return creature.coord;
                }
                else if (Custom.DistLess(creature.coord, cicada.AI.pathFinder.destination, 2))
                {
                    return cicada.AI.pathFinder.destination;
                }
                else
                {
                    return creature.room.GetWorldCoordinate(0.5f * (cicada.firstChunk.pos + creature.room.MiddleOfTile(cicada.abstractCreature.pos.Tile)) + (cicada.flying && !creature.IsTileSolid(0, 0, -1) ? new Vector2(0, -20f) : new Vector2(0, 0f)));
                }
            }
        }

        internal override bool FindDestination(WorldCoordinate basecoord, out WorldCoordinate toPos, out float magnitude)
        {
            if (base.FindDestination(basecoord, out toPos, out magnitude)) return true;
            var basepos = 0.5f * (cicada.firstChunk.pos + creature.room.MiddleOfTile(cicada.abstractCreature.pos.Tile));
            var dest = basepos + this.inputDir * 30f;
            if (cicada.flying) dest.y -= 12f; // nose up goes funny
            if (Mathf.Abs(this.inputDir.y) < 0.1f) // trying to move horizontally, compensate for momentum a bit
            {
                dest.y -= cicada.mainBodyChunk.vel.y * 2f;
            }
            if (UnityEngine.Input.GetKey(KeyCode.L)) RainMeadow.Debug($"pathfinding {basepos} -> {dest}");
            toPos = cicada.room.GetWorldCoordinate(dest);
            magnitude = inputDir.magnitude;
            return true;
        }

        protected override void Resting()
        {
            cicada.AI.behavior = CicadaAI.Behavior.Idle;
        }

        protected override void Moving(float magnitude)
        {
            cicada.AI.behavior = CicadaAI.Behavior.GetUnstuck; // helps with sitting behavior
        }
    }
}

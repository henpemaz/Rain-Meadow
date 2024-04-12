using UnityEngine;
using System;
using RWCustom;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace RainMeadow
{
    class LizardController : GroundCreatureController
    {
        public LizardController(Lizard lizard, OnlineCreature oc, int playerNumber) : base(lizard, oc, playerNumber){

            this.lizard = lizard;
        }

        public Lizard lizard;

        public override bool CanClimbJump => !lizard.applyGravity && !CanGroundJump;

        public override bool CanPoleJump => Climbing;

        public override bool CanGroundJump => HasFooting;
        
        public override bool HasFooting
        {
            get
            {
                if (lizard.applyGravity)
                {
                    return creature.bodyChunks[0].contactPoint.y == -1 || creature.bodyChunks[1].contactPoint.y == -1;
                }
                else
                {
                    return IsTileFooting(1, 0, -1) || IsTileFooting(0, 0, -1);
                }
            }
        }

        public override bool Climbing => !lizard.applyGravity && !IsTileFooting(0, 0, -1) && !IsTileFooting(1, 0, -1) && (GetTile(0).AnyBeam || GetTile(1).AnyBeam);

        public override bool GrabImpl(PhysicalObject pickUpCandidate)
        {
            var chunk = pickUpCandidate.bodyChunks[0];
            lizard.biteControlReset = false;
            lizard.JawOpen = 0f;
            lizard.lastJawOpen = 0f;

            chunk.vel += creature.mainBodyChunk.vel * Mathf.Lerp(creature.mainBodyChunk.mass, 1.1f, 0.5f) / Mathf.Max(1f, chunk.mass);
            if (creature.Grab(chunk.owner, 0, chunk.index, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, lizard.lizardParams.biteDominance * UnityEngine.Random.value, overrideEquallyDominant: true, pacifying: true))
            {
                if (creature.graphicsModule != null)
                {
                    if (chunk.owner is IDrawable)
                    {
                        creature.graphicsModule.AddObjectToInternalContainer(chunk.owner as IDrawable, 0);
                    }
                    else if (chunk.owner.graphicsModule != null)
                    {
                        creature.graphicsModule.AddObjectToInternalContainer(chunk.owner.graphicsModule, 0);
                    }
                }

                creature.room.PlaySound(SoundID.Lizard_Jaws_Grab_NPC, creature.mainBodyChunk);
                return true;
            }

            creature.room.PlaySound(SoundID.Lizard_Jaws_Shut_Miss_Creature, creature.mainBodyChunk);
            return false;
        }

        public static void EnableLizard()
        {
            On.Lizard.Update += Lizard_Update;
            On.Lizard.Act += Lizard_Act;
            On.LizardAI.Update += LizardAI_Update;

            IL.Lizard.CarryObject += Lizard_CarryObject1;

            // color
            On.LizardGraphics.ctor += LizardGraphics_ctor;

            On.LizardGraphics.Update += LizardGraphics_Update;

            On.Lizard.AttemptBite += Lizard_AttemptBite;
            On.Lizard.DamageAttack += Lizard_DamageAttack;


            On.LizardPather.FollowPath += LizardPather_FollowPath;

            On.LizardJumpModule.RunningUpdate += LizardJumpModule_RunningUpdate;
            On.LizardJumpModule.Jump += LizardJumpModule_Jump;
        }

        private static void LizardJumpModule_Jump(On.LizardJumpModule.orig_Jump orig, LizardJumpModule self)
        {
            if (creatureControllers.TryGetValue(self.lizard.abstractCreature, out var c) && c is LizardController l)
            {
                l.superLaunchJump = 0;
                l.forceJump = 10;
            }
            orig(self);
        }

        private static void LizardJumpModule_RunningUpdate(On.LizardJumpModule.orig_RunningUpdate orig, LizardJumpModule self)
        {
            if (creatureControllers.TryGetValue(self.lizard.abstractCreature, out var c) && c is LizardController)
            {
                return;
            }
            orig(self);
        }

        private static MovementConnection LizardPather_FollowPath(On.LizardPather.orig_FollowPath orig, LizardPather self, WorldCoordinate originPos, int? bodyDirection, bool actuallyFollowingThisPath)
        {
            if (creatureControllers.TryGetValue(self.creature, out var c) && c is LizardController l)
            {
                if (originPos == self.destination)// such a silly behavior...
                    return new MovementConnection(MovementConnection.MovementType.Standard, originPos, WorldCoordinate.AddIntVector(originPos, new IntVector2(l.input[0].x, l.input[0].y)), 1);
            }

            return orig(self, originPos, bodyDirection, actuallyFollowingThisPath);
        }

        private static void Lizard_DamageAttack(On.Lizard.orig_DamageAttack orig, Lizard self, BodyChunk chunk, float dmgFac)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                return;
            }
            orig(self, chunk, dmgFac);
        }

        private static void Lizard_AttemptBite(On.Lizard.orig_AttemptBite orig, Lizard self, Creature creature)
        {
            if(OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                return;
            }
            orig(self, creature);
        }

        private static void LizardGraphics_Update(On.LizardGraphics.orig_Update orig, LizardGraphics self)
        {
            orig(self);
            if (creatureControllers.TryGetValue(self.lizard.abstractCreature, out var c) && c is LizardController l)
            {
                if (l.superLaunchJump > 0)
                {
                    float f = Mathf.Pow(Mathf.Clamp01((l.superLaunchJump - 10) / 10f), 2);
                    self.drawPositions[0, 0].y -= 3 * f;
                    self.drawPositions[1, 0].y -= 5 * f;
                    self.drawPositions[2, 0].y += 3 * f;
                    self.tail[0].vel.x -= l.flipDirection * 2f * f;
                    self.tail[0].vel.y += 1f * f;
                }
            }
        }

        // lizard will auto-drop non-creatures or non-eatties
        private static void Lizard_CarryObject1(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel dorun = null;
            ILLabel dontrun = null;
            try
            {
                c.GotoNext(MoveType.AfterLabel,
                i => i.MatchIsinst<Creature>(),
                i => i.MatchBrfalse(out dorun));
                c.GotoPrev(MoveType.After,
                i => i.MatchBgeUn(out dontrun));

                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<Lizard, bool>>((self) => // lizard don't
                {
                    if (creatureControllers.TryGetValue(self.abstractCreature, out var p))
                    {
                        return true;
                    }
                    return false;
                });
                c.Emit(OpCodes.Brtrue, dontrun); // dont run if lizard
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        private static void LizardGraphics_ctor(On.LizardGraphics.orig_ctor orig, LizardGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            if (RainMeadow.creatureCustomizations.TryGetValue(ow as Creature, out var c))
            {
                var col = self.lizard.effectColor;
                c.ModifyBodyColor(ref col);
                RainMeadow.Debug($"{self.lizard} color from {self.lizard.effectColor} to {col}");
                self.lizard.effectColor = col;
            }
        }

        private static void LizardAI_Update(On.LizardAI.orig_Update orig, LizardAI self)
        {
            if (creatureControllers.TryGetValue(self.creature, out var p))
            {
                p.AIUpdate(self);
            }
            else
            {
                orig(self);
            }
        }

        protected override void JumpImpl()
        {
            //RainMeadow.Debug(onlineCreature);
            var cs = creature.bodyChunks;
            var mainBodyChunk = creature.mainBodyChunk;

            // todo take body factors into factor. blue liz jump feels too stronk
            if (canGroundJump > 0 && superLaunchJump >= 20)
            {
                RainMeadow.Debug("lizard super jump");
                superLaunchJump = 0;
                lizard.movementAnimation = null;
                lizard.inAllowedTerrainCounter = 0;
                lizard.gripPoint = null;
                this.jumpBoost = 6f;
                this.forceBoost = 6;
                for (int i = 0; i < cs.Length; i++)
                {
                    BodyChunk chunk = cs[i];
                    chunk.vel.x += 8 * flipDirection;
                    chunk.vel.y += 6;
                }
                creature.room.PlaySound(SoundID.Slugcat_Super_Jump, mainBodyChunk, false, 1f, 1f);
            }
            else if (canPoleJump > 0)
            {
                this.jumpBoost = 0f;
                if (this.input[0].x != 0)
                {
                    RainMeadow.Debug("lizard pole jump");
                    lizard.movementAnimation = null;
                    lizard.inAllowedTerrainCounter = 0;
                    lizard.gripPoint = null;
                    this.forceJump = 10;
                    flipDirection = this.input[0].x;
                    cs[0].vel.x = 6f * flipDirection;
                    cs[0].vel.y = 6f;
                    cs[1].vel.x = 6f * flipDirection;
                    cs[1].vel.y = 5f;
                    cs[2].vel.x = 6f * flipDirection;
                    cs[2].vel.y = 5f;
                    creature.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, mainBodyChunk, false, 1f, 1f);
                    return;
                }
                if (this.input[0].y <= 0)
                {
                    RainMeadow.Debug("lizard pole drop");
                    lizard.movementAnimation = null;
                    lizard.inAllowedTerrainCounter = 0;
                    lizard.gripPoint = null;
                    mainBodyChunk.vel.y = 2f;
                    if (this.input[0].y > -1)
                    {
                        mainBodyChunk.vel.x = 2f * flipDirection;
                    }
                    creature.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, mainBodyChunk, false, 0.3f, 1f);
                    return;
                }// no climb boost
            }
            else if (canGroundJump > 0)
            {
                RainMeadow.Debug("lizard normal jump");
                lizard.movementAnimation = null;
                lizard.inAllowedTerrainCounter = 0;
                lizard.gripPoint = null;
                this.jumpBoost = 6;
                cs[0].vel.y = 4f;
                cs[1].vel.y = 5f;
                cs[2].vel.y = 3f;
                if (input[0].x != 0)
                {
                    var d = input[0].x;
                    cs[0].vel.x += d * 1.2f;
                    cs[1].vel.x += d * 1.2f;
                    cs[2].vel.x += d * 1.2f;
                }

                creature.room.PlaySound(SoundID.Slugcat_Normal_Jump, mainBodyChunk, false, 1f, 1f);
            }
            else if (canClimbJump > 0)
            {
                RainMeadow.Debug("lizard climb jump");
                lizard.movementAnimation = null;
                lizard.inAllowedTerrainCounter = 0;
                lizard.gripPoint = null;
                this.jumpBoost = 3f;
                var jumpdir = (cs[0].pos - cs[1].pos).normalized + inputDir;
                for (int i = 0; i < cs.Length; i++)
                {
                    BodyChunk chunk = cs[i];
                    chunk.vel += jumpdir;
                }
                creature.room.PlaySound(SoundID.Slugcat_Wall_Jump, mainBodyChunk, false, 1f, 1f);
            }
            else throw new InvalidProgrammerException("can't jump");
        }

        private static void Lizard_Act(On.Lizard.orig_Act orig, Lizard self)
        {
            if (creatureControllers.TryGetValue(self.abstractCreature, out var c) && c is LizardController l)
            {
                l.ConsciousUpdate();
            }
            orig(self);
        }

        private static void Lizard_Update(On.Lizard.orig_Update orig, Lizard self, bool eu)
        {
            if (creatureControllers.TryGetValue(self.abstractCreature, out var p))
            {
                p.Update(eu);
            }

            orig(self, eu);
        }

        protected override void LookImpl(Vector2 pos)
        {
            (lizard.graphicsModule as LizardGraphics).lookPos = pos;
        }

        internal override void ConsciousUpdate()
        {
            base.ConsciousUpdate();

            if(lizard.jumpModule is LizardJumpModule jumpModule)
            {
                if(this.superLaunchJump > 10)
                {
                    if (input[0].jmp)
                    {
                        if (jumpModule.actOnJump == null)
                        {
                            // start a new jump
                            RainMeadow.Debug("JumpModule init");
                            var jumpFinder = new LizardJumpModule.JumpFinder(creature.room, jumpModule, lizard.coord.Tile, false);
                            jumpFinder.currentJump.power = 0.5f;
                            jumpFinder.bestJump = jumpFinder.currentJump;
                            jumpFinder.bestJump.goalCell = jumpFinder.startCell;
                            jumpFinder.bestJump.tick = 20;

                            //jumpModule.spin = 1;
                            jumpModule.InitiateJump(jumpFinder, false);
                        }
                        jumpModule.actOnJump.vel = (creature.bodyChunks[0].pos - creature.bodyChunks[1].pos).normalized * 4f + (inputDir.magnitude > 0.5f ? inputDir * 14 + new Vector2(0, 2) : new Vector2(12f * flipDirection, 9f));
                        jumpModule.actOnJump.bestJump.initVel = jumpModule.actOnJump.vel;
                        jumpModule.actOnJump.bestJump.goalCell = lizard.AI.pathFinder.PathingCellAtWorldCoordinate(creature.room.GetWorldCoordinate(creature.bodyChunks[0].pos + jumpModule.actOnJump.vel * 20));
                        canGroundJump = 5; // doesn't interrupt
                        superLaunchJump = 12; // never completes
                        lockInPlace = true;
                        Moving(1f);
                    }

                    else
                    {
                        if (lizard.animation != Lizard.Animation.Jumping)
                        {
                            jumpModule.actOnJump = null;
                        }
                    }
                }
            }

            // lost footing doesn't auto-recover
            if (lizard.inAllowedTerrainCounter < 10)
            {
                if ((forceJump > 0 || input[0].y < 1) && !(creature.bodyChunks[0].contactPoint.y == -1 || creature.bodyChunks[1].contactPoint.y == -1 || creature.IsTileSolid(1, 0, -1) || creature.IsTileSolid(0, 0, -1)))
                {
                    lizard.inAllowedTerrainCounter = 0;
                }
            }
            // footing recovers faster on climbing ledges etc
            if (lizard.inAllowedTerrainCounter < 20 && input[0].x != 0 && (creature.bodyChunks[0].contactPoint.x == input[0].x || creature.bodyChunks[1].contactPoint.x == input[0].x))
            {
                if (lizard.inAllowedTerrainCounter > 0) lizard.inAllowedTerrainCounter = Mathf.Max(lizard.inAllowedTerrainCounter + 1, 10);
            }

            if(inputDir.magnitude > 0f && !lockInPlace)
            {
                creature.bodyChunks[0].vel += inputDir;
                creature.bodyChunks[1].vel -= inputDir;
            }
        }

        protected override void Moving(float magnitude)
        {
            lizard.AI.behavior = LizardAI.Behavior.Travelling;
            lizard.AI.runSpeed = Custom.LerpAndTick(lizard.AI.runSpeed, 0.9f * magnitude, 0.2f, 0.05f);
            lizard.AI.excitement = Custom.LerpAndTick(lizard.AI.excitement, 0.6f, 0.1f, 0.05f);

            var tile0 = creature.room.GetTile(creature.bodyChunks[0].pos);

            // greater air friction because uhhh didnt feel right
            if (lizard.applyGravity && creature.room.aimap.TileAccessibleToCreature(tile0.X, tile0.Y, lizard.Template))
            {
                for (int i = 0; i < creature.bodyChunks.Length; i++)
                {
                    BodyChunk bodyChunk = lizard.bodyChunks[i];
                    if (bodyChunk.submersion < 0.5f)
                    {
                        bodyChunk.vel.x *= 0.95f;
                    }
                }
            }
        }

        protected override void Resting()
        {
            lizard.AI.behavior = LizardAI.Behavior.Idle;
            lizard.AI.runSpeed = Custom.LerpAndTick(lizard.AI.runSpeed, 0f, 0.4f, 0.1f);
            lizard.AI.excitement = Custom.LerpAndTick(lizard.AI.excitement, 0f, 0.1f, 0.05f);

            // pull towards floor
            for (int i = 0; i < lizard.bodyChunks.Length; i++)
            {
                if (lizard.IsTileSolid(i, 0, -1))
                {
                    BodyChunk bodyChunk = lizard.bodyChunks[i];
                    bodyChunk.vel.y -= 0.1f;
                }
            }
        }

        protected override void GripPole(Room.Tile tile0)
        {
            if (lizard.inAllowedTerrainCounter < 10)
            {
                lizard.gripPoint = creature.room.MiddleOfTile(tile0.X, tile0.Y);
            }
        }

        protected override void MovementOverride(MovementConnection movementConnection)
        {
            lizard.commitedToDropConnection = movementConnection;
            lizard.inAllowedTerrainCounter = 0;
        }

        protected override void ClearMovementOverride()
        {
            lizard.commitedToDropConnection = default(MovementConnection);
        }
    }
}

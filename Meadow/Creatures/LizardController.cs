﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using UnityEngine;

namespace RainMeadow
{
    public class LizardController : GroundCreatureController
    {
        public static void EnableLizard()
        {
            // controller
            On.Lizard.Update += Lizard_Update;
            On.Lizard.Act += Lizard_Act;
            On.LizardAI.Update += LizardAI_Update;
            // don't drop it
            IL.Lizard.CarryObject += Lizard_CarryObject1;
            // swim, bastard
            IL.Lizard.SwimBehavior += Lizard_SwimBehavior;

            On.Lizard.SwimBehavior += Lizard_SwimBehavior1;
            On.Lizard.FollowConnection += Lizard_FollowConnection;

            On.Lizard.GripPointBehavior += Lizard_GripPointBehavior;

            // pounce visuals
            On.LizardGraphics.Update += LizardGraphics_Update;
            On.LizardGraphics.ctor += LizardGraphics_ctor;
            // no violence
            On.Lizard.AttemptBite += Lizard_AttemptBite;
            On.Lizard.DamageAttack += Lizard_DamageAttack;
            // path towards input
            On.LizardPather.FollowPath += LizardPather_FollowPath;
            // no auto jumps
            On.LizardJumpModule.RunningUpdate += LizardJumpModule_RunningUpdate;
            On.LizardJumpModule.Jump += LizardJumpModule_Jump;

            On.LizardBreedParams.TerrainSpeed += LizardBreedParams_TerrainSpeed;

            // dev skin
            On.LizardGraphics.InitiateSprites += LizardGraphics_InitiateSprites;
        }

        private static void LizardGraphics_InitiateSprites(On.LizardGraphics.orig_InitiateSprites orig, LizardGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);

            if (creatureControllers.TryGetValue(self.lizard, out var p))
            {
                if (p.customization.IsNightSkySkin(p.onlineCreature))
                {
                    for (int i = 0; i < sLeaser.sprites.Length; i++)
                    {
                        sLeaser.sprites[i].shader = rCam.game.rainWorld.Shaders["RM_NightSkySkin"];
                    }
                }
            }
        }

        private static void Lizard_GripPointBehavior(On.Lizard.orig_GripPointBehavior orig, Lizard self)
        {
            orig(self);
            if (creatureControllers.TryGetValue(self, out var c))
            {
                self.inAllowedTerrainCounter = Mathf.Max(self.inAllowedTerrainCounter, 15);
            }
        }

        private static void Lizard_FollowConnection(On.Lizard.orig_FollowConnection orig, Lizard self, float runSpeed)
        {
            if (creatureControllers.TryGetValue(self, out var c))
            {
                if (Input.GetKey(KeyCode.L)) RainMeadow.Error("following connection: " + self.followingConnection);
                if (Input.GetKey(KeyCode.L)) RainMeadow.Error("pre vel: " + self.mainBodyChunk.vel);
                orig(self, runSpeed);
                if (Input.GetKey(KeyCode.L)) RainMeadow.Error("post vel: " + self.mainBodyChunk.vel);
                return;
            }
            orig(self, runSpeed);
        }

        private static void Lizard_SwimBehavior1(On.Lizard.orig_SwimBehavior orig, Lizard self)
        {
            orig(self);
            if (creatureControllers.TryGetValue(self, out var c))
            {
                if (self.followingConnection.distance == 0)
                {
                    if (Input.GetKey(KeyCode.L)) RainMeadow.Error("following null connection");
                    var originPos = self.room.GetWorldCoordinate(self.mainBodyChunk.pos);
                    self.followingConnection = new MovementConnection(MovementConnection.MovementType.Standard, originPos, WorldCoordinate.AddIntVector(originPos, new IntVector2(c.input[0].x, c.input[0].y)), 1);
                }
            }
        }

        private static void Lizard_SwimBehavior(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);

                // replace "this.Swimmer" checks with custom || swimmer

                ILLabel run = c.DefineLabel();
                // if (this.Swimmer)
                c.GotoNext(moveType: MoveType.AfterLabel,
                     i => i.MatchLdarg(0),
                     i => i.MatchCall<Lizard>("get_Swimmer"),
                     i => i.MatchBrfalse(out _)
                     );
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Lizard self) =>
                {
                    if (creatureControllers.TryGetValue(self, out var controller))
                    {
                        return true;
                    }
                    return false;
                });

                c.Emit(OpCodes.Brtrue, run);
                c.Index += 3;
                c.MarkLabel(run);
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        private static LizardBreedParams.SpeedMultiplier LizardBreedParams_TerrainSpeed(On.LizardBreedParams.orig_TerrainSpeed orig, LizardBreedParams self, AItile.Accessibility acc)
        {
            var result = orig(self, acc);
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                if (acc == AItile.Accessibility.Climb)
                {
                    return new LizardBreedParams.SpeedMultiplier(1f, 1f, 1f, 1f);
                }
            }
            return result;
        }

        private static void Lizard_Update(On.Lizard.orig_Update orig, Lizard self, bool eu)
        {
            if (creatureControllers.TryGetValue(self, out var p))
            {
                p.Update(eu);
            }

            orig(self, eu);
        }

        private static void Lizard_Act(On.Lizard.orig_Act orig, Lizard self)
        {
            if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"liz act pre");
            if (creatureControllers.TryGetValue(self, out var c) && c is LizardController l)
            {
                l.ConsciousUpdate();
            }
            orig(self);
            if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"liz act post");
        }

        private static void LizardAI_Update(On.LizardAI.orig_Update orig, LizardAI self)
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

        private static void LizardJumpModule_Jump(On.LizardJumpModule.orig_Jump orig, LizardJumpModule self)
        {
            if (creatureControllers.TryGetValue(self.lizard, out var c) && c is LizardController l)
            {
                l.superLaunchJump = 0;
                l.forceJump = 10;
            }
            orig(self);
        }

        private static void LizardJumpModule_RunningUpdate(On.LizardJumpModule.orig_RunningUpdate orig, LizardJumpModule self)
        {
            if (creatureControllers.TryGetValue(self.lizard, out var c) && c is LizardController)
            {
                return;
            }
            orig(self);
        }

        private static MovementConnection LizardPather_FollowPath(On.LizardPather.orig_FollowPath orig, LizardPather self, WorldCoordinate originPos, int? bodyDirection, bool actuallyFollowingThisPath)
        {
            if (creatureControllers.TryGetValue(self.creature.realizedCreature, out var c))
            {
                if (originPos == self.destination || (actuallyFollowingThisPath && self.lookingForImpossiblePath))
                {
                    if (Input.GetKey(KeyCode.L) && actuallyFollowingThisPath) RainMeadow.Debug("returning override. lookingForImpossiblePath? " + self.lookingForImpossiblePath);
                    return new MovementConnection(MovementConnection.MovementType.Standard, originPos, self.destination, 1);
                }
                return orig(self, originPos, bodyDirection, actuallyFollowingThisPath);
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
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                return;
            }
            orig(self, creature);
        }

        private static void LizardGraphics_ctor(On.LizardGraphics.orig_ctor orig, LizardGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            if (creatureControllers.TryGetValue(self.lizard, out var c))
            {
                if (c.customization.skin == MeadowProgression.Skin.Lizard_Axo)
                {
                    self.blackSalamander = false;
                }
                if (c.customization.skin == MeadowProgression.Skin.Lizard_Sala)
                {
                    self.blackSalamander = true;
                }
            }
        }

        private static void LizardGraphics_Update(On.LizardGraphics.orig_Update orig, LizardGraphics self)
        {
            orig(self);
            if (creatureControllers.TryGetValue(self.lizard, out var c) && c is LizardController l)
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
                    if (creatureControllers.TryGetValue(self, out var p))
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

        public LizardController(Lizard lizard, OnlineCreature oc, int playerNumber, MeadowAvatarData customization) : base(lizard, oc, playerNumber, customization)
        {
            this.lizard = lizard;
            lizard.abstractCreature.personality.energy = 1f; // stop being lazy
            jumpFactor = 1.2f;
            runSpeed = 4.5f;
            // this.needsLight = false; // has builtin light
            // or so I thought but vanilla light is too small, give it two!
            canZeroGClimb = true;

            var col = lizard.effectColor;
            customization.ModifyBodyColor(ref col);
            RainMeadow.Debug($"{lizard} color from {lizard.effectColor} to {col}");
            lizard.effectColor = col;

            if(lizard.lizardParams.regainFootingCounter < 2)
            {
                // aaaugh
                lizard.lizardParams.regainFootingCounter = 2; // no one will ever know
            }
        }

        public Lizard lizard;

        public override bool HasFooting => lizard.inAllowedTerrainCounter > 10 || lizard.gripPoint != null;

        public override bool IsOnGround => IsTileGround(1, 0, -1) || IsTileGround(0, 0, -1) || (!IsOnPole && IsTileGround(2, 0, -1));

        public override bool IsOnPole => GetTile(0).AnyBeam || GetTile(1).AnyBeam;

        public override bool IsOnCorridor => GetAITile(0).narrowSpace;

        public override bool IsOnClimb
        {
            get
            {
                if (WallClimber)
                {
                    var acc = GetAITile(0).acc;
                    if ((acc == AItile.Accessibility.Climb && !GetTile(0).AnyBeam) || acc == AItile.Accessibility.Wall)
                    {
                        return true;
                    }
                    acc = GetAITile(1).acc;
                    if ((acc == AItile.Accessibility.Climb && !GetTile(1).AnyBeam) || acc == AItile.Accessibility.Wall)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public override bool CanPounce
        {
            get
            {
                if (lizard.jumpModule != null)
                {
                    return HasFooting;
                }
                return base.CanPounce;
            }
        }

        /*
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
        */

        protected override void OnJump()
        {
            lizard.movementAnimation = null;
            lizard.inAllowedTerrainCounter = 0;
            lizard.gripPoint = null;
        }

        protected override void LookImpl(Vector2 pos)
        {
            if (lizard.graphicsModule != null)
            {
                (lizard.graphicsModule as LizardGraphics).lookPos = pos;
            }
        }

        public override void ConsciousUpdate()
        {
            base.ConsciousUpdate();

            if (lizard.jumpModule is LizardJumpModule jumpModule)
            {
                if (this.superLaunchJump > 10)
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
                if (!(WallClimber && input[0].y == 1) 
                    && lizard.gripPoint == null 
                    && creature.bodyChunks[0].contactPoint.y != -1 
                    && creature.bodyChunks[1].contactPoint.y != -1 
                    && !creature.IsTileSolid(1, 0, -1) 
                    && !creature.IsTileSolid(0, 0, -1))
                {
                    lizard.inAllowedTerrainCounter = 0;
                }
            }
            // footing recovers faster on climbing ledges etc
            if (forceJump <= 0 && lizard.inAllowedTerrainCounter < 20 && input[0].x != 0 && (creature.bodyChunks[0].contactPoint.x == input[0].x || creature.bodyChunks[1].contactPoint.x == input[0].x))
            {
                if (lizard.inAllowedTerrainCounter > 0) lizard.inAllowedTerrainCounter = Mathf.Max(lizard.inAllowedTerrainCounter + 1, 10);
            }

            // body points to input
            if (inputDir.magnitude > 0f && !lockInPlace)
            {
                creature.bodyChunks[0].vel += inputDir * 0.4f;
                creature.bodyChunks[2].vel -= inputDir * 0.4f;
            }

            // climb that damn ledge
            if (input[0].x != 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (creature.bodyChunks[i].contactPoint.x == input[0].x
                    && creature.bodyChunks[i].vel.y < 4f
                    && GetTile(i, input[0].x, 0).Solid
                    && !GetTile(i, 0, 1).Solid
                    && !GetTile(i, input[0].x, 1).Solid
                    )
                    {
                        creature.bodyChunks[0].vel += new Vector2(0f, 2f);
                    }
                }
            }

            if (lizard.timeSpentTryingThisMove < 20) // don't panic
            {
                lizard.desperationSmoother = 0f;
            }
        }

        protected override void Moving(float magnitude)
        {
            lizard.AI.behavior = LizardAI.Behavior.Travelling;
            var howmuch = lizard.lizardParams.bodySizeFac * magnitude;

            lizard.AI.runSpeed = Custom.LerpAndTick(lizard.AI.runSpeed, howmuch, 0.2f, 0.05f);
            lizard.AI.excitement = Custom.LerpAndTick(lizard.AI.excitement, 0.8f, 0.1f, 0.05f);

            var tile0 = creature.room.GetTile(creature.bodyChunks[0].pos);

            // greater air friction because uhhh didnt feel right
            if (lizard.applyGravity) // && creature.room.aimap.TileAccessibleToCreature(tile0.X, tile0.Y, lizard.Template))
            {
                for (int i = 0; i < creature.bodyChunks.Length; i++)
                {
                    BodyChunk bodyChunk = lizard.bodyChunks[i];
                    if (Mathf.Abs(bodyChunk.vel.x) > runSpeed * 2f)
                    {
                        bodyChunk.vel.x *= 0.98f;
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
            if (!lizard.gripPoint.HasValue)
            {
                creature.room.PlaySound(SoundID.Lizard_Grab_Pole, creature.mainBodyChunk);
                lizard.gripPoint = creature.room.MiddleOfTile(tile0.X, tile0.Y);
                for (int i = 0; i < lizard.bodyChunks.Length; i++)
                {
                    lizard.bodyChunks[i].vel *= 0.4f;
                }
                lizard.inAllowedTerrainCounter = 0; // for proper grip logic
            }
        }

        protected override void MovementOverride(MovementConnection movementConnection)
        {
            lizard.commitedToDropConnection = movementConnection;
            //lizard.inAllowedTerrainCounter = 0;
        }

        protected override void ClearMovementOverride()
        {
            lizard.commitedToDropConnection = default(MovementConnection);
        }

        protected override void OnCall()
        {
            if (lizard.graphicsModule is LizardGraphics lg && !lg.culled)
            {
                lizard.bubble = Math.Max(this.lizard.bubble, 10);
                lizard.bubbleIntensity = Mathf.Max(0.4f, lizard.bubbleIntensity);
            }
        }

        protected override void PointImpl(Vector2 dir)
        {
            if (lizard.graphicsModule is LizardGraphics lg && !lg.culled)
            {
                lg.limbs[0].mode = Limb.Mode.HuntAbsolutePosition;
                lg.limbs[0].absoluteHuntPos = lizard.DangerPos + dir * 100f;
            }
        }
    }
}

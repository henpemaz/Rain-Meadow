using MonoMod.Cil;
using System.Linq;
using System;
using UnityEngine;
using RWCustom;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace RainMeadow
{
    internal class ScavengerController : GroundCreatureController
    {
        public static void EnableScavenger()
        {
            On.Scavenger.Update += Scavenger_Update;
            On.Scavenger.Act += Scavenger_Act;
            On.ScavengerAI.Update += ScavengerAI_Update;
            IL.Scavenger.Act += Scavenger_Act1;

            // color
            On.ScavengerGraphics.ctor += ScavengerGraphics_ctor;

            On.Scavenger.FollowPath += Scavenger_FollowPath;
        }

        private static MovementConnection Scavenger_FollowPath(On.Scavenger.orig_FollowPath orig, Scavenger self, WorldCoordinate origin, bool actuallyFollowingThisPath)
        {
            if (creatureControllers.TryGetValue(self.abstractCreature, out var c) && c is ScavengerController s)
            {
                if (origin == self.AI.pathFinder.destination) // return null; // such a silly behavior...
                    // actually scav NEEDS an upcoming connection wtf??
                    return new MovementConnection(MovementConnection.MovementType.Standard, origin, WorldCoordinate.AddIntVector(origin, new IntVector2(s.input[0].x, s.input[0].y)), 1);
            }

            return orig(self, origin, actuallyFollowingThisPath);
        }

        // fix "will only move if target > 3 tiles away" issue
        private static void Scavenger_Act1(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.GotoNext(MoveType.After,
                    i => i.MatchLdcI4(1),
                    i => i.MatchStfld<Scavenger>("moving")); // first assignment
                c.GotoNext(MoveType.After,
                    i => i.MatchLdcI4(0),
                    i => i.MatchStfld<Scavenger>("moving")); // occupytile assignment
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Scavenger self) => {
                    if (creatureControllers.TryGetValue(self.abstractCreature, out var s))
                    {
                        self.moving |= (s as ScavengerController).forceMoving;
                    }
                });


                // Act() line 605 this.connections.Count > 2
                c.Index = 0;
                ILLabel run = null;
                c.GotoNext(MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<Scavenger>("connections"),
                    i => i.MatchCallOrCallvirt(out _),
                    i => i.MatchLdcI4(2),
                    i => i.MatchBgt(out run)
                    );
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Scavenger self) => {
                    if (creatureControllers.TryGetValue(self.abstractCreature, out var s))
                    {
                        return self.connections.Count > 0 && (s as ScavengerController).forceMoving;
                    }
                    return false;
                });
                c.Emit(OpCodes.Brtrue, run);

            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw e;
            }
        }

        private static void ScavengerAI_Update(On.ScavengerAI.orig_Update orig, ScavengerAI self)
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

        public ScavengerController(Scavenger scav, OnlineCreature oc, int playerNumber) : base(scav, oc, playerNumber) { }

        private bool forceMoving;

        public Scavenger scavenger => creature as Scavenger;

        //private static MovementConnection nullConnection = new MovementConnection(MovementConnection.MovementType.Standard, new WorldCoordinate(-1, -1, -1, -1), new WorldCoordinate(-1, -1, -1, -1), 1);

        public override bool GrabImpl(PhysicalObject pickUpCandidate)
        {
            var chunk = pickUpCandidate.bodyChunks[0];

            // TODO

            return false;
        }

        private static void ScavengerGraphics_ctor(On.ScavengerGraphics.orig_ctor orig, ScavengerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            if (RainMeadow.creatureCustomizations.TryGetValue(ow as Creature, out var c))
            {
                var color = self.bellyColor.rgb;
                c.ModifyBodyColor(ref color);
                self.bellyColor = color.ToHSL();
                color = self.headColor.rgb;
                c.ModifyBodyColor(ref color);
                self.headColor = color.ToHSL();
                color = self.bellyColor.rgb;
                c.ModifyBodyColor(ref color);
                self.bellyColor = color.ToHSL();
                // blackcolors 

                color = self.eyeColor.rgb;
                c.ModifyEyeColor(ref color);
                self.eyeColor = color.ToHSL();
                color = self.decorationColor.rgb;
                c.ModifyEyeColor(ref color);
                self.decorationColor = color.ToHSL();
            }
        }

        protected override void JumpImpl()
        {
            //RainMeadow.Debug(onlineCreature);
            var cs = creature.bodyChunks;
            var mainBodyChunk = creature.mainBodyChunk;
            var tile = creature.room.aimap.getAItile(mainBodyChunk.pos);

            // todo fake movementconnection

            if (canGroundJump > 0 && superLaunchJump >= 20)
            {
                RainMeadow.Debug("scavenger super jump");
                superLaunchJump = 0;
                scavenger.animation = null;
                scavenger.movMode = Scavenger.MovementMode.Run;
                scavenger.footingCounter = 0;
                scavenger.swingPos = null;
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
                    RainMeadow.Debug("scavenger pole jump");
                    scavenger.animation = null;
                    scavenger.movMode = Scavenger.MovementMode.Run;
                    scavenger.footingCounter = 0;
                    scavenger.swingPos = null;
                    this.forceJump = 10;
                    flipDirection = this.input[0].x;
                    cs[0].vel.x = 6f * flipDirection;
                    cs[0].vel.y = 6f;
                    cs[1].vel.x = 4f * flipDirection;
                    cs[1].vel.y = 4.5f;
                    cs[2].vel.x = 4f * flipDirection;
                    cs[2].vel.y = 4.5f;
                    creature.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, mainBodyChunk, false, 1f, 1f);
                    return;
                }
                if (this.input[0].y <= 0)
                {
                    RainMeadow.Debug("scavenger pole drop");
                    scavenger.animation = null;
                    scavenger.movMode = Scavenger.MovementMode.Run;
                    scavenger.footingCounter = 0;
                    scavenger.swingPos = null;
                    mainBodyChunk.vel.y = 2f;
                    if (this.input[0].y > -1)
                    {
                        mainBodyChunk.vel.x = 2f * flipDirection;
                    }
                    creature.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, mainBodyChunk, false, 0.3f, 1f);
                    return;
                }// no climb boost
            }
            else if (canClimbJump > 0)
            {
                RainMeadow.Debug("scavenger climb jump");
                scavenger.animation = null;
                scavenger.movMode = Scavenger.MovementMode.Run;
                scavenger.footingCounter = 0;
                scavenger.swingPos = null;
                this.jumpBoost = 3f;
                var jumpdir = (cs[0].pos - cs[1].pos).normalized + inputDir;
                for (int i = 0; i < cs.Length; i++)
                {
                    BodyChunk chunk = cs[i];
                    chunk.vel += jumpdir;
                }
                creature.room.PlaySound(SoundID.Slugcat_Wall_Jump, mainBodyChunk, false, 1f, 1f);
            }
            else if (canGroundJump > 0)
            {
                RainMeadow.Debug("scavenger normal jump");
                scavenger.animation = null;
                scavenger.movMode = Scavenger.MovementMode.Run;
                scavenger.footingCounter = 0;
                scavenger.swingPos = null;
                this.jumpBoost = 8;
                cs[0].vel.y = 5f;
                cs[1].vel.y = 5f;
                cs[2].vel.y = 3f;

                creature.room.PlaySound(SoundID.Slugcat_Normal_Jump, mainBodyChunk, false, 1f, 1f);
            }
            else throw new InvalidProgrammerException("can't jump");
        }

        // might need a new hookpoint between movement planing and movement acting
        private static void Scavenger_Act(On.Scavenger.orig_Act orig, Scavenger self)
        {
            if (creatureControllers.TryGetValue(self.abstractCreature, out var c) && c is ScavengerController s)
            {
                s.ConsciousUpdate();

                if (Input.GetKey(KeyCode.L))
                {
                    RainMeadow.Debug($"current AI destination {s.creature.abstractCreature.abstractAI.destination}");
                    RainMeadow.Debug($"moving {s.forceMoving}");
                    RainMeadow.Debug($"movMode {self.movMode}");
                    RainMeadow.Debug($"commitedToMove {self.commitedToMove}");
                    RainMeadow.Debug($"animation {self.animation}");
                    RainMeadow.Debug($"commitedMoveFollowChunk {self.commitedMoveFollowChunk}");
                    RainMeadow.Debug($"drop {self.drop}");
                    RainMeadow.Debug($"ghostCounter {self.ghostCounter}");
                    RainMeadow.Debug($"occupyTile {self.occupyTile}");
                    RainMeadow.Debug($"pathingWithExits {self.pathingWithExits}");
                    RainMeadow.Debug($"pathWithExitsCounter {self.pathWithExitsCounter}");
                    RainMeadow.Debug($"swingPos {self.swingPos}");
                    RainMeadow.Debug($"swingProgress {self.swingProgress}");
                    RainMeadow.Debug($"commitToMoveCounter {self.commitToMoveCounter}");
                    RainMeadow.Debug($"footingCounter {self.footingCounter}");
                    RainMeadow.Debug($"moveModeChangeCounter {self.moveModeChangeCounter}");
                    RainMeadow.Debug($"notFollowingPathToCurrentGoalCounter {self.notFollowingPathToCurrentGoalCounter}");
                    RainMeadow.Debug($"pathWithExitsCounter {self.pathWithExitsCounter}");
                    RainMeadow.Debug($"stuckCounter {self.stuckCounter}");
                    RainMeadow.Debug($"stuckOnShortcutCounter {self.stuckOnShortcutCounter}");
                    RainMeadow.Debug($"swingClimbCounter {self.swingClimbCounter}");
                }
            }
            orig(self);
        }

        public override bool CanClimbJump => !CanGroundJump && scavenger.swingPos != null && scavenger.swingClimbCounter >= 2;

        public override bool CanPoleJump => !CanGroundJump && ((creature.room.aimap.getAItile(creature.bodyChunks[1].pos).acc == AItile.Accessibility.Climb && creature.room.GetTile(creature.bodyChunks[1].pos).AnyBeam) || (creature.room.aimap.getAItile(creature.bodyChunks[2].pos).acc == AItile.Accessibility.Climb && creature.room.GetTile(creature.bodyChunks[2].pos).AnyBeam));

        public override bool CanGroundJump => (scavenger.movMode == Scavenger.MovementMode.Run || scavenger.movMode == Scavenger.MovementMode.StandStill) && (creature.bodyChunks[1].contactPoint.y == -1 || creature.bodyChunks[2].contactPoint.y == -1 || creature.IsTileSolid(1, 0, -1) || creature.IsTileSolid(2, 0, -1));

        public override bool HasFooting => CanGroundJump;

        public override WorldCoordinate CurrentPathfindingPosition
        {
            get
            {
                if (scavenger.occupyTile != new IntVector2(-1, -1))
                {
                    return creature.room.GetWorldCoordinate(scavenger.occupyTile);
                }
                return base.CurrentPathfindingPosition;
            }
        }

        protected override int GetFlip()
        {
            int newFlip = (int) Mathf.Sign(scavenger.flip);
            if (newFlip != 0) return newFlip;
            return flipDirection;
        }

        protected override void Moving(float magnitude)
        {
            scavenger.AI.behavior = ScavengerAI.Behavior.Travel;
            scavenger.AI.runSpeedGoal = Custom.LerpAndTick(scavenger.AI.runSpeedGoal, 0.8f * Mathf.Pow(magnitude, 2f), 0.2f, 0.05f);
            forceMoving = true;
            scavenger.stuckCounter = 5;
        }

        protected override void Resting()
        {
            scavenger.AI.behavior = ScavengerAI.Behavior.Idle;
            scavenger.AI.runSpeedGoal = Custom.LerpAndTick(scavenger.AI.runSpeedGoal, 0.0f, 0.4f, 0.1f);
            scavenger.commitToMoveCounter = 0;
            forceMoving = false;
            //scavenger.movMode = Scavenger.MovementMode.StandStill;
            scavenger.stuckCounter = 5;

        }
        protected override void MovementOverride(MovementConnection movementConnection)
        {
            scavenger.commitedToMove = movementConnection;
            scavenger.commitToMoveCounter = 20;
            scavenger.commitedMoveFollowChunk = 1;
            //scavenger.drop = true;
            forceMoving = true;
        }

        protected override void ClearMovementOverride()
        {
            scavenger.commitToMoveCounter = 0;
        }

        protected override void GripPole(Room.Tile tile0)
        {
            if(scavenger.swingPos == null && scavenger.nextSwingPos == null && creature.mainBodyChunk.vel.y < 0)
            {
                scavenger.swingPos = creature.room.MiddleOfTile(tile0.X, tile0.Y);
                scavenger.swingRadius = 10f;
                scavenger.swingClimbCounter = 10;
                scavenger.movMode = Scavenger.MovementMode.Climb;
                scavenger.drop = false;
                creature.mainBodyChunk.vel.y = 0f;
            }
        }

        private static void Scavenger_Update(On.Scavenger.orig_Update orig, Scavenger self, bool eu)
        {
            if (creatureControllers.TryGetValue(self.abstractCreature, out var s))
            {
                s.Update(eu);
            }

            orig(self, eu);
        }

        protected override void LookImpl(Vector2 pos)
        {
            scavenger.lookPoint = pos;
        }
    }
}
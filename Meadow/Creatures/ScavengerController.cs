using MonoMod.Cil;
using System;
using UnityEngine;
using RWCustom;
using Mono.Cecil.Cil;

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
            if (creatureControllers.TryGetValue(self, out var c) && c is ScavengerController s)
            {
                if (origin == self.AI.pathFinder.destination)// such a silly behavior...
                    // actually scav NEEDS an upcoming connection wtf??
                    //return new MovementConnection(MovementConnection.MovementType.Standard, origin, WorldCoordinate.AddIntVector(origin, new IntVector2(s.input[0].x, s.input[0].y)), 1);
                return new MovementConnection(MovementConnection.MovementType.Standard, origin, origin, 1);
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
                    if (creatureControllers.TryGetValue(self, out var s))
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
                    if (creatureControllers.TryGetValue(self, out var s))
                    {
                        return self.connections.Count > 0 && (s as ScavengerController).forceMoving;
                    }
                    return false;
                });
                c.Emit(OpCodes.Brtrue, run);

                // needs something to lean forward on low conn count
                // basically if I give it a "long" path it walks like a scav walks, on short paths it leans back too much
                int localVec = 0;
                c.GotoNext(MoveType.Before,
                    i => i.MatchCall("SharedPhysics","ExactTerrainRayTracePos"),
                    i => i.MatchStloc(out _),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdloc(out _),
                    i => i.MatchCall<Scavenger>("KnucklePosLegal")
                    );
                c.GotoPrev(MoveType.Before, i => i.MatchStloc(out _));
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Vector2 aimFor, Scavenger self) =>
                {
                    if (creatureControllers.TryGetValue(self, out var c)) // short-pathing-friendly knuckle pos
                    {
                        aimFor -= Custom.DirVec(self.mainBodyChunk.pos, aimFor) * 40f;
                        aimFor += new Vector2(self.flip * 40f, 10f);
                        aimFor += Custom.DirVec(self.mainBodyChunk.pos, aimFor) * 40f;
                    }
                    return aimFor;
                });
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw e;
            }
        }

        private static void ScavengerAI_Update(On.ScavengerAI.orig_Update orig, ScavengerAI self)
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

        public ScavengerController(Scavenger scav, OnlineCreature oc, int playerNumber) : base(scav, oc, playerNumber) 
        {
            scavenger = scav;
            scavenger.abstractCreature.personality.energy = 1f; // no being lazy

            jumpFactor = 1.6f;
        }

        private bool forceMoving;

        public Scavenger scavenger;
        private int forceNoFooting;

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

        protected override void OnJump()
        {
            scavenger.animation = null;
            scavenger.movMode = Scavenger.MovementMode.Run;
            scavenger.footingCounter = 0;
            scavenger.swingPos = null;
            forceNoFooting = (canGroundJump > 0 && superLaunchJump >= 20) ? 10 : 5;
            scavenger.movMode = Scavenger.MovementMode.Run;
        }

        private static void Scavenger_Act(On.Scavenger.orig_Act orig, Scavenger self)
        {
            if (creatureControllers.TryGetValue(self, out var c) && c is ScavengerController s)
            {
                s.ConsciousUpdate();
            }
            orig(self);
        }

        internal override void ConsciousUpdate()
        {
            base.ConsciousUpdate();

            // hold my beer joar
            // patchup non-functional code that detects overshooting the move
            if (scavenger.commitToMoveCounter > 0)
            {
                scavenger.commitToMoveCounter--;
                if (!scavenger.drop)
                {
                    bool continueCommit = scavenger.commitToMoveCounter > 0 && scavenger.room.GetTilePosition(scavenger.bodyChunks[scavenger.commitedMoveFollowChunk].pos) == scavenger.commitedToMove.DestTile; // != > ==
                    int num3 = 0;
                    while (num3 < scavenger.connections.Count && continueCommit) // ! > v
                    {
                        if (scavenger.room.GetTilePosition(scavenger.bodyChunks[scavenger.commitedMoveFollowChunk].pos) == scavenger.connections[num3].DestTile) // != > ==
                        {
                            continueCommit = false;
                        }
                        num3++;
                    }
                    if (continueCommit)
                    {
                        // no op
                    }
                    else
                    {
                        scavenger.commitToMoveCounter = -5;
                    }
                }
            }

            if(forceNoFooting > 0)
            {
                forceNoFooting--;
                scavenger.footingCounter = 0;
            }

            if (Input.GetKey(KeyCode.L))
            {
                RainMeadow.Debug($"current AI destination {scavenger.AI.pathFinder.destination}");
                RainMeadow.Debug($"moving {scavenger.moving}");
                RainMeadow.Debug($"movMode {scavenger.movMode}");
                RainMeadow.Debug($"moveModeChangeCounter {scavenger.moveModeChangeCounter}");
                RainMeadow.Debug($"animation {scavenger.animation}");
                RainMeadow.Debug($"drop {scavenger.drop}");
                RainMeadow.Debug($"commitedToMove {scavenger.commitedToMove}");
                RainMeadow.Debug($"commitedMoveFollowChunk {scavenger.commitedMoveFollowChunk}");
                RainMeadow.Debug($"commitToMoveCounter {scavenger.commitToMoveCounter}");
                RainMeadow.Debug($"ghostCounter {scavenger.ghostCounter}");
                RainMeadow.Debug($"occupyTile {scavenger.occupyTile}");
                RainMeadow.Debug($"pathingWithExits {scavenger.pathingWithExits}");
                RainMeadow.Debug($"pathWithExitsCounter {scavenger.pathWithExitsCounter}");
                RainMeadow.Debug($"notFollowingPathToCurrentGoalCounter {scavenger.notFollowingPathToCurrentGoalCounter}");
                RainMeadow.Debug($"swingPos {scavenger.swingPos}");
                RainMeadow.Debug($"swingProgress {scavenger.swingProgress}");
                RainMeadow.Debug($"swingClimbCounter {scavenger.swingClimbCounter}");
                RainMeadow.Debug($"footingCounter {scavenger.footingCounter}");
                RainMeadow.Debug($"stuckCounter {scavenger.stuckCounter}");
                RainMeadow.Debug($"stuckOnShortcutCounter {scavenger.stuckOnShortcutCounter}");
            }
        }

        public override bool HasFooting
        {
            get
            {
                if (forceNoFooting > 0)
                {
                    return false;
                }
                if (scavenger.occupyTile != new IntVector2(-1, -1))
                {
                    return true;
                }
                return false;
            }
        }

        public override bool OnGround => scavenger.occupyTile != new IntVector2(-1, -1) && creature.room.aimap.getAItile(scavenger.occupyTile).acc == AItile.Accessibility.Floor;

        public override bool OnCorridor => scavenger.movMode == Scavenger.MovementMode.Crawl;

        public override bool OnPole
        {
            get
            {
                if (scavenger.movMode == Scavenger.MovementMode.Climb) return true;
                if (scavenger.swingPos != null) return true;
                if (scavenger.occupyTile != new IntVector2(-1,-1) && creature.room.GetTile(scavenger.occupyTile).AnyBeam && creature.room.aimap.getAItile(scavenger.occupyTile).acc == AItile.Accessibility.Climb) return true;
                return false;
            }
        }

        public override WorldCoordinate CurrentPathfindingPosition
        {
            get
            {
                if (scavenger.swingPos != null)
                {
                    return creature.room.GetWorldCoordinate(scavenger.swingPos.Value);
                }
                if (scavenger.occupyTile != new IntVector2(-1, -1))
                {
                    return creature.room.GetWorldCoordinate(scavenger.occupyTile);
                }
                return base.CurrentPathfindingPosition;
            }
        }

        internal override bool FindDestination(WorldCoordinate basecoord, out WorldCoordinate toPos, out float magnitude)
        {
            var succ = base.FindDestination(basecoord, out toPos, out magnitude); // uses ocuppypos
            if (succ) return true;
            return base.FindDestination(creature.room.GetWorldCoordinate(creature.bodyChunks[2].pos), out toPos, out magnitude); // uses head
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
            var speed = HasFooting ? 1.0f : 0.4f;
            scavenger.AI.runSpeedGoal = Custom.LerpAndTick(scavenger.AI.runSpeedGoal, speed * Mathf.Pow(magnitude, 2f), 0.2f, 0.05f);
            if(scavenger.movMode == Scavenger.MovementMode.Climb && input[0].y > 0)
            {
                scavenger.stuckCounter = Mathf.Max(12, scavenger.stuckCounter);
            }
            forceMoving = true;
        }

        protected override void Resting()
        {
            scavenger.AI.behavior = ScavengerAI.Behavior.Idle;
            scavenger.AI.runSpeedGoal = Custom.LerpAndTick(scavenger.AI.runSpeedGoal, 0.0f, 0.4f, 0.1f);
            scavenger.commitToMoveCounter = 0;
            scavenger.stuckCounter = 0;
            forceMoving = false;
        }

        protected override void MovementOverride(MovementConnection movementConnection)
        {
            //if (!HasFooting) return;
            scavenger.commitedToMove = movementConnection;
            scavenger.commitToMoveCounter = 10;
            scavenger.commitedMoveFollowChunk = 1;
            scavenger.drop = true;
            forceMoving = true;
            scavenger.stuckCounter = Mathf.Min(20, scavenger.stuckCounter);
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
            if (creatureControllers.TryGetValue(self, out var s))
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
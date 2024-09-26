using MonoMod.Cil;
using System;
using UnityEngine;
using RWCustom;
using Mono.Cecil.Cil;
using System.Linq;

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
                var pathFinder = self.AI.pathFinder;
                if (origin == pathFinder.destination || (actuallyFollowingThisPath && pathFinder.lookingForImpossiblePath) || self.movMode == Scavenger.MovementMode.Swim)
                {
                    if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("returning override");
                    return new MovementConnection(MovementConnection.MovementType.Standard, origin, pathFinder.destination, 1);
                }
            }

            return orig(self, origin, actuallyFollowingThisPath);
        }

        
        private static void Scavenger_Act1(ILContext il)
        {
            try
            {
                var locMov = il.Body.Variables.First(v => v.VariableType.FullName == typeof(MovementConnection).FullName);
                var locNum2 = il.Body.Variables.First(v => v.VariableType.FullName == typeof(int).FullName);

                // fix "will only move if target > 3 tiles away" issue
                var c = new ILCursor(il);
                c.GotoNext(MoveType.After,
                    i => i.MatchLdcI4(1),
                    i => i.MatchStfld<Scavenger>("moving")); // first assignment
                c.GotoNext(MoveType.After,
                    i => i.MatchLdcI4(0),
                    i => i.MatchStfld<Scavenger>("moving")); // occupytile assignment
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloca, locMov);
                c.Emit(OpCodes.Ldloca, locNum2);

                c.EmitDelegate((Scavenger self, ref MovementConnection movementConnection, ref int num2) => {
                    if (creatureControllers.TryGetValue(self, out var s))
                    {
                        if ((s as ScavengerController).forceMoving)
                        {
                            self.moving = true;
                            if (self.Submersion >= 1f) // new
                            {
                                num2 = 0;
                                self.occupyTile = self.room.GetTilePosition(self.mainBodyChunk.pos);
                                movementConnection = self.FollowPath(self.room.GetWorldCoordinate(self.occupyTile), true);
                            }
                        }
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

                // swim handling
                // happens in 2 spots smh
                // line 515 -> if movementconnection defined
                // else if (this.movMode == Scavenger.MovementMode.Swim)
                //      if (this.moving)
                //      into (moving && !meadowSpecialSwimCode())
                ILLabel skip = null;
                c.GotoPrev(MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<Scavenger>("movMode"),
                    i => i.MatchLdsfld<Scavenger.MovementMode>("Swim"),
                    i => i.MatchCall("ExtEnum`1<Scavenger/MovementMode>", "op_Equality"),
                    i => i.MatchBrfalse(out skip),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<Scavenger>("moving"),
                    i => i.MatchBrfalse(out _)
                    );
                // should follow the BR at the end of the block to skip to the end of the ifelse chain
                int insertat = c.Index - 3;
                c.GotoLabel(skip); // the else
                c.GotoPrev(i => i.MatchBr(out skip)); // the br to end of ifelsechain

                c.Index = insertat;
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc_2);

                c.EmitDelegate((Scavenger self, MovementConnection movementConnection) =>
                {
                    if (creatureControllers.TryGetValue(self, out var c)) // controlled swim
                    {
                        //if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("swim A");
                        if (self.moving)
                        {
                            Vector2 vector4 = self.room.MiddleOfTile(movementConnection.destinationCoord);
                            if (Mathf.Abs(self.mainBodyChunk.vel.x) > 3f)
                            {
                                self.mainBodyChunk.vel.x *= 0.8f;
                            }

                            self.mainBodyChunk.vel += Custom.DirVec(self.mainBodyChunk.pos, vector4) * 0.5f;

                            if (Mathf.Abs(self.mainBodyChunk.pos.x - vector4.x) < 5f)
                            {
                                self.flip *= 0.9f;
                            }
                            else if (self.mainBodyChunk.pos.x < vector4.x)
                            {
                                self.flip = Mathf.Min(self.flip + 0.07f, 1f);
                            }
                            else
                            {
                                self.flip = Mathf.Max(self.flip - 0.07f, -1f);
                            }
                            self.bodyChunks[2].vel *= 0.9f;
                            self.bodyChunks[2].vel += Vector2.Lerp(Custom.DirVec(self.mainBodyChunk.pos, self.room.MiddleOfTile(self.connections[self.connections.Count - 1].destinationCoord)), new Vector2(0f, 1f), self.bodyChunks[2].submersion) * 2f;
                        }
                        else
                        {
                            if (self.AllowIdleMoves)
                            {
                                self.mainBodyChunk.vel += Vector2.ClampMagnitude(self.room.MiddleOfTile(self.AI.pathFinder.GetDestination) - self.mainBodyChunk.pos, 10f) / 60f;
                            }
                            self.flip = Mathf.Lerp(self.flip, Mathf.Clamp(self.HeadLookDir.x * 1.5f, -1f, 1f), 0.1f);
                        }
                        return false;
                    }
                    return true;
                });
                c.Emit(OpCodes.Brfalse, skip);

                // line 670 generic swim movement
                c.GotoNext(MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<Scavenger>("movMode"),
                    i => i.MatchLdsfld<Scavenger.MovementMode>("Swim"),
                    i => i.MatchCall("ExtEnum`1<Scavenger/MovementMode>", "op_Equality"),
                    i => i.MatchBrfalse(out skip)
                    );

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Scavenger self) =>
                {
                    if (creatureControllers.TryGetValue(self, out var c)) // controlled swim
                    {
                        //if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("swim B");
                        if (self.commitToMoveCounter > 0)
                        {
                            //if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("swim B forced");

                            // new
                            MovementConnection movementConnection = self.commitedToMove;
                            Vector2 vector4 = self.room.MiddleOfTile(movementConnection.destinationCoord) + 10f * c.inputDir;
                            if (Mathf.Abs(self.mainBodyChunk.vel.x) > 4f)
                            {
                                self.mainBodyChunk.vel.x *= 0.8f;
                            }

                            self.mainBodyChunk.vel += Custom.DirVec(self.mainBodyChunk.pos, vector4) * 0.50f;

                            if (Mathf.Abs(self.mainBodyChunk.pos.x - vector4.x) < 5f)
                            {
                                self.flip *= 0.9f;
                            }
                            else if (self.mainBodyChunk.pos.x < vector4.x)
                            {
                                self.flip = Mathf.Min(self.flip + 0.07f, 1f);
                            }
                            else
                            {
                                self.flip = Mathf.Max(self.flip - 0.07f, -1f);
                            }
                            self.bodyChunks[2].vel += Custom.DirVec(self.bodyChunks[2].pos, vector4) * 0.25f;
                            //self.bodyChunks[2].vel *= 0.95f;
                            //self.bodyChunks[2].vel += Vector2.Lerp(Custom.DirVec(self.mainBodyChunk.pos, self.room.MiddleOfTile(self.connections[self.connections.Count - 1].destinationCoord)), new Vector2(0f, 1f), self.bodyChunks[2].submersion) * 2f;
                        }

                        // old but modified
                        //float num15 = Mathf.Lerp(self.room.FloatWaterLevel(self.mainBodyChunk.pos.x), self.room.waterObject.fWaterLevel, 0.5f);
                        //self.bodyChunks[1].vel.y = self.bodyChunks[1].vel.y - self.gravity * (1f - self.bodyChunks[2].submersion);
                        self.bodyChunks[2].vel *= Mathf.Lerp(1f, 0.9f, self.mainBodyChunk.submersion);
                        self.mainBodyChunk.vel.y *= Mathf.Lerp(1f, 0.95f, self.mainBodyChunk.submersion);
                        self.bodyChunks[1].vel.y *= Mathf.Lerp(1f, 0.95f, self.bodyChunks[1].submersion);
                        //BodyChunk mainBodyChunk5 = self.mainBodyChunk;
                        //mainBodyChunk5.vel.y = mainBodyChunk5.vel.y + Mathf.Clamp((num15 - self.mainBodyChunk.pos.y) / 14f, -0.5f, 0.5f);
                        //BodyChunk bodyChunk10 = self.bodyChunks[1];
                        //bodyChunk10.vel.y = bodyChunk10.vel.y + Mathf.Clamp((num15 - self.bodyChunkConnections[0].distance - self.bodyChunks[1].pos.y) / 14f, -0.5f, 0.5f);

                        return false;
                    }
                    return true;
                });
                c.Emit(OpCodes.Brfalse, skip);
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

        public ScavengerController(Scavenger scav, OnlineCreature oc, int playerNumber, MeadowAvatarCustomization customization) : base(scav, oc, playerNumber, customization) 
        {
            scavenger = scav;

            // this actually changes their colors lmao
            //scavenger.abstractCreature.personality.energy = 1f; // no being lazy

            jumpFactor = 1.15f;
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
            scavenger.moveModeChangeCounter = -5;
            scavenger.footingCounter = -5;
            scavenger.swingPos = null;
            scavenger.swingingForbidden = 5;
            forceNoFooting = 5;
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
                scavenger.swingPos = null;
                scavenger.nextSwingPos = null;
                scavenger.footingCounter = 0;
                scavenger.drop = true;
                scavenger.commitToMoveCounter = forceNoFooting;
            }

            if (scavenger.movMode == Scavenger.MovementMode.Climb && input[0].y > 0)
            {
                scavenger.swingingForbidden = Mathf.Max(5, scavenger.swingingForbidden);
                scavenger.stuckCounter = Mathf.Max(5, scavenger.stuckCounter);
            }
            // haha monke
            if (scavenger.movMode == Scavenger.MovementMode.Climb && scavenger.swingPos != null && Vector2.Dot(scavenger.mainBodyChunk.vel, inputDir.normalized) < runSpeed)
            {
                scavenger.mainBodyChunk.vel += inputDir * 0.3f;
            }

            if (Input.GetKey(KeyCode.L))
            {
                RainMeadow.Dump(scavenger);
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

        public override bool IsOnGround => scavenger.occupyTile != new IntVector2(-1, -1) && creature.room.aimap.getAItile(scavenger.occupyTile).acc == AItile.Accessibility.Floor;

        public override bool IsOnCorridor => scavenger.movMode == Scavenger.MovementMode.Crawl;

        public override bool IsOnPole
        {
            get
            {
                if (scavenger.movMode == Scavenger.MovementMode.Climb) return true;
                if (scavenger.swingPos != null) return true;
                if (scavenger.occupyTile != new IntVector2(-1,-1) && creature.room.GetTile(scavenger.occupyTile).AnyBeam && creature.room.aimap.getAItile(scavenger.occupyTile).acc == AItile.Accessibility.Climb) return true;
                return false;
            }
        }

        public override bool IsOnClimb => false;

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

                return creature.room.GetWorldCoordinate(creature.bodyChunks[0].pos);
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
            scavenger.connections.Clear();
            scavenger.connections.Add(movementConnection);
            scavenger.drop = false; //input[0].y < 0;
            forceMoving = true;
            scavenger.stuckCounter = Mathf.Min(20, scavenger.stuckCounter);
        }

        protected override void ClearMovementOverride()
        {
            scavenger.commitToMoveCounter = 0;
        }

        protected override void GripPole(Room.Tile tile0)
        {
            if(scavenger.swingPos == null && scavenger.movMode != Scavenger.MovementMode.Climb && forceNoFooting < 1)
            {
                scavenger.swingPos = creature.room.MiddleOfTile(tile0.X, tile0.Y);
                scavenger.swingRadius = (scavenger.mainBodyChunk.pos - scavenger.swingPos.Value).magnitude;
                scavenger.swingClimbCounter = 15;
                scavenger.occupyTile = new IntVector2(tile0.X, tile0.Y);
                scavenger.movMode = Scavenger.MovementMode.Climb;
                scavenger.drop = false;
                creature.mainBodyChunk.vel.y *= 0.3f;
                creature.mainBodyChunk.vel.x *= 0.3f;
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

        protected override void OnCall()
        {
            if (scavenger.graphicsModule is ScavengerGraphics sg)
            {
                sg.ShockReaction(0.4f);
            }
        }
    }
}
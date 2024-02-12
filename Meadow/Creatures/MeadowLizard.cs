using UnityEngine;
using System;
using RWCustom;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace RainMeadow
{
    class LizardController : CreatureController
    {
        private int canJump;
        private float jumpBoost;

        public LizardController(Lizard creature, OnlineCreature oc, int playerNumber) : base(creature, oc, playerNumber) { }

        public Lizard lizard => creature as Lizard;

        public override bool GrabImpl(PhysicalObject pickUpCandidate)
        {
            var chunk = pickUpCandidate.bodyChunks[0];
            lizard.biteControlReset = false;
            lizard.JawOpen = 0f;
            lizard.lastJawOpen = 0f;

            chunk.vel += creature.mainBodyChunk.vel * Mathf.Lerp(creature.mainBodyChunk.mass, 1.1f, 0.5f) / Mathf.Max(1f, chunk.mass);
            if(creature.Grab(chunk.owner, 0, chunk.index, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, lizard.lizardParams.biteDominance * UnityEngine.Random.value, overrideEquallyDominant: true, pacifying: true))
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
            On.LizardPather.FollowPath += LizardPather_FollowPath;

            On.Lizard.GripPointBehavior += Lizard_GripPointBehavior;
            On.Lizard.SwimBehavior += Lizard_SwimBehavior;
            On.Lizard.FollowConnection += Lizard_FollowConnection;

            IL.Lizard.CarryObject += Lizard_CarryObject1;

            // color
            On.LizardGraphics.ctor += LizardGraphics_ctor;

            On.LizardGraphics.Update += LizardGraphics_Update;
        }

        private static void LizardGraphics_Update(On.LizardGraphics.orig_Update orig, LizardGraphics self)
        {
            orig(self);
            if (creatureControllers.TryGetValue(self.lizard.abstractCreature, out var c) && c is LizardController l)
            {
                if(l.superLaunchJump > 0)
                {
                    self.drawPositions[0, 0].y -= 3;
                    self.drawPositions[1, 0].y += 5;
                    self.drawPositions[2, 0].y += 3;
                    self.tail[0].vel.x -= l.flipDirection;
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
                self.lizard.effectColor = col;
            }
        }

        private static void Lizard_FollowConnection(On.Lizard.orig_FollowConnection orig, Lizard self, float runSpeed)
        {
            if (Input.GetKey(KeyCode.L)) RainMeadow.Debug(self.followingConnection);
            orig(self, runSpeed);
        }

        private static void Lizard_SwimBehavior(On.Lizard.orig_SwimBehavior orig, Lizard self)
        {
            if (Input.GetKey(KeyCode.L)) RainMeadow.DebugMe();
            orig(self);
        }

        private static void Lizard_GripPointBehavior(On.Lizard.orig_GripPointBehavior orig, Lizard self)
        {
            if (Input.GetKey(KeyCode.L)) RainMeadow.DebugMe();
            orig(self);
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

        private void Jump()
        {
            RainMeadow.Debug(onlineCreature);
            var cs = creature.bodyChunks;
            if (superLaunchJump >= 20)
            {
                RainMeadow.Debug("jumped");
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
                creature.room.PlaySound(SoundID.Slugcat_Super_Jump, creature.mainBodyChunk, false, 1f, 1f);
            }
            else
            {
                RainMeadow.Debug("failed");
            }
        }

        // might need a new hookpoint between movement planing and movement acting
        private static void Lizard_Act(On.Lizard.orig_Act orig, Lizard self)
        {
            if (creatureControllers.TryGetValue(self.abstractCreature, out var c) && c is LizardController l)
            {
                l.ConsciousUpdate();

                // todo JUMP
                var room = self.room;
                var chunks = self.bodyChunks;
                var nc = chunks.Length;

                // pounce facilitator
                //if (l.input[0].x == 0 && l.input[0].y == 0 && l.input[0].jmp)
                //{
                //    for (int i = 0; i < nc; i++)
                //    {
                //        if (self.IsTileSolid(i, 0, -1))
                //        {
                //            BodyChunk bodyChunk = self.bodyChunks[i];
                //            bodyChunk.vel.y -= 0.2f;
                //        }
                //    }
                //}

                // pounce
                // maybe make this "canPounce" virtual
                //chunks[1].ContactPoint.y < 0 && chunks[2].ContactPoint.y < 0

                
                RainMeadow.Trace($"legs? {self.LegsGripping}");
                if (self.LegsGripping == 4 && self.IsTileSolid(1, 0, -1) && l.input[0].x == 0 && l.input[0].y <= 0)
                {
                    if (l.input[0].jmp)
                    {
                        l.wantToJump = 0;
                        if (l.superLaunchJump < 20)
                        {
                            l.superLaunchJump++;
                        }
                    }
                    if (!l.input[0].jmp && l.input[1].jmp)
                    {
                        l.wantToJump = 1;
                        l.canJump = 1;
                    }
                }
                else if (l.superLaunchJump > 0)
                {
                    l.superLaunchJump--;
                }

                if (self.LegsGripping >= 2)
                {
                    RainMeadow.Trace("can jump");
                    l.canJump = 5;
                }

                if (l.canJump > 0 && l.wantToJump > 0)
                {
                    l.canJump = 0;
                    l.wantToJump = 0;
                    l.Jump();
                }

                if (l.jumpBoost > 0f && (l.input[0].jmp || l.forceBoost > 0) && self.bodyChunks[0].ContactPoint.y != -1 && self.bodyChunks[1].ContactPoint.y != -1)
                {
                    RainMeadow.Trace("jump boost");
                    l.jumpBoost -= 1.5f;
                    BodyChunk bodyChunk = self.bodyChunks[0];
                    bodyChunk.vel.y = bodyChunk.vel.y + (l.jumpBoost + 1f) * 0.3f;
                    BodyChunk bodyChunk2 = self.bodyChunks[1];
                    bodyChunk2.vel.y = bodyChunk2.vel.y + (l.jumpBoost + 1f) * 0.3f;
                    BodyChunk bodyChunk3 = self.bodyChunks[2];
                    bodyChunk3.vel.y = bodyChunk3.vel.y + (l.jumpBoost + 1f) * 0.3f;
                }
                else
                {
                    l.jumpBoost = 0f;
                }

                l.flipDirection = (chunks[0].pos.x > chunks[1].pos.x) ? 1 : -1;

                // move
                //var basepos = 0.5f * (self.bodyChunks[0].pos + self.bodyChunks[1].pos);
                var basepos = self.bodyChunks[0].pos;
                var basecoord = self.room.GetWorldCoordinate(basepos);
                if (l.inputDir != Vector2.zero)
                {
                    self.AI.behavior = LizardAI.Behavior.Travelling;
                    self.AI.runSpeed = Custom.LerpAndTick(self.AI.runSpeed, 0.8f, 0.2f, 0.05f);
                    self.AI.excitement = Custom.LerpAndTick(self.AI.excitement, 0.5f, 0.1f, 0.05f);

                    var toPos = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(l.inputDir * 1.42f));

                    if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"moving towards {toPos.Tile}");
                    if (l.inputDir.x != 0) // to sides
                    {
                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("sides");
                        var solidAhead = room.GetTile(toPos).Solid; // ahead blocked
                        if (solidAhead && room.aimap.TileAccessibleToCreature(toPos.Tile + new IntVector2(0, 1), self.Template)) // try up
                        {
                            if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("up");
                            toPos = WorldCoordinate.AddIntVector(toPos, new IntVector2(0, 1));
                        }
                        else if (!solidAhead && !room.aimap.TileAccessibleToCreature(toPos.Tile, self.Template) && room.aimap.TileAccessibleToCreature(toPos.Tile + new IntVector2(0, -1), self.Template)) // try down
                        {
                            if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("down");
                            toPos = WorldCoordinate.AddIntVector(toPos, new IntVector2(0, -1));
                        }
                        var furtherOut = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(l.inputDir * 2.84f));
                        if (room.aimap.TileAccessibleToCreature(furtherOut.Tile, self.Template) && QuickConnectivity.Check(room, self.Template, basecoord.Tile, furtherOut.Tile, 10) > 0)
                        {
                            if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("reaching");
                            toPos = furtherOut;
                        }
                        else if (room.aimap.TileAccessibleToCreature(furtherOut.Tile + new IntVector2(0, 1), self.Template) && QuickConnectivity.Check(room, self.Template, basecoord.Tile, furtherOut.Tile + new IntVector2(0, 1), 10) > 0)
                        {
                            if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("up");
                            toPos = WorldCoordinate.AddIntVector(furtherOut, new IntVector2(0, 1));
                        }
                        else if (room.aimap.TileAccessibleToCreature(furtherOut.Tile + new IntVector2(0, -1), self.Template) && QuickConnectivity.Check(room, self.Template, basecoord.Tile, furtherOut.Tile + new IntVector2(0, -1), 10) > 0)
                        {
                            if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("down");
                            toPos = WorldCoordinate.AddIntVector(furtherOut, new IntVector2(0, -1));
                        }
                        else if (!room.aimap.TileAccessibleToCreature(toPos.Tile, self.Template)) // already not accessible, improve
                        {
                            if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("blind reaching");
                            toPos = furtherOut;
                        }
                    }
                    else
                    {
                        if (!room.GetTile(toPos).Solid) // ahead unblocked
                        {
                            toPos = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(l.inputDir * 2.2f));
                        }
                    }

                    // minimun speed
                    if ((self.graphicsModule as LizardGraphics)?.frontLegsGrabbing > 0)
                    {
                        var inDirection = Vector2.Dot(self.mainBodyChunk.vel, l.inputDir);
                        if (inDirection < 1)
                        {
                            self.mainBodyChunk.vel += l.inputDir * (1 - inDirection);
                        }
                    }

                    if (toPos != self.abstractCreature.abstractAI.destination)
                    {
                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"new destination {toPos.Tile}");
                        l.ForceAIDestination(toPos);
                    }
                }
                else
                {
                    self.AI.behavior = LizardAI.Behavior.Idle;
                    self.AI.runSpeed = Custom.LerpAndTick(self.AI.runSpeed, 0, 0.4f, 0.1f);
                    self.AI.excitement = Custom.LerpAndTick(self.AI.excitement, 0.2f, 0.1f, 0.05f);

                    for (int i = 0; i < nc; i++)
                    {
                        if (self.IsTileSolid(i, 0, -1))
                        {
                            BodyChunk bodyChunk = self.bodyChunks[i];
                            bodyChunk.vel.y -= 0.1f;
                        }
                    }
                    if (l.inputDir == Vector2.zero && l.inputLastDir != Vector2.zero) // let go
                    {
                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"resting at {basecoord.Tile}");
                        l.ForceAIDestination(basecoord);
                    }
                }
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"current AI destination {l.creature.abstractCreature.abstractAI.destination}");
            }
            orig(self);
        }

        private static MovementConnection LizardPather_FollowPath(On.LizardPather.orig_FollowPath orig, LizardPather self, WorldCoordinate originPos, int? bodyDirection, bool actuallyFollowingThisPath)
        {
            if (creatureControllers.TryGetValue(self.creature, out var p))
            {
                // path will be null if calculating from an inaccessible tile
                // lizard has code to "pick a nearby accessible tile to calculate from"
                // path will be always a path "out" of the current cell, even if current cell is destination
                // should be fine as long as runspeed = 0, just gotta not fall for it here

                if (!actuallyFollowingThisPath && self.destination == originPos) return null; // prevent bad followups
                if (actuallyFollowingThisPath)
                {
                    bool needOverride = false;
                    if (self.destination == originPos && originPos != self.creature.pos) { if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("bad originPos override"); originPos = self.creature.pos; }
                    if (self.destination == originPos) { if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("at destination"); return null; }
                    return orig(self, originPos, bodyDirection, actuallyFollowingThisPath);
                }
            }
            return orig(self, originPos, bodyDirection, actuallyFollowingThisPath);
        }

        private static void Lizard_Update(On.Lizard.orig_Update orig, Lizard self, bool eu)
        {
            if (creatureControllers.TryGetValue(self.abstractCreature, out var p))
            {
                p.Update(eu);
            }

            orig(self, eu);
        }
    }
}

using MonoMod.Cil;
using System.Linq;
using System;
using UnityEngine;
using RWCustom;
using Mono.Cecil.Cil;

namespace RainMeadow
{
    internal class ScavengerController : CreatureController
    {
        public static void EnableScavenger()
        {
            On.Scavenger.Update += Scavenger_Update;
            On.Scavenger.Act += Scavenger_Act;
            On.ScavengerAI.Update += ScavengerAI_Update;
            IL.Scavenger.Act += Scavenger_Act1;

            // color
            On.ScavengerGraphics.ctor += ScavengerGraphics_ctor;
        }

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
                    if(creatureControllers.TryGetValue(self.abstractCreature, out var s))
                    {
                        self.moving = (s as ScavengerController).forceMoving;
                    }
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

        private float jumpBoost;
        private int forceJump;
        private int canGroundJump;
        private int canPoleJump;
        private int canSwingJump;
        private bool forceMoving;
        private IntVector2? lastEnteringShortcut;

        public Scavenger scavenger => creature as Scavenger;

        private static MovementConnection nullConnection = new MovementConnection(MovementConnection.MovementType.Standard, new WorldCoordinate(-1, -1, -1, -1), new WorldCoordinate(-1, -1, -1, -1), 1);

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

        private void Jump()
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
            else if (canSwingJump > 0)
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

                var room = self.room;
                var chunks = self.bodyChunks;
                var nc = chunks.Length;

                var aiTile0 = self.room.aimap.getAItile(chunks[0].pos);
                var aiTile1 = self.room.aimap.getAItile(chunks[1].pos);
                var tile0 = self.room.GetTile(chunks[0].pos);
                var tile1 = self.room.GetTile(chunks[1].pos);

                if (self.movMode == Scavenger.MovementMode.Climb && self.swingPos != null && self.swingClimbCounter >= 2)
                {
                    RainMeadow.Trace("can swing jump");
                    s.canSwingJump = 5;
                }
                else if (self.movMode == Scavenger.MovementMode.Climb && ((aiTile0.acc == AItile.Accessibility.Climb && tile0.AnyBeam) || (aiTile1.acc == AItile.Accessibility.Climb && tile1.AnyBeam)) )
                {
                    RainMeadow.Trace("can pole jump");
                    s.canPoleJump = 5;
                }
                else if ((self.movMode == Scavenger.MovementMode.Run || self.movMode == Scavenger.MovementMode.StandStill) && (chunks[0].contactPoint.y == -1 || chunks[1].contactPoint.y == -1 || self.IsTileSolid(1, 0, -1) || self.IsTileSolid(0, 0, -1)))
                {
                    RainMeadow.Trace("can jump");
                    s.canGroundJump = 5;
                }

                if (s.canGroundJump > 0 && s.input[0].x == 0 && s.input[0].y <= 0 && (s.input[0].jmp || s.input[1].jmp))
                {
                    if (s.input[0].jmp)
                    {
                        // todo if jumpmodule animate
                        s.wantToJump = 0;
                        if (s.superLaunchJump < 20)
                        {
                            s.superLaunchJump++;
                        }
                    }
                    if (!s.input[0].jmp && s.input[1].jmp)
                    {
                        s.wantToJump = 1;
                        s.canGroundJump = 1;
                    }
                }
                else if (s.superLaunchJump > 0)
                {
                    s.superLaunchJump--;
                }

                if (s.wantToJump > 0 && (s.canSwingJump > 0 || s.canPoleJump > 0 || s.canGroundJump > 0))
                {
                    s.Jump();
                    s.canSwingJump = 0;
                    s.canPoleJump = 0;
                    s.canGroundJump = 0;
                    s.wantToJump = 0;
                }
                if (s.canSwingJump > 0) s.canSwingJump--;
                if (s.canPoleJump > 0) s.canPoleJump--;
                if (s.canGroundJump > 0) s.canGroundJump--;
                if (s.forceJump > 0) s.forceJump--;

                if (s.jumpBoost > 0f && (s.input[0].jmp || s.forceBoost > 0))
                {
                    s.jumpBoost -= 1.5f;
                    self.bodyChunks[0].vel.y += (s.jumpBoost + 1f) * 0.3f;
                    self.bodyChunks[1].vel.y += (s.jumpBoost + 1f) * 0.25f;
                    self.bodyChunks[2].vel.y += (s.jumpBoost + 1f) * 0.25f;
                }
                else
                {
                    s.jumpBoost = 0f;
                }

                // facing
                if (Mathf.Abs(Vector2.Dot(Vector2.right, chunks[0].pos - chunks[1].pos)) > 0.5f)
                {
                    s.flipDirection = (chunks[0].pos.x > chunks[1].pos.x) ? 1 : -1;
                }
                else if (Mathf.Abs(s.inputDir.x) > 0.5f)
                {
                    s.flipDirection = s.inputDir.x > 0 ? 1 : -1;
                }

                //// lost footing doesn't auto-recover
                //if (self.inAllowedTerrainCounter < 10)
                //{
                //    if (s.input[0].y < 1 && !(chunks[0].contactPoint.y == -1 || chunks[1].contactPoint.y == -1 || self.IsTileSolid(1, 0, -1) || self.IsTileSolid(0, 0, -1)))
                //    {
                //        self.inAllowedTerrainCounter = 0;
                //    }
                //}

                // move
                //var basepos = 0.5f * (self.bodyChunks[0].pos + self.bodyChunks[1].pos);
                var basepos = self.bodyChunks[1].pos;
                var basecoord = self.room.GetWorldCoordinate(basepos);
                if (s.inputDir != Vector2.zero)
                {
                    self.AI.behavior = ScavengerAI.Behavior.Travel;
                    self.AI.runSpeedGoal = Custom.LerpAndTick(self.AI.runSpeedGoal, 0.8f, 0.2f, 0.05f);
                    //self.AI.excitement = Custom.LerpAndTick(self.AI.excitement, 0.5f, 0.1f, 0.05f);

                    var toPos = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(s.inputDir * 1.42f));
                    bool reachable = room.aimap.TileAccessibleToCreature(toPos.Tile, self.Template);
                    bool keeplooking = true; // this could be turned into a helper and an early return

                    if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"moving towards {toPos.Tile}");
                    if (s.forceJump > 0) // jumping
                    {
                        if (self.commitedToMove == nullConnection) self.commitedToMove = new MovementConnection(MovementConnection.MovementType.Standard, basecoord, toPos, 2);
                        reachable = false;
                        keeplooking = false;
                    }
                    else if (true)//self.inAllowedTerrainCounter > 10)
                    {
                        if (self.enteringShortCut == null && self.shortcutDelay < 1)
                        {
                            for (int i = 0; i < nc; i++)
                            {
                                if (room.GetTile(chunks[i].pos).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
                                {
                                    var scdata = room.shortcutData(room.GetTilePosition(chunks[i].pos));
                                    if (scdata.shortCutType != ShortcutData.Type.DeadEnd)
                                    {
                                        IntVector2 intVector = room.ShorcutEntranceHoleDirection(room.GetTilePosition(chunks[i].pos));
                                        if (s.input[0].x == -intVector.x && s.input[0].y == -intVector.y)
                                        {
                                            RainMeadow.Debug("creature entering shortcut");
                                            self.enteringShortCut = new IntVector2?(room.GetTilePosition(chunks[i].pos));
                                            reachable = false;
                                            keeplooking = false;

                                            if (scdata.shortCutType == ShortcutData.Type.NPCTransportation)
                                            {
                                                var whackamoles = room.shortcuts.Where(s => s.shortCutType == ShortcutData.Type.NPCTransportation).ToList();
                                                var index = whackamoles.IndexOf(self.room.shortcuts.FirstOrDefault(s => s.StartTile == scdata.StartTile));
                                                if (index > -1 && whackamoles.Count > 0)
                                                {
                                                    var newindex = (index + 1) % whackamoles.Count;
                                                    RainMeadow.Debug($"creature entered at {index} will exit at {newindex} mapped to {self.NPCTransportationDestination}");
                                                    self.NPCTransportationDestination = whackamoles[newindex].startCoord;
                                                    // needs to be set as destination as well otherwise might be overriden
                                                    toPos = self.NPCTransportationDestination;
                                                    reachable = true;
                                                    keeplooking = false;
                                                }
                                                else
                                                {
                                                    RainMeadow.Error("shortcut issue");
                                                }

                                                self.commitedToMove = nullConnection;
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        if (keeplooking && s.inputDir.y > 0) // climbing has priority
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                int num = (i > 0) ? ((i == 1) ? -1 : 1) : 0;
                                var tile = room.GetTile(basecoord + new IntVector2(num, 1));
                                if (!tile.Solid && tile.verticalBeam)
                                {
                                    if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("pole close");
                                    toPos = WorldCoordinate.AddIntVector(basecoord, new IntVector2(0, 1));
                                    reachable = true;
                                    keeplooking = false;
                                    break;
                                }
                            }
                            for (int i = 0; i < 3; i++)
                            {
                                int num = (i > 0) ? ((i == 1) ? -1 : 1) : 0;
                                var tileup1 = room.GetTile(basecoord + new IntVector2(num, 1));
                                var tileup2 = room.GetTile(basecoord + new IntVector2(num, 2));
                                if (!tileup1.Solid && tileup2.verticalBeam)
                                {
                                    if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("pole far");
                                    toPos = WorldCoordinate.AddIntVector(basecoord, new IntVector2(0, 2));
                                    reachable = true;
                                    keeplooking = false;
                                    break;
                                }
                            }
                        }
                        if (keeplooking)
                        {
                            if (s.inputDir.x != 0) // to sides
                            {
                                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("sides");
                                if (reachable)
                                {
                                    if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("ahead");
                                }
                                else if (room.aimap.TileAccessibleToCreature(toPos.Tile + new IntVector2(0, 1), self.Template)) // try up
                                {
                                    if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("up");
                                    toPos = WorldCoordinate.AddIntVector(toPos, new IntVector2(0, 1));
                                    reachable = true;
                                }
                                else if (room.aimap.TileAccessibleToCreature(toPos.Tile + new IntVector2(0, -1), self.Template)) // try down
                                {
                                    if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("down");
                                    toPos = WorldCoordinate.AddIntVector(toPos, new IntVector2(0, -1));
                                    reachable = true;
                                }

                                // if can reach further out, it goes faster and smoother
                                var furtherOut = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(s.inputDir * 3f));
                                if (room.aimap.TileAccessibleToCreature(furtherOut.Tile, self.Template) && QuickConnectivity.Check(room, self.Template, basecoord.Tile, furtherOut.Tile, 10) > 0)
                                {
                                    if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("reaching further");
                                    toPos = furtherOut;
                                    reachable = true;
                                }
                                else if (room.aimap.TileAccessibleToCreature(furtherOut.Tile + new IntVector2(0, 1), self.Template) && QuickConnectivity.Check(room, self.Template, basecoord.Tile, furtherOut.Tile + new IntVector2(0, 1), 10) > 0)
                                {
                                    if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("further up");
                                    toPos = WorldCoordinate.AddIntVector(furtherOut, new IntVector2(0, 1));
                                    reachable = true;
                                }
                                else if (room.aimap.TileAccessibleToCreature(furtherOut.Tile + new IntVector2(0, -1), self.Template) && QuickConnectivity.Check(room, self.Template, basecoord.Tile, furtherOut.Tile + new IntVector2(0, -1), 10) > 0)
                                {
                                    if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("further down");
                                    toPos = WorldCoordinate.AddIntVector(furtherOut, new IntVector2(0, -1));
                                    reachable = true;
                                }

                                if (!reachable)
                                {
                                    // no pathing
                                    if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("unpathable");
                                    // don't let go of beams/walls/ceilings
                                    if (room.aimap.TileAccessibleToCreature(tile0.X, tile0.Y, self.Template) && aiTile0.acc >= AItile.Accessibility.Climb)// && self.inAllowedTerrainCounter > 10)
                                    {
                                        //self.AI.runSpeed *= 0.5f;
                                        toPos = basecoord;
                                    }
                                    else // force movement
                                    {
                                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("forced move");
                                        self.commitedToMove = new MovementConnection(MovementConnection.MovementType.Standard, basecoord, furtherOut, 2);
                                        self.commitToMoveCounter = 20;
                                        self.drop = true;
                                        //toPos = furtherOut;
                                        //reachable = true;
                                        s.forceMoving = true;
                                    }
                                }
                            }
                            else
                            {
                                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("vertical");

                                if (keeplooking)
                                {
                                    if (reachable)
                                    {
                                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("ahead");
                                    }
                                    if (keeplooking)
                                    {
                                        var furtherOut = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(s.inputDir * 2.2f));
                                        if (!room.GetTile(toPos).Solid && !room.GetTile(furtherOut).Solid && room.aimap.TileAccessibleToCreature(furtherOut.Tile, self.Template)) // ahead unblocked, move further
                                        {
                                            if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("reaching");
                                            toPos = furtherOut;
                                            reachable = true;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (s.forceJump <= 0 && self.swingPos == null && s.inputDir.y > 0 && tile0.AnyBeam)
                    {
                        self.swingPos = room.MiddleOfTile(tile0.X, tile0.Y);
                        self.swingRadius = 50f;
                        self.swingClimbCounter = 40;
                    }

                    //// minimun directional speed
                    //if (reachable && (self.graphicsModule as LizardGraphics)?.frontLegsGrabbing > 0)
                    //{
                    //    var inDirection = Vector2.Dot(self.mainBodyChunk.vel, s.inputDir);
                    //    var minSpeed = 0.35f;
                    //    if (inDirection < minSpeed)
                    //    {
                    //        self.mainBodyChunk.vel += s.inputDir * (minSpeed - inDirection);
                    //    }
                    //}

                    //// fake extra airfriction in gravity mode
                    //if (self.applyGravity && room.aimap.TileAccessibleToCreature(tile0.X, tile0.Y, self.Template))
                    //{
                    //    for (int i = 0; i < nc; i++)
                    //    {
                    //        BodyChunk bodyChunk = self.bodyChunks[i];
                    //        if (bodyChunk.submersion < 0.5f)
                    //        {
                    //            bodyChunk.vel.x *= 0.95f;
                    //        }
                    //    }
                    //}


                    if (reachable && toPos != self.abstractCreature.abstractAI.destination)
                    {
                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"new destination {toPos.Tile}");
                        s.ForceAIDestination(toPos);
                        s.forceMoving = true;
                    }
                }
                else
                {
                    // rest
                    self.AI.behavior = ScavengerAI.Behavior.Idle;
                    self.AI.runSpeedGoal = Custom.LerpAndTick(self.AI.runSpeedGoal, 0, 0.4f, 0.1f);
                    //self.AI.excitement = Custom.LerpAndTick(self.AI.excitement, 0.2f, 0.1f, 0.05f);
                    self.commitedToMove = nullConnection;
                    s.forceMoving = false;


                    //// pull towards floor
                    //for (int i = 0; i < nc; i++)
                    //{
                    //    if (self.IsTileSolid(i, 0, -1))
                    //    {
                    //        BodyChunk bodyChunk = self.bodyChunks[i];
                    //        bodyChunk.vel.y -= 0.1f;
                    //    }
                    //}

                    if (basecoord != self.abstractCreature.abstractAI.destination)
                    {
                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"resting at {basecoord.Tile}");
                        s.ForceAIDestination(basecoord);
                    }
                }
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"current AI destination {s.creature.abstractCreature.abstractAI.destination}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"moving {s.forceMoving}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"movMode {self.movMode}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"commitedToMove {self.commitedToMove}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"animation {self.animation}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"commitedMoveFollowChunk {self.commitedMoveFollowChunk}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"drop {self.drop}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"ghostCounter {self.ghostCounter}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"occupyTile {self.occupyTile}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"pathingWithExits {self.pathingWithExits}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"pathWithExitsCounter {self.pathWithExitsCounter}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"swingPos {self.swingPos}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"swingProgress {self.swingProgress}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"commitToMoveCounter {self.commitToMoveCounter}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"footingCounter {self.footingCounter}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"moveModeChangeCounter {self.moveModeChangeCounter}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"notFollowingPathToCurrentGoalCounter {self.notFollowingPathToCurrentGoalCounter}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"pathWithExitsCounter {self.pathWithExitsCounter}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"stuckCounter {self.stuckCounter}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"stuckOnShortcutCounter {self.stuckOnShortcutCounter}");
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"swingClimbCounter {self.swingClimbCounter}");

                if (s.lastEnteringShortcut != self.enteringShortCut)
                {
                    RainMeadow.Debug($"shortcut was {s.lastEnteringShortcut} is {self.enteringShortCut}");
                    //RainMeadow.Debug($"followingConnection {self.connections}");
                    RainMeadow.Debug($"commitedToDropConnection {self.commitedToMove}");
                }

                s.lastEnteringShortcut = self.enteringShortCut;
            }
            orig(self);
        }

        private static void Scavenger_Update(On.Scavenger.orig_Update orig, Scavenger self, bool eu)
        {
            if (creatureControllers.TryGetValue(self.abstractCreature, out var p))
            {
                p.Update(eu);
            }

            orig(self, eu);
        }
    }
}
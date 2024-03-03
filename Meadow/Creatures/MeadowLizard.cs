using UnityEngine;
using System;
using RWCustom;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Linq;

namespace RainMeadow
{
    class LizardController : CreatureController
    {
        private float jumpBoost;
        private int forceJump;
        private int canGroundJump;
        private int canPoleJump;
        private int canClimbJump;
        private IntVector2? lastEnteringShortcut;

        public LizardController(Lizard creature, OnlineCreature oc, int playerNumber) : base(creature, oc, playerNumber) { }

        public Lizard lizard => creature as Lizard;

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

        private void Jump()
        {
            //RainMeadow.Debug(onlineCreature);
            var cs = creature.bodyChunks;
            var mainBodyChunk = creature.mainBodyChunk;
            var tile = creature.room.aimap.getAItile(mainBodyChunk.pos);

            // todo if jump module use that instead

            // todo take body factors into factor. blue liz jump too stronk

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
                    cs[1].vel.x = 4f * flipDirection;
                    cs[1].vel.y = 4.5f;
                    cs[2].vel.x = 4f * flipDirection;
                    cs[2].vel.y = 4.5f;
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
            else if (canGroundJump > 0)
            {
                RainMeadow.Debug("lizard normal jump");
                lizard.movementAnimation = null;
                lizard.inAllowedTerrainCounter = 10; // regain footing faster
                lizard.gripPoint = null;
                this.jumpBoost = 8;
                cs[0].vel.y = 5f;
                cs[1].vel.y = 5f;
                cs[2].vel.y = 3f;

                creature.room.PlaySound(SoundID.Slugcat_Normal_Jump, mainBodyChunk, false, 1f, 1f);
            }
            else throw new InvalidProgrammerException("can't jump");
        }

        // might need a new hookpoint between movement planing and movement acting
        private static void Lizard_Act(On.Lizard.orig_Act orig, Lizard self)
        {
            if (creatureControllers.TryGetValue(self.abstractCreature, out var c) && c is LizardController l)
            {
                l.ConsciousUpdate();

                var room = self.room;
                var chunks = self.bodyChunks;
                var nc = chunks.Length;

                var aiTile0 = self.room.aimap.getAItile(chunks[0].pos);
                var tile0 = self.room.GetTile(chunks[0].pos);

                if (l.specialInput[0].direction != Vector2.zero)
                {
                    (self.graphicsModule as LizardGraphics).lookPos = self.DangerPos + 500 * l.specialInput[0].direction;
                }


                RainMeadow.Trace($"legs? {self.LegsGripping}");

                if (self.LegsGripping >= 2 && (aiTile0.acc == AItile.Accessibility.Wall || aiTile0.acc == AItile.Accessibility.Ceiling))
                {
                    RainMeadow.Trace("can climb jump");
                    l.canClimbJump = 5;
                }
                else if (self.LegsGripping >= 2 && aiTile0.acc == AItile.Accessibility.Climb && tile0.AnyBeam)
                {
                    RainMeadow.Trace("can pole jump");
                    l.canPoleJump = 5;
                }
                else if (self.LegsGripping >= 2 && (chunks[0].contactPoint.y == -1 || chunks[1].contactPoint.y == -1 || self.IsTileSolid(1, 0, -1) || self.IsTileSolid(0, 0, -1)))
                {
                    RainMeadow.Trace("can jump");
                    l.canGroundJump = 5;
                }

                if (l.canGroundJump > 0 && l.input[0].x == 0 && l.input[0].y <= 0 && (l.input[0].jmp || l.input[1].jmp))
                {
                    if (l.input[0].jmp)
                    {
                        // todo if jumpmodule animate
                        l.wantToJump = 0;
                        if (l.superLaunchJump < 20)
                        {
                            l.superLaunchJump++;
                        }
                    }
                    if (!l.input[0].jmp && l.input[1].jmp)
                    {
                        l.wantToJump = 1;
                        l.canGroundJump = 1;
                    }
                }
                else if (l.superLaunchJump > 0)
                {
                    l.superLaunchJump--;
                }

                if (l.wantToJump > 0 && (l.canClimbJump > 0 || l.canPoleJump > 0 || l.canGroundJump > 0))
                {
                    l.Jump();
                    l.canClimbJump = 0;
                    l.canPoleJump = 0;
                    l.canGroundJump = 0;
                    l.wantToJump = 0;
                }
                if (l.canClimbJump > 0) l.canClimbJump--;
                if (l.canPoleJump > 0) l.canPoleJump--;
                if (l.canGroundJump > 0) l.canGroundJump--;
                if (l.forceJump > 0) l.forceJump--;

                if (l.jumpBoost > 0f && (l.input[0].jmp || l.forceBoost > 0))
                {
                    l.jumpBoost -= 1.5f;
                    self.bodyChunks[0].vel.y += (l.jumpBoost + 1f) * 0.3f;
                    self.bodyChunks[1].vel.y += (l.jumpBoost + 1f) * 0.25f;
                    self.bodyChunks[2].vel.y += (l.jumpBoost + 1f) * 0.25f;
                }
                else
                {
                    l.jumpBoost = 0f;
                }

                // facing
                if (Mathf.Abs(Vector2.Dot(Vector2.right, chunks[0].pos - chunks[1].pos)) > 0.5f)
                {
                    l.flipDirection = (chunks[0].pos.x > chunks[1].pos.x) ? 1 : -1;
                }
                else if (Mathf.Abs(l.inputDir.x) > 0.5f)
                {
                    l.flipDirection = l.inputDir.x > 0 ? 1 : -1;
                }

                // lost footing doesn't auto-recover
                if (self.inAllowedTerrainCounter < 10)
                {
                    if (l.input[0].y < 1 && !(chunks[0].contactPoint.y == -1 || chunks[1].contactPoint.y == -1 || self.IsTileSolid(1, 0, -1) || self.IsTileSolid(0, 0, -1)))
                    {
                        self.inAllowedTerrainCounter = 0;
                    }
                }

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
                    bool reachable = room.aimap.TileAccessibleToCreature(toPos.Tile, self.Template);
                    bool keeplooking = true; // this could be turned into a helper and an early return

                    if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"moving towards {toPos.Tile}");
                    if (l.forceJump > 0) // jumping
                    {
                        if (self.commitedToDropConnection == null) self.commitedToDropConnection = new MovementConnection(MovementConnection.MovementType.Standard, basecoord, toPos, 2);
                        reachable = false;
                        keeplooking = false;
                    }
                    else if (self.inAllowedTerrainCounter > 10)
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
                                        if (l.input[0].x == -intVector.x && l.input[0].y == -intVector.y)
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

                                                self.commitedToDropConnection = null;
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        if (keeplooking && l.inputDir.y > 0) // climbing has priority
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
                                var tile1 = room.GetTile(basecoord + new IntVector2(num, 1));
                                var tile2 = room.GetTile(basecoord + new IntVector2(num, 2));
                                if (!tile1.Solid && tile2.verticalBeam)
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
                            if (l.inputDir.x != 0) // to sides
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
                                var furtherOut = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(l.inputDir * 2.84f));
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
                                    if (room.aimap.TileAccessibleToCreature(tile0.X, tile0.Y, self.Template) && aiTile0.acc >= AItile.Accessibility.Climb && self.inAllowedTerrainCounter > 10)
                                    {
                                        self.AI.runSpeed *= 0.5f;
                                        toPos = basecoord;
                                    }
                                    else // force movement
                                    {
                                        self.commitedToDropConnection = new MovementConnection(MovementConnection.MovementType.Standard, basecoord, furtherOut, 2);
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
                                        var furtherOut = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(l.inputDir * 2.2f));
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

                    if (l.forceJump <= 0 && self.inAllowedTerrainCounter < 10 && l.inputDir.y > 0 && tile0.AnyBeam)
                    {
                        self.gripPoint = room.MiddleOfTile(tile0.X, tile0.Y);
                    }

                    // minimun directional speed
                    if (reachable && (self.graphicsModule as LizardGraphics)?.frontLegsGrabbing > 0)
                    {
                        var inDirection = Vector2.Dot(self.mainBodyChunk.vel, l.inputDir);
                        var minSpeed = 0.35f;
                        if (inDirection < minSpeed)
                        {
                            self.mainBodyChunk.vel += l.inputDir * (minSpeed - inDirection);
                        }
                    }

                    // fake extra airfriction in gravity mode
                    if (self.applyGravity && room.aimap.TileAccessibleToCreature(tile0.X, tile0.Y, self.Template))
                    {
                        for (int i = 0; i < nc; i++)
                        {
                            BodyChunk bodyChunk = self.bodyChunks[i];
                            if (bodyChunk.submersion < 0.5f)
                            {
                                bodyChunk.vel.x *= 0.95f;
                            }
                        }
                    }


                    if (reachable && toPos != self.abstractCreature.abstractAI.destination)
                    {
                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"new destination {toPos.Tile}");
                        l.ForceAIDestination(toPos);
                    }
                }
                else
                {
                    // rest
                    self.AI.behavior = LizardAI.Behavior.Idle;
                    self.AI.runSpeed = Custom.LerpAndTick(self.AI.runSpeed, 0, 0.4f, 0.1f);
                    self.AI.excitement = Custom.LerpAndTick(self.AI.excitement, 0.2f, 0.1f, 0.05f);
                    self.commitedToDropConnection = null;

                    // pull towards floor
                    for (int i = 0; i < nc; i++)
                    {
                        if (self.IsTileSolid(i, 0, -1))
                        {
                            BodyChunk bodyChunk = self.bodyChunks[i];
                            bodyChunk.vel.y -= 0.1f;
                        }
                    }

                    if (basecoord != self.abstractCreature.abstractAI.destination)
                    {
                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"resting at {basecoord.Tile}");
                        l.ForceAIDestination(basecoord);
                    }
                }
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"current AI destination {l.creature.abstractCreature.abstractAI.destination}");

                if(l.lastEnteringShortcut != self.enteringShortCut)
                {
                    RainMeadow.Debug($"shortcut was {l.lastEnteringShortcut} is {self.enteringShortCut}");
                    RainMeadow.Debug($"followingConnection {self.followingConnection}");
                    RainMeadow.Debug($"commitedToDropConnection {self.commitedToDropConnection}");
                }

                l.lastEnteringShortcut = self.enteringShortCut;
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
    }
}

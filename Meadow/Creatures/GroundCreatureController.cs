using RWCustom;
using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    internal abstract class GroundCreatureController : CreatureController
    {
        protected float jumpBoost;
        protected int forceJump;
        protected int canGroundJump;
        protected int canPoleJump;
        protected int canClimbJump;

        public abstract bool CanClimbJump { get; }
        public abstract bool CanPoleJump { get; }
        public abstract bool CanGroundJump { get; }

        protected abstract void JumpImpl();
        public GroundCreatureController(Creature creature, OnlineCreature oc, int playerNumber) : base(creature, oc, playerNumber)
        {

        }

        protected abstract void LookImpl(Vector2 pos);

        internal override void ConsciousUpdate()
        {
            base.ConsciousUpdate();

            var s = this; // todo remove rename
            var self = creature; // todo remove rename

            var room = self.room;
            var chunks = self.bodyChunks;
            var nc = chunks.Length;

            var aiTile0 = self.room.aimap.getAItile(chunks[0].pos);
            var aiTile1 = self.room.aimap.getAItile(chunks[1].pos);
            var tile0 = self.room.GetTile(chunks[0].pos);
            var tile1 = self.room.GetTile(chunks[1].pos);

            if (s.specialInput[0].direction != Vector2.zero)
            {
                LookImpl(self.DangerPos + 500 * s.specialInput[0].direction);
            }

            if (CanClimbJump)
            {
                RainMeadow.Trace("can swing jump");
                s.canClimbJump = 5;
            }
            else if (CanPoleJump)
            {
                RainMeadow.Trace("can pole jump");
                s.canPoleJump = 5;
            }
            else if (CanGroundJump)
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

            if (s.wantToJump > 0 && (s.canClimbJump > 0 || s.canPoleJump > 0 || s.canGroundJump > 0))
            {
                s.JumpImpl();
                s.canClimbJump = 0;
                s.canPoleJump = 0;
                s.canGroundJump = 0;
                s.wantToJump = 0;
            }
            if (s.canClimbJump > 0) s.canClimbJump--;
            if (s.canPoleJump > 0) s.canPoleJump--;
            if (s.canGroundJump > 0) s.canGroundJump--;
            if (s.forceJump > 0) s.forceJump--;

            if (s.jumpBoost > 0f && (s.input[0].jmp || s.forceBoost > 0))
            {
                s.jumpBoost -= 1.5f;
                chunks[0].vel.y += (s.jumpBoost + 1f) * 0.3f;
                for (int i = 1; i < nc; i++)
                {
                    chunks[1].vel.y += (s.jumpBoost + 1f) * 0.25f;
                    chunks[2].vel.y += (s.jumpBoost + 1f) * 0.25f;
                }
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
                this.Moving();

                var toPos = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(s.inputDir * 1.42f));
                bool reachable = room.aimap.TileAccessibleToCreature(toPos.Tile, self.Template);
                bool keeplooking = true; // this could be turned into a helper and an early return

                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"moving towards {toPos.Tile}");
                if (s.forceJump > 0) // jumping
                {
                    this.MovementOverride(new MovementConnection(MovementConnection.MovementType.Standard, basecoord, toPos, 2));
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

                                            ClearMovementOverride();
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
                                    this.MovementOverride(new MovementConnection(MovementConnection.MovementType.Standard, basecoord, furtherOut, 2));
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

                if (s.forceJump <= 0 && s.inputDir.y > 0 && tile0.AnyBeam) // maybe "no footing" condition?
                {
                    GripPole(tile0);
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


                if (reachable )
                {
                    Moving();
                    if(toPos != self.abstractCreature.abstractAI.destination)
                    {
                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"new destination {toPos.Tile}");
                        s.ForceAIDestination(toPos);
                    }
                }
                else
                {
                    Resting();
                }
            }
            else
            {
                Resting();


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
        }

        protected abstract void Resting();
        protected abstract void GripPole(Room.Tile tile0);
        protected abstract void ClearMovementOverride();
        protected abstract void MovementOverride(MovementConnection movementConnection);
        protected abstract void Moving();

        internal override void Update(bool eu)
        {
            base.Update(eu);
        }
    }
}
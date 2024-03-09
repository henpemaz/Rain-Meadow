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
        public int superLaunchJump;

        public abstract bool CanClimbJump { get; }
        public abstract bool CanPoleJump { get; }
        public abstract bool CanGroundJump { get; }

        protected abstract void JumpImpl();
        public GroundCreatureController(Creature creature, OnlineCreature oc, int playerNumber) : base(creature, oc, playerNumber)
        {

        }

        internal override void ConsciousUpdate()
        {
            base.ConsciousUpdate();

            var room = creature.room;
            var chunks = creature.bodyChunks;
            var nc = chunks.Length;

            var aiTile0 = creature.room.aimap.getAItile(chunks[0].pos);
            var aiTile1 = creature.room.aimap.getAItile(chunks[1].pos);
            var tile0 = creature.room.GetTile(chunks[0].pos);
            var tile1 = creature.room.GetTile(chunks[1].pos);

            bool localTrace = Input.GetKey(KeyCode.L);

            if (CanClimbJump)
            {
                RainMeadow.Trace("can swing jump");
                this.canClimbJump = 5;
            }
            else if (CanPoleJump)
            {
                RainMeadow.Trace("can pole jump");
                this.canPoleJump = 5;
            }
            else if (CanGroundJump)
            {
                RainMeadow.Trace("can jump");
                this.canGroundJump = 5;
            }

            if (this.canGroundJump > 0 && this.input[0].x == 0 && this.input[0].y <= 0 && (this.input[0].jmp || this.input[1].jmp))
            {
                if (this.input[0].jmp)
                {
                    // todo if jumpmodule animate
                    this.wantToJump = 0;
                    if (this.superLaunchJump < 20)
                    {
                        this.superLaunchJump++;
                    }
                }
                if (!this.input[0].jmp && this.input[1].jmp)
                {
                    this.wantToJump = 1;
                    this.canGroundJump = 1;
                }
            }
            else if (this.superLaunchJump > 0)
            {
                this.superLaunchJump--;
            }

            if (this.wantToJump > 0 && (this.canClimbJump > 0 || this.canPoleJump > 0 || this.canGroundJump > 0))
            {
                this.JumpImpl();
                this.canClimbJump = 0;
                this.canPoleJump = 0;
                this.canGroundJump = 0;
                this.wantToJump = 0;
            }
            if (this.canClimbJump > 0) this.canClimbJump--;
            if (this.canPoleJump > 0) this.canPoleJump--;
            if (this.canGroundJump > 0) this.canGroundJump--;
            if (this.forceJump > 0) this.forceJump--;

            if (this.jumpBoost > 0f && (this.input[0].jmp || this.forceBoost > 0))
            {
                this.jumpBoost -= 1.5f;
                chunks[0].vel.y += (this.jumpBoost + 1f) * 0.3f;
                for (int i = 1; i < nc; i++)
                {
                    chunks[1].vel.y += (this.jumpBoost + 1f) * 0.25f;
                    chunks[2].vel.y += (this.jumpBoost + 1f) * 0.25f;
                }
            }
            else
            {
                this.jumpBoost = 0f;
            }

            // facing
            if (Mathf.Abs(Vector2.Dot(Vector2.right, (chunks[0].pos - chunks[1].pos).normalized)) > 0.5f)
            {
                this.flipDirection = (chunks[0].pos.x > chunks[1].pos.x) ? 1 : -1;
            }
            else if (input[0].x != 0)
            {
                this.flipDirection = this.input[0].x;
            }
            else if (Mathf.Abs(specialInput[0].direction.x) > 0.2f)
            {
                this.flipDirection = (int)Mathf.Sign(specialInput[0].direction.x);
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
            var basepos = creature.bodyChunks[1].pos;
            var basecoord = creature.room.GetWorldCoordinate(basepos);
            if (this.inputDir != Vector2.zero)
            {
                this.Moving();

                var toPos = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(this.inputDir * 1.42f));
                bool reachable = room.aimap.TileAccessibleToCreature(toPos.Tile, creature.Template);
                bool keeplooking = true; // this could be turned into a helper and an early return

                if (localTrace) RainMeadow.Debug($"moving towards {toPos.Tile}");
                if (this.forceJump > 0) // jumping
                {
                    this.MovementOverride(new MovementConnection(MovementConnection.MovementType.Standard, basecoord, toPos, 2));
                    reachable = false;
                    keeplooking = false;
                }
                else if (true)//self.inAllowedTerrainCounter > 10)
                {
                    if (creature.enteringShortCut == null && creature.shortcutDelay < 1)
                    {
                        for (int i = 0; i < nc; i++)
                        {
                            if (room.GetTile(chunks[i].pos).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
                            {
                                var scdata = room.shortcutData(room.GetTilePosition(chunks[i].pos));
                                if (scdata.shortCutType != ShortcutData.Type.DeadEnd)
                                {
                                    IntVector2 intVector = room.ShorcutEntranceHoleDirection(room.GetTilePosition(chunks[i].pos));
                                    if (this.input[0].x == -intVector.x && this.input[0].y == -intVector.y)
                                    {
                                        RainMeadow.Debug("creature entering shortcut");
                                        creature.enteringShortCut = new IntVector2?(room.GetTilePosition(chunks[i].pos));
                                        reachable = false;
                                        keeplooking = false;

                                        if (scdata.shortCutType == ShortcutData.Type.NPCTransportation)
                                        {
                                            var whackamoles = room.shortcuts.Where(s => s.shortCutType == ShortcutData.Type.NPCTransportation).ToList();
                                            var index = whackamoles.IndexOf(creature.room.shortcuts.FirstOrDefault(s => s.StartTile == scdata.StartTile));
                                            if (index > -1 && whackamoles.Count > 0)
                                            {
                                                var newindex = (index + 1) % whackamoles.Count;
                                                RainMeadow.Debug($"creature entered at {index} will exit at {newindex} mapped to {creature.NPCTransportationDestination}");
                                                creature.NPCTransportationDestination = whackamoles[newindex].startCoord;
                                                // needs to be set as destination as well otherwise might be overriden
                                                toPos = creature.NPCTransportationDestination;
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
                    if (keeplooking && this.input[0].y > 0) // climbing has priority
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            int num = (i > 0) ? ((i == 1) ? -1 : 1) : 0;
                            var tile = room.GetTile(basecoord + new IntVector2(num, 1));
                            if (!tile.Solid && tile.verticalBeam)
                            {
                                if (localTrace) RainMeadow.Debug("pole close");
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
                                if (localTrace) RainMeadow.Debug("pole far");
                                toPos = WorldCoordinate.AddIntVector(basecoord, new IntVector2(0, 2));
                                reachable = true;
                                keeplooking = false;
                                break;
                            }
                        }
                    }
                    if (keeplooking)
                    {
                        if (this.input[0].x != 0) // to sides
                        {
                            if (localTrace) RainMeadow.Debug("sides");
                            if (reachable)
                            {
                                if (localTrace) RainMeadow.Debug("ahead");
                            }
                            else if (room.aimap.TileAccessibleToCreature(toPos.Tile + new IntVector2(0, 1), creature.Template)) // try up
                            {
                                if (localTrace) RainMeadow.Debug("up");
                                toPos = WorldCoordinate.AddIntVector(toPos, new IntVector2(0, 1));
                                reachable = true;
                            }
                            else if (room.aimap.TileAccessibleToCreature(toPos.Tile + new IntVector2(0, -1), creature.Template)) // try down
                            {
                                if (localTrace) RainMeadow.Debug("down");
                                toPos = WorldCoordinate.AddIntVector(toPos, new IntVector2(0, -1));
                                reachable = true;
                            }

                            // if can reach further out, it goes faster and smoother
                            var furtherOut = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(this.inputDir * 3f));
                            if (room.aimap.TileAccessibleToCreature(furtherOut.Tile, creature.Template) && QuickConnectivity.Check(room, creature.Template, basecoord.Tile, furtherOut.Tile, 10) > 0)
                            {
                                if (localTrace) RainMeadow.Debug("reaching further");
                                toPos = furtherOut;
                                reachable = true;
                            }
                            else if (room.aimap.TileAccessibleToCreature(furtherOut.Tile + new IntVector2(0, 1), creature.Template) && QuickConnectivity.Check(room, creature.Template, basecoord.Tile, furtherOut.Tile + new IntVector2(0, 1), 10) > 0)
                            {
                                if (localTrace) RainMeadow.Debug("further up");
                                toPos = WorldCoordinate.AddIntVector(furtherOut, new IntVector2(0, 1));
                                reachable = true;
                            }
                            else if (room.aimap.TileAccessibleToCreature(furtherOut.Tile + new IntVector2(0, -1), creature.Template) && QuickConnectivity.Check(room, creature.Template, basecoord.Tile, furtherOut.Tile + new IntVector2(0, -1), 10) > 0)
                            {
                                if (localTrace) RainMeadow.Debug("further down");
                                toPos = WorldCoordinate.AddIntVector(furtherOut, new IntVector2(0, -1));
                                reachable = true;
                            }

                            if (!reachable)
                            {
                                // no pathing
                                if (localTrace) RainMeadow.Debug("unpathable");
                                // don't let go of beams/walls/ceilings
                                if (room.aimap.TileAccessibleToCreature(tile0.X, tile0.Y, creature.Template) && aiTile0.acc >= AItile.Accessibility.Climb)// && self.inAllowedTerrainCounter > 10)
                                {
                                    //self.AI.runSpeed *= 0.5f;
                                    toPos = basecoord;
                                }
                                else // force movement
                                {
                                    if (localTrace) RainMeadow.Debug("forced move");
                                    this.MovementOverride(new MovementConnection(MovementConnection.MovementType.Standard, basecoord, furtherOut, 2));
                                }
                            }
                        }
                        else
                        {
                            if (localTrace) RainMeadow.Debug("vertical");

                            if (keeplooking)
                            {
                                if (reachable)
                                {
                                    if (localTrace) RainMeadow.Debug("ahead");
                                }
                                if (keeplooking)
                                {
                                    var furtherOut = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(this.inputDir * 2.2f));
                                    if (!room.GetTile(toPos).Solid && !room.GetTile(furtherOut).Solid && room.aimap.TileAccessibleToCreature(furtherOut.Tile, creature.Template)) // ahead unblocked, move further
                                    {
                                        if (localTrace) RainMeadow.Debug("reaching");
                                        toPos = furtherOut;
                                        reachable = true;
                                    }
                                }
                            }
                        }
                    }
                }

                if (this.forceJump <= 0 && this.input[0].y > 0 && tile0.AnyBeam) // maybe "no footing" condition?
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
                    if(toPos != creature.abstractCreature.abstractAI.destination)
                    {
                        if (localTrace) RainMeadow.Debug($"new destination {toPos.Tile}");
                        this.ForceAIDestination(toPos);
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

                if (basecoord != creature.abstractCreature.abstractAI.destination)
                {
                    if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"resting at {basecoord.Tile}");
                    this.ForceAIDestination(basecoord);
                }
            }
        }

        protected abstract void Resting();
        protected abstract void GripPole(Room.Tile tile0);
        protected abstract void ClearMovementOverride();
        protected abstract void MovementOverride(MovementConnection movementConnection);
        protected abstract void Moving();


        public override void CheckInput()
        {
            base.CheckInput();
            if (this.superLaunchJump > 10 && this.input[0].jmp && this.input[1].jmp && this.input[0].y < 1)
            {
                this.input[0].x = 0;
                this.input[0].analogueDir.x *= 0;
            }
            else if (this.superLaunchJump >= 20)
            {
                this.input[0].x = 0;
                this.input[0].analogueDir.x *= 0;
            }
        }
        internal override void Update(bool eu)
        {
            base.Update(eu);

            if (this.forceBoost > 0) this.forceBoost--;
        }

        public int forceBoost;
    }
}
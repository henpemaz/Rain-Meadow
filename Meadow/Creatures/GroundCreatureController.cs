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
        public bool lockInPlace;

        public abstract bool HasFooting { get; }
        public abstract bool CanClimbJump { get; }
        public abstract bool CanPoleJump { get; }
        public abstract bool CanGroundJump { get; }

        protected abstract void JumpImpl();
        public GroundCreatureController(Creature creature, OnlineCreature oc, int playerNumber) : base(creature, oc, playerNumber)
        {

        }

        public virtual WorldCoordinate CurrentPathfindingPosition
        {
            get
            {
                return creature.coord;
            }
        }

        internal virtual bool FindDestination(WorldCoordinate basecoord, out WorldCoordinate toPos, out float magnitude)
        {

            var room = creature.room;
            var chunks = creature.bodyChunks;
            var nc = chunks.Length;

            var template = creature.Template;

            var tile0 = creature.room.GetTile(chunks[0].pos);
            var tile1 = creature.room.GetTile(chunks[1].pos);

            bool localTrace = Input.GetKey(KeyCode.L);

            toPos = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(this.inputDir.normalized * 1.42f));
            magnitude = 0.5f;
            var previousAccessibility = room.aimap.getAItile(basecoord).acc;

            var currentTile = room.GetTile(basecoord);
            var currentAccessibility = room.aimap.getAItile(toPos).acc;
            var currentLegality = template.AccessibilityResistance(currentAccessibility).legality;

            if (localTrace) RainMeadow.Debug($"moving from {basecoord.Tile} towards {toPos.Tile}");
            
            if (this.forceJump > 0) // jumping
            {
                this.MovementOverride(new MovementConnection(MovementConnection.MovementType.Standard, basecoord, toPos, 2));
                return true;
            }

            // problematic when climbing
            if (this.input[0].y > 0 && (tile0.AnyBeam || tile1.AnyBeam)) // maybe "no footing" condition?
            {
                GripPole(tile0.AnyBeam ? tile0 : tile1);
            }

            // prio 1: entering shortcut
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
                                        return true;
                                    }
                                    else
                                    {
                                        RainMeadow.Error("shortcut issue");
                                    }

                                    ClearMovementOverride();
                                }
                                return true;
                            }
                        }
                    }
                }
            }

            // prio 2: climb
            if (this.input[0].y > 0 && previousAccessibility <= AItile.Accessibility.Climb || currentTile.WaterSurface)
            {
                bool climbing = false;
                for (int i = 0; i < 3; i++)
                {
                    int num = (i > 0) ? ((i == 1) ? -1 : 1) : 0;
                    var tile = room.GetTile(basecoord + new IntVector2(num, 1));
                    if (!tile.Solid && tile.verticalBeam)
                    {
                        if (localTrace) RainMeadow.Debug("pole close");
                        toPos = WorldCoordinate.AddIntVector(basecoord, new IntVector2(num, 1));
                        climbing = true;
                        break;
                    }
                }
                if(!climbing || inputDir.y > 0.75f) // not found yet, OR pulling the stick hard
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int num = (i > 0) ? ((i == 1) ? -1 : 1) : 0;
                        var tileup1 = room.GetTile(basecoord + new IntVector2(num, 1));
                        var tileup2 = room.GetTile(basecoord + new IntVector2(num, 2));
                        if (!tileup1.Solid && tileup2.verticalBeam)
                        {
                            if (localTrace) RainMeadow.Debug("pole far");
                            toPos = WorldCoordinate.AddIntVector(basecoord, new IntVector2(0, 2));
                            climbing = true;
                            break;
                        }
                    }
                }
                if (climbing) return true;
            }

            var targetAccessibility = currentAccessibility;
            var furtherOut = toPos;

            // run once at current accessibility level
            // if not found and any higher accessibility level available, run again once
            while (true)
            {
                if (this.input[0].x != 0) // to sides
                {
                    if (localTrace) RainMeadow.Debug("sides");
                    if (currentLegality <= PathCost.Legality.Unwanted)
                    {
                        if (localTrace) RainMeadow.Debug("ahead");
                    }
                    else if (room.aimap.TileAccessibleToCreature(toPos.Tile + new IntVector2(0, 1), creature.Template)) // try up
                    {
                        if (localTrace) RainMeadow.Debug("up");
                        toPos = WorldCoordinate.AddIntVector(toPos, new IntVector2(0, 1));
                        currentAccessibility = room.aimap.getAItile(toPos).acc;
                        currentLegality = template.AccessibilityResistance(currentAccessibility).legality;
                    }
                    else if (room.aimap.TileAccessibleToCreature(toPos.Tile + new IntVector2(0, -1), creature.Template)) // try down
                    {
                        if (localTrace) RainMeadow.Debug("down");
                        toPos = WorldCoordinate.AddIntVector(toPos, new IntVector2(0, -1));
                        currentAccessibility = room.aimap.getAItile(toPos).acc;
                        currentLegality = template.AccessibilityResistance(currentAccessibility).legality;
                    }

                    if (inputDir.magnitude > 0.75f)
                    {
                        // if can reach further out, it goes faster and smoother
                        furtherOut = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(this.inputDir.normalized * 3f));
                        if (room.aimap.TileAccessibleToCreature(furtherOut.Tile, creature.Template) && QuickConnectivity.Check(room, creature.Template, basecoord.Tile, furtherOut.Tile, 6) > 0)
                        {
                            if (localTrace) RainMeadow.Debug("reaching further");
                            toPos = furtherOut;
                            magnitude = 1f;
                            currentAccessibility = room.aimap.getAItile(toPos).acc;
                            currentLegality = template.AccessibilityResistance(currentAccessibility).legality;
                        }
                        else if (room.aimap.TileAccessibleToCreature(furtherOut.Tile + new IntVector2(0, 1), creature.Template) && QuickConnectivity.Check(room, creature.Template, basecoord.Tile, furtherOut.Tile + new IntVector2(0, 1), 6) > 0)
                        {
                            if (localTrace) RainMeadow.Debug("further up");
                            toPos = WorldCoordinate.AddIntVector(furtherOut, new IntVector2(0, 1));
                            magnitude = 1f;
                            currentAccessibility = room.aimap.getAItile(toPos).acc;
                            currentLegality = template.AccessibilityResistance(currentAccessibility).legality;
                        }
                        else if (room.aimap.TileAccessibleToCreature(furtherOut.Tile + new IntVector2(0, -1), creature.Template) && QuickConnectivity.Check(room, creature.Template, basecoord.Tile, furtherOut.Tile + new IntVector2(0, -1), 6) > 0)
                        {
                            if (localTrace) RainMeadow.Debug("further down");
                            toPos = WorldCoordinate.AddIntVector(furtherOut, new IntVector2(0, -1));
                            magnitude = 1f;
                            currentAccessibility = room.aimap.getAItile(toPos).acc;
                            currentLegality = template.AccessibilityResistance(currentAccessibility).legality;
                        }
                    }
                }
                else
                {
                    if (localTrace) RainMeadow.Debug("vertical");

                    if (currentLegality <= PathCost.Legality.Unwanted)
                    {
                        if (localTrace) RainMeadow.Debug("ahead");
                    }
                    else if (room.aimap.TileAccessibleToCreature(toPos.Tile + new IntVector2(1, 0), creature.Template)) // right
                    {
                        if (localTrace) RainMeadow.Debug("right");
                        toPos = WorldCoordinate.AddIntVector(toPos, new IntVector2(1, 0));
                        currentAccessibility = room.aimap.getAItile(toPos).acc;
                        currentLegality = template.AccessibilityResistance(currentAccessibility).legality;
                    }
                    else if (room.aimap.TileAccessibleToCreature(toPos.Tile + new IntVector2(-1, 0), creature.Template)) // left
                    {
                        if (localTrace) RainMeadow.Debug("left");
                        toPos = WorldCoordinate.AddIntVector(toPos, new IntVector2(-1, 0));
                        currentAccessibility = room.aimap.getAItile(toPos).acc;
                        currentLegality = template.AccessibilityResistance(currentAccessibility).legality;
                    }
                    if (inputDir.magnitude > 0.75f)
                    {
                        furtherOut = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(this.inputDir.normalized * 2.2f));
                        if (!room.GetTile(toPos).Solid && !room.GetTile(furtherOut).Solid && room.aimap.TileAccessibleToCreature(furtherOut.Tile, creature.Template)) // ahead unblocked, move further
                        {
                            if (localTrace) RainMeadow.Debug("reaching");
                            toPos = furtherOut;
                            magnitude = 1f;
                            currentAccessibility = room.aimap.getAItile(toPos).acc;
                            currentLegality = template.AccessibilityResistance(currentAccessibility).legality;
                        }
                    }
                }

                // any higher accessibilities to check?
                bool higherAcc = false;
                while (targetAccessibility < AItile.Accessibility.Solid) higherAcc |= template.AccessibilityResistance(++targetAccessibility).Allowed;
                if (currentLegality > PathCost.Legality.Unwanted && higherAcc)
                {
                    // not found, run again
                    continue;
                }
                break;
            }

            if (currentLegality <= PathCost.Legality.Unwanted) // found
            {
                return true;
            }
            else
            {
                // no pathing
                if (localTrace) RainMeadow.Debug("unpathable");
                // don't let go of beams/walls/ceilings
                if (HasFooting && room.aimap.getAItile(toPos).acc < AItile.Accessibility.Solid) // force movement
                {
                    if (localTrace) RainMeadow.Debug("forced move to " + toPos.Tile);
                    magnitude = 1f;
                    this.MovementOverride(new MovementConnection(MovementConnection.MovementType.DropToFloor, basecoord, toPos, 1));
                    return true;
                }
                else
                {
                    if (localTrace) RainMeadow.Debug("unable to move");
                    return false;
                }
            }
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
            if (CanPoleJump)
            {
                RainMeadow.Trace("can pole jump");
                this.canPoleJump = 5;
            }
            if (CanGroundJump)
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
                    else
                    {
                        lockInPlace = true;
                    }
                    if (this.superLaunchJump > 10)
                    {
                        lockInPlace = true;
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

            this.flipDirection = GetFlip();

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

            if (onlineCreature.isMine)
            {
                var mcd = onlineCreature.GetData<MeadowCreatureData>();
                var basecoord = CurrentPathfindingPosition;
                if (!lockInPlace && this.inputDir != Vector2.zero)
                {
                    if (specialInput[0].direction.magnitude < 0.2f)
                    {
                        LookImpl(creature.DangerPos + 200 * inputDir);
                    }
                    // todo have remote send us this instead of pathfinding for remote entities
                    if (FindDestination(basecoord, out var toPos, out float magnitude))
                    {
                        Moving(magnitude);
                        mcd.moveSpeed = magnitude;
                        if (toPos != creature.abstractCreature.abstractAI.destination)
                        {
                            if (localTrace) RainMeadow.Debug($"new destination {toPos.Tile}");
                            this.ForceAIDestination(toPos);
                            mcd.destination = toPos;
                        }
                    }
                    else
                    {
                        Resting();
                        mcd.moveSpeed = 0f;
                        if (basecoord != creature.abstractCreature.abstractAI.destination)
                        {
                            if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"resting at {basecoord.Tile}");
                            this.ForceAIDestination(basecoord);
                            mcd.destination = basecoord;
                        }
                    }
                }
                else
                {
                    Resting();
                    mcd.moveSpeed = 0f;
                    if (basecoord != creature.abstractCreature.abstractAI.destination)
                    {
                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"resting at {basecoord.Tile}");
                        this.ForceAIDestination(basecoord);
                        mcd.destination = basecoord;
                    }
                }
            }
            else
            {
                var mcd = onlineCreature.GetData<MeadowCreatureData>();
                if (mcd.moveSpeed > 0f)
                {
                    Moving(mcd.moveSpeed);
                }
                else
                {
                    Resting();
                }
                if (mcd.destination != creature.abstractCreature.abstractAI.destination)
                {
                    this.ForceAIDestination(mcd.destination);
                }
            }

            lockInPlace = false;
        }

        protected virtual int GetFlip()
        {
            BodyChunk[] chunks = creature.bodyChunks;
            // facing
            if (Mathf.Abs(Vector2.Dot(Vector2.right, (chunks[0].pos - chunks[1].pos).normalized)) > 0.5f)
            {
                return (chunks[0].pos.x > chunks[1].pos.x) ? 1 : -1;
            }
            else if (input[0].x != 0)
            {
                return this.input[0].x;
            }
            else if (Mathf.Abs(specialInput[0].direction.x) > 0.2f)
            {
                return (int)Mathf.Sign(specialInput[0].direction.x);
            }
            return flipDirection;
        }

        protected abstract void Resting();
        protected abstract void GripPole(Room.Tile tile0);
        protected abstract void ClearMovementOverride();
        protected abstract void MovementOverride(MovementConnection movementConnection);
        protected abstract void Moving(float magnitude);

        internal override void Update(bool eu)
        {
            base.Update(eu);

            if (this.forceBoost > 0) this.forceBoost--;
        }

        public int forceBoost;
    }
}
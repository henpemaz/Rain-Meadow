using RWCustom;
using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public abstract class GroundCreatureController : CreatureController
    {
        public GroundCreatureController(Creature creature, OnlineCreature oc, int playerNumber, MeadowAvatarCustomization customization) : base(creature, oc, playerNumber, customization)
        {
            this._wallClimber = creature.Template.AccessibilityResistance(AItile.Accessibility.Wall).Allowed;
        }

        public float jumpBoost;
        public int forceJump;
        public int canCorridorBoost;
        public int canGroundJump;
        public int canPoleJump;
        public int canClimbJump;
        public int superLaunchJump;

        public abstract bool HasFooting { get; }
        public abstract bool OnGround { get; }
        public abstract bool OnPole { get; }
        public abstract bool OnCorridor { get; }

        private readonly bool _wallClimber;
        protected bool canZeroGClimb;
        public bool WallClimber => _wallClimber || (creature.room.gravity == 0f && canZeroGClimb);

        public Room.Tile GetTile(int bChunk)
        {
            return creature.room.GetTile(creature.room.GetTilePosition(creature.bodyChunks[bChunk].pos));
        }

        public Room.Tile GetTile(int bChunk, int relativeX, int relativeY)
        {
            return creature.room.GetTile(creature.room.GetTilePosition(creature.bodyChunks[bChunk].pos) + new IntVector2(relativeX, relativeY));
        }

        public AItile GetAITile(int bChunk)
        {
            return creature.room.aimap.getAItile(creature.room.GetTilePosition(creature.bodyChunks[bChunk].pos));
        }

        public bool IsTileGround(int bChunk, int relativeX, int relativeY)
        {
            switch (creature.room.GetTile(creature.room.GetTilePosition(creature.bodyChunks[bChunk].pos) + new IntVector2(relativeX, relativeY)).Terrain)
            {
                case Room.Tile.TerrainType.Solid:
                case Room.Tile.TerrainType.Floor:
                case Room.Tile.TerrainType.Slope:
                    return true;
            }
            return false;
        }

        protected float jumpFactor = 1f;
        protected abstract void OnJump();
        protected virtual void Jump()
        {
            var cs = creature.bodyChunks;
            var cc = cs.Length;
            var mainBodyChunk = creature.mainBodyChunk;

            // todo take body factors into factor. blue liz jump feels too stronk
            if (canGroundJump > 0 && superLaunchJump >= 20)
            {
                RainMeadow.Debug("super jump");
                superLaunchJump = 0;
                OnJump();
                this.jumpBoost = 6f;
                this.forceBoost = 6;
                for (int i = 0; i < cs.Length; i++)
                {
                    BodyChunk chunk = cs[i];
                    chunk.vel.x += 8 * jumpFactor * flipDirection;
                    chunk.vel.y += 6 * jumpFactor;
                }
                creature.room.PlaySound(SoundID.Slugcat_Super_Jump, mainBodyChunk, false, 1f, 1f);
            }
            else if (canPoleJump > 0)
            {
                this.jumpBoost = 0f;
                if (creature.room.GetTilePosition(cs[0].pos).x == creature.room.GetTilePosition(cs[1].pos).x && // aligned
                    ((GetTile(0).verticalBeam && !GetTile(0, 0, 1).verticalBeam)
                    || (GetTile(1).verticalBeam && !GetTile(0).verticalBeam)))
                {
                    RainMeadow.Debug("beamtip jump");
                    OnJump();
                    this.forceJump = 10;
                    this.jumpBoost = 8f;
                    flipDirection = this.input[0].x;
                    var dir = new Vector2(this.input[0].x, 2f).normalized;
                    cs[0].vel += 8f * jumpFactor * dir;
                    for (int i = 1; i < cc; i++)
                    {
                        cs[i].vel += 7f * jumpFactor * dir;
                    }
                    creature.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, mainBodyChunk, false, 1f, 1f);
                    return;
                }
                if (this.input[0].x != 0)
                {
                    RainMeadow.Debug("pole jump");
                    OnJump();
                    this.forceJump = 10;
                    flipDirection = this.input[0].x;
                    cs[0].vel.x = 6f * jumpFactor * flipDirection;
                    cs[0].vel.y = 6f * jumpFactor;
                    for (int i = 1; i < cc; i++)
                    {
                        cs[i].vel.x = 6f * jumpFactor * flipDirection;
                        cs[i].vel.y = 5f * jumpFactor;
                    }
                    creature.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, mainBodyChunk, false, 1f, 1f);
                    return;
                }
                if (this.input[0].y <= 0)
                {
                    RainMeadow.Debug("pole drop");
                    OnJump();
                    mainBodyChunk.vel.y = 2f * jumpFactor;
                    if (this.input[0].y > -1)
                    {
                        mainBodyChunk.vel.x = 2f * jumpFactor * flipDirection;
                    }
                    creature.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, mainBodyChunk, false, 0.3f, 1f);
                    return;
                }// no climb boost
            }
            else if (canGroundJump > 0)
            {
                RainMeadow.Debug("normal jump");
                OnJump();
                this.jumpBoost = 6;
                cs[0].vel.y = 4f * jumpFactor;
                for (int i = 1; i < cc; i++)
                {
                    cs[i].vel.y = 4.5f * jumpFactor;
                }
                if (input[0].x != 0)
                {
                    var d = input[0].x;
                    cs[0].vel.x += d * 1.2f * jumpFactor;
                    for (int i = 1; i < cc; i++)
                    {
                        cs[i].vel.x += d * 1.2f * jumpFactor;
                    }
                }

                creature.room.PlaySound(SoundID.Slugcat_Normal_Jump, mainBodyChunk, false, 1f, 1f);
            }
            else if (canClimbJump > 0)
            {
                RainMeadow.Debug("climb jump");
                OnJump();
                this.jumpBoost = 3f;
                var jumpdir = (cs[0].pos - cs[1].pos).normalized + inputDir;
                for (int i = 0; i < cc; i++)
                {
                    cs[i].vel += jumpdir * jumpFactor;
                }
                creature.room.PlaySound(SoundID.Slugcat_Wall_Jump, mainBodyChunk, false, 1f, 1f);
            }
            else throw new InvalidProgrammerException("can't jump");
        }


        internal override bool FindDestination(WorldCoordinate basecoord, out WorldCoordinate toPos, out float magnitude)
        {
            if (base.FindDestination(basecoord, out toPos, out magnitude)) return true;

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
            if (this.input[0].y > 0 && (tile0.AnyBeam || tile1.AnyBeam) && !HasFooting)
            {
                RainMeadow.Debug("grip!");
                GripPole(tile0.AnyBeam ? tile0 : tile1);
            }

            // climb
            if (this.input[0].y > 0 && previousAccessibility <= AItile.Accessibility.Climb || currentTile.WaterSurface)
            {
                bool climbing = false;
                for (int i = 0; i < 3; i++)
                {
                    int num = (i > 0) ? ((i == 1) ? -1 : 1) : 0;
                    var tile = room.GetTile(basecoord + new IntVector2(num, 1));
                    var aitile = room.aimap.getAItile(tile.X, tile.Y);
                    if (!tile.Solid && (tile.verticalBeam || aitile.acc == AItile.Accessibility.Climb))
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
                        var aitile = room.aimap.getAItile(tileup2.X, tileup2.Y);
                        if (!tileup1.Solid && (tileup2.verticalBeam || aitile.acc == AItile.Accessibility.Climb))
                        {
                            if (localTrace) RainMeadow.Debug("pole far");
                            toPos = WorldCoordinate.AddIntVector(basecoord, new IntVector2(num, 2));
                            climbing = true;
                            break;
                        }
                    }
                }
                if (climbing) return true;
            }

            var targetAccessibility = currentAccessibility;

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
                        WorldCoordinate furtherOut = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(this.inputDir.normalized * 3f));
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
                        WorldCoordinate furtherOut = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(this.inputDir.normalized * 2.2f));
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

                // ended up unused, need better engineering of "stick to same acc mode unless not available"
                //// any higher accessibilities to check?
                //bool higherAcc = false;
                //while (targetAccessibility < AItile.Accessibility.Solid) higherAcc |= template.AccessibilityResistance(++targetAccessibility).Allowed;
                //if (currentLegality > PathCost.Legality.Unwanted && higherAcc)
                //{
                //    // not found, run again
                //    continue;
                //}
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
                
                if (!OnPole // don't let go of beams/walls/ceilings
                    && room.aimap.getAItile(toPos).acc < AItile.Accessibility.Solid // no
                    && (input[0].y != 1 || input[0].x != 0)) // not straight up
                {
                    // force movement
                    if (localTrace) RainMeadow.Debug("forced move to " + toPos.Tile);
                    magnitude = 1f;
                    this.MovementOverride(new MovementConnection(MovementConnection.MovementType.Standard, basecoord, toPos, 2));
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
            bool localTrace = UnityEngine.Input.GetKey(KeyCode.L);
            
            if(HasFooting)
            {
                if (OnCorridor)
                {
                    if (localTrace) RainMeadow.Debug("can corridor boost");
                    this.canCorridorBoost = 5;
                }
                if (OnGround)
                {
                    if(localTrace) RainMeadow.Debug("can ground jump");
                    this.canGroundJump = 5;
                }
                else if (OnPole)
                {
                    if (localTrace) RainMeadow.Debug("can pole jump");
                    this.canPoleJump = 5;
                }
                else
                {
                    if (localTrace) RainMeadow.Debug("can climb jump");
                    this.canClimbJump = 5;
                }
            }
            else
            {
                if (localTrace) RainMeadow.Debug("no footing");
            }
            
            if (this.canGroundJump > 0)
            {
                if (this.input[0].jmp && (this.superLaunchJump > 10 || (this.input[0].x == 0 && this.input[0].y <= 0)))
                {
                    if (localTrace) RainMeadow.Debug("charging pounce");
                    this.wantToJump = 0;
                    if (this.superLaunchJump <= 20)
                    {
                        this.superLaunchJump++;
                    }
                    if (this.superLaunchJump > 10)
                    {
                        lockInPlace = true;
                    }
                }
                else
                {
                    if (this.superLaunchJump > 0) this.superLaunchJump--;
                    if (this.input[0].jmp && !this.input[1].jmp) // directional jump, will use boost
                    {
                        this.wantToJump = 5;
                    }
                }
                if (!this.input[0].jmp && this.input[1].jmp)
                {
                    if(this.superLaunchJump >= 20)
                    {
                        this.wantToJump = 1;
                    } 
                    else if (this.superLaunchJump > 2 && this.superLaunchJump <= 10) // regular jump attempt, will miss boost because released
                    {
                        this.wantToJump = 5;
                    }
                }
            }
            else if (this.superLaunchJump > 0) this.superLaunchJump--;

            if (this.wantToJump > 0 && (this.canClimbJump > 0 || this.canPoleJump > 0 || this.canGroundJump > 0))
            {
                if (localTrace) RainMeadow.Debug("jumping");
                this.Jump();
                this.canClimbJump = 0;
                this.canPoleJump = 0;
                this.canGroundJump = 0;
                this.superLaunchJump = 0;
                this.wantToJump = 0;
            }

            if (this.jumpBoost > 0f && (this.input[0].jmp || this.forceBoost > 0))
            {
                this.jumpBoost -= 1.5f;
                var chunks = creature.bodyChunks;
                var nc = chunks.Length;
                chunks[0].vel.y += (this.jumpBoost + 1f) * 0.3f;
                for (int i = 1; i < nc; i++)
                {
                    chunks[i].vel.y += (this.jumpBoost + 1f) * 0.25f;
                }
            }
            else
            {
                this.jumpBoost = 0f;
            }

            this.flipDirection = GetFlip();
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

        protected abstract void GripPole(Room.Tile tile0);
        protected abstract void ClearMovementOverride();
        protected abstract void MovementOverride(MovementConnection movementConnection);

        internal override void Update(bool eu)
        {
            base.Update(eu);

            if (this.canClimbJump > 0) this.canClimbJump--;
            if (this.canPoleJump > 0) this.canPoleJump--;
            if (this.canGroundJump > 0) this.canGroundJump--;
            if (this.forceJump > 0) this.forceJump--;
            if (this.forceBoost > 0) this.forceBoost--;
        }

        public int forceBoost;
    }
}
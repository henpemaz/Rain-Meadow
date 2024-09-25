using RWCustom;
using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public abstract class GroundCreatureController : CreatureController
    {
        public static void Enable()
        {
            On.StandardPather.FollowPath += StandardPather_FollowPath;
            On.Creature.Update += Creature_Update;
        }

        // hookpoint before physics update
        private static void Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
        {
            if (CreatureController.creatureControllers.TryGetValue(self, out var c))
            {
                if (c.input[0].y == -1)
                {
                    self.GoThroughFloors = true; // most creatures set this based on pathfinding movement type *after* act
                                                 // but we need to override it
                }
            }

            orig(self, eu);
        }

        private static MovementConnection StandardPather_FollowPath(On.StandardPather.orig_FollowPath orig, StandardPather self, WorldCoordinate originPos, bool actuallyFollowingThisPath)
        {
            if (CreatureController.creatureControllers.TryGetValue(self.creature.realizedCreature, out var c))
            {
                if (originPos == self.destination || (actuallyFollowingThisPath && self.lookingForImpossiblePath))
                {
                    if (Input.GetKey(KeyCode.L) && actuallyFollowingThisPath) RainMeadow.Debug("returning override. lookingForImpossiblePath? " + self.lookingForImpossiblePath);
                    return new MovementConnection(MovementConnection.MovementType.Standard, originPos, self.destination, 1);
                }
                return orig(self, originPos, actuallyFollowingThisPath);
            }

            return orig(self, originPos, actuallyFollowingThisPath);
        }

        public GroundCreatureController(Creature creature, OnlineCreature oc, int playerNumber, MeadowAvatarCustomization customization) : base(creature, oc, playerNumber, customization)
        {
            this._wallClimber = creature.Template.AccessibilityResistance(AItile.Accessibility.Wall).Allowed;
            this._swimWithPathing = template.MovementLegalInRelationToWater(true, false);
        }

        public float jumpBoost;
        public int forceJump;
        public int forceBoost;

        public int canCorridorBoost;
        public int canWallJump;
        public int canGroundJump;
        public int canPoleJump;
        public int canClimbJump;
        public int canWaterJump;
        public int superLaunchJump;

        public abstract bool HasFooting { get; }
        public abstract bool IsOnGround { get; }
        public abstract bool IsOnPole { get; }
        public abstract bool IsOnCorridor { get; }
        public abstract bool IsOnClimb { get; }
        public virtual bool CanPounce { get { return IsOnGround; } }

        public virtual int OnWall { get { return creature.mainBodyChunk.contactPoint.x; } }
        public virtual bool IsOnWaterSurface => creature.mainBodyChunk.submersion is > 0.3f and < 1;

        protected const float zeroGTreshold = 0.3f;
        private readonly bool _wallClimber;
        protected bool canZeroGClimb;
        public bool WallClimber => _wallClimber || (creature.room.gravity <= zeroGTreshold && canZeroGClimb);

        private readonly bool _swimWithPathing;

        protected float runSpeed = 4.5f;

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
                canGroundJump = 0;
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
                    cs[0].vel *= 0.4f;
                    cs[0].vel += 8f * jumpFactor * dir;
                    for (int i = 1; i < cc; i++)
                    {
                        cs[i].vel *= 0.4f;
                        cs[i].vel += 7f * jumpFactor * dir;
                    }
                    creature.room.PlaySound(SoundID.Slugcat_From_Vertical_Pole_Jump, mainBodyChunk, false, 1f, 1f);
                    canPoleJump = 0;
                    return;
                }
                if (creature.room.GetTilePosition(cs[0].pos).x == creature.room.GetTilePosition(cs[1].pos).x && // aligned
                    ((GetTile(0).horizontalBeam && !GetTile(0, 0, 1).horizontalBeam)
                    || (GetTile(1).horizontalBeam && !GetTile(0).horizontalBeam)))
                {
                    RainMeadow.Debug("horiz beam jump");
                    OnJump();
                    this.forceJump = 10;
                    this.jumpBoost = 8f;
                    flipDirection = this.input[0].x;
                    var dir = new Vector2(this.input[0].x, this.input[0].y * 2f).normalized;
                    cs[0].vel *= 0.4f;
                    cs[0].vel += 8f * jumpFactor * dir;
                    for (int i = 1; i < cc; i++)
                    {
                        cs[i].vel += 5f * jumpFactor * dir;
                    }
                    creature.room.PlaySound(SoundID.Slugcat_From_Horizontal_Pole_Jump, mainBodyChunk, false, 1f, 1f);
                    canPoleJump = 0;
                    return;
                }
                if (this.input[0].x != 0)
                {
                    RainMeadow.Debug("pole side jump");
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
                    canPoleJump = 0;
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
                    canPoleJump = 0;
                    return;
                }// no climb boost
            }
            else if (canCorridorBoost > 0)
            {
                RainMeadow.Debug("corridor jump");
                OnJump();
                this.jumpBoost = 6;
                var jumpdir = (cs[0].pos - cs[1].pos).normalized + inputDir;
                cs[0].vel += jumpdir * 3f * jumpFactor;
                creature.room.PlaySound(SoundID.Slugcat_Corridor_Horizontal_Slide_Success, mainBodyChunk, false, 1f, 1f);
                canCorridorBoost = 0;
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
                canGroundJump = 0;
                canWallJump = 0;
            }
            else if (canWallJump != 0)
            {
                RainMeadow.Debug("wall jump");
                OnJump();
                this.jumpBoost = 6f;
                this.forceBoost = 6;
                var jumpdir = new Vector2(-Mathf.Sign(canWallJump), 1.74f).normalized;
                RainMeadow.Debug("jumpdir " + jumpdir);
                cs[0].vel.y = 0f;
                cs[0].vel += jumpdir * 8f * jumpFactor;
                for (int i = 1; i < cc; i++)
                {
                    cs[i].vel.y = 0f;
                    cs[i].vel += jumpdir * 7f * jumpFactor;
                }
                this.forceInputDir = new IntVector2((int)Mathf.Sign(jumpdir.x), input[0].y);
                this.forceInputCounter = 10;
                this.forceJump = 6;
                creature.room.PlaySound(SoundID.Slugcat_Wall_Jump, mainBodyChunk, false, 1f, 1f);
                canWallJump = 0;
            }
            else if (canWaterJump > 0)
            {
                RainMeadow.Debug("water jump");
                OnJump();
                this.jumpBoost = 8f;
                var jumpdir = (cs[0].pos - cs[1].pos).normalized + inputDir;
                for (int i = 0; i < cc; i++)
                {
                    cs[i].vel *= 0.4f;
                    cs[i].vel += jumpdir * 8f * jumpFactor;
                }
                creature.room.PlaySound(SoundID.Slugcat_Wall_Jump, mainBodyChunk, false, 1f, 1f);
                canWaterJump = 0;
                canGroundJump = 0;
            }
            else if (canClimbJump > 0)
            {
                RainMeadow.Debug("climb jump");
                OnJump();
                this.jumpBoost = 3f;
                var jumpdir = (cs[0].pos - cs[1].pos).normalized + inputDir;
                for (int i = 0; i < cc; i++)
                {
                    cs[0].vel *= 0.4f;
                    cs[i].vel += jumpdir * 3f * jumpFactor;
                }
                creature.room.PlaySound(SoundID.Slugcat_Wall_Jump, mainBodyChunk, false, 1f, 1f);
                canClimbJump = 0;
            }
            else
            {
                RainMeadow.Debug("Unhandled jump");
            }
        }

        protected bool TileAccessible(Room room, IntVector2 pos)
        {
            // skip dumb checks
            AItile aitile = room.aimap.getAItile(pos);
            return template.AccessibilityResistance(aitile.acc).Allowed || (aitile.acc != AItile.Accessibility.Solid && (aitile.AnyWater || room.gravity < zeroGTreshold));
        }

        internal override bool FindDestination(WorldCoordinate basecoord, out WorldCoordinate toPos, out float magnitude)
        {
            if (base.FindDestination(basecoord, out toPos, out magnitude)) return true;

            var room = creature.room;
            var chunks = creature.bodyChunks;
            var nc = chunks.Length;

            var template = creature.Template;

            bool localTrace = Input.GetKey(KeyCode.L);

            toPos = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(this.inputDir.normalized * 1.42f));
            magnitude = 0.5f;
            var previousAccessibility = room.aimap.getAItile(basecoord).acc;

            var currentTile = room.GetTile(basecoord);
            var currentAiTile = room.aimap.getAItile(toPos);
            //var currentAccessibility = currentAiTile.acc;
            //var currentLegality = template.AccessibilityResistance(currentAccessibility).legality;
            //void NewTile(WorldCoordinate newPos, out AItile currentAiTile, out AItile.Accessibility currentAccessibility, out PathCost.Legality currentLegality)
            //{
            //    currentAiTile = room.aimap.getAItile(newPos);
            //    currentAccessibility = currentAiTile.acc;
            //    currentLegality = template.AccessibilityResistance(currentAccessibility).legality;
            //}

            if (localTrace) RainMeadow.Debug($"moving from {basecoord.Tile} towards {toPos.Tile}");
            
            if (this.forceJump > 0) // jumping
            {
                this.MovementOverride(new MovementConnection(MovementConnection.MovementType.Standard, basecoord, toPos, 2));
                return true;
            }

            // problematic when climbing
            if (this.input[0].y > 0)
            {
                var tile0 = creature.room.GetTile(chunks[0].pos);
                var tile1 = creature.room.GetTile(chunks[1].pos);
                if (tile0.AnyBeam && !tile0.DeepWater && !HasFooting)
                {
                    RainMeadow.Debug("grip!");
                    GripPole(tile0);
                }
                else if (tile1.AnyBeam && !tile1.DeepWater && !HasFooting)
                {
                    RainMeadow.Debug("grip!");
                    GripPole(tile1);
                }
            }

            // climb to beam
            bool climbing = false;
            if (this.input[0].y > 0 && (previousAccessibility <= AItile.Accessibility.Climb || currentTile.WaterSurface) && !currentTile.DeepWater)
            {
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
                        magnitude = 1f;
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
                            magnitude = 1f;
                            break;
                        }
                    }
                }
            }

            if (!climbing)
            {
                
                if (this.input[0].x != 0) // to sides
                {
                    if (localTrace) RainMeadow.Debug("sides");
                    if (template.AccessibilityResistance(currentAiTile.acc).legality <= PathCost.Legality.Unwanted)
                    {
                        if (localTrace) RainMeadow.Debug("ahead");
                    }
                    else if (TileAccessible(room, toPos.Tile + new IntVector2(0, 1))) // try up
                    {
                        if (localTrace) RainMeadow.Debug("up");
                        toPos = WorldCoordinate.AddIntVector(toPos, new IntVector2(0, 1));
                        currentAiTile = room.aimap.getAItile(toPos);
                    }
                    else if (TileAccessible(room, toPos.Tile + new IntVector2(0, -1))) // try down
                    {
                        if (localTrace) RainMeadow.Debug("down");
                        toPos = WorldCoordinate.AddIntVector(toPos, new IntVector2(0, -1));
                        currentAiTile = room.aimap.getAItile(toPos);
                    }

                    if (inputDir.magnitude > 0.75f)
                    {
                        // if can reach further out, it goes faster and smoother
                        WorldCoordinate furtherOut = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(this.inputDir.normalized * 3f));
                        if (TileAccessible(room, furtherOut.Tile) && QuickConnectivity.Check(room, creature.Template, basecoord.Tile, furtherOut.Tile, 6) > 0)
                        {
                            if (localTrace) RainMeadow.Debug("reaching further");
                            toPos = furtherOut;
                            magnitude = 1f;
                            currentAiTile = room.aimap.getAItile(toPos);
                        }
                        else if (TileAccessible(room, furtherOut.Tile + new IntVector2(0, 1)) && QuickConnectivity.Check(room, creature.Template, basecoord.Tile, furtherOut.Tile + new IntVector2(0, 1), 6) > -1)
                        {
                            if (localTrace) RainMeadow.Debug("further up");
                            toPos = WorldCoordinate.AddIntVector(furtherOut, new IntVector2(0, 1));
                            magnitude = 1f;
                            currentAiTile = room.aimap.getAItile(toPos);
                        }
                        else if (TileAccessible(room, furtherOut.Tile + new IntVector2(0, -1)) && QuickConnectivity.Check(room, creature.Template, basecoord.Tile, furtherOut.Tile + new IntVector2(0, -1), 6) > -1)
                        {
                            if (localTrace) RainMeadow.Debug("further down");
                            toPos = WorldCoordinate.AddIntVector(furtherOut, new IntVector2(0, -1));
                            magnitude = 1f;
                            currentAiTile = room.aimap.getAItile(toPos); ;
                        }
                    }
                }
                else
                {
                    if (localTrace) RainMeadow.Debug("vertical");

                    if (template.AccessibilityResistance(currentAiTile.acc).legality <= PathCost.Legality.Unwanted)
                    {
                        if (localTrace) RainMeadow.Debug("ahead");
                    }
                    else if (TileAccessible(room, toPos.Tile + new IntVector2(1, 0))) // right
                    {
                        if (localTrace) RainMeadow.Debug("right");
                        toPos = WorldCoordinate.AddIntVector(toPos, new IntVector2(1, 0));
                        currentAiTile = room.aimap.getAItile(toPos);
                    }
                    else if (TileAccessible(room, toPos.Tile + new IntVector2(-1, 0))) // left
                    {
                        if (localTrace) RainMeadow.Debug("left");
                        toPos = WorldCoordinate.AddIntVector(toPos, new IntVector2(-1, 0));
                        currentAiTile = room.aimap.getAItile(toPos);
                    }
                    if (inputDir.magnitude > 0.75f)
                    {
                        WorldCoordinate furtherOut = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(this.inputDir.normalized * 2.2f));
                        if (!room.GetTile(toPos).Solid && !room.GetTile(furtherOut).Solid && TileAccessible(room, furtherOut.Tile)) // ahead unblocked, move further
                        {
                            if (localTrace) RainMeadow.Debug("reaching");
                            toPos = furtherOut;
                            magnitude = 1f;
                            currentAiTile = room.aimap.getAItile(toPos);
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
                //break;
            }
            var pathFinder = creature.abstractCreature.abstractAI.RealAI.pathFinder;
            if (pathFinder.PathingCellAtWorldCoordinate(basecoord).reachable 
                && pathFinder.PathingCellAtWorldCoordinate(toPos).reachable 
                && QuickConnectivity.Check(room, creature.Template, basecoord.Tile, toPos.Tile, 12) > -1) // found and pathfinder-pathable
            {
                if (localTrace) RainMeadow.Debug("pathable");
                return true;
            }
            else
            { 
                // no pathing
                if (localTrace) RainMeadow.Debug("unpathable");
                pathFinder.OutOfElement();

                if (room.aimap.getAItile(toPos).acc < AItile.Accessibility.Solid // no go
                    && (
                        currentAiTile.DeepWater // on water
                        ||
                        climbing // climbing target
                        ||( // otherwise
                            !IsOnPole // don't let go of pole mode
                            && (input[0].y != 1 || input[0].x != 0) // not straight up
                            )
                    ) 
                )
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

            if (HasFooting)
            {
                if (IsOnCorridor)
                {
                    if (localTrace) RainMeadow.Debug("can corridor boost");
                    this.canCorridorBoost = 5;
                }
                else if (IsOnGround)
                {
                    if (localTrace) RainMeadow.Debug("can ground jump");
                    this.canGroundJump = 5;
                }
                else if (IsOnPole)
                {
                    if (localTrace) RainMeadow.Debug("can pole jump");
                    this.canPoleJump = 5;
                }
                else if (IsOnClimb)
                {
                    if (localTrace) RainMeadow.Debug("can climb jump");
                    this.canClimbJump = 5;
                }
            }
            var wallValue = OnWall;
            if (!WallClimber && wallValue != 0 && wallValue == input[0].x)
            {
                if (localTrace) RainMeadow.Debug("can walljump");
                this.canWallJump = 5 * wallValue;
            }
            if (IsOnWaterSurface)
            {
                if (localTrace) RainMeadow.Debug("can water jump");
                this.canWaterJump = 5;
            }
            

            if (this.CanPounce)
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

            if (this.wantToJump > 0 && (this.canClimbJump > 0 || this.canPoleJump > 0 || this.canGroundJump > 0 || this.canWallJump != 0 || canCorridorBoost > 0 || canWaterJump > 0))
            {
                if (localTrace) RainMeadow.Debug("jumping");
                this.Jump();
                this.wantToJump = 0;
            }

            if (this.jumpBoost > 0f && (this.input[0].jmp || this.forceBoost > 0))
            {
                this.jumpBoost -= 1.5f;
                var chunks = creature.bodyChunks;
                var nc = chunks.Length;
                if (IsOnCorridor)
                {
                    chunks[0].vel += (this.jumpBoost + 1f) * 0.3f * RWCustom.Custom.DirVec(chunks[1].pos, chunks[0].pos);
                }
                else
                {
                    chunks[0].vel.y += (this.jumpBoost + 1f) * 0.3f;
                    for (int i = 1; i < nc; i++)
                    {
                        chunks[i].vel.y += (this.jumpBoost + 1f) * 0.25f;
                    }
                }
            }
            else
            {
                this.jumpBoost = 0f;
            }

            if (mcd.moveSpeed > 0f)
            {
                var mainchunk = creature.mainBodyChunk;
                if (inputDir.x != 0 && (Mathf.Abs(mainchunk.vel.x) < runSpeed || Mathf.Sign(mainchunk.vel.x) != Mathf.Sign(inputDir.x)))
                {
                    mainchunk.vel.x += 0.75f * inputDir.x * mcd.moveSpeed;
                }
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

            if (this.canWallJump > 0) this.canWallJump--;
            else if (this.canWallJump < 0) this.canWallJump++;
            if (this.canClimbJump > 0) this.canClimbJump--;
            if (this.canPoleJump > 0) this.canPoleJump--;
            if (this.canGroundJump > 0) this.canGroundJump--;
            if (this.canCorridorBoost > 0) this.canCorridorBoost--;
            if (this.canWaterJump > 0) this.canWaterJump--;

            if (this.forceJump > 0) this.forceJump--;
            if (this.forceBoost > 0) this.forceBoost--;
        }
    }
}
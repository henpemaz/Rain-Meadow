using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public partial class MeadowCustomization
    {
        public static void EnableLizard()
        {
            On.Lizard.Update += Lizard_Update;
            On.Lizard.Act += Lizard_Act;
            On.LizardAI.Update += LizardAI_Update;
            On.LizardPather.FollowPath += LizardPather_FollowPath;

            On.Lizard.GripPointBehavior += Lizard_GripPointBehavior;
            On.Lizard.SwimBehavior += Lizard_SwimBehavior;
            On.Lizard.FollowConnection += Lizard_FollowConnection;

            // color
            On.LizardGraphics.ctor += LizardGraphics_ctor;
        }

        private static void LizardGraphics_ctor(On.LizardGraphics.orig_ctor orig, LizardGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            if (creatureCustomizations.TryGetValue(ow as Creature, out var c))
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
            if (creatureController.TryGetValue(self.creature, out var p))
            {
                p.AIUpdate(self);
            }
            else
            {
                orig(self);
            }
        }

        // might need a new hookpoint between movement planing and movement acting
        private static void Lizard_Act(On.Lizard.orig_Act orig, Lizard self)
        {
            if (creatureController.TryGetValue(self.abstractCreature, out var p))
            {
                p.ConsciousUpdate();

                // todo JUMP
                var room = self.room;
                var chunks = self.bodyChunks;
                var nc = chunks.Length;

                // move
                //var basepos = 0.5f * (self.bodyChunks[0].pos + self.bodyChunks[1].pos);
                var basepos = self.bodyChunks[0].pos;
                var basecoord = self.room.GetWorldCoordinate(basepos);
                if (p.inputDir != Vector2.zero)
                {
                    self.AI.behavior = LizardAI.Behavior.Travelling;
                    self.AI.runSpeed = Custom.LerpAndTick(self.AI.runSpeed, 0.8f, 0.2f, 0.05f);
                    self.AI.excitement = Custom.LerpAndTick(self.AI.excitement, 0.5f, 0.1f, 0.05f);

                    var toPos = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(p.inputDir * 1.42f));

                    if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"moving towards {toPos.Tile}");
                    if (p.inputDir.x != 0) // to sides
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
                        var furtherOut = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(p.inputDir * 2.84f));
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
                            toPos = WorldCoordinate.AddIntVector(basecoord, IntVector2.FromVector2(p.inputDir * 2.2f));
                        }
                    }

                    if ((self.graphicsModule as LizardGraphics)?.frontLegsGrabbing > 0 && room.aimap.TileAccessibleToCreature(toPos.Tile, self.Template))
                    {
                        var inDirection = Vector2.Dot(self.mainBodyChunk.vel, p.inputDir);
                        if (inDirection < 2)
                        {
                            self.mainBodyChunk.vel += p.inputDir * (2 - inDirection);
                        }
                    }

                    if (toPos != self.abstractCreature.abstractAI.destination)
                    {
                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"new destination {toPos.Tile}");
                        p.ForceAIDestination(toPos);
                    }
                }
                else
                {
                    self.AI.behavior = LizardAI.Behavior.Idle;
                    self.AI.runSpeed = Custom.LerpAndTick(self.AI.runSpeed, 0, 0.4f, 0.1f);
                    self.AI.excitement = Custom.LerpAndTick(self.AI.excitement, 0.2f, 0.1f, 0.05f);

                    for (int i = 0; i < 3; i++)
                    {
                        if (self.IsTileSolid(i, 0, -1))
                        {
                            BodyChunk bodyChunk = self.bodyChunks[i];
                            bodyChunk.vel.y -= 0.1f;
                        }
                    }
                    if (p.inputDir == Vector2.zero && p.inputLastDir != Vector2.zero) // let go
                    {
                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"resting at {basecoord.Tile}");
                        p.ForceAIDestination(basecoord);
                    }
                }
                if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"current AI destination {p.creature.abstractCreature.abstractAI.destination}");
            }
            orig(self);
        }

        private static MovementConnection LizardPather_FollowPath(On.LizardPather.orig_FollowPath orig, LizardPather self, WorldCoordinate originPos, int? bodyDirection, bool actuallyFollowingThisPath)
        {
            if (creatureController.TryGetValue(self.creature, out var p))
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
            if (creatureController.TryGetValue(self.abstractCreature, out var p))
            {
                p.Update(eu);
            }

            orig(self, eu);
        }

        private class LizardController : CreatureController
        {
            public LizardController(Lizard creature, int playerNumber) : base(creature, playerNumber) { }

            public Lizard lizard => creature as Lizard;

            public override bool GrabImpl(PhysicalObject pickUpCandidate)
            {
                foreach (BodyChunk chunk in creature.bodyChunks)
                {
                    if (Custom.DistLess(creature.mainBodyChunk.pos + Custom.DirVec(creature.bodyChunks[1].pos, creature.mainBodyChunk.pos) * lizard.lizardParams.biteInFront, chunk.pos, chunk.rad + lizard.lizardParams.biteRadBonus))
                    {
                        lizard.biteControlReset = false;
                        lizard.JawOpen = 0f;
                        lizard.lastJawOpen = 0f;


                        chunk.vel += creature.mainBodyChunk.vel * Mathf.Lerp(creature.mainBodyChunk.mass, 1.1f, 0.5f) / Mathf.Max(1f, chunk.mass);
                        creature.Grab(chunk.owner, 0, chunk.index, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, lizard.lizardParams.biteDominance * UnityEngine.Random.value, overrideEquallyDominant: true, pacifying: true);

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
                }
                return false;
            }
        }
    }
}

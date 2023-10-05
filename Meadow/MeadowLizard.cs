using static MonoMod.InlineRT.MonoModRule;
using UnityEngine;
using System;

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

        }

        private static void Lizard_FollowConnection(On.Lizard.orig_FollowConnection orig, Lizard self, float runSpeed)
        {
            if (Input.GetKey(KeyCode.L)) RainMeadow.DebugMe();
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

                // todo
                var room = self.room;
                var chunks = self.bodyChunks;
                var nc = chunks.Length;

                // move
                var basepos = 0.5f * (self.firstChunk.pos + room.MiddleOfTile(self.abstractCreature.pos.Tile));
                if (p.inputDir != Vector2.zero)
                {
                    
                    self.AI.behavior = LizardAI.Behavior.Travelling;
                    self.AI.runSpeed = RWCustom.Custom.LerpAndTick(self.AI.runSpeed, 0.8f, 0.2f, 0.05f);
                    self.AI.excitement = RWCustom.Custom.LerpAndTick(self.AI.excitement, 0.5f, 0.1f, 0.05f);
                    var dest = basepos + p.inputDir * 40f;
                    var destCoord = self.room.GetWorldCoordinate(dest);
                    if(destCoord != self.abstractCreature.abstractAI.destination)
                    {
                        self.AI.pathFinder.AbortCurrentGenerationPathFinding(); // ignore previous dest
                        self.abstractCreature.abstractAI.SetDestination(self.room.GetWorldCoordinate(dest));
                    }
                }
                else
                {
                    self.AI.behavior = LizardAI.Behavior.Idle;
                    self.AI.runSpeed = RWCustom.Custom.LerpAndTick(self.AI.runSpeed, 0, 0.4f, 0.1f);
                    self.AI.excitement = RWCustom.Custom.LerpAndTick(self.AI.excitement, 0.2f, 0.1f, 0.05f);

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
                        self.abstractCreature.abstractAI.SetDestination(self.room.GetWorldCoordinate(basepos));
                    }
                }
            }
            orig(self);
        }

        private static MovementConnection LizardPather_FollowPath(On.LizardPather.orig_FollowPath orig, LizardPather self, WorldCoordinate originPos, int? bodyDirection, bool actuallyFollowingThisPath)
        {
            if (creatureController.TryGetValue(self.creature, out var p))
            {
                if (actuallyFollowingThisPath)
                {
                    // path will be null if calculating from an inaccessible tile
                    // lizard has code to "pick a nearby accessible tile to calculate from"
                    // todo always use main pos for pathing?

                    // path will be always a path "out" of the current cell, even if current cell is destination
                    // should be fine as long as runspeed = 0, just gotta not fall for it here
                    bool needOverride;
                    var path = orig(self, originPos, bodyDirection, actuallyFollowingThisPath);

                    if (path == null && p.inputDir != Vector2.zero)
                    {
                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("wants to move but can't");
                        // todo generate connection
                    }
                    else if (path != null && p.inputDir == Vector2.zero)
                    {
                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug("doesn't want to move but is following path");
                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"{originPos} - {bodyDirection} - {path} - {path.distance}");
                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"{p.creature.abstractCreature.pos} - {p.creature.abstractCreature.abstractAI.destination}");
                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"{p.creature.mainBodyChunk.pos} - {p.creature.room.GetWorldCoordinate(p.creature.mainBodyChunk.pos)}");
                        if (Input.GetKey(KeyCode.L)) RainMeadow.Debug($"{self.destination} - {self.nextDestination}");
                        //if (Input.GetKey(KeyCode.L)) RainMeadow.Debug(Environment.StackTrace);
                    }
                    else if (path != null && p.inputDir != Vector2.zero)
                    {
                        // todo prevent moving in the wrong direction
                        // dot product of input * move should > 0
                    }

                    return path;
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

        class LizardController : CreatureController
        {
            public LizardController(Lizard creature, int playerNumber) : base(creature, playerNumber) { }

            public Lizard lizard => creature as Lizard;

            public override bool GrabImpl(PhysicalObject pickUpCandidate)
            {
                foreach (BodyChunk chunk in creature.bodyChunks)
                {
                    if (RWCustom.Custom.DistLess(creature.mainBodyChunk.pos + RWCustom.Custom.DirVec(creature.bodyChunks[1].pos, creature.mainBodyChunk.pos) * lizard.lizardParams.biteInFront, chunk.pos, chunk.rad + lizard.lizardParams.biteRadBonus))
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

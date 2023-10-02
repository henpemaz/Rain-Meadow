using static MonoMod.InlineRT.MonoModRule;
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
                    self.AI.pathFinder.AbortCurrentGenerationPathFinding(); // ignore previous dest
                    self.AI.behavior = LizardAI.Behavior.Travelling;
                    self.AI.runSpeed = 1; // todo lerp
                    var dest = basepos + p.inputDir * 40f;
                    self.abstractCreature.abstractAI.SetDestination(self.room.GetWorldCoordinate(dest));
                }
                else
                {
                    self.AI.behavior = LizardAI.Behavior.Idle;
                    if (p.inputDir == Vector2.zero && p.inputLastDir != Vector2.zero) // let go
                    {
                        self.abstractCreature.abstractAI.SetDestination(self.room.GetWorldCoordinate(basepos));
                    }
                }
            }

            orig(self);
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
            public LizardController(Lizard creature, int playerNumber) : base(creature, playerNumber)
            {
            }

            protected Lizard lizard => creature as Lizard;

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

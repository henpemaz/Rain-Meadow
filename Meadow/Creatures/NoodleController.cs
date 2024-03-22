using UnityEngine;
using RWCustom;

namespace RainMeadow
{
    class NoodleController : AirCreatureController
    {
        private bool actLock; // act is hooked both at base and an override
        private NeedleWorm noodle => creature as NeedleWorm;

        public static void EnableNoodle()
        {
            On.BigNeedleWorm.Update += BigNeedleWorm_Update;
            On.NeedleWorm.Act += NeedleWorm_Act;
            On.BigNeedleWormAI.Update += BigNeedleWormAI_Update;

            On.SmallNeedleWorm.Update += SmallNeedleWorm_Update;
            On.SmallNeedleWorm.Act += SmallNeedleWorm_Act;
            On.SmallNeedleWormAI.Update += SmallNeedleWormAI_Update;
        }

        internal override bool FindDestination(WorldCoordinate basecoord, out WorldCoordinate toPos, out float magnitude)
        {
            if (base.FindDestination(basecoord, out toPos, out magnitude)) return true;
            var basepos = 0.5f * (creature.firstChunk.pos + creature.room.MiddleOfTile(creature.abstractCreature.pos.Tile));
            var dest = basepos + this.inputDir * 80f;
            if (noodle.flying > 0) dest.y -= 12f; // nose up goes funny
            if (Mathf.Abs(this.inputDir.y) < 0.1f) // trying to move horizontally, compensate for momentum a bit
            {
                dest.y -= creature.mainBodyChunk.vel.y * 2f;
            }
            toPos = creature.room.GetWorldCoordinate(dest);
            magnitude = inputDir.magnitude;
            return true;
        }

        private static void SmallNeedleWormAI_Update(On.SmallNeedleWormAI.orig_Update orig, SmallNeedleWormAI self)
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

        private static void SmallNeedleWorm_Act(On.SmallNeedleWorm.orig_Act orig, SmallNeedleWorm self)
        {
            if (creatureControllers.TryGetValue(self.abstractCreature, out var p))
            {
                (p as NoodleController).actLock = true;
                p.ConsciousUpdate();
                (p as NoodleController).actLock = false;
            }

            orig(self);
        }

        private static void SmallNeedleWorm_Update(On.SmallNeedleWorm.orig_Update orig, SmallNeedleWorm self, bool eu)
        {
            if (creatureControllers.TryGetValue(self.abstractCreature, out var p))
            {
                p.Update(eu);
            }

            orig(self, eu);
        }

        private static void NeedleWorm_Act(On.NeedleWorm.orig_Act orig, NeedleWorm self)
        {
            if (creatureControllers.TryGetValue(self.abstractCreature, out var p) && !(p as NoodleController).actLock)
            {
                p.ConsciousUpdate();
            }

            orig(self);
        }

        private static void BigNeedleWormAI_Update(On.BigNeedleWormAI.orig_Update orig, BigNeedleWormAI self)
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

        private static void BigNeedleWorm_Update(On.BigNeedleWorm.orig_Update orig, BigNeedleWorm self, bool eu)
        {
            if (creatureControllers.TryGetValue(self.abstractCreature, out var p))
            {
                p.Update(eu);
            }

            orig(self, eu);
        }

        public NoodleController(Creature creature, OnlineCreature oc, int playerNumber) : base(creature, oc, playerNumber)
        {
        }

        public override bool GrabImpl(PhysicalObject pickUpCandidate)
        {
            // todo
            return false;
        }

        protected override void LookImpl(Vector2 pos)
        {
            //noodle.lookDir = (pos - creature.DangerPos).normalized;
            //noodle.getToLookDir = noodle.lookDir;
        }

        protected override void Moving(float magnitude)
        {
            noodle.AI.behavior = NeedleWormAI.Behavior.GetUnstuck;
            noodle.AI.flySpeed = Custom.LerpAndTick(noodle.AI.flySpeed, 0.8f * magnitude, 0.2f, 0.05f);
        }

        protected override void Resting()
        {
            noodle.AI.behavior = NeedleWormAI.Behavior.Idle;
            noodle.AI.flySpeed = Custom.LerpAndTick(noodle.AI.flySpeed, 0, 0.4f, 0.1f);
        }
    }
}

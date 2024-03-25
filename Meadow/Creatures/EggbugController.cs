using RWCustom;
using System;
using UnityEngine;

namespace RainMeadow
{
    public class EggbugController : GroundCreatureController
    {
        public static void EnableEggbug()
        {
            On.EggBug.Update += EggBug_Update;
            On.EggBug.Act += EggBug_Act;

            On.EggBugAI.Update += EggBugAI_Update;
            On.EggBug.Run += EggBug_Run;
        }

        private static void EggBug_Run(On.EggBug.orig_Run orig, EggBug self, MovementConnection followingConnection)
        {
            if (creatureControllers.TryGetValue(self.abstractCreature, out var c))
            {
                if (followingConnection.startCoord == self.abstractCreature.abstractAI.destination)
                    return;
            }

            orig(self, followingConnection);
        }

        private static void EggBugAI_Update(On.EggBugAI.orig_Update orig, EggBugAI self)
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

        private static void EggBug_Act(On.EggBug.orig_Act orig, EggBug self)
        {
            if (creatureControllers.TryGetValue(self.abstractCreature, out var p))
            {
                p.ConsciousUpdate();
            }

            orig(self);
        }

        private static void EggBug_Update(On.EggBug.orig_Update orig, EggBug self, bool eu)
        {
            if (creatureControllers.TryGetValue(self.abstractCreature, out var p))
            {
                p.Update(eu);
            }

            orig(self, eu);
        }

        public EggbugController(EggBug creature, OnlineCreature oc, int playerNumber) : base(creature, oc, playerNumber)
        {
            if (creature.grasps == null) creature.grasps = new Creature.Grasp[1];
        }

        EggBug eggbug => creature as EggBug;

        public override bool HasFooting => true;

        public override bool CanClimbJump => false;

        public override bool CanPoleJump => false;

        public override bool CanGroundJump => true;

        public override bool GrabImpl(PhysicalObject pickUpCandidate)
        {
            return false;
        }

        protected override void GripPole(Room.Tile tile0)
        {
            
        }

        protected override void JumpImpl()
        {
            
        }

        protected override void MovementOverride(MovementConnection movementConnection)
        {
            
        }

        protected override void ClearMovementOverride()
        {
            
        }

        protected override void LookImpl(Vector2 pos)
        {
            // ???
        }

        protected override void Moving(float magnitude)
        {
            eggbug.runSpeed = Custom.LerpAndTick(eggbug.runSpeed, magnitude, 0.2f, 0.05f);
        }

        protected override void Resting()
        {
            eggbug.runSpeed = Custom.LerpAndTick(eggbug.runSpeed, 0, 0.4f, 0.1f);
        }
    }
}

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
            On.EggBug.Swim += EggBug_Swim;

            On.EggBugAI.Update += EggBugAI_Update;
            On.EggBug.Run += EggBug_Run;
        }

        private static void EggBug_Swim(On.EggBug.orig_Swim orig, EggBug self)
        {
            if (creatureControllers.TryGetValue(self, out var p))
            {
                p.ConsciousUpdate();
            }
            orig(self);
        }

        private static void EggBug_Run(On.EggBug.orig_Run orig, EggBug self, MovementConnection followingConnection)
        {
            if (creatureControllers.TryGetValue(self, out var c))
            {
                if (followingConnection.startCoord == self.abstractCreature.abstractAI.destination)
                    return;
            }
            orig(self, followingConnection);
        }

        private static void EggBugAI_Update(On.EggBugAI.orig_Update orig, EggBugAI self)
        {
            if (creatureControllers.TryGetValue(self.creature.realizedCreature, out var p))
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
            if (creatureControllers.TryGetValue(self, out var p))
            {
                p.ConsciousUpdate();
            }
            orig(self);
        }

        private static void EggBug_Update(On.EggBug.orig_Update orig, EggBug self, bool eu)
        {
            if (creatureControllers.TryGetValue(self, out var p))
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

        public override bool HasFooting => eggbug.Footing;
        public override bool OnGround => IsTileGround(0, 0, -1) || IsTileGround(1, 0, -1);
        public override bool OnPole => HasFooting && !OnGround && GetTile(0).AnyBeam;
        public override bool OnCorridor => eggbug.currentlyClimbingCorridor;

        protected override void GripPole(Room.Tile tile0)
        {
            
        }

        protected override void OnJump()
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

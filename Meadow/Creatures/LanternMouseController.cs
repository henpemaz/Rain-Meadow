using UnityEngine;

namespace RainMeadow
{
    public class LanternMouseController : GroundCreatureController
    {
        public static void EnableMouse()
        {
            On.LanternMouse.Update += LanternMouse_Update;
            On.LanternMouse.Act += LanternMouse_Act;

            On.MouseAI.Update += MouseAI_Update; ;
        }

        private static void MouseAI_Update(On.MouseAI.orig_Update orig, MouseAI self)
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

        private static void LanternMouse_Act(On.LanternMouse.orig_Act orig, LanternMouse self)
        {
            if (creatureControllers.TryGetValue(self.abstractCreature, out var p))
            {
                p.ConsciousUpdate();
            }
            orig(self);
        }

        private static void LanternMouse_Update(On.LanternMouse.orig_Update orig, LanternMouse self, bool eu)
        {
            if (creatureControllers.TryGetValue(self.abstractCreature, out var p))
            {
                p.Update(eu);
            }
            orig(self, eu);
        }

        private readonly LanternMouse mouse;

        public LanternMouseController(LanternMouse mouse, OnlineCreature oc, int playerNumber) : base(mouse, oc, playerNumber)
        {
            this.mouse = mouse;
        }

        public override bool HasFooting => mouse.Footing;

        public override bool CanClimbJump => !mouse.Footing;

        public override bool CanPoleJump => !mouse.Footing;

        public override bool CanGroundJump => mouse.Footing;

        public override bool GrabImpl(PhysicalObject pickUpCandidate)
        {
            //throw new NotImplementedException();
            return false;
        }

        protected override void ClearMovementOverride()
        {
            //throw new NotImplementedException();
        }

        protected override void GripPole(Room.Tile tile0)
        {
            //throw new NotImplementedException();
        }

        protected override void JumpImpl()
        {
            //throw new NotImplementedException();
        }

        protected override void LookImpl(Vector2 pos)
        {
            (mouse.graphicsModule as MouseGraphics).lookDir = (mouse.mainBodyChunk.pos - pos) / 500f;
        }

        protected override void MovementOverride(MovementConnection movementConnection)
        {
            //throw new NotImplementedException();
        }

        protected override void Moving(float magnitude)
        {
            //throw new NotImplementedException();
        }

        protected override void Resting()
        {
            //throw new NotImplementedException();
        }
    }
}

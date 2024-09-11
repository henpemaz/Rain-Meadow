using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public class LanternMouseController : GroundCreatureController
    {
        public static void EnableMouse()
        {
            On.LanternMouse.Update += LanternMouse_Update;
            On.LanternMouse.Act += LanternMouse_Act;
            On.LanternMouse.Hang += LanternMouse_Hang;

            On.MouseAI.Update += MouseAI_Update;
            On.MouseAI.DangleTile += MouseAI_DangleTile; // no dangling

        }

        private static MouseAI.Dangle? MouseAI_DangleTile(On.MouseAI.orig_DangleTile orig, MouseAI self, IntVector2 tile, bool noAccessMap)
        {
            if (creatureControllers.TryGetValue(self.creature.realizedCreature, out var p))
            {
                return null;
            }
            return orig(self, tile, noAccessMap);
        }

        private static void MouseAI_Update(On.MouseAI.orig_Update orig, MouseAI self)
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

        private static void LanternMouse_Hang(On.LanternMouse.orig_Hang orig, LanternMouse self)
        {
            if (creatureControllers.TryGetValue(self, out var p))
            {
                p.ConsciousUpdate();
            }
            orig(self);
        }

        private static void LanternMouse_Act(On.LanternMouse.orig_Act orig, LanternMouse self)
        {
            if (creatureControllers.TryGetValue(self, out var p))
            {
                p.ConsciousUpdate();
            }
            orig(self);
        }

        private static void LanternMouse_Update(On.LanternMouse.orig_Update orig, LanternMouse self, bool eu)
        {
            if (creatureControllers.TryGetValue(self, out var p))
            {
                p.Update(eu);
            }
            orig(self, eu);
        }

        private readonly LanternMouse mouse;

        public LanternMouseController(LanternMouse mouse, OnlineCreature oc, int playerNumber, MeadowAvatarCustomization customization) : base(mouse, oc, playerNumber, customization)
        {
            this.mouse = mouse;
            jumpFactor = 1.5f; // y u so smol
        }

        public override bool HasFooting => mouse.Footing;

        public override bool OnPole => GetTile(0).AnyBeam;

        public override bool OnGround => IsTileGround(0, 0, -1) || IsTileGround(1, 0, -1);

        public override bool OnCorridor => mouse.currentlyClimbingCorridor;

        
        protected override void GripPole(Room.Tile tile0)
        {
            if (mouse.footingCounter < 10)
            {
                creature.room.PlaySound(SoundID.Mouse_Scurry, creature.mainBodyChunk);
                for (int i = 0; i < creature.bodyChunks.Length; i++)
                {
                    creature.bodyChunks[i].vel *= 0.25f;
                }
                creature.mainBodyChunk.vel += 0.2f * (creature.room.MiddleOfTile(tile0.X, tile0.Y) - creature.mainBodyChunk.pos);
                mouse.footingCounter = 10;
            }
        }

        protected override void OnJump()
        {
            mouse.footingCounter = 0;
        }

        protected override void LookImpl(Vector2 pos)
        {
            (mouse.graphicsModule as MouseGraphics).lookDir = (mouse.mainBodyChunk.pos - pos) / 500f;
        }

        protected override void MovementOverride(MovementConnection movementConnection)
        {
            mouse.specialMoveCounter = 15;
            mouse.specialMoveDestination = movementConnection.DestTile;
        }

        protected override void ClearMovementOverride()
        {
            mouse.specialMoveCounter = 0;
        }

        protected override void Moving(float magnitude)
        {
            mouse.runSpeed = Custom.LerpAndTick(mouse.runSpeed, magnitude, 0.2f, 0.05f);
        }

        protected override void Resting()
        {
            mouse.runSpeed = Custom.LerpAndTick(mouse.runSpeed, 0, 0.4f, 0.1f);
        }

        internal override void ConsciousUpdate()
        {
            base.ConsciousUpdate();
            if (mouse.specialMoveCounter > 0 && !mouse.room.aimap.TileAccessibleToCreature(mouse.mainBodyChunk.pos, mouse.Template) && !mouse.room.aimap.TileAccessibleToCreature(mouse.bodyChunks[1].pos, mouse.Template))
            {
                mouse.footingCounter = 0;
            }

            if(superLaunchJump > 10 && (mouse.room.aimap.getAItile(mouse.bodyChunks[1].pos).acc == AItile.Accessibility.Floor && !mouse.IsTileSolid(0, 0, 1) && !mouse.IsTileSolid(1, 0, 1)))
            {
                // undo sitting
                mouse.sitting = false;
                mouse.profileFac = Mathf.Sign(mouse.profileFac);
                mouse.mainBodyChunk.vel.y -= 3f;
                mouse.bodyChunks[1].vel.y += 3f;
                mouse.mainBodyChunk.vel.x += mouse.profileFac;
                mouse.bodyChunks[1].vel.x -= mouse.profileFac;
            }

            mouse.voiceCounter = 10; // shh
        }

        protected override void OnCall()
        {
            if (mouse.graphicsModule is MouseGraphics mg)
            {
                mg.head.vel += Custom.DirVec(mouse.bodyChunks[1].pos, mouse.bodyChunks[0].pos);
                mg.ouchEyes = Mathf.Max(12, mg.ouchEyes);

                if (voice.Display)
                {
                    int amount = Mathf.FloorToInt(Mathf.Lerp(2, 4, voice.Volume));
                    for (int i = 0; i < amount; i++)
                    {
                        this.mouse.room.AddObject(new MouseSpark(this.mouse.mainBodyChunk.pos, this.mouse.mainBodyChunk.vel + Custom.DegToVec(360f * Random.value) * 6f * Random.value, 40f, mg.BodyColor));
                    }
                }
            }
        }
    }
}

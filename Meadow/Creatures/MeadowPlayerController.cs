using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowPlayerController : CreatureController
    {
        private Player player;

        public static void Enable()
        {
            On.Player.Update += Player_Update;
            On.Player.MovementUpdate += Player_MovementUpdate;

            On.ShortcutHelper.ctor += ShortcutHelper_ctor;
        }

        private static void ShortcutHelper_ctor(On.ShortcutHelper.orig_ctor orig, ShortcutHelper self, Room room)
        {
            orig(self, room);
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                for (int i = 0; i < room.shortcuts.Length; i++)
                {
                    if (room.shortcuts[i].shortCutType == ShortcutData.Type.NPCTransportation)
                    {
                        self.pushers[i].wrongHole = false;
                    }
                }
            }
        }

        private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            if (creatureControllers.TryGetValue(self, out var p))
            {
                p.ConsciousUpdate();
            }
            orig(self, eu);
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            if (creatureControllers.TryGetValue(self, out var p))
            {
                if (OnlineManager.lobby.gameMode is MeadowGameMode)
                {
                    self.airInLungs = 1f;
                }
                p.Update(eu);
            }
            orig(self, eu);
            if (OnlineManager.lobby != null)
            {
                if (RainMeadow.tracing)
                {
                    RainMeadow.Dump(self);
                }
            }
        }

        protected override void LookImpl(Vector2 pos)
        {
            if (player.graphicsModule != null) (player.graphicsModule as PlayerGraphics).LookAtPoint(pos, 1000f);
        }

        protected override void Resting()
        {
            // no op
        }

        protected override void Moving(float magnitude)
        {
            // no op
        }

        protected override void PointImpl(Vector2 dir)
        {
            //int which = dir.x > 0 ? 1 : 0; // bugs above head
            int which = 0;
            (player.graphicsModule as PlayerGraphics).hands[which].reachingForObject = true;
            (player.graphicsModule as PlayerGraphics).hands[which].absoluteHuntPos = player.mainBodyChunk.pos + 100f * dir;
        }

        internal override void ConsciousUpdate()
        {
            base.ConsciousUpdate();
            player.pickUpCandidate = null; // prevent whiplash grab

        }

        protected override void OnCall()
        {
            if (player.graphicsModule is PlayerGraphics pg)
            {
                player.Blink(10);
                pg.head.vel += 2f * Custom.DirVec(player.bodyChunks[1].pos, player.bodyChunks[0].pos);
            }
        }

        public MeadowPlayerController(Player player, OnlineCreature oc, int playerNumber, MeadowAvatarData customization) : base(player, oc, playerNumber, customization)
        {
            player.controller = new ProxyController(this);
            this.player = player;
            this.needsLight = false; // doesn't make much of a difference plus slightly different color logic
            player.glowing = true;
        }

        private class ProxyController : Player.PlayerController
        {
            private MeadowPlayerController mpc;

            public ProxyController(MeadowPlayerController meadowPlayerController)
            {
                this.mpc = meadowPlayerController;
            }

            public override Player.InputPackage GetInput()
            {
                var input = mpc.input[0];
                input.pckp = false;
                input.thrw = false;

                if (mpc.pointCounter > 10) // this sucks, cant use lockinplace
                {
                    input.x = 0;
                    input.y = 0;
                    input.analogueDir = Vector2.zero;
                }

                return input;
            }
        }
    }
}

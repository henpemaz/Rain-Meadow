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
                if(OnlineManager.lobby.gameMode is MeadowGameMode)
                {
                    self.airInLungs = 1f;
                }
                p.Update(eu);
            }
            orig(self, eu);
        }

        public override bool GrabImpl(PhysicalObject pickUpCandidate)
        {
            //throw new NotImplementedException();
            return false;
        }

        protected override void LookImpl(Vector2 pos)
        {
            (player.graphicsModule as PlayerGraphics).LookAtPoint(pos, 1000f);
        }

        protected override void Resting()
        {
            // no op
        }

        protected override void Moving(float magnitude)
        {
            // no op
        } 

        public MeadowPlayerController(Player player, OnlineCreature oc, int playerNumber) : base(player, oc, playerNumber)
        {
            player.controller = new ProxyController(this);
            this.player = player;
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
                return mpc.input[0];
            }
        }
    }
}

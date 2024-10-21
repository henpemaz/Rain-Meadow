using HUD;
using Rewired;
using UnityEngine;

namespace RainMeadow
{
    public class Pointing : HudPart
    {
        private Creature realizedPlayer;
        public static int hand;
        private Controller controller;
        private Vector2 finalHandPos;
        public int currentlyPointingHand;

        public Pointing(HUD.HUD hud) : base(hud)
        {
            controller = RWCustom.Custom.rainWorld.options.controls[0].GetActiveController();
            finalHandPos = Vector2.zero;
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);

            if (!Input.GetKey(RainMeadow.rainMeadowOptions.PointingKey.Value))
                return; 
                
            if (OnlineManager.lobby.playerAvatars[OnlineManager.mePlayer].FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac)
            {
                realizedPlayer = ac.realizedCreature;
            }
            else
            {
                return;
            }

            Vector2 pointingVector = GetOnlinePointingVector();
            if (pointingVector == Vector2.zero)
                return;

            hand = UpdateHandPosition();
            controller = RWCustom.Custom.rainWorld.options.controls[0].GetActiveController();

            Vector2 targetPosition = realizedPlayer.mainBodyChunk.pos + pointingVector * 100f;

            finalHandPos = controller is Joystick ? targetPosition : Futile.mousePosition;
            (realizedPlayer.graphicsModule as PlayerGraphics).LookAtPoint(finalHandPos, 10f);

            if (hand > -1)
            {
                var handModule = (realizedPlayer.graphicsModule as PlayerGraphics).hands[hand];
                handModule.reachingForObject = true;
                handModule.absoluteHuntPos = finalHandPos;

            }
        }
        private int UpdateHandPosition()
        {
            for (int handy = 1; handy >= 0; handy--)
            {
                if ((realizedPlayer.grasps[handy] == null || realizedPlayer.grasps[handy].grabbed is Weapon) &&
                    (realizedPlayer.graphicsModule as PlayerGraphics).hands[1 - handy].reachedSnapPosition)
                {
                    hand = handy;
                    break;
                }
            }
            return hand;
        }

        public static Vector2 GetOnlinePointingVector()
        {
           var controller = RWCustom.Custom.rainWorld.options.controls[0].GetActiveController();
            if (controller is Joystick joystick)
            {
                Vector2 direction = new Vector2(joystick.GetAxis(2), joystick.GetAxis(3));
                return Vector2.ClampMagnitude(direction, 1f);
            }

            return Futile.mousePosition;
        }
    }
}

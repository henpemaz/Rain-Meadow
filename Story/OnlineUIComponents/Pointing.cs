using HUD;
using UnityEngine;
using Rewired;

namespace RainMeadow
{
    public class Pointing : HudPart
    {
        private Creature realizedPlayer;
        private int hand;
        private Controller controller;
        private Vector2 finalHandPos;

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

            UpdateHandPosition();
            Vector2 targetPosition = realizedPlayer.mainBodyChunk.pos + pointingVector * 100f;

            finalHandPos = controller is Joystick ? targetPosition : Futile.mousePosition;
            (realizedPlayer.graphicsModule as PlayerGraphics).LookAtPoint(finalHandPos, 10f);

            if (hand > -1)
            {
                var handModule = (realizedPlayer.graphicsModule as PlayerGraphics).hands[hand];

                var pg = (realizedPlayer.graphicsModule as PlayerGraphics);
                var p = (realizedPlayer as Player);
                handModule.reachingForObject = true;
                handModule.absoluteHuntPos = finalHandPos;

                //if (realizedPlayer.grasps[hand] != null && realizedPlayer.grasps[hand].grabbed is Weapon)
                //{
                //    pg.spearDir = Vector3.Slerp(finalHandPos, RWCustom.Custom.DegToVec(pg.spearDir), 0.5f).x;
                //    //finalHandPos = Vector3.Slerp(finalHandPos, RWCustom.Custom.DegToVec((80f + Mathf.Cos((float)(p.animationFrame + (p.leftFoot ? 9 : 3)) / 12f * 2f * (float)System.Math.PI) * 4f * pg.spearDir) * pg.spearDir), Mathf.Abs((pg.spearDir)));
                //    //(realizedPlayer.graphicsModule as PlayerGraphics).spearDir = ;
                //}
                


            }
        }
        private void UpdateHandPosition()
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
        }

        private Vector2 GetOnlinePointingVector()
        {
            if (controller is Joystick joystick)
            {
                Vector2 direction = new Vector2(joystick.GetAxis(2), joystick.GetAxis(3));
                return Vector2.ClampMagnitude(direction, 1f);
            }

            return Futile.mousePosition;
        }
    }
}

using HUD;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using HarmonyLib;
using static RainMeadow.CreatureController;

namespace RainMeadow
{
    public class Pointing : HudPart
    {

        Player playerPointing;
        int hand;
        Vector2 pointDirection;


        public Pointing(HUD.HUD hud) : base(hud)
        {
            playerPointing = (hud.owner as Player);

        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            if (Input.GetKey(RainMeadow.rainMeadowOptions.FriendsListKey.Value))
            {
                Vector2 vector = OnlinePointing();
                if (vector == Vector2.zero)
                {
                    return;
                }


                for (int num2 = 1; num2 >= 0; num2--)
                {
                    if ((playerPointing.grasps[num2] == null || playerPointing.grasps[num2].grabbed is Weapon) && (playerPointing.graphicsModule as PlayerGraphics).hands[1 - num2].reachedSnapPosition)
                    {
                        hand = num2;
                    }
                }

                float num3 = 100f;


                Vector2 vector2 = new Vector2(playerPointing.mainBodyChunk.pos.x + vector.x * num3, playerPointing.mainBodyChunk.pos.y + vector.y * num3);
                (playerPointing.graphicsModule as PlayerGraphics).LookAtPoint(vector2, 10f);
                if (hand > -1)
                {
                    (playerPointing.graphicsModule as PlayerGraphics).hands[hand].reachingForObject = true;
                    (playerPointing.graphicsModule as PlayerGraphics).hands[hand].absoluteHuntPos = vector2;
                }
            }
        }


        public override void Update()
        {
            base.Update();

        }

        public Vector2 OnlinePointing()
        {

            SpecialInput specialInput = default;
            var controller = RWCustom.Custom.rainWorld.options.controls[0].GetActiveController();
            if (controller is Rewired.Joystick joystick)
            {
                specialInput.direction = Vector2.ClampMagnitude(new Vector2(joystick.GetAxis(2), joystick.GetAxis(3)), 1f);
            }
            else
            {
                if (Input.GetMouseButton(0))
                {
                    specialInput.direction = Vector2.ClampMagnitude((((Vector2)Futile.mousePosition) - playerPointing.input[0].IntVec.ToVector2().normalized) / 500f, 1f);
                }
            }


            if (specialInput.direction != Vector2.zero)
            {

                return specialInput.direction;
            }

            return Vector2.ClampMagnitude((((Vector2)Futile.mousePosition) - playerPointing.input[0].IntVec.ToVector2().normalized) / 500f, 1f);

        }

    }
}
using HUD;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using HarmonyLib;
using static RainMeadow.CreatureController;
using RainMeadow.GameModes;

namespace RainMeadow
{
    public class Pointing : HudPart
    {
        public Creature realizedPlayer;
        int hand;

        public Pointing(HUD.HUD hud) : base(hud)
        {
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);

            if (Input.GetKey(RainMeadow.rainMeadowOptions.FriendsListKey.Value)) // TODO: Change this input
            {
                if (OnlineManager.lobby.playerAvatars[OnlineManager.mePlayer].FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac)
                {
                    realizedPlayer = ac.realizedCreature;

                } else
                {
                    return;
                }

                Vector2 vector = OnlinePointing();
                if (vector == Vector2.zero)
                {
                    return;
                }

                for (int handy = 1; handy >= 0; handy--)
                {
                    if ((realizedPlayer.grasps[handy] == null || realizedPlayer.grasps[handy].grabbed is Weapon) && (realizedPlayer.graphicsModule as PlayerGraphics).hands[1 - handy].reachedSnapPosition)
                    {
                        hand = handy;
                    }
                }

                float num3 = 100f;


                Vector2 vector2 = new Vector2(realizedPlayer.mainBodyChunk.pos.x + vector.x * num3, realizedPlayer.mainBodyChunk.pos.y + vector.y * num3);
                (realizedPlayer.graphicsModule as PlayerGraphics).LookAtPoint(vector2, 10f);
                if (hand > -1)
                {
                    (realizedPlayer.graphicsModule as PlayerGraphics).hands[hand].reachingForObject = true;
                    (realizedPlayer.graphicsModule as PlayerGraphics).hands[hand].absoluteHuntPos = vector2;
                }
            }
        }

        public Vector2 OnlinePointing()
        {

            SpecialInput specialInput = default;
            // Good for when we have a specific choice in Options menu, but doesn't work well with the "ANY" input
            var controller = RWCustom.Custom.rainWorld.options.controls[0].GetActiveController();
            if (controller is Rewired.Joystick joystick)
            {
                specialInput.direction = Vector2.ClampMagnitude(new Vector2(joystick.GetAxis(2), joystick.GetAxis(3)), 1f);
            }
            else
            {
                if (Input.GetMouseButton(0))
                {
                    specialInput.direction = Vector2.ClampMagnitude((((Vector2)Futile.mousePosition) - (realizedPlayer as Player).input[0].IntVec.ToVector2().normalized) / 500f, 1f);
                }
            }


            if (specialInput.direction != Vector2.zero)
            {

                return specialInput.direction;
            }

            return Vector2.zero;

        }

    }
}
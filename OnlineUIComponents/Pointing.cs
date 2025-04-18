﻿using HUD;
using Rewired;
using UnityEngine;

namespace RainMeadow
{
    public class Pointing : HudPart
    {
        public const float LookInterest = 10f;
        public Pointing(HUD.HUD hud) : base(hud) { }

        /// <summary>
        /// Local draw update, counterintuitively this takes care of the input on the player side
        /// that will later be broadcast to everyone else
        /// </summary>
        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            if (Input.GetKey(RainMeadow.rainMeadowOptions.PointingKey.Value))
            {
                if (OnlineManager.lobby.gameMode.avatars[0] is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac)
                {
                    int handIndex = GetHandIndex(ac.realizedCreature);
                    if (handIndex >= 0) LookAtPoint(ac.realizedCreature, GetOnlinePointingVector(), handIndex);
                }
            }
        }

        /// <summary>
        /// Implementation of "Look at", used only by this class for the specific slugcat
        /// </summary>
        private static void LookAtPoint(Creature realizedPlayer, Vector2 pointingVector, int handIndex)
        {
            var controller = RWCustom.Custom.rainWorld.options.controls[0].GetActiveController();
            Vector2 targetPosition = realizedPlayer.mainBodyChunk.pos + pointingVector * 100f;
            Vector2 finalHandPos = controller is Joystick ? targetPosition : Futile.mousePosition;
            if (realizedPlayer.graphicsModule is PlayerGraphics playerGraphics)
            {
                playerGraphics.LookAtPoint(finalHandPos, Pointing.LookInterest);
                var handModule = playerGraphics.hands[handIndex];
                handModule.reachingForObject = true;
                handModule.absoluteHuntPos = finalHandPos;
            }
        }

        /// <summary>
        /// Obtains the available (free) hand that can be used for pointing
        /// </summary>
        internal static int GetHandIndex(Creature realizedPlayer)
        {
            for (int i = 1; i >= 0 && realizedPlayer is not null; i--)
            {
                if ((realizedPlayer.grasps[i] == null || realizedPlayer.grasps[i].grabbed is Weapon) && realizedPlayer.graphicsModule is PlayerGraphics playerGraphics && playerGraphics.hands[1 - i].reachedSnapPosition)
                {
                    return i;
                }
            }
            return -1;
        }

        private static Vector2 GetOnlinePointingVector()
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

using UnityEngine;

namespace RainMeadow
{
    public static class InputOverride
    {
        public static void StopPlayerMovement(Player p)
        {
            if (p != null)
            {
                p.input[0].x = 0;
                p.input[0].y = 0;
                p.input[0].analogueDir *= 0f;
                p.input[0].jmp = false;
                p.input[0].thrw = false;
                p.input[0].pckp = false;
                p.input[0].mp = false;
            } else
            {
                RainMeadow.Debug("Player is null while trying to stop movement");
            }
        }


        public static void HoldFire(Player p)
        {
            p.input[0].thrw = false;


        }

        public static void StopSpecialSkill(Player p)
        {
            if (p.wantToJump > 0 && p.input[0].pckp)
            {
                p.input[0].pckp = false;
            }

        }

        public static float MoveMenuItemFromYInput(float y)
        {
            var controller = RWCustom.Custom.rainWorld.options.controls[0].GetActiveController();
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            float verticalInput = 0;
            if (controller is Rewired.Joystick js)
            {
                verticalInput = js.GetAxis(3);
            }

            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) || verticalInput > 0 || scrollInput < 0)
            {
                return y -= RainMeadow.rainMeadowOptions.ScrollSpeed.Value;

            }

            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W) || verticalInput < 0 || scrollInput > 0)
            {
               return y += RainMeadow.rainMeadowOptions.ScrollSpeed.Value;
            }
            return y;
        }

    }
}

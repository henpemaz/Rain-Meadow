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
            if (controller is Rewired.Joystick js) scrollInput -= js.GetAxis(3);
            scrollInput += (ChatHud.isLogToggled == false && Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) ? -1f : 0f;
            scrollInput += (ChatHud.isLogToggled == false && Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) ? 1f : 0f;
            scrollInput = Mathf.Clamp(scrollInput, -1.0f, 1.0f);

            return y + scrollInput * RainMeadow.rainMeadowOptions.ScrollSpeed.Value;
        }

    }
}

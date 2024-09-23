using System;
using System.Drawing;
using HUD;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class BanHammer
    {
        public static void ShowBan(ProcessManager manager)
        {

            Action confirmProceed = () =>
            {
                manager.dialog = null;
            };

            DialogNotify informBadUser = new DialogNotify("You were removed from the previous online game", manager, confirmProceed);


            manager.ShowDialog(informBadUser);
            

        }


    }
}
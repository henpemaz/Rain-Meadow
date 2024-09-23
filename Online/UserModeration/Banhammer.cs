using System;
using System.Drawing;
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

            Action cancelProceed = () =>
            {
                manager.dialog = null;


            };

            DialogConfirm informBadUser = new DialogConfirm("You were kicked from the previous game", manager, confirmProceed, cancelProceed);


            manager.ShowDialog(informBadUser);

        }


    }
}
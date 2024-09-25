using System;
using Menu;

using System.Collections.Generic;


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

            if (manager.dialog != null)
            {

                manager.dialog = null;
            }


            manager.ShowDialog(informBadUser);

        }

        public static void BanUser(OnlinePhysicalObject steamUser)
        {
            var onlinePlayer = (steamUser.owner as OnlinePlayer);
            onlinePlayer.InvokeRPC(RPCs.KickToLobby);
            if (OnlineManager.lobby.bannedUsers == null)
            {
                OnlineManager.lobby.bannedUsers = new List<string>();
            }
            if (!OnlineManager.lobby.bannedUsers.Contains(onlinePlayer.id.name))
            {
                OnlineManager.lobby.bannedUsers.Add(onlinePlayer.id.name);
            }
            OnlineManager.lobby.OnPlayerDisconnect(onlinePlayer);

        }

    }
}
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
            if (OnlineManager.lobby.bannedUsers.list == null)
            {
                OnlineManager.lobby.bannedUsers.list = new List<MeadowPlayerId>();
            }
            if (!OnlineManager.lobby.bannedUsers.list.Contains(onlinePlayer.id))
            {
                OnlineManager.lobby.bannedUsers.list.Add(onlinePlayer.id);
            }
            OnlineManager.lobby.OnPlayerDisconnect(onlinePlayer);

        }

    }
}
using Menu;
using System;


namespace RainMeadow
{
    public class BanHammer
    {
        public static void ShowBan(ProcessManager manager)
        {

            Action confirmProceed = () =>
            {
                manager.dialog = null;
                if (OnlineManager.lobby != null)
                {
                    OnlineManager.LeaveLobby(); // kill anything leftover
                }
            };

            DialogNotify informBadUser = new DialogNotify(Utils.Translate("You were removed from the previous online game"), manager, confirmProceed);

            if (manager.dialog != null)
            {

                manager.dialog = null;
            }


            manager.ShowDialog(informBadUser);

        }

        public static void BanUser(OnlinePlayer steamUser)
        {
            steamUser.InvokeRPC(RPCs.KickToLobby);
            if (OnlineManager.lobby.bannedUsers == null)
            {
                OnlineManager.lobby.bannedUsers = new();
            }
            if (!OnlineManager.lobby.bannedUsers.list.Contains(steamUser.id))
            {
                OnlineManager.lobby.bannedUsers.list.Add(steamUser.id);
            }

        }

        public static void KickUser(OnlinePlayer steamUser) => steamUser.InvokeRPC(RPCs.KickToLobby);
    }
}
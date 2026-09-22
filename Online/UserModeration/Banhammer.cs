using RainMeadow.UI;
using RainMeadow.UI.Dialogs;

namespace RainMeadow
{
    public class BanHammer
    {
        public static void ShowBan(ProcessManager manager)
        {
            NotifyDialog dialog = new(
                manager,
                "You were removed from the previous online game",
                UIUtils.SINGLE_LINE_DIALOG_SIZE
            );
            dialog.OnContinue += () =>
            {
                if (OnlineManager.lobby != null)
                    OnlineManager.LeaveLobby(); // kill anything leftover
            };

            while (manager.dialog != null)
                manager.StopSideProcess(manager.dialog);

            manager.ShowDialog(dialog);
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
    }
}

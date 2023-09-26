using Steamworks;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        private void MenuHooks()
        {
            On.Menu.MainMenu.ctor += MainMenu_ctor;
            On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;
        }

        private void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (ID == Ext_ProcessID.LobbySelectMenu)
            {
                self.currentMainLoop = new LobbySelectMenu(self);
            }
            if (ID == Ext_ProcessID.ArenaLobbyMenu)
            {
                self.currentMainLoop = new ArenaLobbyMenu(self);
            }
            if (ID == Ext_ProcessID.LobbyMenu)
            {
                self.currentMainLoop = new LobbyMenu(self);
            }

#if !LOCAL_P2P
            if (ID == ProcessManager.ProcessID.IntroRoll)
            {
                var args = System.Environment.GetCommandLineArgs();
                for (var i = 0; i < args.Length; i++)
                {
                    if (args[i] == "+connect_lobby")
                    {
                        if (args.Length > i + 1 && ulong.TryParse(args[i + 1], out var id)) {
                            Debug($"joining lobby with id {id} from the command line");
                            MatchmakingManager.instance.JoinLobby(new LobbyInfo(new CSteamID(id), "", "", 0));
                        }
                        else
                        {
                            Error($"found +connect_lobby but no valid lobby id in the command line");
                        }
                        break;
                    }
                }
            }
#endif
            orig(self, ID);
        }

        private void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);

            MatchmakingManager.instance.LeaveLobby();

            var meadowButton = new Menu.SimpleButton(self, self.pages[0], self.Translate("MEADOW"), "MEADOW", Vector2.zero, new Vector2(Menu.MainMenu.GetButtonWidth(self.CurrLang), 30f));
            self.AddMainMenuButton(meadowButton, () =>
            {
#if !LOCAL_P2P
                if (!SteamManager.Instance.m_bInitialized || !SteamUser.BLoggedOn())
                {
                    self.manager.ShowDialog(new Menu.DialogNotify("You need Steam active to play Rain Meadow", self.manager, null));
                    return;
                }
#endif
                self.manager.RequestMainProcessSwitch(Ext_ProcessID.LobbySelectMenu);
            }, self.mainMenuButtons.Count - 2);
        }
    }
}
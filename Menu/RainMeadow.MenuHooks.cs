using System;
using UnityEngine;

namespace RainMeadow
{
    partial class RainMeadow
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
            if (ID == Ext_ProcessID.LobbyMenu)
            {
                self.currentMainLoop = new LobbyMenu(self);
            }
            orig(self, ID);
        }

        private void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);
            var meadowButton = new Menu.SimpleButton(self, self.pages[0], self.Translate("MEADOW"), "MEADOW", Vector2.zero, new Vector2(Menu.MainMenu.GetButtonWidth(self.CurrLang), 30f));
            meadowButton.buttonBehav.greyedOut = SteamManager.Instance.m_bInitialized;
            self.AddMainMenuButton(meadowButton, new Action(() => { manager.RequestMainProcessSwitch(Ext_ProcessID.LobbySelectMenu); }), 2);
        }
    }
}
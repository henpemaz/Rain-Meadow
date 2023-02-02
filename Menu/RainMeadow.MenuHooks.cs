using System;
using UnityEngine;

namespace RainMeadow
{
    partial class RainMeadow
    {
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
            float buttonWidth = Menu.MainMenu.GetButtonWidth(self.CurrLang);
            Vector2 pos = new Vector2(683f - buttonWidth / 2f, 0f);
            Vector2 size = new Vector2(buttonWidth, 30f);
            var meadowButton = new Menu.SimpleButton(self, self.pages[0], self.Translate("MEADOW"), "MEADOW", pos, size);
            meadowButton.buttonBehav.greyedOut = SteamManager.Instance.m_bInitialized;
            self.AddMainMenuButton(meadowButton, new Action(() => { manager.RequestMainProcessSwitch(Ext_ProcessID.LobbySelectMenu); }), 2);
        }
    }
}
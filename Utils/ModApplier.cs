using Menu;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace RainMeadow
{
    internal class ModApplier : ModManager.ModApplyer
    {
        public DialogAsyncWait? dialogBox;
        public Dialog? checkUserConfirmation;

        private readonly Menu.Menu menu;

        public event Action<ModApplier>? OnFinish;

        public ModApplier(ProcessManager manager, List<bool> pendingEnabled, List<int> pendingLoadOrder) : base(manager, pendingEnabled, pendingLoadOrder)
        {
            On.RainWorld.Update += RainWorld_Update;
            menu = (Menu.Menu)manager.currentMainLoop;
        }

        private void Cancel()
        {
            On.RainWorld.Update -= RainWorld_Update;
            this.finished = true;
        }

        private void RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
        {
            orig(self);

            Update();
        }

        public new void Update()
        {
            base.Update();

            dialogBox?.SetText(menu.Translate("mod_menu_apply_mods") + Environment.NewLine + statusText);

            if (IsFinished())
            {
                Cancel();

                //go to main menu (to finish applying changes) and then rejoin the lobby
                manager.dialog = null;
                manager.rainWorld.options.Save();
                if (this.applyError != null)
                {
                    Action cancelProceed = () =>
                    {
                        manager.dialog = null;
                        checkUserConfirmation = null;
                        //OnlineManager.LeaveLobby(); //redundant, since the lobby should almost certainly already be left
                        manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
                    };
                    new DialogNotify("Error loading mods!", new Vector2(480f, 320f), manager, cancelProceed);
                }
                else
                {
                    menu.PlaySound(SoundID.MENU_Switch_Page_Out);
                    manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                    while (!manager.modFinalizationDone)
                        Thread.Sleep(1);
                }

                OnFinish?.Invoke(this);
            }
        }

        public void ShowConfirmation(List<ModManager.Mod> modsToEnable, List<ModManager.Mod> modsToDisable, List<string> unknownMods)
        {
            var modMismatchString = menu.Translate("Mod Mismatch!") + Environment.NewLine;

            if (modsToEnable.Count > 0)
                modMismatchString += Environment.NewLine + menu.Translate("Mods that have to be enabled: ") + string.Join(", ", modsToEnable.ConvertAll(mod => mod.LocalizedName));
            if (modsToDisable.Count > 0)
                modMismatchString += Environment.NewLine + menu.Translate("Mods that have to be disabled: ") + string.Join(", ", modsToDisable.ConvertAll(mod => mod.LocalizedName));
            if (unknownMods.Count > 0)
                modMismatchString += Environment.NewLine + menu.Translate("Mods that have to be installed: ") + string.Join(", ", unknownMods);
            else
                modMismatchString += Environment.NewLine + Environment.NewLine + menu.Translate("Apply these changes now?");

            // modMismatchString += Environment.NewLine + Environment.NewLine + menu.Translate("You will be returned to the Lobby Select screen");

            Action confirmProceed = () =>
            {
                manager.dialog = null;
                checkUserConfirmation = null;
                dialogBox = new DialogAsyncWait(menu, menu.Translate("mod_menu_apply_mods"), new Vector2(480f, 320f));
                manager.ShowDialog(dialogBox);
                Start(filesInBadState);
            };

            Action cancelProceed = () =>
            {
                manager.dialog = null;
                checkUserConfirmation = null;
                OnlineManager.LeaveLobby();
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
                Cancel();
            };

            // disable auto-apply for now
            //nah that seems like a really useful feature I'mma re-add it - TheLazyCowboy1
            if (unknownMods.Count > 0)
            {
                checkUserConfirmation = new DialogNotify(modMismatchString, new Vector2(480f, 320f), manager, cancelProceed);
            }
            else
            {
                checkUserConfirmation = new DialogConfirm(modMismatchString, new Vector2(480f, 320f), manager, confirmProceed, cancelProceed);
            }
            //checkUserConfirmation = new DialogNotify(modMismatchString, new Vector2(480f, 320f), manager, cancelProceed);

            manager.ShowDialog(checkUserConfirmation);
        }

        public void ConfirmReorder()
        {
            var modMismatchString = menu.Translate("Warning: Differing Mod Load Orders!")
                + Environment.NewLine + menu.Translate("This may cause unstable play.")
                + Environment.NewLine + Environment.NewLine + menu.Translate("Reorder your mods now?");

            Action confirmProceed = () =>
            {
                manager.dialog = null;
                checkUserConfirmation = null;
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
                OnlineManager.LeaveLobby();
                dialogBox = new DialogAsyncWait(menu, menu.Translate("mod_menu_apply_mods"), new Vector2(480f, 320f));
                manager.ShowDialog(dialogBox);
                Start(filesInBadState);
            };

            Action cancelProceed = () =>
            {
                manager.dialog = null;
                checkUserConfirmation = null;
                Cancel();
            };

            // disable auto-apply for now
            //nah that seems like a really useful feature I'mma re-add it - TheLazyCowboy1
            checkUserConfirmation = new DialogConfirm(modMismatchString, new Vector2(480f, 320f), manager, confirmProceed, cancelProceed);

            manager.ShowDialog(checkUserConfirmation);
        }

        public void ShowMissingDLCMessage(List<ModManager.Mod> missingDLC)
        {
            var modMismatchString = menu.Translate("Cannot join due to missing DLC!") + Environment.NewLine;

            modMismatchString += Environment.NewLine + menu.Translate("Missing DLC Mods that have to be enabled: ") + string.Join(", ", missingDLC.ConvertAll(mod => mod.LocalizedName));

            Action cancelProceed = () =>
            {
                manager.dialog = null;
                checkUserConfirmation = null;
                OnlineManager.LeaveLobby();
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
                Cancel();
            };

            checkUserConfirmation = new DialogNotify(modMismatchString, new Vector2(480f, 320f), manager, cancelProceed);

            manager.ShowDialog(checkUserConfirmation);
        }
    }
}

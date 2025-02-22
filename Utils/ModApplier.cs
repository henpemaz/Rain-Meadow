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

        public bool ended = false;
        public bool cancelled = false;

        private readonly Menu.Menu menu;

        public event Action<ModApplier>? OnFinish;

        public ModApplier(ProcessManager manager, List<bool> pendingEnabled, List<int> pendingLoadOrder) : base(manager, pendingEnabled, pendingLoadOrder)
        {
            On.RainWorld.Update += RainWorld_Update;
            menu = (Menu.Menu)manager.currentMainLoop;
        }

        private void Cancel()
        {
            cancelled = true;
            EndModApplier();
        }
        private void EndModApplier()
        {
            On.RainWorld.Update -= RainWorld_Update;
            this.finished = true;
            this.ended = true;

            ClearPopups();
        }
        private void ClearPopups()
        {
            dialogBox?.RemoveSprites();
            dialogBox?.HackHide();
            dialogBox = null;
            manager.dialog?.HackHide();
            manager.dialog = null;
            checkUserConfirmation?.HackHide();
            checkUserConfirmation = null;
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

            if (!ended && IsFinished())
            {
                EndModApplier();

                manager.rainWorld.options.Save();

                //go to main menu (to finish applying changes) and then rejoin the lobby
                if (this.applyError != null)
                {
                    Action cancelProceed = () =>
                    {
                        ClearPopups();
                        //OnlineManager.LeaveLobby(); //redundant, since the lobby should almost certainly already be left
                        manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
                    };
                    checkUserConfirmation = new DialogNotify("Error loading mods!", new Vector2(480f, 320f), manager, cancelProceed);
                    manager.ShowDialog(checkUserConfirmation);
                }
                else if (!this.requiresRestart)
                {
                    RainMeadow.Debug("Finalizing mod reordering");
                    menu.PlaySound(SoundID.MENU_Switch_Page_Out);
                    manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu); //requires a process switch to finalize mods
                    Thread.Sleep(1000); //wait for mod finalization to begin
                    while (!manager.modFinalizationDone)
                        Thread.Sleep(10); //wait for finalization to finish
                    Thread.Sleep(200); //extra wait just in case
                }

                OnFinish?.Invoke(this);
            }
        }

        public void ShowConfirmation(List<ModManager.Mod> modsToEnable, List<ModManager.Mod> modsToDisable, List<string> unknownMods)
        {
            //leave lobby immediately; we'll have to change mods to join it
            if (OnlineManager.lobby != null)
            {
                OnlineManager.LeaveLobby();
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
            }

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
                ClearPopups();
                dialogBox = new DialogAsyncWait(menu, menu.Translate("mod_menu_apply_mods"), new Vector2(480f, 320f));
                manager.ShowDialog(dialogBox);
                Start(filesInBadState);
            };

            // disable auto-apply for now
            //nah that seems like a really useful feature I'mma re-add it - TheLazyCowboy1
            if (unknownMods.Count > 0)
            {
                checkUserConfirmation = new DialogNotify(modMismatchString, new Vector2(480f, 320f), manager, Cancel);
            }
            else
            {
                checkUserConfirmation = new DialogConfirm(modMismatchString, new Vector2(480f, 320f), manager, confirmProceed, Cancel);
            }
            //checkUserConfirmation = new DialogNotify(modMismatchString, new Vector2(480f, 320f), manager, cancelProceed);

            manager.ShowDialog(checkUserConfirmation);
        }

        public void ConfirmReorder()
        {
            //note: lobby isn't left immediately, because the user still has the option to join

            var modMismatchString = menu.Translate("Warning: Differing Mod Load Orders!")
                + Environment.NewLine + menu.Translate("This may cause unstable play.")
                + Environment.NewLine + Environment.NewLine + menu.Translate("Reorder your mods now?");

            Action confirmProceed = () =>
            {
                ClearPopups();
                if (OnlineManager.lobby != null)
                {
                    OnlineManager.LeaveLobby();
                    manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
                }
                dialogBox = new DialogAsyncWait(menu, menu.Translate("mod_menu_apply_mods"), new Vector2(480f, 320f));
                manager.ShowDialog(dialogBox);
                Start(filesInBadState);
            };

            // disable auto-apply for now
            //nah that seems like a really useful feature I'mma re-add it - TheLazyCowboy1
            checkUserConfirmation = new DialogConfirm(modMismatchString, new Vector2(480f, 320f), manager, confirmProceed, EndModApplier);

            manager.ShowDialog(checkUserConfirmation);
        }

        public void ShowMissingDLCMessage(List<ModManager.Mod> missingDLC)
        {
            //leave lobby immediately; we don't want non-DLC players in DLC-exclusive lobbies
            if (OnlineManager.lobby != null)
            {
                OnlineManager.LeaveLobby();
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
            }

            var modMismatchString = menu.Translate("Cannot join due to missing DLC!") + Environment.NewLine;

            modMismatchString += Environment.NewLine + menu.Translate("Missing DLC Mods that have to be enabled: ") + string.Join(", ", missingDLC.ConvertAll(mod => mod.LocalizedName));

            checkUserConfirmation = new DialogNotify(modMismatchString, new Vector2(480f, 320f), manager, Cancel);

            manager.ShowDialog(checkUserConfirmation);
        }
    }
}

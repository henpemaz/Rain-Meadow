using Menu;
using RainMeadow.UI;
using RainMeadow.UI.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace RainMeadow
{
    internal class ModApplier : ModManager.ModApplyer
    {
        public DialogAsyncWait? dialogBox;

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
        private void EndModApplier() => EndModApplier(true);
        private void EndModApplier(bool clearPopups)
        {
            On.RainWorld.Update -= RainWorld_Update;
            this.finished = true;
            this.ended = true;

            if (clearPopups)
                ClearPopups();
        }
        private void ClearPopups()
        {
            while (manager.dialog != null)
                manager.StopSideProcess(manager.dialog);
            dialogBox = null;
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
                EndModApplier(!this.requiresRestart);

                manager.rainWorld.options.Save();

                if (this.applyError != null)
                {
                    //error popup
                    void cancelProceed()
                    {
                        ClearPopups();
                        manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
                    }
                    manager.ShowDialog(
                        new NotifyDialog(
                            manager,
                            "Error loading mods!",
                            UIUtils.SINGLE_LINE_DIALOG_SIZE,
                            cancelProceed
                        )
                    );
                }
                else if (!this.requiresRestart)
                {
                    //loading mods without a restart required (e.g: loading/unloading MSC or Remix)
                    RainMeadow.Debug("Finalizing mod reordering");
                    menu.PlaySound(SoundID.MENU_Switch_Page_Out);
                    manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu); //requires a process switch to finalize mods
                    Thread.Sleep(1000); //wait for mod finalization to begin
                    while (!manager.modFinalizationDone)
                        Thread.Sleep(5); //wait for finalization to finish
                }
                else
                {
                    //Indicate that a restart is required
                    dialogBox?.SetText(menu.Translate("A restart is required to finish applying the mod changes.") + Environment.NewLine + Environment.NewLine + menu.Translate("Restarting now..."));
                }
                OnFinish?.Invoke(this);
            }
        }

        public void ShowConfirmation(List<string> modsToEnable, List<string> modsToDisable, List<string> unknownMods)
        {
            //leave lobby immediately; we'll have to change mods to join it
            if (OnlineManager.lobby != null)
            {
                OnlineManager.LeaveLobby();
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
            }

            void ConfirmProceed()
            {
                ClearPopups();
                dialogBox = new DialogAsyncWait(
                    manager,
                    menu.Translate("mod_menu_apply_mods"),
                    new Vector2(480f, 320f)
                );
                manager.ShowDialog(dialogBox);
                Start(filesInBadState);
            }

            ScrollableDialog.IScrollableDialog scrollableDialog =
                unknownMods.Count > 0
                    ? new ScrollableDialog.Notify(
                        manager,
                        "Mod Mismatch!",
                        new Vector2(520f, 420f),
                        Cancel,
                        timeOut: 0f
                    )
                    : new ScrollableDialog.Confirm(
                        manager,
                        "Mod Mismatch!",
                        new Vector2(520f, 420f),
                        ConfirmProceed,
                        Cancel
                    );

            if (modsToEnable.Count > 0)
            {
                scrollableDialog.TextScroller.AddText(
                    menu.Translate("Mods that have to be enabled: "),
                    true
                );
                scrollableDialog.TextScroller.AddText(modsToEnable);
                scrollableDialog.TextScroller.AddBlankLine();
            }

            if (modsToDisable.Count > 0)
            {
                scrollableDialog.TextScroller.AddText(
                    menu.Translate("Mods that have to be disabled: "),
                    true
                );
                scrollableDialog.TextScroller.AddText(modsToDisable);
                scrollableDialog.TextScroller.AddBlankLine();
            }

            if (unknownMods.Count > 0)
            {
                scrollableDialog.TextScroller.AddText(
                    menu.Translate("Mods that have to be installed: "),
                    true
                );
                scrollableDialog.TextScroller.AddText(unknownMods);
            }
            else
            {
                scrollableDialog.TextScroller.AddText(
                    menu.Translate("Apply these changes now?"),
                    true
                );
                scrollableDialog.TextScroller.AddText(
                    menu.Translate("A restart may take place to sync game objects")
                );
            }

            manager.ShowDialog(scrollableDialog as Dialog);
        }

        public void ConfirmReorder()
        {
            //note: lobby isn't left immediately, because the user still has the option to join
            void confirmProceed()
            {
                ClearPopups();
                if (OnlineManager.lobby != null)
                {
                    OnlineManager.LeaveLobby();
                    manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
                }
                dialogBox = new DialogAsyncWait(manager, menu.Translate("mod_menu_apply_mods"), new Vector2(480f, 320f));
                manager.ShowDialog(dialogBox);
                Start(filesInBadState);
            }

            manager.ShowDialog(
                new ConfirmCancelDialog(
                    menu.manager,
                    "Warning: Differing Mod Load Orders!<LINE>This may cause unstable play.<LINE><LINE>Reorder your mods now?",
                    UIUtils.DEFAULT_DIALOG_SIZE,
                    confirmProceed,
                    EndModApplier
                )
            );
        }

        public void ShowMissingDLCMessage(List<string> missingDLC)
        {
            //leave lobby immediately; we don't want non-DLC players in DLC-exclusive lobbies
            if (OnlineManager.lobby != null)
            {
                OnlineManager.LeaveLobby();
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
            }

            var modMismatchString = menu.Translate("Cannot join due to missing DLC!") + Environment.NewLine;

            modMismatchString += Environment.NewLine + menu.Translate("Missing DLC Mods that have to be enabled: ") + string.Join(", ", missingDLC);

            manager.ShowDialog(
                new NotifyDialog(
                    manager,
                    modMismatchString,
                    UIUtils.DEFAULT_DIALOG_SIZE,
                    Cancel
                )
            );
        }
    }
}

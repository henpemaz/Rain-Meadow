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
                EndModApplier(!this.requiresRestart);

                manager.rainWorld.options.Save();

                if (this.applyError != null)
                {
                    //error popup
                    Action cancelProceed = () =>
                    {
                        ClearPopups();
                        manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
                    };
                    checkUserConfirmation = new DialogNotify(menu.Translate("Error loading mods!"), new Vector2(480f, 320f), manager, cancelProceed);
                    manager.ShowDialog(checkUserConfirmation);
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

            var lines = new List<ScrollableConfirmDialog.Line>();
            AddModSection(lines, menu.Translate("Mods that have to be enabled: "), modsToEnable);
            AddModSection(lines, menu.Translate("Mods that have to be disabled: "), modsToDisable);
            if (unknownMods.Count > 0)
            {
                AddModSection(lines, menu.Translate("Mods that have to be installed: "), unknownMods);
            }
            else
            {
                lines.Add(new(""));
                lines.Add(new(menu.Translate("Apply these changes now?"), true));
                lines.Add(new(menu.Translate("A restart may take place to sync game objects")));
            }

            Action confirmProceed = () =>
            {
                ClearPopups();
                dialogBox = new DialogAsyncWait(menu, menu.Translate("mod_menu_apply_mods"), new Vector2(480f, 320f));
                manager.ShowDialog(dialogBox);
                Start(filesInBadState);
            };

            if (unknownMods.Count > 0)
            {
                checkUserConfirmation = new ScrollableConfirmDialog(manager, menu.Translate("Mod Mismatch!"), lines, new Vector2(520f, 420f), Cancel, null, false);
            }
            else
            {
                checkUserConfirmation = new ScrollableConfirmDialog(manager, menu.Translate("Mod Mismatch!"), lines, new Vector2(520f, 420f), confirmProceed, Cancel, true);
            }

            manager.ShowDialog(checkUserConfirmation);
        }

        private static void AddModSection(List<ScrollableConfirmDialog.Line> lines, string header, List<string> mods)
        {
            if (mods.Count == 0)
                return;
            if (lines.Count > 0)
                lines.Add(new(""));
            lines.Add(new(header, true));
            foreach (string mod in mods)
                lines.Add(new(mod));
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

            checkUserConfirmation = new DialogConfirm(modMismatchString, new Vector2(480f, 320f), manager, confirmProceed, EndModApplier);

            manager.ShowDialog(checkUserConfirmation);
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

            checkUserConfirmation = new DialogNotify(modMismatchString, new Vector2(480f, 320f), manager, Cancel);

            manager.ShowDialog(checkUserConfirmation);
        }
    }
}

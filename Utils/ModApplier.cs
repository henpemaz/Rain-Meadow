using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class ModApplier : ModManager.ModApplyer
    {
        public DialogAsyncWait dialogBox;
        public DialogConfirm checkUserConfirmation;
        public DialogNotify requiresRestartDialog;
        public string modMismatchString;


        private readonly Menu.Menu menu;
        private List<ModManager.Mod> modsToEnable;
        private List<ModManager.Mod> modsToDisable;
        private List<string> unknownMods;


        public event Action<ModApplier> OnFinish;

        public ModApplier(ProcessManager manager, List<bool> pendingEnabled, List<int> pendingLoadOrder) : base(manager, pendingEnabled, pendingLoadOrder)
        {
            On.RainWorld.Update += RainWorld_Update;
            menu = (Menu.Menu)manager.currentMainLoop;
            this.modsToDisable = new List<ModManager.Mod>();
            this.modsToEnable = new List<ModManager.Mod>();
            this.unknownMods = new List<string>();
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
                On.RainWorld.Update -= RainWorld_Update;
                if (dialogBox != null)
                {
                    dialogBox.RemoveSprites();
                    manager.dialog = null;
                    manager.ShowNextDialog();
                    dialogBox = null;
                }

                OnFinish?.Invoke(this);
                manager.rainWorld.options.Save();
            }
        }

        public void ShowConfirmation(List<ModManager.Mod> modsToEnable, List<ModManager.Mod> modsToDisable, List<string> unknownMods)
        {
            modMismatchString = menu.Translate("Mod mismatch detected.") + Environment.NewLine;

            Action confirmProceed = () =>
            {
                Start(filesInBadState);
            };

            Action cancelProceed = () =>
            {
                manager.dialog = null;
                requiresRestartDialog = null;
                OnlineManager.LeaveLobby();
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
            };

            if (modsToEnable.Count > 0)
            {
                modMismatchString += Environment.NewLine + menu.Translate("Mods that will be enabled: ") + string.Join(", ", modsToEnable.ConvertAll(mod => mod.LocalizedName));
                this.modsToEnable = modsToEnable;
            }
            if (modsToDisable.Count > 0)
            {
                modMismatchString += Environment.NewLine + menu.Translate("Mods that will be disabled: ") + string.Join(", ", modsToDisable.ConvertAll(mod => mod.LocalizedName));
                this.modsToDisable = modsToDisable;
            }
            if (unknownMods.Count > 0)
            {
                modMismatchString += Environment.NewLine + menu.Translate("Unable to find the following mods, please install them: ") + string.Join(", ", unknownMods);
                this.unknownMods = unknownMods;
            }
            else
            {
                modMismatchString += Environment.NewLine + Environment.NewLine + menu.Translate("Please confirm to auto-apply these changes, or cancel and return to lobby");
            }

            checkUserConfirmation = new DialogConfirm(modMismatchString, new Vector2(480f, 320f), manager, confirmProceed, cancelProceed);

            manager.ShowDialog(checkUserConfirmation);
        }

        public new void Start(bool filesInBadState)
        {
            if (unknownMods.Count > 0)
            {
                manager.dialog = null;
                requiresRestartDialog = null;
                OnlineManager.LeaveLobby();
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
                return;
            }

            if (requiresRestartDialog != null)
            {
                manager.dialog = null;
                manager.ShowNextDialog();
                requiresRestartDialog = null;
            }

            base.Start(filesInBadState);
        }
    }
}

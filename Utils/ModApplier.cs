using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    internal class ModApplier : ModManager.ModApplyer
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
            On.ModManager.ModApplyer.ApplyModsThread += ModApplyer_ApplyModsThread;
            menu = (Menu.Menu)manager.currentMainLoop;
            this.modsToDisable = new List<ModManager.Mod>();
            this.modsToEnable = new List<ModManager.Mod>();
            this.unknownMods = new List<string>();

        }

        private void ModApplyer_ApplyModsThread(On.ModManager.ModApplyer.orig_ApplyModsThread orig, ModManager.ModApplyer self)
        {
            if (modsToDisable.Count > 0)
            {

                for (int i = 0; i < modsToDisable.Count; i++)
                {

                    var matchingMod = ModManager.InstalledMods.FirstOrDefault(m => m.id == modsToDisable[i].id);
                    if (matchingMod != null)
                    {
                        if (pendingEnabled[i] != matchingMod.enabled)
                        {
                            pendingEnabled[i] = false;
                        }

                    }

                }
            }

            if (modsToEnable.Count > 0)
            {
                for (int i = 0; i < modsToEnable.Count; i++)
                {

                    var matchingMod = ModManager.InstalledMods.FirstOrDefault(m => m.id == modsToEnable[i].id);

                    if (matchingMod != null)
                    {
                        matchingMod.enabled = false;
                    }
                }
            }
            orig(self);

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


            if (modsToEnable.Count > 0)
            {
                modMismatchString += Environment.NewLine + menu.Translate("Mods that have to be enabled: ") + string.Join(", ", modsToEnable.ConvertAll(mod => mod.LocalizedName));
                this.modsToEnable = modsToEnable;
            }
            if (modsToDisable.Count > 0)
            {
                modMismatchString += Environment.NewLine + menu.Translate("Mods that have to be disabled: ") + string.Join(", ", modsToDisable.ConvertAll(mod => mod.LocalizedName));
                this.modsToDisable = modsToDisable;
            }
            if (unknownMods.Count > 0)
            {
                modMismatchString += Environment.NewLine + menu.Translate("Unable to find the following mods, please install them: ") + string.Join(", ", unknownMods);
                this.unknownMods = unknownMods;
            }

            modMismatchString += Environment.NewLine + Environment.NewLine + menu.Translate("Please press OK to return to the lobby select menu");

            Action confirmProceed = () =>
            {
                manager.dialog = null;
                requiresRestartDialog = null;
                OnlineManager.LeaveLobby();
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);

                //Start(filesInBadState);
                
            };

            Action cancelProceed = () =>
            {
                manager.dialog = null;
                requiresRestartDialog = null;
                OnlineManager.LeaveLobby();
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);

            };


            //checkUserConfirmation = new DialogConfirm(modMismatchString, new Vector2(480f, 320f), manager, confirmProceed, cancelProceed);

            requiresRestartDialog = new DialogNotify(modMismatchString, manager, confirmProceed);

            //manager.ShowDialog(checkUserConfirmation);

            manager.ShowDialog(requiresRestartDialog);



        }

        public new void Start(bool filesInBadState) // todo fix me
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

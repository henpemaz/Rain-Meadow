using Menu;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    internal class ModApplier : ModManager.ModApplyer
    {
        public DialogAsyncWait dialogBox;
        public DialogNotify requiresRestartDialog;
        private readonly Menu.Menu menu;
        private List<ModManager.Mod> modsToEnable;
        private List<ModManager.Mod> modsToDisable;


        public event Action<ModApplier> OnFinish;

        public ModApplier(ProcessManager manager, List<bool> pendingEnabled, List<int> pendingLoadOrder) : base(manager, pendingEnabled, pendingLoadOrder)
        {
            On.RainWorld.Update += RainWorld_Update;
            On.ModManager.ModApplyer.ApplyModsThread += ModApplyer_ApplyModsThread;
            menu = (Menu.Menu)manager.currentMainLoop;
            this.modsToDisable = new List<ModManager.Mod>();
            this.modsToEnable = new List<ModManager.Mod>();

        }

        private void ModApplyer_ApplyModsThread(On.ModManager.ModApplyer.orig_ApplyModsThread orig, ModManager.ModApplyer self)
        {
            if (modsToDisable.Count > 0)
            {
                for (int i = 0; i < modsToDisable.Count; i++)
                {
                    int installedModIndex = ModManager.InstalledMods.FindIndex(mod => mod.id == modsToDisable[i].id);
                    if (installedModIndex != -1)
                    {
                        ModManager.InstalledMods[installedModIndex].enabled = false;
                        pendingEnabled[installedModIndex] = false;
                    }
                }
            }

            if (modsToEnable.Count > 0)
            {
                for (int i = 0; i < modsToEnable.Count; i++)
                {
                    int installedModIndex = ModManager.InstalledMods.FindIndex(mod => mod.id == modsToEnable[i].id);
                    if (installedModIndex != -1)
                    {
                        ModManager.InstalledMods[installedModIndex].enabled = false;
                        pendingEnabled[installedModIndex] = true;
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

        public bool ShowConfirmation(List<ModManager.Mod> modsToEnable, List<ModManager.Mod> modsToDisable, List<string> unknownMods)
        {
            string text = menu.Translate("Mod mismatch detected.") + Environment.NewLine;

            if (modsToEnable.Count > 0)
            {
                text += Environment.NewLine + menu.Translate("Mods that will be enabled: ") + string.Join(", ", modsToEnable.ConvertAll(mod => mod.LocalizedName));
                this.modsToEnable = modsToEnable;
            }
            if (modsToDisable.Count > 0)
            {
                text += Environment.NewLine + menu.Translate("Mods that will be disabled: ") + string.Join(", ", modsToDisable.ConvertAll(mod => mod.LocalizedName));
                this.modsToDisable = modsToDisable;
            }
            if (unknownMods.Count > 0)
            {
                text += Environment.NewLine + menu.Translate("Unable to find the following mods, please install them: ") + string.Join(", ", unknownMods);
            }

            text += Environment.NewLine + Environment.NewLine + menu.Translate("Rain World may be restarted for these changes to take effect");

            requiresRestartDialog = new DialogNotify(text, new Vector2(480f, 320f), manager, () =>
            {
                Start(false);
            });

            manager.ShowDialog(requiresRestartDialog);
            return false;
        }

        public new void Start(bool filesInBadState)
        {


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

using Menu;
using System;
using System.Collections.Generic;
using System.Threading;
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
        private List<ModManager.Mod> modsToEnable;
        private List<ModManager.Mod> modsToDisable;

        public event Action<ModApplier> OnFinish;

        public ModApplier(ProcessManager manager, List<bool> pendingEnabled, List<int> pendingLoadOrder) : base(manager, pendingEnabled, pendingLoadOrder)
        {
            On.RainWorld.Update += RainWorld_Update;
            On.ModManager.ModApplyer.Update += ModApplyer_Update;
            menu = (Menu.Menu)manager.currentMainLoop;

        }

        private void ModApplyer_Update(On.ModManager.ModApplyer.orig_Update orig, ModManager.ModApplyer self)
        {
            orig(self);
            RainMeadow.Debug("IS MMF ENABLED " + ModManager.MMF);


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
        public bool ShowConfirmation(List<ModManager.Mod> modsToEnable, List<ModManager.Mod> modsToDisable, List<string> unknownMods)
        {
            string text = "Mod mismatch detected." + Environment.NewLine;
            string text = "Mod mismatch detected." + Environment.NewLine;

            if (modsToEnable.Count > 0)
            {
                text += Environment.NewLine + "Mods that will be enabled: " + string.Join(", ", modsToEnable.ConvertAll(mod => mod.LocalizedName));
                this.modsToEnable = modsToEnable;
            }
            if (modsToDisable.Count > 0)
            {
                text += Environment.NewLine + "Mods that will be disabled: " + string.Join(", ", modsToDisable.ConvertAll(mod => mod.LocalizedName));
            }
            this.modsToDisable = modsToDisable;
            if (unknownMods.Count > 0)
            {
                text += Environment.NewLine + "Unable to find those mods, please install them: " + string.Join(", ", unknownMods);
            }

            text += Environment.NewLine + Environment.NewLine + "Rain World will be restarted for these changes to take effect";

            requiresRestartDialog = new DialogNotify(text, new Vector2(480f, 320f), manager, () =>
            {
                Start(false);
            });

            manager.ShowDialog(requiresRestartDialog);
            return false;
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

            if (modsToDisable.Count > 0)
            {
                foreach (ModManager.Mod mod in this.modsToDisable)
                {
                    ModManager.ActiveMods.Remove(mod);
                    RainMeadow.Debug($"Disabled mod: {mod.name}");

                }

                foreach (var mod in ModManager.ActiveMods)
                {
                    RainMeadow.Debug("now enabled mods: " + mod.name);
                }

            }


            /*
                        if (modsToEnable.Count > 0 && modsToEnable is not null)
                        {
                            foreach (ModManager.Mod mod in this.modsToEnable)
                            {
                                ModManager.ActiveMods.Add(mod);
                                RainMeadow.Debug($"Enabled mod: {mod.name}");
                            }
                        }*/

           

            base.Start(filesInBadState);
            // All of this works but does not actually chnage the active mods, why?

        }
    }
}

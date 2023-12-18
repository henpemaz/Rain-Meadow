using Menu;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    internal class ModApplyer : ModManager.ModApplyer
    {
        public DialogBoxAsyncWait dialogBox;
        public DialogBoxNotify requiresRestartDialog;
        private readonly Menu.Menu menu;

        public event Action<ModApplyer> OnFinish;

        public ModApplyer(ProcessManager manager, List<bool> pendingEnabled, List<int> pendingLoadOrder) : base(manager, pendingEnabled, pendingLoadOrder)
        {
            On.RainWorld.Update += RainWorld_Update;
            On.Menu.SimpleButton.Update += SimpleButton_Update;
            On.Menu.SimpleButton.Clicked += SimpleButton_Clicked;
            menu = (Menu.Menu)UnityEngine.Object.FindObjectOfType<RainWorld>().processManager.currentMainLoop;
        }

        private void SimpleButton_Clicked(On.Menu.SimpleButton.orig_Clicked orig, SimpleButton self)
        {
            if (self == requiresRestartDialog.continueButton)
            {
                Start(false);
            }

            orig(self);
        }

        private void SimpleButton_Update(On.Menu.SimpleButton.orig_Update orig, SimpleButton self)
        {
            orig(self);
            if (self.owner != requiresRestartDialog)
            {
                self.buttonBehav.greyedOut = true;
            }
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
                On.Menu.SimpleButton.Update -= SimpleButton_Update;
                if (dialogBox != null)
                {
                    menu.pages[0].subObjects.Remove(dialogBox);
                    dialogBox.RemoveSprites();
                    dialogBox = null;
                }

                OnFinish?.Invoke(this);
                manager.rainWorld.options.Save();
            }
        }

        public void ShowConfirmation(List<ModManager.Mod> modsToEnable, List<ModManager.Mod> modsToDisable, List<string> unknownMods)
        {
            // TODO: Disable all ui elements

            string text = "Mod mismatch detected" + Environment.NewLine;

            if (modsToEnable.Count > 0) text += Environment.NewLine + "Mods that will be enabled: " + string.Join(", ", modsToEnable.ConvertAll(mod => mod.LocalizedName));
            if (modsToDisable.Count > 0) text += Environment.NewLine + "Mods that will be disabled: " + string.Join(", ", modsToDisable.ConvertAll(mod => mod.LocalizedName));
            if (unknownMods.Count > 0) text += Environment.NewLine + "Unable to find those mods, please install them: " + string.Join(", ", unknownMods);

            requiresRestartDialog = new DialogBoxNotify(menu, menu.pages[0], text, "MEADOW-APPLY_MODS", new Vector2(menu.manager.rainWorld.options.ScreenSize.x / 2f - 240f + (1366f - this.manager.rainWorld.options.ScreenSize.x) / 2f, 224f), new Vector2(480f, 320f), false);
            menu.pages[0].subObjects.Add(requiresRestartDialog);
        }

        public new void Start(bool filesInBadState)
        {
            if (requiresRestartDialog != null)
            {
                menu.pages[0].subObjects.Remove(requiresRestartDialog);
                requiresRestartDialog.RemoveSprites();
                requiresRestartDialog = null;
            }

            dialogBox = new DialogBoxAsyncWait(menu, menu.pages[0], "Applying mods...", new Vector2(manager.rainWorld.options.ScreenSize.x / 2f - 240f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, 224f), new Vector2(480f, 320f), false);

            menu.pages[0].subObjects.Add(dialogBox);

            base.Start(filesInBadState);
        }
    }
}

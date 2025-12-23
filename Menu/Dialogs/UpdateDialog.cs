using Menu;
using Menu.Remix.MixedUI;
using System;
using UnityEngine;

namespace RainMeadow
{
    public class UpdateDialog : MenuDialogBox
    {
        public static string Text
        {
            get
            {
                return Utils.Translate("Rain Meadow version ") + RainMeadow.NewVersionAvailable + Utils.Translate(" is now available.") 
                    + Environment.NewLine + Environment.NewLine +
                    Utils.Translate("Update to join the newest lobbies and get the latest features & fixes.");
            }
        }

        public static string HelpText
        {
            get
            {
                return Utils.Translate("For Steam: Restart your game, if Rain Meadow doesn't update automatically resubscribe to force an update.")
                     + Environment.NewLine + Environment.NewLine +
                     Utils.Translate("For Other Platforms: Visit our GitHub releases page to download the latest release.")
                     + Environment.NewLine + Environment.NewLine +
                     Utils.Translate("Updating won't affect your save data.");
            }
        }

        private SimpleButton okButton;
        private SimpleButton helpButton;
        private SimpleButton backButton;

        private bool helpScreen = false;

        public UpdateDialog(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, bool forceWrapping = false) 
            : base(menu, owner, Text, pos, size, forceWrapping)
        {
            Populate();
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            switch (message)
            {
                case "Continue":
                    Clear();
                    if (menu is LobbySelectMenu lobbySelectMenu)
                    {
                        lobbySelectMenu.HideDialog();
                    }
                    break;
                case "Help":
                    Clear();
                    descriptionLabel.text = HelpText.WrapText(bigText: false, size.x - 40f, false);
                    helpScreen = true;
                    Populate();
                    break;
                case "Ret":
                    Clear();
                    descriptionLabel.text = Text.WrapText(bigText: false, size.x - 40f, false);
                    helpScreen = false;
                    Populate();
                    break;
            }
        }

        public void Populate()
        {
            if (helpScreen)
            {
                backButton = new SimpleButton(menu, this, menu.Translate("BACK"), "Ret",
                    new Vector2((size.x * 0.5f) - 55f, Mathf.Max(size.y * 0.04f, 7f)), new Vector2(110f, 30f));
                subObjects.Add(backButton);
            }
            else
            {
                okButton = new SimpleButton(menu, this, menu.Translate("CONTINUE"), "Continue",
                    new Vector2((size.x * 0.5f) - 55f - 110f, Mathf.Max(size.y * 0.04f, 7f)), new Vector2(110f, 30f));
                helpButton = new SimpleButton(menu, this, menu.Translate("How to Update"), "Help",
                    new Vector2((size.x * 0.5f) - 55f + 110f, Mathf.Max(size.y * 0.04f, 7f)), new Vector2(110f, 30f));
                subObjects.Add(okButton);
                subObjects.Add(helpButton);
            }
        }

        public void Clear()
        {
            if (okButton != null)
            {
                okButton.RemoveSprites();
                subObjects.Remove(okButton);
                page.selectables.Remove(okButton);
            }
            if (helpButton != null)
            {
                helpButton.RemoveSprites();
                subObjects.Remove(helpButton);
                page.selectables.Remove(helpButton);
            }
            if (backButton != null)
            {
                backButton.RemoveSprites();
                subObjects.Remove(backButton);
                page.selectables.Remove(backButton);
            }
            menu.selectedObject = null;
            page.lastSelectedObject = null;
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            Clear();
        }
    }
}

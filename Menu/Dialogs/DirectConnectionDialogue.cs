using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;
namespace RainMeadow
{
    public class DirectConnectionDialogue : MenuDialogBox
    {
        protected MenuTabWrapper tabWrapper;
        public SymbolButton cancelButton;
        public SimpleButton continueButton;
        public OpTextBox IPBox;
        public UIelementWrapper textBoxWrapper;

        public DirectConnectionDialogue(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, bool forceWrapping = false)
            : base(menu, owner, "Direct Connection... ", pos, size, forceWrapping)
        {
            this.tabWrapper = new MenuTabWrapper(menu, this);
            subObjects.Add(tabWrapper);

            Vector2 center = size / 2f;
            IPBox = new OpTextBox(new Configurable<string>(""), center + new Vector2(-80f, -15f), 160f);
            IPBox.accept = OpTextBox.Accept.StringASCII;
            IPBox.allowSpace = true;

            textBoxWrapper = new UIelementWrapper(this.tabWrapper, IPBox);

            Vector2 where = new Vector2((center.x - 55f), 20f);

            continueButton = new SimpleButton(menu, this, menu.Translate("CONFIRM"), "DIRECT_JOIN", where, new Vector2(110f, 30f));
            subObjects.Add(continueButton);

            cancelButton = new SymbolButton(menu, this, "Menu_Symbol_Clear_All", "HIDE_DIALOG", size - new Vector2(40f, 40f));
            subObjects.Add(cancelButton);
        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            tabWrapper.RemoveSprites();
            continueButton.RemoveSprites();
        }


        public static IPEndPoint? GetEndPointByName(string name)
        {
            string[] parts = name.Split(':');
            if (parts.Length != 2) {
                RainMeadow.Debug("Invalid IP format without colon: " + name);
                return null;
            }


            IPAddress address;
            try {
                address = IPAddress.Parse(parts[0]);
            } catch (FormatException) {
                RainMeadow.Debug("Invalid IP format: " + parts[0]);
                return null;
            }

            if (!short.TryParse(parts[1], out short port)) {
                RainMeadow.Debug("Invalid port format: " + parts[1]);
                return null;
            }
            
            return new IPEndPoint(address, port);
        }
    }
}

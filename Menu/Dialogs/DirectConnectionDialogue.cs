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
    public class DirectConnectionDialogue : MenuDialogBox, CheckBox.IOwnCheckBox
    {
        protected MenuTabWrapper tabWrapper;
        public SymbolButton cancelButton;
        public SimpleButton continueButton;
        public OpTextBox IPBox;

        public CheckBox passwordCheckBox;
        public OpTextBox passwordBox;
        public UIelementWrapper textBoxWrapper;
        public UIelementWrapper passwordBoxWrapper;
        public UIelementWrapper passwordLabelWrapper;


        void CheckBox.IOwnCheckBox.SetChecked(Menu.CheckBox box, bool check) {
            if (box == passwordCheckBox) {
                passwordBox.greyedOut = !check;
                if (!check) {
                    passwordBox.value = "";
                }
            }
        }


        bool CheckBox.IOwnCheckBox.GetChecked(Menu.CheckBox box) {
            if (box == passwordCheckBox) return !passwordBox.greyedOut;
            return false;
        }

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

            Vector2 passwordpos = center + new Vector2(-80f, 30f);


            passwordLabelWrapper = new UIelementWrapper(this.tabWrapper, new OpLabel(center + new Vector2(-80f, -40f), new Vector2(100f, 20f), "Password:"));
            passwordBox = new OpTextBox(new Configurable<string>(""), center + new Vector2(-80f, -60f), 160f);
            passwordBox.greyedOut = true;
            passwordBox.accept = OpTextBox.Accept.StringASCII;
            passwordBox.allowSpace = true;

            passwordBoxWrapper = new UIelementWrapper(this.tabWrapper, passwordBox);
            passwordCheckBox = new CheckBox(menu, this, this, center + new Vector2(-110f, -60f), 0, "", "");
            subObjects.Add(passwordCheckBox);


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



    }
}

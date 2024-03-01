using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Menu;
using Menu.Remix;
using RWCustom;
namespace RainMeadow
{
    public class CustomInputDialogueBox : MenuDialogBox
    {
        protected MenuTabWrapper tabWrapper;

        public SimpleButton continueButton;
        public OpTextBox textBox;
        public UIelementWrapper textBoxWrapper;

        public CustomInputDialogueBox(Menu.Menu menu, MenuObject owner, string text, string signalText, Vector2 pos, Vector2 size, bool forceWrapping = false)
            : base(menu, owner, text, pos, size, forceWrapping)
        {
            owner.subObjects.Add(this.tabWrapper = new MenuTabWrapper(menu, owner));

            Vector2 center = new Vector2((pos.x + size.x / 2f), (pos.y + size.y / 2f));
            var placePos = center;
            placePos.x -= 80f;
            placePos.y -= 15f;
            textBox = new OpTextBox(new Configurable<string>(""), placePos, 160f);
            textBoxWrapper = new UIelementWrapper(this.tabWrapper, textBox);

            Vector2 where = new Vector2((pos.x + size.x / 2f - 55f),(pos.y + 20f));

            continueButton = new SimpleButton(menu, owner, menu.Translate("CONFIRM"),signalText, where, new Vector2(110f, 30f));
            owner.subObjects.Add(continueButton);

        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            tabWrapper.RemoveSprites();
            continueButton.RemoveSprites();

            owner.subObjects.Remove(tabWrapper);
            owner.subObjects.Remove(continueButton);
            while (base.page.selectables.Contains(continueButton))
            {
                base.page.selectables.Remove(continueButton);
            }

            menu.selectedObject = null;
            base.page.lastSelectedObject = null;
        }
    }
}

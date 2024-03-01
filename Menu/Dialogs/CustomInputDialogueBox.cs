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

        public SimplerButton continueButton;
        public OpTextBox textBox;
        public UIelementWrapper textBoxWrapper;

        public CustomInputDialogueBox(Menu.Menu menu, MenuObject owner, string text, Vector2 pos, Vector2 size, bool forceWrapping = false)
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

            continueButton = new SimplerButton(menu, owner, menu.Translate("CONFIRM"), where, new Vector2(110f, 30f));
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

        public static Vector2 CalculateDialogBoxSize(string displayText, bool dialogUsesWordWrapping = true)
        {
            string text = Custom.ReplaceWordWrapLineDelimeters(displayText).Replace("\r\n", "\n");
            float num = Mathf.Clamp(LabelTest.GetWidth(text) + 44f, 200f, 600f);
            if (dialogUsesWordWrapping)
            {
                text = displayText.WrapText(bigText: false, num, forceWrapping: true);
            }

            float num2 = LabelTest.LineHeight(bigText: false);
            if (InGameTranslator.LanguageID.UsesLargeFont(Custom.rainWorld.inGameTranslator.currentLanguage))
            {
                num2 *= 2f;
            }

            return new Vector2(num, Mathf.Max(num2 * (float)text.Split('\n').Length + 100f, 120f));
        }
    }
}

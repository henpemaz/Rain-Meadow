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
    public class CustomDialogBoxForPassword : MenuDialogBox
    {
        public SimpleButton continueButton;
        public float timeOut = 1f;
        public SimplerButton passwordInputConfirmation;

        public CustomDialogBoxForPassword(UIelementWrapper textWrap, Menu.Menu menu, MenuObject owner, string text, string signalText, Vector2 pos, Vector2 size, bool forceWrapping = false)
            : base(menu, owner, text, pos, size, forceWrapping)
        {
            continueButton = new SimpleButton(menu, owner, menu.Translate("CONFIRM"), signalText, new Vector2((int)(pos.x + size.x / 2f - 55f), (int)(pos.y + 20f)), new Vector2(110f, 30f));

            passwordInputConfirmation.OnClick += (_) =>
            {
                RainMeadow.Debug("CLICKED");
            };

            owner.subObjects.Add(continueButton);
            owner.subObjects.Add(textWrap);
            owner.subObjects.Add(passwordInputConfirmation);

            base.page.selectables.Add(continueButton);
            for (int i = 0; i < 4; i++)
            {
                continueButton.nextSelectable[i] = continueButton;
            }

            menu.selectedObject = continueButton;
            base.page.lastSelectedObject = continueButton;
            continueButton.buttonBehav.greyedOut = true;
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            continueButton.RemoveSprites();
            owner.subObjects.Remove(continueButton);
            while (base.page.selectables.Contains(continueButton))
            {
                base.page.selectables.Remove(continueButton);
            }

            menu.selectedObject = null;
            base.page.lastSelectedObject = null;
        }

        public override void Update()
        {
            base.Update();
            timeOut -= 0.025f;
            if (timeOut < 0f)
            {
                timeOut = 0f;
                continueButton.buttonBehav.greyedOut = false;
            }
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

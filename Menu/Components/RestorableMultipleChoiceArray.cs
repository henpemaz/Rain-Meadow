using Menu;
using RainMeadow.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    public class RestorableMultipleChoiceArray(Menu.Menu menu, MenuObject owner, MultipleChoiceArray.IOwnMultipleChoiceArray reportTo, Vector2 pos, string text, string IDString, float textWidth, float width, int buttonsCount, bool textInBoxes, bool splitText) : MultipleChoiceArray(menu, owner, reportTo, pos, text, IDString, textWidth, width, buttonsCount, textInBoxes, splitText), IRestorableMenuObject
    {
        public void RestoreSprites()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                MultipleChoiceButton btn = buttons[i];

                if (textInBoxes) btn.label.Container.AddChild(btn.label.label);
                else btn.Container.AddChild(btn.symbolSprite);

                for (int j = 0; j < btn.roundedRect.sprites.Length; j++)
                    btn.roundedRect.Container.AddChild(btn.roundedRect.sprites[j]);
            }

            for (int k = 0; k < lines.Length; k++) Container.AddChild(lines[k]);
            label.Container.AddChild(label.label);
        }

        public void RestoreSelectables()
        {
            for (int i = 0; i < buttons.Length; i++) page.selectables.Add(buttons[i]);
        }
    }
}

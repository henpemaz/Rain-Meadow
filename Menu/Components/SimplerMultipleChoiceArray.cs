using System;
using Menu;
using RainMeadow.UI.Interfaces;
using UnityEngine;

namespace RainMeadow.UI.Components;

public class SimplerMultipleChoiceArray : MultipleChoiceArray, MultipleChoiceArray.IOwnMultipleChoiceArray, IRestorableMenuObject
{
    private int selectedButtonIndex;
    public event Action<int>? OnClick;

    public SimplerMultipleChoiceArray(Menu.Menu menu, MenuObject owner, Vector2 pos, string text, float textWidth, float width, int buttonsCount, bool textInBoxes = false, bool splitText = false)
        : base(menu, owner, null, pos, text, null, textWidth, width, buttonsCount, textInBoxes, splitText)
    {
        reportTo = this;
    }

    public int GetSelected(MultipleChoiceArray array)
    {
        if (array == this) return selectedButtonIndex;
        throw new Exception("Another MultipleChoiceArray is using a SimplerMultipleChoiceArray as a MultipleChoiceArray handler");
    }

    public void SetSelected(MultipleChoiceArray array, int i)
    {
        if (array != this) throw new Exception("Another MultipleChoiceArray is using a SimplerMultipleChoiceArray as a MultipleChoiceArray handler");
        selectedButtonIndex = i;
        OnClick?.Invoke(i);
    }

    public void RestoreSprites()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            MultipleChoiceButton btn = buttons[i];

            if (textInBoxes) Container.AddChild(btn.label.label);
            else Container.AddChild(btn.symbolSprite);

            for (int j = 0; j < btn.roundedRect.sprites.Length; j++)
                Container.AddChild(btn.roundedRect.sprites[j]);
        }

        for (int k = 0; k < lines.Length; k++) Container.AddChild(lines[k]);
        Container.AddChild(label.label);
    }

    public void RestoreSelectables()
    {
        for (int i = 0; i < buttons.Length; i++) page.selectables.Add(buttons[i]);
    }
}
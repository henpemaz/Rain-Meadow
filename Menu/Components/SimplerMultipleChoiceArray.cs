using System;
using Menu;
using UnityEngine;

namespace RainMeadow.UI.Components;

public class SimplerMultipleChoiceArray : MultipleChoiceArray, MultipleChoiceArray.IOwnMultipleChoiceArray
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
}
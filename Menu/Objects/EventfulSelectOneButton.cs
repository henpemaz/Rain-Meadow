using System;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class EventfulSelectOneButton(
        Menu.Menu menu,
        MenuObject owner,
        string displayText,
        string buttonGroupKey,
        Vector2 pos,
        Vector2 size,
        SelectOneButton[] buttonArray,
        int buttonArrayIndex,
        string description = ""
    )
        : SelectOneButton(
            menu,
            owner,
            displayText,
            buttonGroupKey,
            pos,
            size,
            buttonArray,
            buttonArrayIndex
        ),
            IHaveADescription
    {
        public string Description { get; set; } = description;

        public override void Clicked()
        {
            base.Clicked();
            OnClick?.Invoke(this);
        }

        public event Action<EventfulSelectOneButton> OnClick;
    }
}

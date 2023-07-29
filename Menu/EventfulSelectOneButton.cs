﻿using Menu;
using System;
using UnityEngine;

namespace RainMeadow
{
    public class EventfulSelectOneButton : SelectOneButton
    {
        public EventfulSelectOneButton(Menu.Menu menu, MenuObject owner, string displayText, string buttonGroupKey, Vector2 pos, Vector2 size, SelectOneButton[] buttonArray, int buttonArrayIndex, string description = "") : base(menu, owner, displayText, buttonGroupKey, pos, size, buttonArray, buttonArrayIndex)
        {
            this.description = description;
        }
        private readonly string description;
        public string Description => description;

        public override void Clicked() { base.Clicked(); OnClick?.Invoke(this); }
        public event Action<EventfulSelectOneButton> OnClick;
    }
}

using Menu;
using System;
using UnityEngine;

namespace RainMeadow
{
    internal class EventfulBigArrowButton : BigArrowButton
    {
        public EventfulBigArrowButton(Menu.Menu menu, MenuObject owner, Vector2 pos, int direction) : base(menu, owner, "", pos, direction)
        {
        }

        public override void Clicked() { base.Clicked(); OnClick?.Invoke(this); }
        public event Action<EventfulBigArrowButton> OnClick;
    }
}
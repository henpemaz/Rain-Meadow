using Menu;
using System;
using UnityEngine;

namespace RainMeadow
{
    public class EventfulScrollButton : LevelSelector.ScrollButton
    {
        public EventfulScrollButton(Menu.Menu menu, MenuObject owner, Vector2 pos, int direction, float width) : base(menu, owner, "", pos, direction)
        {
            this.size.x = width;
            this.roundedRect.size.x = width;
        }

        public override void Singal(MenuObject sender, string message) { OnClick?.Invoke(this); }
        public event Action<EventfulScrollButton> OnClick;
    }
}

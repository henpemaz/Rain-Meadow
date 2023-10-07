using Menu;
using System;
using UnityEngine;

namespace RainMeadow
{
    internal class EventfulHoldButton : HoldButton
    {
        public EventfulHoldButton(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, float fillTime) : base(menu, owner, displayText, "", pos, fillTime)
        {
        }

        public override void Update()
        {
            var was = hasSignalled;
            base.Update();
            if(!was && hasSignalled)
            {
                OnClick?.Invoke(this);
            }
        }
        public event Action<EventfulHoldButton> OnClick;
    }
}
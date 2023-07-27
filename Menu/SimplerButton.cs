using Menu;
using System;
using UnityEngine;

namespace RainMeadow
{
    public class SimplerButton : SimpleButton, IHaveADescription
    {
        public SimplerButton(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size, string description = "") : base(menu, owner, displayText, "", pos, size)
        {
            this.description = description;
        }
        private readonly string description;
        public string Description => description;

        public override void Clicked() { base.Clicked(); OnClick?.Invoke(this); }
        public event Action<SimplerButton> OnClick;
    }
}

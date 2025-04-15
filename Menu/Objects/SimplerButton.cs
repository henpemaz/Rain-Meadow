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

        public string description;
        public string Description
        {
            get => description;
            set => description = value;
        }

        public override void Clicked() { base.Clicked(); OnClick?.Invoke(this); }
        public event Action<SimplerButton> OnClick;
    }
}

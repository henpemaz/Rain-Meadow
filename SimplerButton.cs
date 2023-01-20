using Menu;
using System;
using UnityEngine;

namespace RainMeadow
{
    class SimplerButton : SimpleButton
        {
            public SimplerButton(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size) : base(menu, owner, displayText, "", pos, size){}
            public override void Clicked() { base.Clicked(); OnClick?.Invoke(this); }
            public event Action<SimplerButton> OnClick;
        }
}

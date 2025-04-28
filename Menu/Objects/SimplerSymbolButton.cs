using Menu;
using System;
using UnityEngine;

namespace RainMeadow
{
    public class SimplerSymbolButton : SymbolButton
    {
        public SimplerSymbolButton(Menu.Menu menu, MenuObject owner, string symbolName, string singalText, Vector2 pos) : base(menu, owner, symbolName, singalText, pos)
        {
            this.pos = pos;
            this.signalText = singalText;

        }

        public override void Clicked() { base.Clicked(); OnClick?.Invoke(this); }
        public event Action<SymbolButton> OnClick;
        public void ResetSubscriptions() => OnClick = delegate { };
    }
}

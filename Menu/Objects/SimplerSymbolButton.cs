using System;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class SimplerSymbolButton : SymbolButton
    {
        public SimplerSymbolButton(Menu.Menu menu, MenuObject owner, string symbolName, string singalText, Vector2 pos) : base(menu, owner, symbolName, singalText, pos)
        {
            this.pos = pos;
            this.signalText = singalText;
            this.symbolSprite = new FSprite(symbolName);
            this.Container.AddChild(this.symbolSprite);
            
        }

        public override void Clicked() { base.Clicked(); OnClick?.Invoke(this); }
        public event Action<SymbolButton> OnClick;
    }
}

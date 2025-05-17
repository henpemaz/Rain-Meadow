using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using UnityEngine;
using static RainMeadow.ButtonScroller;

namespace RainMeadow.UI.Components
{
    public class ScrollSymbolButton : SimplerSymbolButton, IPartOfButtonScroller
    {
        public float Alpha { get; set; } = 1;
        public Vector2 Pos { get => pos; set => pos = value; }
        public Vector2 Size { get => size; set => size = value; }
        public ScrollSymbolButton(Menu.Menu menu, MenuObject owner, string symbolName, string signalText, Vector2 pos, Vector2 size = default) : base(menu, owner, symbolName, signalText, pos)
        {
            this.size = size == default ? this.size : size;
        }
        public override void Update()
        {
            base.Update();
            buttonBehav.greyedOut = forceGreyOut || Alpha < 1;
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            roundedRect.size = size;
        }

        public bool forceGreyOut;
    }
}

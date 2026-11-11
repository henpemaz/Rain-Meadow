using Menu;
using RainMeadow.UI.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.UI.Systems
{
    public class ButtonScrollerPartObject : MenuScrollObject
    {
        public ButtonScroller.IPartOfButtonScroller scrollerObj;
        public override Vector2 LocalPos { get => scrollerObj.Pos; set => scrollerObj.Pos = value; }
        public override Vector2 Size { get => scrollerObj.Size; set => scrollerObj.Size = value; }
        public override float LocalAlpha { get => scrollerObj.Alpha; set => scrollerObj.Alpha = value; }

        public ButtonScrollerPartObject(ButtonScroller.IPartOfButtonScroller scrollerObj) : base((MenuObject)scrollerObj, true)
        {
            this.scrollerObj = scrollerObj;
        }
    }
}

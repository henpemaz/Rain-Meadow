using Menu;
using System;
using UnityEngine;

namespace RainMeadow
{
    public class Floater : MenuObject
    {
        private readonly PositionedMenuObject powner;
        private readonly Vector2 sinAmpl;
        private readonly Vector2 sinFreq;
        private readonly Vector2 target;
        private Vector2 rootPos;

        public Floater(Menu.Menu menu, PositionedMenuObject powner, Vector2 initOffset, Vector2 sinFreq, Vector2 sinAmpl) : base(menu, powner)
        {
            this.powner = powner;
            this.sinFreq = sinFreq;
            this.sinAmpl = sinAmpl;
            this.target = powner.pos;
            powner.pos += initOffset;
            powner.lastPos = powner.pos;
            this.rootPos = powner.pos;
        }

        public override void Update()
        {
            base.Update();
            rootPos = Vector2.Lerp(rootPos, target, (0.035f + ((target.y / 20000)) * 4) / 1.5f);
            powner.pos = rootPos
                + new Vector2(Mathf.Sin((Time.time * sinFreq.x) + (((float)target.y * Mathf.PI) / 200f))
                            , Mathf.Sin((Time.time * sinFreq.y) + (((float)target.y * Mathf.PI) / 200f)))
                    * sinAmpl;
                ;
        }
    }
}

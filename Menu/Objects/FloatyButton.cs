using Menu;
using System;
using UnityEngine;

namespace RainMeadow
{
    public class FloatyButton : SimplerButton
    {
        private float initx;
        private float inity;
        private float inittime;
        private float hypox;
        public FloatyButton(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size, string description = "") : base(menu, owner, displayText, pos, size, description)
        {
            this.initx = pos.x;
            this.inity = pos.y;
            //pos.x = initx + Mathf.Sin((Time.time * 4f) + (((float)pos.y * Mathf.PI) / 200f)) * 5f;
            //pos.y = inity + Mathf.Sin((Time.time * 3.5f) + (((float)pos.y * Mathf.PI) / 200f));
            pos.x += 1000;
            hypox = pos.x;
            base.pos = pos;
            base.lastPos = pos;
        }

        public override void Update()
        {
            base.Update();
            hypox = Mathf.Lerp(hypox, initx, (0.035f + ((inity / 20000)) * 4) / 1.5f);
            pos.x = Mathf.Sin((Time.time * 4f) + (((float)inity * Mathf.PI) / 200f)) * 5f + hypox;
            pos.y = inity + Mathf.Sin((Time.time * 3.5f) + (((float)inity * Mathf.PI) / 200f));
        }
    }
}

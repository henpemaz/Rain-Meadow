using Menu;
using System;
using UnityEngine;

namespace RainMeadow
{
    public class SimplerButton : SimpleButton, IHaveADescription
    {
        private float initx;
        private float inity;
        private bool ispausemenu;
        private float inittime;

        private float hypox;
        public SimplerButton(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size, string description = "", bool ispausemenu = false) : base(menu, owner, displayText, "", pos, size)
        {
            this.description = description;
            this.initx = pos.x;
            this.inity = pos.y;
            if (ispausemenu)
            {
                //pos.x = initx + Mathf.Sin((Time.time * 4f) + (((float)pos.y * Mathf.PI) / 200f)) * 5f;
                //pos.y = inity + Mathf.Sin((Time.time * 3.5f) + (((float)pos.y * Mathf.PI) / 200f));
                pos.x += 1000;
            }
            hypox = pos.x;
            base.pos = pos;
            base.lastPos = pos;
            this.ispausemenu = ispausemenu;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (ispausemenu)
            {
                hypox = Mathf.Lerp(hypox, initx, (0.035f+((inity/20000))*4)/1.5f);
                pos.x =  Mathf.Sin((Time.time * 4f) + (((float)inity * Mathf.PI) / 200f)) * 5f + hypox;
                pos.y = inity + Mathf.Sin((Time.time * 3.5f) + (((float)inity * Mathf.PI) / 200f)) ;
            }
        }

        private readonly string description;
        public string Description => description;

        public override void Clicked() { base.Clicked(); OnClick?.Invoke(this); }
        public event Action<SimplerButton> OnClick;
    }
}

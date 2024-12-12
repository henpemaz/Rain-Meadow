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
        private float xOffset;
        public float progress = 0f;

        public SimplerButton(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size, string description = "", bool ispausemenu = false) : base(menu, owner, displayText, "", pos, size)
        {
            this.description = description;
            this.initx = pos.x;
            this.inity = pos.y;
            if (ispausemenu) pos.x += 600; 
            xOffset = pos.x;
            base.pos = pos;
            base.lastPos = pos;
            this.ispausemenu = ispausemenu;
        }
        public override void Update()
        {
            base.Update(); 
            if (ispausemenu)
            {
                float targetxOffset = Mathf.Lerp(initx + 600f, initx, 1f - Mathf.Pow(1f - progress, 3));
                xOffset = Mathf.Lerp(xOffset, targetxOffset, inity / (xOffset > targetxOffset ? 1800f : 520f) - 0.05f);
                pos.x = xOffset + Mathf.Sin((Time.time * 3.5f) + (inity * Mathf.PI / 200f)) * progress * 3.5f;
                pos.y = inity + Mathf.Sin((Time.time * 3f) + (inity * Mathf.PI / 200f)) * progress;
            }
        }

        private readonly string description;
        public string Description => description;

        public override void Clicked() { base.Clicked(); OnClick?.Invoke(this); }
        public event Action<SimplerButton> OnClick;
    }
}

using Menu;
using System;
using UnityEngine;

namespace RainMeadow
{
    public class FloatyButton : SimplerButton
    {
        private float initx;
        private float inity;
        private float xPos;
        public float progress = 0f;
        public FloatyButton(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size, string description = "") : base(menu, owner, displayText, pos, size, description)
        {
            this.initx = pos.x;
            this.inity = pos.y;
            pos.x += 600;
            xPos = pos.x;
            base.pos = pos;
            base.lastPos = pos;
        }
        public override void Update()
        {
            base.Update();
            float targetxPos = Mathf.Lerp(initx + 600f, initx, 1f - Mathf.Pow(1f - progress, 3));
            xPos = Mathf.Lerp(xPos, targetxPos, inity / (xPos > targetxPos ? 1800f : 520f) - 0.05f);
            pos.x = xPos + Mathf.Sin((Time.time * 3.5f) + (inity * Mathf.PI / 200f)) * progress * 3.5f;
            pos.y = inity+ Mathf.Sin((Time.time * 3f)   + (inity * Mathf.PI / 200f)) * progress;
        }
    }
}

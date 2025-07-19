using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class Floater : MenuObject
    {
        //"This place is not a place of honor... no highly esteemed deed is commemorated here... nothing valued is here."

        private PositionedMenuObject powner;
        private readonly Vector2 sinAmpl;
        private readonly Vector2 sinFreq;
        private readonly Vector2 initOffset;

        private float progress;
        private readonly Vector2 rootroot;
        private Vector2 centerpos;

        public Floater(PauseMenu menu, PositionedMenuObject powner, Vector2 initOffset, Vector2 sinFreq, Vector2 sinAmpl) : base(menu, powner)
        {
            this.powner = powner;
            rootroot = powner.pos;
            centerpos = rootroot + initOffset;

            powner.pos = centerpos;
            powner.lastPos = centerpos;

            this.initOffset = initOffset;
            this.sinFreq = sinFreq;
            this.sinAmpl = sinAmpl;
        }

        public override void Update()
        {
            base.Update();
            progress = (menu as PauseMenu).blackFade;

            // slide in
            Vector2 targetPos = Vector2.Lerp(rootroot + initOffset, rootroot, 1f - Mathf.Pow(1f - progress, 2.4f));
            centerpos = Vector2.Lerp(centerpos, targetPos, rootroot.y / (centerpos.x > targetPos.x ? 1800f : 520f) + 0.05f);
            
            // oscilate
            if (RainMeadow.rainMeadowOptions.DisableMeadowPauseAnimation.Value)
            {
                powner.pos = centerpos + Vector2.one * progress;
            }
            else
            {
                powner.pos = centerpos + new Vector2(
                    Mathf.Sin((Time.time * sinFreq.x) + (rootroot.y * Mathf.PI / 200f)) * progress,
                    Mathf.Sin((Time.time * sinFreq.y) + (rootroot.y * Mathf.PI / 200f)) * progress) * sinAmpl;
            }
        }
    }
}

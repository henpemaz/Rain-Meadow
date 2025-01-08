using Menu;
using Steamworks;
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
        private readonly Vector2 initOffset;
        private Vector2 anchorlastpos;
        private Vector2 rootPos;
        private Vector2 bufferpos;
        public float progress = 0f;
        private float lastprogress = 0f;
        float velocityorsmthlol;
        public Floater(Menu.Menu menu, PositionedMenuObject powner, float velocityorsmthlol, Vector2 initOffset, Vector2 sinFreq, Vector2 sinAmpl) : base(menu, powner)
        {
            this.powner = powner;
            this.sinFreq = sinFreq;
            this.sinAmpl = sinAmpl;
            this.initOffset = initOffset;
            this.target = powner.pos;
            powner.pos += initOffset;
            powner.lastPos = powner.pos;
            anchorlastpos = powner.lastPos;
            this.rootPos = powner.pos;
            this.velocityorsmthlol = velocityorsmthlol;
        }

        public override void Update()
        {
            base.Update();
            Vector2 targetPos = Vector2.Lerp(target + initOffset, target, 1f - Mathf.Pow(1f - progress, 2.4f));
            rootPos = Vector2.Lerp(rootPos, targetPos, Mathf.Lerp((velocityorsmthlol / (lastprogress < progress ? 10f : 2f)), 0.9f, (lastprogress < progress ? 0f : (Mathf.Pow(1f - progress, 2.5f)))));
            //actually, what if instead of all this big damn formula, we just made them fade out? Whatever.
            lastprogress = progress;
            anchorlastpos = powner.pos;
            powner.lastPos = anchorlastpos;
            bufferpos = rootPos + new Vector2(
                Mathf.Sin((Time.time * sinFreq.x) + (target.y * Mathf.PI / 200f)) * progress,
                Mathf.Sin((Time.time * sinFreq.y) + (target.y * Mathf.PI / 200f)) * progress) * sinAmpl;
        }
        public override void GrafUpdate(float timeStacker)
        {
            if (bufferpos != Vector2.zero) { powner.pos = bufferpos; }
            powner.lastPos = anchorlastpos;
            base.GrafUpdate(timeStacker);
        }
    }
}

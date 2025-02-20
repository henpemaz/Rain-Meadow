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
        private readonly Vector2[] targets;
        private readonly Vector2 initOffset;
        private Vector2[] rootPoses;

        public float progress = 0f;
        private float lastprogress = 0f;
        float velocityorsmthlol;
        private Vector2 rootroot;
        public Floater(Menu.Menu menu, PositionedMenuObject powner, float velocityorsmthlol, Vector2 initOffset, Vector2 sinFreq, Vector2 sinAmpl) : base(menu, powner)
        {
            this.powner = powner;
            rootroot = powner.pos;

            this.sinFreq = sinFreq;
            this.sinAmpl = sinAmpl;
            this.initOffset = initOffset;

            targets =           new Vector2[powner.subObjects.Count];
            rootPoses =         new Vector2[powner.subObjects.Count];

            for (int i = 0; i < powner.subObjects.Count; i++)
            {
                var thing = powner.subObjects[i];
                if (thing is PositionedMenuObject PMO)
                {
                    targets[i] = PMO.pos;
                    PMO.pos += initOffset;
                    rootPoses[i] = PMO.pos;
                    powner.subObjects[i] = PMO;
                }
            }
            this.velocityorsmthlol = velocityorsmthlol;
        }

        public override void Update()
        {
            base.Update();

            for (int i = 0; i < powner.subObjects.Count; i++)
            {
                Vector2 dummala = new Vector2(0f, 0f);
                var thing = powner.subObjects[i];
                if (thing is PositionedMenuObject PMO) 
                {
                    Vector2 targetPos = Vector2.Lerp(targets[i] + initOffset, targets[i], 1f - Mathf.Pow(1f - progress, 2.4f));
                    rootPoses[i] = Vector2.Lerp(rootPoses[i], targetPos, Mathf.Lerp((velocityorsmthlol / (lastprogress < progress ? 10f : 2f)), 0.9f, (lastprogress < progress ? 0f : (Mathf.Pow(1f - progress, 2.5f)))));

                    if (RainMeadow.rainMeadowOptions.DisableMeadowPauseAnimation.Value)
                    {
                        dummala = rootPoses[i] + Vector2.one * progress;
                    }
                    else
                    {
                        dummala = rootPoses[i] + new Vector2(
                            Mathf.Sin((Time.time * sinFreq.x) + (rootroot.y * Mathf.PI / 200f)) * progress,
                            Mathf.Sin((Time.time * sinFreq.y) + (rootroot.y * Mathf.PI / 200f)) * progress) * sinAmpl;
                    }

                    PMO.pos = dummala;
                    powner.subObjects[i] = PMO;
                    //FUCK THIS, DUCKTAPE TIMEEEEE
                    if (powner is FloatyCheckBox hehe) { if (i == 0) hehe.SetHummala(dummala, progress); }
                    else if (powner is FloatySlider ahah) { if (i == 0) ahah.SetHummala(dummala, progress); };//
                }
            }
            lastprogress = progress;
        }
    }
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

            if (RainMeadow.rainMeadowOptions.DisableMeadowPauseAnimation.Value)
            {
                pos.x = xPos + progress * 3.5f;
                pos.y = inity + progress;
            }
            else
            {
                pos.x = xPos + Mathf.Sin((Time.time * 3.5f) + (inity * Mathf.PI / 200f)) * progress * 3.5f;
                pos.y = inity + Mathf.Sin((Time.time * 3f) + (inity * Mathf.PI / 200f)) * progress;
            }
        }
    }
    public class FloatyCheckBox : CheckBox
    {
        private float progress;
        private Vector2 hummala;
        private Vector2 dummala;
        
        public FloatyCheckBox(Menu.Menu menu, MenuObject owner, IOwnCheckBox reportTo, Vector2 pos, float textWidth, string displayText, string IDString, bool textOnRight = false)
            : base(menu, owner, reportTo, pos, textWidth, displayText, IDString, textOnRight)
        {
        }
        public void SetHummala(Vector2 newhummala, float progress)
        {
            this.progress = progress;   
            dummala = hummala;
            hummala = newhummala;
        }
        public override void GrafUpdate(float timeStacker)
        {
            if (progress < 0.01f) 
            { 
                pos += new Vector2(0f, 20000f); 
                lastPos += new Vector2(0f, 20000f);
                base.pos += new Vector2(0f, 20000f);
                base.lastPos += new Vector2(0f, 20000f);

            }
            base.GrafUpdate(timeStacker);
            
            Vector2 hehe = Vector2.Lerp(dummala, hummala, timeStacker) + pos;
            Vector2 teehee = Vector2.Lerp(lastSize, size, timeStacker);
            symbolSprite.x = hehe.x + teehee.x / 2f;
            symbolSprite.y = hehe.y + teehee.y / 2f;

            if (progress < 0.01f)
            {
                pos -= new Vector2(0f, 20000f); 
                lastPos -= new Vector2(0f, 20000f);
                base.pos -= new Vector2(0f, 20000f);
                base.lastPos -= new Vector2(0f, 20000f);
            }
        }
    }
    public class FloatySlider : HorizontalSlider
    {
        public Vector2 initpos;
        public float progress = 0f;
        private float lastprogress = 0f;

        private Vector2 hummala;
        private Vector2 dummala;
        private Vector2 badonka = new Vector2(8000f, 200);
        private Vector2 badabonka = new Vector2(8000f, 200);
        public FloatySlider(Menu.Menu menu, MenuObject owner, string text, Vector2 pos, Vector2 size, SliderID ID, bool subtleSlider)
            : base(menu, owner, text, pos, size, ID, subtleSlider)
        {
            lastPos = new Vector2(20000f, 0f);
            initpos = pos;
        }
        public override void Update()
        {
            base.Update();

            badabonka = badonka;
            Vector2 targetPos = Vector2.Lerp(initpos + new Vector2(750f, 0f), initpos, 1f - Mathf.Pow(1f - progress, 2.4f));
            badonka = Vector2.Lerp(badonka, targetPos, Mathf.Lerp((1.0f / (lastprogress < progress ? 10f : 2f)), 0.9f, (lastprogress < progress ? 0f : (Mathf.Pow(1f - progress, 2.5f)))));
            lastprogress = progress;
        }
        public void SetHummala(Vector2 newhummala, float progress)
        {
            this.progress = progress;
            dummala = hummala;
            hummala = newhummala;
        }
        public override void GrafUpdate(float timeStacker)
        {
            if (progress < 0.01f)
            {
                pos += new Vector2(0f, 20000f);
                lastPos += new Vector2(0f, 20000f);
                anchorPoint += new Vector2(0f, 20000f);
            }
            base.GrafUpdate(timeStacker);
            anchorPoint  = Vector2.Lerp(badabonka, badonka, timeStacker);

            Vector2 screehehe = Vector2.Lerp(dummala, hummala, timeStacker) + pos + (new Vector2(0, 4f));
            Vector2 screeteehee = Vector2.Lerp(lastSize, size, timeStacker);
            menuLabel.label.x = screehehe.x + screeteehee.x / 2f;
            menuLabel.label.y = screehehe.y + screeteehee.y / 2f;

            if (progress < 0.01f)
            {
                pos -= new Vector2(0f, 20000f);
                lastPos -= new Vector2(0f, 20000f);
                anchorPoint -= new Vector2(0f, 20000f);
            }
        }
    }
}

using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public class OnlinePlayerArrow : OnlinePlayerHudPart
    {
        public FSprite arrowSprite;
        public FSprite gradient;
        public FLabel label;
        public int counter;
        public int fadeAwayCounter;
        public float alpha;
        public float lastAlpha;
        public float blink;
        public float lastBlink;

        public OnlinePlayerArrow(PlayerSpecificOnlineHud owner) : base(owner)
        {
            this.owner = owner;
            this.pos = new Vector2(-1000f, -1000f);
            this.lastPos = this.pos;
            this.gradient = new FSprite("Futile_White", true);
            this.gradient.shader = owner.hud.rainWorld.Shaders["FlatLight"];
            this.gradient.color = new Color(0f, 0f, 0f);
            owner.hud.fContainers[0].AddChild(this.gradient);
            this.gradient.alpha = 0f;
            this.gradient.x = -1000f;
            this.label = new FLabel(Custom.GetFont(), owner.clientSettings.owner.id.name);
            this.label.color = owner.clientSettings.SlugcatColor();
            owner.hud.fContainers[0].AddChild(this.label);
            this.label.alpha = 0f;
            this.label.x = -1000f;
            this.arrowSprite = new FSprite("Multiplayer_Arrow", true);
            this.arrowSprite.color = owner.clientSettings.SlugcatColor();
            owner.hud.fContainers[0].AddChild(this.arrowSprite);
            this.arrowSprite.alpha = 0f;
            this.arrowSprite.x = -1000f;
            this.blink = 1f;
        }

        public override void Update()
        {
            base.Update();
            this.lastAlpha = this.alpha;
            this.lastBlink = this.blink;
            this.blink = Mathf.Max(0f, this.blink - 0.0125f);

            this.pos = owner.drawpos + new Vector2(0f, 60f);
            this.alpha = Custom.LerpAndTick(this.alpha, owner.needed ? 1 : 0, 0.08f, 0.033333335f);

            this.counter++;
        }

        public override void Draw(float timeStacker)
        {
            Vector2 vector = Vector2.Lerp(this.lastPos, this.pos, timeStacker) + new Vector2(0.01f, 0.01f);
            float num = Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(this.lastAlpha, this.alpha, timeStacker)), 0.7f);
            this.gradient.x = vector.x;
            this.gradient.y = vector.y + 10f;
            this.gradient.scale = Mathf.Lerp(80f, 110f, num) / 16f;
            this.gradient.alpha = 0.17f * Mathf.Pow(num, 2f);
            this.arrowSprite.x = vector.x;
            this.arrowSprite.y = vector.y;
            this.arrowSprite.rotation = RWCustom.Custom.VecToDeg(owner.pointDir * -1);
            this.label.x = vector.x;
            this.label.y = vector.y + 20f;
            Color color = owner.clientSettings.SlugcatColor();
            if (this.counter % 6 < 2 && this.lastBlink > 0f)
            {
                if (((Vector3)(Vector4)color).magnitude > 1.56f)
                {
                    color = Color.Lerp(color, new Color(0.9f, 0.9f, 0.9f), Mathf.InverseLerp(0f, 0.5f, Mathf.Lerp(this.lastBlink, this.blink, timeStacker)));
                }
                else
                {
                    color = Color.Lerp(color, new Color(1f, 1f, 1f), Mathf.InverseLerp(0f, 0.5f, Mathf.Lerp(this.lastBlink, this.blink, timeStacker)));
                }
            }
            this.label.color = color;
            this.arrowSprite.color = color;
            this.label.alpha = num;
            this.arrowSprite.alpha = num;
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            this.gradient.RemoveFromContainer();
            this.arrowSprite.RemoveFromContainer();
            this.label.RemoveFromContainer();
        }
    }
}

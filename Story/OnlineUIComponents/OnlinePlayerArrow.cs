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

            if (this.owner.abstractPlayer == null || this.owner.camera.room == null || this.owner.abstractPlayer.Room != this.owner.camera.room.abstractRoom || this.owner.RealizedPlayer == null)
            {
                this.pos = new Vector2(-1000f, -1000f);
                this.lastPos = this.pos;
                return;
            }

            if (this.owner.RealizedPlayer.room == null)
            {
                Vector2? vector = this.owner.camera.game.shortcuts.OnScreenPositionOfInShortCutCreature(this.owner.camera.room, this.owner.RealizedPlayer);
                if (vector != null)
                {
                    this.pos = vector.Value - this.owner.camera.pos;
                }
            }
            else
            {
                this.pos = Vector2.Lerp(this.owner.RealizedPlayer.bodyChunks[0].pos, this.owner.RealizedPlayer.bodyChunks[1].pos, 0.33333334f) + new Vector2(0f, 60f) - this.owner.camera.pos;
            }
            this.alpha = Custom.LerpAndTick(this.alpha, Mathf.InverseLerp(80f, 20f, (float)this.fadeAwayCounter), 0.08f, 0.033333335f);
            if (this.owner.RealizedPlayer.input[0].x != 0 || this.owner.RealizedPlayer.input[0].y != 0 || this.owner.RealizedPlayer.input[0].jmp || this.owner.RealizedPlayer.input[0].thrw || this.owner.RealizedPlayer.input[0].pckp)
            {
                this.fadeAwayCounter++;
            }
            if (this.counter > 10 && !Custom.DistLess(this.owner.RealizedPlayer.firstChunk.lastPos, this.owner.RealizedPlayer.firstChunk.pos, 3f))
            {
                this.fadeAwayCounter++;
            }
            if (this.fadeAwayCounter > 0)
            {
                this.fadeAwayCounter++;
                if (this.fadeAwayCounter > 120 && this.alpha == 0f && this.lastAlpha == 0f)
                {
                    this.slatedForDeletion = true;
                }
            }
            else if (this.counter > 200)
            {
                this.fadeAwayCounter++;
            }
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

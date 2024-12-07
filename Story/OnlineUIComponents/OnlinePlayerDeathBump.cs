using HUD;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public class OnlinePlayerDeathBump : OnlinePlayerHudPart
    {
        public FSprite symbolSprite;
        public FSprite gradient;
        public float alpha;
        public float lastAlpha;
        public int counter = -20;
        private float blink;
        private float lastBlink;
        public bool removeAsap;
        public Color onlineColor;

        public bool PlayerHasExplosiveSpearInThem
        {
            get
            {
                if (this.owner.RealizedPlayer == null)
                {
                    return false;
                }
                if (this.owner.RealizedPlayer.abstractCreature.stuckObjects.Count == 0)
                {
                    return false;
                }
                for (int i = 0; i < this.owner.RealizedPlayer.abstractCreature.stuckObjects.Count; i++)
                {
                    if (this.owner.RealizedPlayer.abstractCreature.stuckObjects[i].A is AbstractSpear && (this.owner.RealizedPlayer.abstractCreature.stuckObjects[i].A as AbstractSpear).explosive)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public void SetPosToPlayer()
        {
            this.pos = owner.drawpos + new Vector2(0, 30f);
            this.pos.x = Mathf.Clamp(this.pos.x, 30f, this.owner.camera.sSize.x - 30f);
            this.pos.y = Mathf.Clamp(this.pos.y, 30f, this.owner.camera.sSize.y - 30f);
            this.lastPos = this.pos;
        }

        public OnlinePlayerDeathBump(PlayerSpecificOnlineHud owner, SlugcatCustomization customization) : base(owner)
        {
            this.owner = owner;
            this.SetPosToPlayer();
            this.gradient = new FSprite("Futile_White", true);
            this.gradient.shader = owner.hud.rainWorld.Shaders["FlatLight"];
            if ((owner.abstractPlayer.state as PlayerState).slugcatCharacter != SlugcatStats.Name.Night)
            {
                this.gradient.color = new Color(0f, 0f, 0f);
            }
            onlineColor = customization.bodyColor;
            owner.hud.fContainers[0].AddChild(this.gradient);
            this.gradient.alpha = 0f;
            this.gradient.x = -1000f;
            this.symbolSprite = new FSprite("Multiplayer_Death", true);
            this.symbolSprite.color = onlineColor;
            owner.hud.fContainers[0].AddChild(this.symbolSprite);
            this.symbolSprite.alpha = 0f;
            this.symbolSprite.x = -1000f;
            
        }

        public override void Update()
        {
            base.Update();
            this.lastAlpha = this.alpha;
            this.lastBlink = this.blink;
            if (this.counter < 0)
            {
                this.SetPosToPlayer();
                if (this.owner.RealizedPlayer == null || this.owner.RealizedPlayer.room == null || !this.owner.RealizedPlayer.room.ViewedByAnyCamera(this.owner.RealizedPlayer.mainBodyChunk.pos, 200f) || this.removeAsap || this.owner.RealizedPlayer.grabbedBy.Count > 0)
                {
                    this.counter = 0;
                }
                else if (Custom.DistLess(this.owner.RealizedPlayer.bodyChunks[0].pos, this.owner.RealizedPlayer.bodyChunks[0].lastLastPos, 6f) && Custom.DistLess(this.owner.RealizedPlayer.bodyChunks[1].pos, this.owner.RealizedPlayer.bodyChunks[1].lastLastPos, 6f) && !this.PlayerHasExplosiveSpearInThem)
                {
                    this.counter++;
                }
                if (this.counter < 0)
                {
                    return;
                }
            }
            this.counter++;
            if (this.removeAsap)
            {
                this.counter += 10;
            }
            if (this.counter < 40)
            {
                this.alpha = Mathf.Sin(Mathf.InverseLerp(0f, 40f, (float)this.counter) * 3.1415927f);
                this.blink = Custom.LerpAndTick(this.blink, 1f, 0.07f, 0.033333335f);
                if (this.counter == 5 && !this.removeAsap)
                {
                    this.owner.hud.fadeCircles.Add(new FadeCircle(this.owner.hud, 10f, 10f, 0.82f, 30f, 4f, this.pos, this.owner.hud.fContainers[1]));
                    this.owner.hud.PlaySound(SoundID.UI_Multiplayer_Player_Dead_A);
                    return;
                }
            }
            else
            {
                if (this.counter == 40 && !this.removeAsap)
                {
                    FadeCircle fadeCircle = new FadeCircle(this.owner.hud, 20f, 30f, 0.94f, 60f, 4f, this.pos, this.owner.hud.fContainers[1]);
                    fadeCircle.alphaMultiply = 0.5f;
                    fadeCircle.fadeThickness = false;
                    this.owner.hud.fadeCircles.Add(fadeCircle);
                    this.alpha = 1f;
                    this.blink = 0f;
                    this.owner.hud.PlaySound(SoundID.UI_Multiplayer_Player_Dead_B);
                    return;
                }
                if (this.counter <= 220)
                {
                    this.alpha = Mathf.InverseLerp(220f, 110f, (float)this.counter);
                    return;
                }
                if (this.counter > 220)
                {
                    this.slatedForDeletion = true;
                }
            }
        }

        public override void Draw(float timeStacker)
        {
            Vector2 vector = Vector2.Lerp(this.lastPos, this.pos, timeStacker) + new Vector2(0.01f, 0.01f);
            float num = Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(this.lastAlpha, this.alpha, timeStacker)), 0.7f);
            this.gradient.x = vector.x;
            this.gradient.y = vector.y + 10f;
            this.gradient.scale = Mathf.Lerp(80f, 110f, num) / 16f;
            this.gradient.alpha = 0.17f * Mathf.Pow(num, 2f);
            this.symbolSprite.x = vector.x;
            this.symbolSprite.y = Mathf.Min(vector.y + Custom.SCurve(Mathf.InverseLerp(40f, 130f, (float)this.counter + timeStacker), 0.8f) * 80f, this.owner.camera.sSize.y - 30f);
            Color color = this.onlineColor;

            if (this.counter % 6 < 2 && this.lastBlink > 0f)
            {
                if ((this.owner.abstractPlayer.state as PlayerState).slugcatCharacter == SlugcatStats.Name.White)
                {
                    color = Color.Lerp(color, new Color(0.9f, 0.9f, 0.9f), Mathf.InverseLerp(0f, 0.5f, Mathf.Lerp(this.lastBlink, this.blink, timeStacker)));
                }
                else
                {
                    color = Color.Lerp(color, new Color(1f, 1f, 1f), Mathf.InverseLerp(0f, 0.5f, Mathf.Lerp(this.lastBlink, this.blink, timeStacker)));
                }
            }
            this.symbolSprite.color = color;
            this.symbolSprite.alpha = num;
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            this.gradient.RemoveFromContainer();
            this.symbolSprite.RemoveFromContainer();
        }
    }
}

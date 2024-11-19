using HUD;
using RWCustom;
using UnityEngine;

namespace RainMeadow.Arena.Nightcat
{
    public class NightcatHUD : OnlinePlayerHudPart
    {
        public FSprite symbolSprite;
        public FSprite gradient;
        public float alpha;
        public float lastAlpha;
        public int counter = -20;
        private float blink;
        private float lastBlink;
        public bool removeAsap;

        public bool PlayerHasExplosiveSpearInThem
        {
            get
            {
                if (owner.RealizedPlayer == null)
                {
                    return false;
                }
                if (owner.RealizedPlayer.abstractCreature.stuckObjects.Count == 0)
                {
                    return false;
                }
                for (int i = 0; i < owner.RealizedPlayer.abstractCreature.stuckObjects.Count; i++)
                {
                    if (owner.RealizedPlayer.abstractCreature.stuckObjects[i].A is AbstractSpear && (owner.RealizedPlayer.abstractCreature.stuckObjects[i].A as AbstractSpear).explosive)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public void SetPosToPlayer()
        {
            pos = owner.drawpos + new Vector2(0, 30f);
            pos.x = Mathf.Clamp(pos.x, 30f, owner.camera.sSize.x - 30f);
            pos.y = Mathf.Clamp(pos.y, 30f, owner.camera.sSize.y - 30f);
            lastPos = pos;
        }

        public NightcatHUD(PlayerSpecificOnlineHud owner) : base(owner)
        {
            this.owner = owner;
            SetPosToPlayer();
            gradient = new FSprite("Futile_White", true);
            gradient.shader = owner.hud.rainWorld.Shaders["FlatLight"];
            if ((owner.abstractPlayer.state as PlayerState).slugcatCharacter != SlugcatStats.Name.Night)
            {
                gradient.color = new Color(0f, 0f, 0f);
            }
            owner.hud.fContainers[0].AddChild(gradient);
            gradient.alpha = 0f;
            gradient.x = -1000f;
            symbolSprite = new FSprite("GuidanceMoon", true);
            symbolSprite.color = PlayerGraphics.DefaultSlugcatColor((owner.abstractPlayer.state as PlayerState).slugcatCharacter);
            owner.hud.fContainers[0].AddChild(symbolSprite);
            symbolSprite.alpha = 0f;
            symbolSprite.x = -1000f;
        }

        public override void Update()
        {
            base.Update();
            lastAlpha = alpha;
            lastBlink = blink;
            if (counter < 0)
            {
                SetPosToPlayer();
                if (owner.RealizedPlayer == null || owner.RealizedPlayer.room == null || !owner.RealizedPlayer.room.ViewedByAnyCamera(owner.RealizedPlayer.mainBodyChunk.pos, 200f) || removeAsap || owner.RealizedPlayer.grabbedBy.Count > 0)
                {
                    counter = 0;
                }
                else if (Custom.DistLess(owner.RealizedPlayer.bodyChunks[0].pos, owner.RealizedPlayer.bodyChunks[0].lastLastPos, 6f) && Custom.DistLess(owner.RealizedPlayer.bodyChunks[1].pos, owner.RealizedPlayer.bodyChunks[1].lastLastPos, 6f) && !PlayerHasExplosiveSpearInThem)
                {
                    counter++;
                }
                if (counter < 0)
                {
                    return;
                }
            }
            counter++;
            if (removeAsap)
            {
                counter += 3;
            }
            if (counter < 40)
            {
                alpha = Mathf.Sin(Mathf.InverseLerp(0f, 40f, counter) * 3.1415927f);
                blink = Custom.LerpAndTick(blink, 1f, 0.07f, 0.033333335f);
            }
            else
            {

                if (counter <= 220)
                {
                    alpha = Mathf.InverseLerp(220f, 110f, counter);
                    return;
                }
                if (counter > 220)
                {
                    slatedForDeletion = true;
                }
            }
        }

        public override void Draw(float timeStacker)
        {
            Vector2 vector = Vector2.Lerp(lastPos, pos, timeStacker) + new Vector2(0.01f, 0.01f);
            float num = Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(lastAlpha, alpha, timeStacker)), 0.7f);
            gradient.x = vector.x;
            gradient.y = vector.y + 10f;
            gradient.scale = Mathf.Lerp(80f, 110f, num) / 16f;
            gradient.alpha = 0.17f * Mathf.Pow(num, 2f);
            symbolSprite.x = vector.x;
            symbolSprite.y = Mathf.Min(vector.y + Custom.SCurve(Mathf.InverseLerp(40f, 130f, counter + timeStacker), 0.8f) * 80f, owner.camera.sSize.y - 30f);
            Color color = PlayerGraphics.DefaultSlugcatColor((owner.abstractPlayer.state as PlayerState).slugcatCharacter);
            if (counter % 6 < 2 && lastBlink > 0f)
            {
                if ((owner.abstractPlayer.state as PlayerState).slugcatCharacter == SlugcatStats.Name.White)
                {
                    color = Color.Lerp(color, new Color(0.9f, 0.9f, 0.9f), Mathf.InverseLerp(0f, 0.5f, Mathf.Lerp(lastBlink, blink, timeStacker)));
                }
                else
                {
                    color = Color.Lerp(color, new Color(1f, 1f, 1f), Mathf.InverseLerp(0f, 0.5f, Mathf.Lerp(lastBlink, blink, timeStacker)));
                }
            }
            symbolSprite.color = color;
            symbolSprite.alpha = num;
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            gradient.RemoveFromContainer();
            symbolSprite.RemoveFromContainer();
        }
    }
}

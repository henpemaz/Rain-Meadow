using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowTokenCoin
    {
        public Vector2 pos;
        public float scale;
        public float alpha;

        public class MeadowCoin : CosmeticSprite
        {
            public MeadowCoin(Vector2 pos, Vector2 vel, Color color, bool underWater)
            {
                this.pos = pos;
                this.vel = vel;
                this.color = color;
                this.underWater = underWater;
                this.lastPos = pos;
                this.lastLastPos = pos;
                this.lifeTime = Mathf.Lerp(20f, 40f, Random.value);
                this.life = 1f;
                this.dir = Custom.VecToDeg(vel.normalized);
                SpecialEvents.LoadElement("meadowcoin");
            }

            public override void Update(bool eu)
            {
                this.lastLastPos = this.lastPos;
                base.Update(eu);
                this.dir += Mathf.Lerp(-1f, 1f, Random.value) * 50f;
                this.vel *= 0.8f;
                this.vel += Custom.DegToVec(this.dir) * Mathf.Lerp(0.2f, 0.2f, this.life);
                this.life -= 1f / this.lifeTime;
                if (this.life < 0f)
                {
                    this.Destroy();
                }
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites = new FSprite[1];
                sLeaser.sprites[0] = new FSprite("meadowcoin", true);
                sLeaser.sprites[0].anchorX = 0.5f;
                sLeaser.sprites[0].anchorY = 0.5f;

                if (this.underWater)
                {
                    sLeaser.sprites[0].alpha = 0.5f;
                }
                this.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Foreground"));
            }

            public override void DrawSprites(
                RoomCamera.SpriteLeaser sLeaser,
                RoomCamera rCam,
                float timeStacker,
                Vector2 camPos
            )
            {
                Vector2 vector = Vector2.Lerp(this.lastPos, this.pos, timeStacker);
                sLeaser.sprites[0].x = vector.x - camPos.x;
                sLeaser.sprites[0].y = vector.y - camPos.y;

                // Increased from 0.15f to 0.3f to double the size
                float size = 0.05f * Mathf.InverseLerp(0f, 0.5f, this.life);
                sLeaser.sprites[0].scale = size;

                sLeaser.sprites[0].isVisible = (
                    Random.value < Mathf.InverseLerp(0f, 0.5f, this.life)
                );

                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            }

            public override void ApplyPalette(
                RoomCamera.SpriteLeaser sLeaser,
                RoomCamera rCam,
                RoomPalette palette
            ) { }

            public override void AddToContainer(
                RoomCamera.SpriteLeaser sLeaser,
                RoomCamera rCam,
                FContainer newContatiner
            )
            {
                base.AddToContainer(sLeaser, rCam, newContatiner);
            }

            private float dir;
            private float life;
            private float lifeTime;
            public Color color;
            private Vector2 lastLastPos;
            private bool underWater;
        }
    }
}

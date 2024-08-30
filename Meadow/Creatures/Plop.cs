using UnityEngine;

namespace RainMeadow
{
    public class Plop : CosmeticSprite // hud fadecircle in roomspace
    {
        private Creature creature;
        private float lastRad;
        private float rad;
        private float radSpeed;
        private float radSlowDown;
        private float lifeTime;
        private float lastThickness;
        private float thickness;
        private float fade;
        private float lastFade;
        private float life;

        public Plop(Creature creature, float rad, float radSpeed, float radSlowDown, float lifeTime, float thickness)
        {
            this.creature = creature;
            this.rad = rad;
            this.radSpeed = radSpeed;
            this.radSlowDown = radSlowDown;
            this.lifeTime = lifeTime;
            this.thickness = thickness;
            this.pos = creature.DangerPos;
            this.lastPos = pos;
            this.life = 1f;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            this.lastRad = this.rad;
            this.lastThickness = this.thickness;
            this.lastFade = this.fade;

            this.pos = creature.DangerPos;
            this.rad += this.radSpeed;
            this.thickness = this.life * this.thickness;
            this.radSpeed *= this.radSlowDown;
            this.life -= 1f / this.lifeTime;
            this.fade = Mathf.Pow(this.life, 0.5f) * 0.15f;

            if (creature.abstractPhysicalObject.Room != room.abstractRoom) Destroy();
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1] { new FSprite("Futile_White") { shader = rCam.game.rainWorld.Shaders["VectorCircleFadable"] } };
            ApplyPalette(sLeaser, rCam, rCam.currentPalette);
            AddToContainer(sLeaser, rCam, null);
            base.InitiateSprites(sLeaser, rCam);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 vector = Vector2.Lerp(this.lastPos, this.pos, timeStacker) - camPos;
            float drawRad = Mathf.Lerp(this.lastRad, this.rad, timeStacker);
            float drawThickness = Mathf.Lerp(this.lastThickness, this.thickness, timeStacker);
            if (drawThickness > drawRad)
            {
                drawThickness = drawRad;
            }

            FSprite fSprite = sLeaser.sprites[0];
            fSprite.x = vector.x;
            fSprite.y = vector.y;
            fSprite.scale = drawRad / 8f;
            fSprite.alpha = drawThickness / drawRad;
            fSprite.color = new Color(0f, 0f, Mathf.Lerp(this.lastFade, this.fade, timeStacker));

            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("HUD");
            }
            base.AddToContainer(sLeaser, rCam, newContatiner);
        }
    }
}

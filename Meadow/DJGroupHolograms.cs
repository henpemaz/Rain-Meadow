using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.Meadow
{
    public class DJGroupHolograms : CosmeticSprite
    {
        private Creature creature;
        private Room room;
        public DJGroupHolograms(Creature creature, Room room)
        {
            this.creature = creature;
            this.pos = creature.DangerPos;
            this.lastPos = pos;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);



            if (creature.abstractPhysicalObject.Room != room.abstractRoom) Destroy();
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1] { new FSprite("Futile_White") { shader = rCam.game.rainWorld.Shaders["Hologram"] } };
            ApplyPalette(sLeaser, rCam, rCam.currentPalette);
            AddToContainer(sLeaser, rCam, null);
            base.InitiateSprites(sLeaser, rCam);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            //Vector2 vector = Vector2.Lerp(this.lastPos, this.pos, timeStacker) - camPos;
            //float drawRad = Mathf.Lerp(this.lastRad, this.rad, timeStacker);
            //float drawThickness = Mathf.Lerp(this.lastThickness, this.thickness, timeStacker);
            //if (drawThickness > drawRad)
            //{
            //    drawThickness = drawRad;
            //}
            //
            //FSprite fSprite = sLeaser.sprites[0];
            //fSprite.x = vector.x;
            //fSprite.y = vector.y;
            //fSprite.scale = drawRad / 8f;
            //fSprite.alpha = drawThickness / drawRad;
            //fSprite.color = new Color(0f, 0f, Mathf.Lerp(this.lastFade, this.fade, timeStacker));
            //
            //base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
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

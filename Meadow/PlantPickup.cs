using RWCustom;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using static MonoMod.InlineRT.MonoModRule;

namespace RainMeadow
{
    internal class PlantPickup : UpdatableAndDeletable, IDrawable
    {
        public Vector2 pos;

        public PlantPickup(Vector2 pos)
        {
            this.pos = pos;
            this.LoadFile("meadowPlant");
        }

        public void LoadFile(string fileName)
        {
            if (Futile.atlasManager.GetAtlasWithName(fileName) != null)
            {
                return;
            }
            string str = AssetManager.ResolveFilePath("illustrations" + Path.DirectorySeparatorChar.ToString() + fileName + ".png");
            Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            AssetManager.SafeWWWLoadTexture(ref texture, "file:///" + str, true, true);
            HeavyTexturesCache.LoadAndCacheAtlasFromTexture(fileName, texture, false);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[0] = new FSprite("meadowPlant");
            sLeaser.sprites[0].shader = room.game.rainWorld.Shaders["RM_LeveIltem"];

            sLeaser.sprites[1] = new FSprite("Futile_White");
            sLeaser.sprites[1].shader = room.game.rainWorld.Shaders["FlatLightBehindTerrain"];
            sLeaser.sprites[1].scale = 75f / 16f;
            sLeaser.sprites[1].alpha = (0.4f);
            sLeaser.sprites[1].color = RainWorld.GoldRGB;

            rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[0]);
            rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[1]);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) { }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 newPos = this.pos - camPos;
            sLeaser.sprites[0].SetPosition(newPos);
            sLeaser.sprites[1].SetPosition(newPos);
            if (base.slatedForDeletetion || this.room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }
    }
}

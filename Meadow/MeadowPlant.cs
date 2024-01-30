using System.IO;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowPlant : MeadowCollectible
    {
        private float popping;
        private float rad = 10f;

        public MeadowPlant(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject)
        {
            RainMeadow.Debug("MeadowPlant");
        }

        public override void InitiateGraphicsModule()
        {
            RainMeadow.Debug("InitiateGraphicsModule");
            if (base.graphicsModule == null)
            {
                this.graphicsModule = new MeadowPlantGraphics(this);
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            if (abstractCollectible.Expired)
            {
                this.Destroy();
            }

            if (this.popping > 0f)
            {
                this.popping += 0.033333335f;
                if (this.popping >= 1f)
                {
                    this.popping = 1f;
                }
            }
            else
            {
                int num5 = 0;
                while (num5 < this.room.game.Players.Count && this.popping == 0f)
                {
                    if (this.room.game.Players[num5].realizedCreature != null && this.room.game.Players[num5].realizedCreature.room == this.room)
                    {
                        for (int num6 = 0; num6 < this.room.game.Players[num5].realizedCreature.bodyChunks.Length; num6++)
                        {
                            if (RWCustom.Custom.DistLess(this.room.game.Players[num5].realizedCreature.bodyChunks[num6].pos, this.firstChunk.pos, this.room.game.Players[num5].realizedCreature.bodyChunks[num6].rad + this.rad))
                            {
                                this.abstractCollectible.Collect();
                                this.popping = 0.01f;
                                break;
                            }
                        }
                    }
                    num5++;
                }
            }
        }

        public class MeadowPlantGraphics : GraphicsModule
        {
            public Vector2 pos;
            MeadowPlant plant => owner as MeadowPlant;

            public MeadowPlantGraphics(PhysicalObject ow) : base(ow, false)
            {
                RainMeadow.Debug("MeadowPlantGraphics");
                this.pos = ow.firstChunk.pos;
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
                HeavyTexturesCache.LoadAndCacheAtlasFromTexture(fileName, texture, false); // todo this becomes a spritesheet
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                RainMeadow.Debug("MeadowPlantGraphics.InitiateSprites");
                base.InitiateSprites(sLeaser, rCam);
                sLeaser.sprites = new FSprite[3];
                sLeaser.sprites[0] = new FSprite("meadowPlant");
                sLeaser.sprites[0].shader = owner.room.game.rainWorld.Shaders["RM_LeveIltem"];

                sLeaser.sprites[1] = new FSprite("Futile_White");
                sLeaser.sprites[1].shader = owner.room.game.rainWorld.Shaders["FlatLightBehindTerrain"];
                sLeaser.sprites[1].scale = 75f / 16f;
                sLeaser.sprites[1].alpha = (0.4f);
                sLeaser.sprites[1].color = RWCustom.Custom.HSL2RGB(RainWorld.AntiGold.hue, 0.6f, 0.2f);

                sLeaser.sprites[2] = new FSprite("Futile_White");
                sLeaser.sprites[2].shader = owner.room.game.rainWorld.Shaders["FlatLightBehindTerrain"];
                sLeaser.sprites[2].scale = 75f / 16f;
                sLeaser.sprites[2].alpha = (0.4f);
                sLeaser.sprites[2].color = RainWorld.GoldRGB; // maybe "effect color"

                this.AddToContainer(sLeaser, rCam, null);
                this.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
            }

            public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) {
                base.AddToContainer(sLeaser, rCam, newContatiner);
                rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[0]);
                rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[1]);
                rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[2]);
            }

            public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) {
                base.ApplyPalette(sLeaser, rCam, palette);
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                Vector2 newPos = this.pos - camPos;
                float scale = 1f - plant.popping;
                sLeaser.sprites[0].scale = scale;
                sLeaser.sprites[1].scale = scale * 75f / 16f;
                sLeaser.sprites[2].scale = scale * 75f / 16f;
                sLeaser.sprites[0].SetPosition(newPos);
                sLeaser.sprites[1].SetPosition(newPos);
                sLeaser.sprites[2].SetPosition(newPos);
                if (owner.slatedForDeletetion || owner.room != rCam.room)
                {
                    sLeaser.CleanSpritesAndRemove();
                }
            }
        }
    }
}

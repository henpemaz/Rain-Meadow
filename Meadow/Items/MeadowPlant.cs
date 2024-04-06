using RWCustom;
using System.IO;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowPlant : MeadowCollectible, IDrawable
    {
        private float popping;
        private float rad = 10f;
        private MeadowGameMode mgm;
        private Creature avatarCreature;

        public MeadowPlant(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject)
        {
            RainMeadow.Debug("MeadowPlant");
            this.LoadFile("meadowPlant");
            this.mgm = OnlineManager.lobby.gameMode as MeadowGameMode;
            avatarCreature = mgm.avatar.creature.realizedCreature;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

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

                if (avatarCreature == null)
                {
                    avatarCreature = mgm.avatar.creature.realizedCreature;
                }
                else if (avatarCreature.room == this.room)
                {
                    // collect logic moved here
                    if (Custom.DistLess(avatarCreature.mainBodyChunk.pos, this.placePos, 18f))
                    {
                        this.room.PlaySound(SoundID.HUD_Karma_Reinforce_Flicker, this.placePos);
                        this.popping = 0.01f;
                        abstractCollectible.Collect();

                        for (int num6 = 0; num6 < 10; num6++)
                        {
                            this.room.AddObject(new MeadowCollectToken.TokenSpark(this.placePos + Custom.RNV() * 2f, Custom.RNV() * 11f * Random.value + Custom.DirVec(avatarCreature.mainBodyChunk.pos, this.placePos) * 5f * Random.value, new Color(203f/255f, 252f/255f, 147f/255f), false));
                        }
                    }
                }
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

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            RainMeadow.Debug("MeadowPlantGraphics.InitiateSprites");
            sLeaser.sprites = new FSprite[3];
            sLeaser.sprites[0] = new FSprite("meadowPlant");
            sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["RM_LeveIltem"];

            sLeaser.sprites[1] = new FSprite("Futile_White");
            sLeaser.sprites[1].shader = rCam.game.rainWorld.Shaders["FlatLightBehindTerrain"];
            sLeaser.sprites[1].scale = 75f / 16f;
            sLeaser.sprites[1].alpha = (0.4f);
            sLeaser.sprites[1].color = RWCustom.Custom.HSL2RGB(RainWorld.AntiGold.hue, 0.6f, 0.2f);

            sLeaser.sprites[2] = new FSprite("Futile_White");
            sLeaser.sprites[2].shader = rCam.game.rainWorld.Shaders["FlatLightBehindTerrain"];
            sLeaser.sprites[2].scale = 75f / 16f;
            sLeaser.sprites[2].alpha = (0.4f);
            sLeaser.sprites[2].color = RainWorld.GoldRGB; // maybe "effect color"

            this.AddToContainer(sLeaser, rCam, null);
            this.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[0]);
            rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[1]);
            rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[2]);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 newPos = this.firstChunk.pos - camPos;
            float scale = 1f - popping;
            sLeaser.sprites[0].scale = scale;
            sLeaser.sprites[1].scale = scale * 75f / 16f;
            sLeaser.sprites[2].scale = scale * 75f / 16f;
            sLeaser.sprites[0].SetPosition(newPos);
            sLeaser.sprites[1].SetPosition(newPos);
            sLeaser.sprites[2].SetPosition(newPos);
            if (slatedForDeletetion || room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }
    }
}

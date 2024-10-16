using UnityEngine;
using RWCustom;

namespace RainMeadow
{
    public class MeadowCollectToken : MeadowCollectible, IDrawable
    {
        public int LightSprite => 0;
        public int MainSprite => 1;
        public int TrailSprite => 2;
        public int LineSprite(int line) => 3 + line;
        public int GoldSprite => 7;
        public int TotalSprites => 8;

        public MeadowCollectToken(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject)
        {
            this.mgm = OnlineManager.lobby.gameMode as MeadowGameMode;
            avatarCreature = mgm.avatars[0].creature.realizedCreature;

            if (this.abstractPhysicalObject.type == RainMeadow.Ext_PhysicalObjectType.MeadowTokenRed)
            {
                TokenColor = MeadowProgression.TokenRedColor;
            }
            else if (this.abstractPhysicalObject.type == RainMeadow.Ext_PhysicalObjectType.MeadowTokenBlue)
            {
                TokenColor = MeadowProgression.TokenBlueColor;
            }
            else if (this.abstractPhysicalObject.type == RainMeadow.Ext_PhysicalObjectType.MeadowTokenGold)
            {
                TokenColor = MeadowProgression.TokenGoldColor;
            }

            this.lines = new Vector2[4, 4];
            this.lines[0, 2] = new Vector2(-7f, 0f);
            this.lines[1, 2] = new Vector2(0f, 11f);
            this.lines[2, 2] = new Vector2(7f, 0f);
            this.lines[3, 2] = new Vector2(0f, -11f);
            this.trail = new Vector2[5];
        }

        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);

            var pos = placePos + new Vector2(0, 20);
            this.underWaterMode = (room.GetTilePosition(pos).y < room.defaultWaterLevel);
            this.stalk = new MeadowCollectToken.TokenStalk(room, pos, pos + new Vector2(0f, -40f), this.TokenColor, this);
            room.AddObject(this.stalk);
            this.pos = pos;
            this.hoverPos = pos;
            this.lastPos = pos;
            for (int i = 0; i < this.lines.GetLength(0); i++)
            {
                this.lines[i, 0] = this.pos;
                this.lines[i, 1] = this.pos;
            }
            for (int j = 0; j < this.trail.Length; j++)
            {
                this.trail[j] = this.pos;
            }
            if (abstractCollectible.collectedLocally)
            {
                this.RemoveFromRoom();
                return;
            }
            this.soundLoop = new StaticSoundLoop(SoundID.Token_Idle_LOOP, this.pos, room, 0f, 1f);
            this.glitchLoop = new StaticSoundLoop(SoundID.Token_Upset_LOOP, this.pos, room, 0f, 1f);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (this.room == null) return;
            this.sinCounter += Random.value * this.power;
            this.sinCounter2 += (1f + Mathf.Lerp(-10f, 10f, Random.value) * this.glitch) * this.power;
            float num = Mathf.Sin(this.sinCounter2 / 20f);
            num = Mathf.Pow(Mathf.Abs(num), 0.5f) * Mathf.Sign(num);
            this.soundLoop.Update();
            this.soundLoop.pos = this.pos;
            this.soundLoop.pitch = 1f + 0.25f * num * this.glitch;
            this.soundLoop.volume = Mathf.Pow(this.power, 0.5f) * Mathf.Pow(1f - this.glitch, 0.5f);
            this.glitchLoop.Update();
            this.glitchLoop.pos = this.pos;
            this.glitchLoop.pitch = Mathf.Lerp(0.75f, 1.25f, this.glitch) - 0.25f * num * this.glitch;
            this.glitchLoop.volume = Mathf.Pow(Mathf.Sin(Mathf.Clamp(this.glitch, 0f, 1f) * 3.1415927f), 0.1f) * Mathf.Pow(this.power, 0.1f);
            this.lastPos = this.pos;
            for (int i = 0; i < this.lines.GetLength(0); i++)
            {
                this.lines[i, 1] = this.lines[i, 0];
            }
            this.lastGlitch = this.glitch;
            this.lastExpand = this.expand;
            for (int j = this.trail.Length - 1; j >= 1; j--)
            {
                this.trail[j] = this.trail[j - 1];
            }
            this.trail[0] = this.lastPos;
            this.lastPower = this.power;
            this.power = Custom.LerpAndTick(this.power, this.poweredOn ? 1f : 0f, 0.07f, 0.025f);
            this.glitch = Mathf.Max(this.glitch, 1f - this.power);
            this.pos += this.vel;
            for (int k = 0; k < this.lines.GetLength(0); k++)
            {
                if (this.stalk != null)
                {
                    this.lines[k, 0] += this.stalk.head - this.stalk.lastHead;
                }
                if (Mathf.Pow(Random.value, 0.1f + this.glitch * 5f) > this.lines[k, 3].x)
                {
                    this.lines[k, 0] = Vector2.Lerp(this.lines[k, 0], this.pos + new Vector2(this.lines[k, 2].x * num, this.lines[k, 2].y), Mathf.Pow(Random.value, 1f + this.lines[k, 3].x * 17f));
                }
                if (Random.value < Mathf.Pow(this.lines[k, 3].x, 0.2f) && Random.value < Mathf.Pow(this.glitch, 0.8f - 0.4f * this.lines[k, 3].x))
                {
                    this.lines[k, 0] += Custom.RNV() * 17f * this.lines[k, 3].x * this.power;
                    this.lines[k, 3].y = Mathf.Max(this.lines[k, 3].y, this.glitch);
                }
                this.lines[k, 3].x = Custom.LerpAndTick(this.lines[k, 3].x, this.lines[k, 3].y, 0.01f, 0.033333335f);
                this.lines[k, 3].y = Mathf.Max(0f, this.lines[k, 3].y - 0.014285714f);
                if (Random.value < 1f / Mathf.Lerp(210f, 20f, this.glitch))
                {
                    this.lines[k, 3].y = Mathf.Max(this.glitch, (Random.value < 0.5f) ? this.generalGlitch : Random.value);
                }
            }
            this.vel *= 0.995f;
            this.vel += Vector2.ClampMagnitude(this.hoverPos + new Vector2(0f, Mathf.Sin(this.sinCounter / 15f) * 7f) - this.pos, 15f) / 81f;
            this.vel += Custom.RNV() * Random.value * Random.value * Mathf.Lerp(0.06f, 0.4f, this.glitch);
            this.pos += Custom.RNV() * Mathf.Pow(Random.value, 7f - 6f * this.generalGlitch) * Mathf.Lerp(0.06f, 1.2f, this.glitch);
            if (this.expandAroundCreature != null) // collected, animating
            {
                // todo creaturecontroller blink
                // this.expandAroundCreature.Blink(5);
                if (!this.contract)
                {
                    this.expand += 0.033333335f;
                    if (this.expand > 1f)
                    {
                        this.expand = 1f;
                        this.contract = true;
                    }
                    this.generalGlitch = 0f;
                    this.glitch = Custom.LerpAndTick(this.glitch, this.expand * 0.5f, 0.07f, 0.06666667f);
                    float num2 = Custom.SCurve(Mathf.InverseLerp(0.35f, 0.55f, this.expand), 0.4f);
                    Vector2 b = Vector2.Lerp(this.expandAroundCreature.mainBodyChunk.pos + new Vector2(0f, 40f), Vector2.Lerp(this.expandAroundCreature.bodyChunks[1].pos, this.expandAroundCreature.mainBodyChunk.pos + Custom.DirVec(this.expandAroundCreature.bodyChunks[1].pos, this.expandAroundCreature.mainBodyChunk.pos) * 10f, 0.65f), this.expand);
                    for (int l = 0; l < this.lines.GetLength(0); l++)
                    {
                        Vector2 b2 = Vector2.Lerp(this.lines[l, 2] * (2f + 5f * Mathf.Pow(this.expand, 0.5f)), Custom.RotateAroundOrigo(this.lines[l, 2] * (2f + 2f * Mathf.Pow(this.expand, 0.5f)), Custom.AimFromOneVectorToAnother(this.expandAroundCreature.bodyChunks[1].pos, this.expandAroundCreature.mainBodyChunk.pos)), num2);
                        this.lines[l, 0] = Vector2.Lerp(this.lines[l, 0], Vector2.Lerp(this.pos, b, Mathf.Pow(num2, 2f)) + b2, Mathf.Pow(this.expand, 0.5f));
                        this.lines[l, 3] *= 1f - this.expand;
                    }
                    this.hoverPos = Vector2.Lerp(this.hoverPos, b, Mathf.Pow(this.expand, 2f));
                    this.pos = Vector2.Lerp(this.pos, b, Mathf.Pow(this.expand, 2f));
                    this.vel *= 1f - this.expand;
                }
                else
                {
                    this.generalGlitch *= 1f - this.expand;
                    this.glitch = 0.15f;
                    this.expand -= 1f / Mathf.Lerp(60f, 2f, this.expand);
                    Vector2 a = Vector2.Lerp(this.expandAroundCreature.bodyChunks[1].pos, this.expandAroundCreature.mainBodyChunk.pos + Custom.DirVec(this.expandAroundCreature.bodyChunks[1].pos, this.expandAroundCreature.mainBodyChunk.pos) * 10f, Mathf.Lerp(1f, 0.65f, this.expand));
                    for (int m = 0; m < this.lines.GetLength(0); m++)
                    {
                        Vector2 b3 = Custom.RotateAroundOrigo(Vector2.Lerp((Random.value > this.expand) ? this.lines[m, 2] : this.lines[Random.Range(0, 4), 2], this.lines[Random.Range(0, 4), 2], Random.value * (1f - this.expand)) * (4f * Mathf.Pow(this.expand, 0.25f)), Custom.AimFromOneVectorToAnother(this.expandAroundCreature.bodyChunks[1].pos, this.expandAroundCreature.mainBodyChunk.pos)) * Mathf.Lerp(Random.value, 1f, this.expand);
                        this.lines[m, 0] = a + b3;
                        this.lines[m, 3] *= 1f - this.expand;
                    }
                    this.pos = a;
                    this.hoverPos = a;
                    if (this.expand < 0f)
                    {
                        int num3 = 0;
                        while (num3 < 20)
                        {
                            this.room.AddObject(new MeadowCollectToken.TokenSpark(this.pos + Custom.RNV() * 2f, Custom.RNV() * 16f * Random.value, Color.Lerp(this.TokenColor, new Color(1f, 1f, 1f), 0.5f + 0.5f * Random.value), this.underWaterMode));
                            num3++;
                        }
                        this.room.PlaySound(SoundID.Token_Collected_Sparks, this.pos);

                        // we want the collectible gone but the abstract must stay
                        RainMeadow.Debug("RemoveFromRoom: " + abstractCollectible.online);
                        this.RemoveFromRoom();

                        // feedback would be here but better done from progression
                    }
                }
            }
            else // not collected, looking for target
            {
                this.generalGlitch = Mathf.Max(0f, this.generalGlitch - 0.008333334f);
                if (Random.value < 0.0027027028f)
                {
                    this.generalGlitch = Random.value;
                }
                if (!Custom.DistLess(this.pos, this.hoverPos, 11f))
                {
                    this.pos += Custom.DirVec(this.hoverPos, this.pos) * (11f - Vector2.Distance(this.pos, this.hoverPos)) * 0.7f;
                }
                float f = Mathf.Sin(Mathf.Clamp(this.glitch, 0f, 1f) * 3.1415927f);
                if (Random.value < 0.05f + 0.35f * Mathf.Pow(f, 0.5f) && Random.value < this.power)
                {
                    this.room.AddObject(new MeadowCollectToken.TokenSpark(this.pos + Custom.RNV() * 6f * this.glitch, Custom.RNV() * Mathf.Lerp(2f, 9f, Mathf.Pow(f, 2f)) * Random.value, this.GoldCol(this.glitch), this.underWaterMode));
                }
                this.glitch = Custom.LerpAndTick(this.glitch, this.generalGlitch / 2f, 0.01f, 0.033333335f);
                if (Random.value < 1f / Mathf.Lerp(360f, 10f, this.generalGlitch))
                {
                    this.glitch = Mathf.Pow(Random.value, 1f - 0.85f * this.generalGlitch);
                }

                if (avatarCreature == null)
                {
                    avatarCreature = mgm.avatars[0].creature.realizedCreature;
                }
                else if (avatarCreature.room == this.room)
                {
                    // collect logic moved here
                    for (int i = 0; i < avatarCreature.bodyChunks.Length; i++)
                    {
                        if (Custom.DistLess(avatarCreature.bodyChunks[i].pos, this.pos, 18f) || Custom.DistLess(avatarCreature.bodyChunks[i].pos, this.placePos, 18f))
                        {
                            expandAroundCreature = avatarCreature;
                            this.expand = 0.01f;
                            this.room.PlaySound(SoundID.Token_Collect, this.pos);
                            abstractCollectible.Collect();
                            for (int num6 = 0; num6 < 10; num6++)
                            {
                                this.room.AddObject(new MeadowCollectToken.TokenSpark(this.pos + Custom.RNV() * 2f, Custom.RNV() * 11f * Random.value + Custom.DirVec(avatarCreature.bodyChunks[i].pos, this.pos) * 5f * Random.value, this.GoldCol(this.glitch), this.underWaterMode));
                            }
                            break;
                        }
                    }
                }
                // if OTHER creatures around, also expand but do not collect
                float num4 = float.MaxValue;
                float num5 = 140f;
                for (int i = 0; i < room.abstractRoom.creatures.Count; i++)
                {
                    var creature = room.abstractRoom.creatures[i].realizedCreature;
                    if (creature == null) continue;
                    num4 = Mathf.Min(num4, Vector2.Distance(creature.mainBodyChunk.pos, this.pos));
                    
                    if (Custom.DistLess(creature.mainBodyChunk.pos, this.pos, num5))
                    {
                        if (Custom.DistLess(this.pos, this.hoverPos, 80f))
                        {
                            this.pos += Custom.DirVec(this.pos, creature.mainBodyChunk.pos) * Custom.LerpMap(Vector2.Distance(this.pos, creature.mainBodyChunk.pos), 40f, num5, 2.2f, 0f, 0.5f) * Random.value;
                        }
                        if (Random.value < 0.05f && Random.value < Mathf.InverseLerp(num5, 40f, Vector2.Distance(this.pos, creature.mainBodyChunk.pos)))
                        {
                            this.glitch = Mathf.Max(this.glitch, Random.value * 0.5f);
                        }
                    }
                }
                if (this.poweredOn && (this.expand == 0f && !this.contract && Random.value < Mathf.InverseLerp(num5 + 160f, num5 + 460f, num4)))
                {
                    this.poweredOn = false;
                    this.room.PlaySound(SoundID.Token_Turn_Off, this.pos);
                }
                else if (!this.poweredOn && Random.value < Mathf.InverseLerp(num5 + 60f, num5 - 20f, num4))
                {
                    this.poweredOn = true;
                    this.room.PlaySound(SoundID.Token_Turn_On, this.pos);
                }
            }
        }

        public Color GoldCol(float g)
        {
            return Color.Lerp(this.TokenColor, new Color(1f, 1f, 1f), 0.4f + 0.4f * Mathf.Max(this.contract ? 0.5f : (this.expand * 0.5f), Mathf.Pow(g, 0.5f)));
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[this.TotalSprites];
            sLeaser.sprites[this.LightSprite] = new FSprite("Futile_White", true);
            sLeaser.sprites[this.LightSprite].shader = rCam.game.rainWorld.Shaders[this.underWaterMode ? "UnderWaterLight" : "FlatLight"];
            sLeaser.sprites[this.GoldSprite] = new FSprite("Futile_White", true);
            sLeaser.sprites[this.GoldSprite].color = Color.Lerp(new Color(0f, 0f, 0f), RainWorld.GoldRGB, 0.2f);
            sLeaser.sprites[this.GoldSprite].shader = rCam.game.rainWorld.Shaders["FlatLight"];
            sLeaser.sprites[this.MainSprite] = new FSprite("JetFishEyeA", true);
            sLeaser.sprites[this.MainSprite].shader = rCam.game.rainWorld.Shaders["Hologram"];
            sLeaser.sprites[this.TrailSprite] = new FSprite("JetFishEyeA", true);
            sLeaser.sprites[this.TrailSprite].shader = rCam.game.rainWorld.Shaders["Hologram"];
            for (int i = 0; i < 4; i++)
            {
                sLeaser.sprites[this.LineSprite(i)] = new FSprite("pixel", true);
                sLeaser.sprites[this.LineSprite(i)].anchorY = 0f;
                sLeaser.sprites[this.LineSprite(i)].shader = rCam.game.rainWorld.Shaders["Hologram"];
            }
            this.AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 vector = Vector2.Lerp(this.lastPos, this.pos, timeStacker);
            float num = Mathf.Lerp(this.lastGlitch, this.glitch, timeStacker);
            float num2 = Mathf.Lerp(this.lastExpand, this.expand, timeStacker);
            float num3 = Mathf.Lerp(this.lastPower, this.power, timeStacker);
            sLeaser.sprites[this.GoldSprite].x = vector.x - camPos.x;
            sLeaser.sprites[this.GoldSprite].y = vector.y - camPos.y;
            sLeaser.sprites[this.GoldSprite].alpha = 0.75f * Mathf.Lerp(Mathf.Lerp(0.8f, 0.5f, Mathf.Pow(num, 0.6f + 0.2f * Random.value)), 0.7f, num2) * num3;
            sLeaser.sprites[this.GoldSprite].scale = Mathf.Lerp(110f, 300f, num2) / 16f;
            Color color = this.GoldCol(num);
            sLeaser.sprites[this.MainSprite].color = color;
            sLeaser.sprites[this.MainSprite].x = vector.x - camPos.x;
            sLeaser.sprites[this.MainSprite].y = vector.y - camPos.y;
            sLeaser.sprites[this.MainSprite].alpha = (1f - num) * Mathf.InverseLerp(0.5f, 0f, num2) * num3 * (this.underWaterMode ? 0.5f : 1f);
            sLeaser.sprites[this.MainSprite].isVisible = (!this.contract && num3 > 0f);
            sLeaser.sprites[this.TrailSprite].color = color;
            sLeaser.sprites[this.TrailSprite].x = Mathf.Lerp(this.trail[this.trail.Length - 1].x, this.trail[this.trail.Length - 2].x, timeStacker) - camPos.x;
            sLeaser.sprites[this.TrailSprite].y = Mathf.Lerp(this.trail[this.trail.Length - 1].y, this.trail[this.trail.Length - 2].y, timeStacker) - camPos.y;
            sLeaser.sprites[this.TrailSprite].alpha = 0.75f * (1f - num) * Mathf.InverseLerp(0.5f, 0f, num2) * num3 * (this.underWaterMode ? 0.5f : 1f);
            sLeaser.sprites[this.TrailSprite].isVisible = (!this.contract && num3 > 0f);
            sLeaser.sprites[this.TrailSprite].scaleX = ((Random.value < num) ? (1f + 20f * Random.value * this.glitch) : 1f);
            sLeaser.sprites[this.TrailSprite].scaleY = ((Random.value < num) ? (1f + 2f * Random.value * Random.value * this.glitch) : 1f);
            sLeaser.sprites[this.LightSprite].x = vector.x - camPos.x;
            sLeaser.sprites[this.LightSprite].y = vector.y - camPos.y;
            if (this.underWaterMode)
            {
                sLeaser.sprites[this.LightSprite].alpha = Mathf.Pow(0.9f * (1f - num) * Mathf.InverseLerp(0.5f, 0f, num2) * num3, 0.5f);
                sLeaser.sprites[this.LightSprite].scale = Mathf.Lerp(60f, 120f, num) / 16f;
            }
            else
            {
                sLeaser.sprites[this.LightSprite].alpha = 0.9f * (1f - num) * Mathf.InverseLerp(0.5f, 0f, num2) * num3;
                sLeaser.sprites[this.LightSprite].scale = Mathf.Lerp(20f, 40f, num) / 16f;
            }
            sLeaser.sprites[this.LightSprite].color = Color.Lerp(this.TokenColor, color, 0.4f);
            sLeaser.sprites[this.LightSprite].isVisible = (!this.contract && num3 > 0f);
            for (int i = 0; i < 4; i++)
            {
                Vector2 vector2 = Vector2.Lerp(this.lines[i, 1], this.lines[i, 0], timeStacker);
                int num4 = (i == 3) ? 0 : (i + 1);
                Vector2 vector3 = Vector2.Lerp(this.lines[num4, 1], this.lines[num4, 0], timeStacker);
                float num5 = 1f - (1f - Mathf.Max(this.lines[i, 3].x, this.lines[num4, 3].x)) * (1f - num);
                num5 = Mathf.Pow(num5, 2f);
                num5 *= 1f - num2;
                if (Random.value < num5)
                {
                    vector3 = Vector2.Lerp(vector2, vector3, Random.value);
                    if (this.stalk != null)
                    {
                        vector2 = this.stalk.EyePos(timeStacker);
                    }
                    if (this.expandAroundCreature != null && (Random.value < this.expand || this.contract))
                    {
                        vector2 = Vector2.Lerp(this.expandAroundCreature.mainBodyChunk.lastPos, this.expandAroundCreature.mainBodyChunk.pos, timeStacker);
                    }
                }
                sLeaser.sprites[this.LineSprite(i)].x = vector2.x - camPos.x;
                sLeaser.sprites[this.LineSprite(i)].y = vector2.y - camPos.y;
                sLeaser.sprites[this.LineSprite(i)].scaleY = Vector2.Distance(vector2, vector3);
                sLeaser.sprites[this.LineSprite(i)].rotation = Custom.AimFromOneVectorToAnother(vector2, vector3);
                sLeaser.sprites[this.LineSprite(i)].alpha = (1f - num5) * num3 * (this.underWaterMode ? 0.2f : 1f);
                sLeaser.sprites[this.LineSprite(i)].color = color;
                sLeaser.sprites[this.LineSprite(i)].isVisible = (num3 > 0f);
            }
            if (base.slatedForDeletetion || this.room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("Water");
            }
            var grabShaders = rCam.ReturnFContainer("GrabShaders");
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].RemoveFromContainer();
            }
            newContatiner.AddChild(sLeaser.sprites[this.GoldSprite]);
            for (int j = 0; j <= this.TrailSprite; j++)
            {
                newContatiner.AddChild(sLeaser.sprites[j]);
            }
            for (int l = 0; l < 4; l++)
            {
                grabShaders.AddChild(sLeaser.sprites[this.LineSprite(l)]);
            }
        }

        public Color TokenColor;
        public Vector2 hoverPos;
        public Vector2 pos;
        public Vector2 lastPos;
        public Vector2 vel;
        public float sinCounter;
        public float sinCounter2;
        public Vector2[] trail;
        private float expand;
        private float lastExpand;
        private bool contract;
        private MeadowGameMode mgm;
        public Vector2[,] lines;
        public bool underWaterMode;
        public Creature expandAroundCreature;
        private float glitch;
        private float lastGlitch;
        private float generalGlitch;
        public MeadowCollectToken.TokenStalk stalk;
        private bool poweredOn;
        private float power;
        private float lastPower;
        private StaticSoundLoop soundLoop;
        private StaticSoundLoop glitchLoop;
        private Creature avatarCreature;

        public class TokenStalk : UpdatableAndDeletable, IDrawable
        {
            public int BaseSprite => 0;
            public int Arm1Sprite => 1;
            public int Arm2Sprite => 2;
            public int Arm3Sprite => 3;
            public int Arm4Sprite => 4;
            public int Arm5Sprite => 5;
            public int ArmJointSprite => 6;
            public int SocketSprite => 7;
            public int HeadSprite => 8;
            public int LampSprite => 9;
            public int CoordSprite(int s) => 10 + s;
            public int TotalSprites => 10 + this.coord.GetLength(0);

            public float alive
            {
                get
                {
                    if (this.token == null)
                    {
                        return 0f;
                    }
                    return 0.25f + 0.75f * this.token.power;
                }
            }

            public TokenStalk(Room room, Vector2 hoverPos, Vector2 basePos, Color tokenColor, MeadowCollectToken token)
            {
                this.token = token;
                this.hoverPos = hoverPos;
                this.basePos = basePos;
                if (token != null)
                {
                    this.lampPower = 1f;
                    this.lastLampPower = 1f;
                }
                this.lampColor = Color.Lerp(tokenColor, new Color(1f, 1f, 1f), 0.4f);
                Random.State state = Random.state;
                Random.InitState((int)(hoverPos.x * 10f) + (int)(hoverPos.y * 10f));
                this.curveLerps = new float[2, 5];
                for (int i = 0; i < this.curveLerps.GetLength(0); i++)
                {
                    this.curveLerps[i, 0] = 1f;
                    this.curveLerps[i, 1] = 1f;
                }
                this.curveLerps[0, 3] = Random.value * 360f;
                this.curveLerps[1, 3] = Mathf.Lerp(10f, 20f, Random.value);
                this.flip = ((Random.value < 0.5f) ? -1f : 1f);
                this.mainDir = Custom.DirVec(basePos, hoverPos);
                this.coordLength = Vector2.Distance(basePos, hoverPos) * 0.6f;
                this.coord = new Vector2[(int)(this.coordLength / this.coordSeg), 3];
                this.armLength = Vector2.Distance(basePos, hoverPos) / 2f;
                this.armPos = basePos + this.mainDir * this.armLength;
                this.lastArmPos = this.armPos;
                this.armGetToPos = this.armPos;
                for (int j = 0; j < this.coord.GetLength(0); j++)
                {
                    this.coord[j, 0] = this.armPos;
                    this.coord[j, 1] = this.armPos;
                }
                this.head = hoverPos - this.mainDir * this.headDist;
                this.lastHead = this.head;
                Random.state = state;
            }

            public override void Update(bool eu)
            {
                this.lastArmPos = this.armPos;
                this.armPos += this.armVel;
                this.armPos = Custom.MoveTowards(this.armPos, this.armGetToPos, (0.8f + this.armLength / 150f) / 2f);
                this.armVel *= 0.8f;
                this.armVel += Vector2.ClampMagnitude(this.armGetToPos - this.armPos, 4f) / 11f;
                this.lastHead = this.head;
                this.head += this.headVel;
                this.headVel *= 0.8f;
                if (this.token != null && (this.token.slatedForDeletetion || token.room == null))
                {
                    this.token = null;
                }
                this.lastLampPower = this.lampPower;
                this.lastSinCounter = this.sinCounter;
                this.sinCounter += Random.value * this.lampPower;
                if (this.token != null)
                {
                    this.lampPower = Custom.LerpAndTick(this.lampPower, 1f, 0.02f, 0.016666668f);
                }
                else
                {
                    this.lampPower = Mathf.Max(0f, this.lampPower - 0.008333334f);
                }
                if (!Custom.DistLess(this.head, this.armPos, this.coordLength))
                {
                    this.headVel -= Custom.DirVec(this.armPos, this.head) * (Vector2.Distance(this.armPos, this.head) - this.coordLength) * 0.8f;
                    this.head -= Custom.DirVec(this.armPos, this.head) * (Vector2.Distance(this.armPos, this.head) - this.coordLength) * 0.8f;
                }
                this.headVel += (Vector2) Vector3.Slerp(Custom.DegToVec(this.GetCurveLerp(0, 0.5f, 1f)), new Vector2(0f, 1f), 0.4f) * 0.4f;
                this.lastHeadDir = this.headDir;
                Vector2 vector = this.hoverPos;
                if (this.token != null && this.token.expand == 0f && !this.token.contract)
                {
                    vector = Vector2.Lerp(this.hoverPos, this.token.pos, this.alive);
                }
                this.headVel -= Custom.DirVec(vector, this.head) * (Vector2.Distance(vector, this.head) - this.headDist) * 0.8f;
                this.head -= Custom.DirVec(vector, this.head) * (Vector2.Distance(vector, this.head) - this.headDist) * 0.8f;
                this.headDir = Custom.DirVec(this.head, vector);
                if (Random.value < 1f / Mathf.Lerp(300f, 60f, this.alive))
                {
                    Vector2 b = this.basePos + this.mainDir * this.armLength * 0.7f + Custom.RNV() * Random.value * this.armLength * Mathf.Lerp(0.1f, 0.3f, this.alive);
                    if (SharedPhysics.RayTraceTilesForTerrain(this.room, this.armGetToPos, b))
                    {
                        this.armGetToPos = b;
                    }
                    this.NewCurveLerp(0, this.curveLerps[0, 3] + Mathf.Lerp(-180f, 180f, Random.value), Mathf.Lerp(1f, 2f, this.alive));
                    this.NewCurveLerp(1, Mathf.Lerp(10f, 20f, Mathf.Pow(Random.value, 0.75f)), Mathf.Lerp(0.4f, 0.8f, this.alive));
                }
                this.headDist = this.GetCurveLerp(1, 0.5f, 1f);
                if (this.token != null)
                {
                    this.keepDistance = Custom.LerpAndTick(this.keepDistance, Mathf.Sin(Mathf.Clamp01(this.token.glitch) * 3.1415927f) * this.alive, 0.006f, this.alive / ((this.keepDistance < this.token.glitch) ? 40f : 80f));
                }
                this.headDist = Mathf.Lerp(this.headDist, 50f, Mathf.Pow(this.keepDistance, 0.5f));
                Vector2 a = Custom.DirVec(Custom.InverseKinematic(this.basePos, this.armPos, this.armLength * 0.65f, this.armLength * 0.35f, this.flip), this.armPos);
                for (int i = 0; i < this.coord.GetLength(0); i++)
                {
                    float num = Mathf.InverseLerp(-1f, (float)this.coord.GetLength(0), (float)i);
                    Vector2 a2 = Custom.Bezier(this.armPos, this.armPos + a * this.coordLength * 0.5f, this.head, this.head - this.headDir * this.coordLength * 0.5f, num);
                    this.coord[i, 1] = this.coord[i, 0];
                    this.coord[i, 0] += this.coord[i, 2];
                    this.coord[i, 2] *= 0.8f;
                    this.coord[i, 2] += (a2 - this.coord[i, 0]) * Mathf.Lerp(0f, 0.25f, Mathf.Sin(num * 3.1415927f));
                    this.coord[i, 0] += (a2 - this.coord[i, 0]) * Mathf.Lerp(0f, 0.25f, Mathf.Sin(num * 3.1415927f));
                    if (i > 2)
                    {
                        this.coord[i, 2] += Custom.DirVec(this.coord[i - 2, 0], this.coord[i, 0]);
                        this.coord[i - 2, 2] -= Custom.DirVec(this.coord[i - 2, 0], this.coord[i, 0]);
                    }
                    if (i > 3)
                    {
                        this.coord[i, 2] += Custom.DirVec(this.coord[i - 3, 0], this.coord[i, 0]) * 0.5f;
                        this.coord[i - 3, 2] -= Custom.DirVec(this.coord[i - 3, 0], this.coord[i, 0]) * 0.5f;
                    }
                    if (num < 0.5f)
                    {
                        this.coord[i, 2] += a * Mathf.InverseLerp(0.5f, 0f, num) * Mathf.InverseLerp(5f, 0f, (float)i);
                    }
                    else
                    {
                        this.coord[i, 2] -= this.headDir * Mathf.InverseLerp(0.5f, 1f, num);
                    }
                }
                this.ConnectCoord();
                this.ConnectCoord();
                for (int j = 0; j < this.coord.GetLength(0); j++)
                {
                    SharedPhysics.TerrainCollisionData terrainCollisionData = this.scratchTerrainCollisionData.Set(this.coord[j, 0], this.coord[j, 1], this.coord[j, 2], 2f, new IntVector2(0, 0), true);
                    terrainCollisionData = SharedPhysics.HorizontalCollision(this.room, terrainCollisionData);
                    terrainCollisionData = SharedPhysics.VerticalCollision(this.room, terrainCollisionData);
                    this.coord[j, 0] = terrainCollisionData.pos;
                    this.coord[j, 2] = terrainCollisionData.vel;
                }
                for (int k = 0; k < this.curveLerps.GetLength(0); k++)
                {
                    this.curveLerps[k, 1] = this.curveLerps[k, 0];
                    this.curveLerps[k, 0] = Mathf.Min(1f, this.curveLerps[k, 0] + this.curveLerps[k, 4]);
                }
                base.Update(eu);
            }

            private void NewCurveLerp(int curveLerp, float to, float speed)
            {
                if (this.curveLerps[curveLerp, 0] < 1f || this.curveLerps[curveLerp, 1] < 1f)
                {
                    return;
                }
                this.curveLerps[curveLerp, 2] = this.curveLerps[curveLerp, 3];
                this.curveLerps[curveLerp, 3] = to;
                this.curveLerps[curveLerp, 4] = speed / Mathf.Abs(this.curveLerps[curveLerp, 2] - this.curveLerps[curveLerp, 3]);
                this.curveLerps[curveLerp, 0] = 0f;
                this.curveLerps[curveLerp, 1] = 0f;
            }

            private float GetCurveLerp(int curveLerp, float sCurveK, float timeStacker)
            {
                return Mathf.Lerp(this.curveLerps[curveLerp, 2], this.curveLerps[curveLerp, 3], Custom.SCurve(Mathf.Lerp(this.curveLerps[curveLerp, 1], this.curveLerps[curveLerp, 0], timeStacker), sCurveK));
            }

            private void ConnectCoord()
            {
                this.coord[0, 2] -= Custom.DirVec(this.armPos, this.coord[0, 0]) * (Vector2.Distance(this.armPos, this.coord[0, 0]) - this.coordSeg);
                this.coord[0, 0] -= Custom.DirVec(this.armPos, this.coord[0, 0]) * (Vector2.Distance(this.armPos, this.coord[0, 0]) - this.coordSeg);
                for (int i = 1; i < this.coord.GetLength(0); i++)
                {
                    if (!Custom.DistLess(this.coord[i - 1, 0], this.coord[i, 0], this.coordSeg))
                    {
                        Vector2 a = Custom.DirVec(this.coord[i, 0], this.coord[i - 1, 0]) * (Vector2.Distance(this.coord[i - 1, 0], this.coord[i, 0]) - this.coordSeg);
                        this.coord[i, 2] += a * 0.5f;
                        this.coord[i, 0] += a * 0.5f;
                        this.coord[i - 1, 2] -= a * 0.5f;
                        this.coord[i - 1, 0] -= a * 0.5f;
                    }
                }
                this.coord[this.coord.GetLength(0) - 1, 2] -= Custom.DirVec(this.head, this.coord[this.coord.GetLength(0) - 1, 0]) * (Vector2.Distance(this.head, this.coord[this.coord.GetLength(0) - 1, 0]) - this.coordSeg);
                this.coord[this.coord.GetLength(0) - 1, 0] -= Custom.DirVec(this.head, this.coord[this.coord.GetLength(0) - 1, 0]) * (Vector2.Distance(this.head, this.coord[this.coord.GetLength(0) - 1, 0]) - this.coordSeg);
            }

            public Vector2 EyePos(float timeStacker)
            {
                return Vector2.Lerp(this.lastHead, this.head, timeStacker) + (Vector2) Vector3.Slerp(this.lastHeadDir, this.headDir, timeStacker) * 3f;
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites = new FSprite[this.TotalSprites];
                sLeaser.sprites[this.BaseSprite] = new FSprite("Circle20", true);
                sLeaser.sprites[this.BaseSprite].scaleX = 0.5f;
                sLeaser.sprites[this.BaseSprite].scaleY = 0.7f;
                sLeaser.sprites[this.BaseSprite].rotation = Custom.VecToDeg(this.mainDir);
                sLeaser.sprites[this.Arm1Sprite] = new FSprite("pixel", true);
                sLeaser.sprites[this.Arm1Sprite].scaleX = 4f;
                sLeaser.sprites[this.Arm1Sprite].anchorY = 0f;
                sLeaser.sprites[this.Arm2Sprite] = new FSprite("pixel", true);
                sLeaser.sprites[this.Arm2Sprite].scaleX = 3f;
                sLeaser.sprites[this.Arm2Sprite].anchorY = 0f;
                sLeaser.sprites[this.Arm3Sprite] = new FSprite("pixel", true);
                sLeaser.sprites[this.Arm3Sprite].scaleX = 1.5f;
                sLeaser.sprites[this.Arm3Sprite].scaleY = this.armLength * 0.6f;
                sLeaser.sprites[this.Arm3Sprite].anchorY = 0f;
                sLeaser.sprites[this.Arm4Sprite] = new FSprite("pixel", true);
                sLeaser.sprites[this.Arm4Sprite].scaleX = 3f;
                sLeaser.sprites[this.Arm4Sprite].scaleY = 8f;
                sLeaser.sprites[this.Arm5Sprite] = new FSprite("pixel", true);
                sLeaser.sprites[this.Arm5Sprite].scaleX = 6f;
                sLeaser.sprites[this.Arm5Sprite].scaleY = 8f;
                sLeaser.sprites[this.ArmJointSprite] = new FSprite("JetFishEyeA", true);
                sLeaser.sprites[this.LampSprite] = new FSprite("tinyStar", true);
                sLeaser.sprites[this.SocketSprite] = new FSprite("pixel", true);
                sLeaser.sprites[this.SocketSprite].scaleX = 5f;
                sLeaser.sprites[this.SocketSprite].scaleY = 9f;
                sLeaser.sprites[this.HeadSprite] = new FSprite("pixel", true);
                sLeaser.sprites[this.HeadSprite].scaleX = 4f;
                sLeaser.sprites[this.HeadSprite].scaleY = 6f;
                for (int i = 0; i < this.coord.GetLength(0); i++)
                {
                    sLeaser.sprites[this.CoordSprite(i)] = new FSprite("pixel", true);
                    sLeaser.sprites[this.CoordSprite(i)].scaleX = ((i % 2 == 0) ? 2f : 3f);
                    sLeaser.sprites[this.CoordSprite(i)].scaleY = 5f;
                }
                this.AddToContainer(sLeaser, rCam, null);
            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                sLeaser.sprites[this.BaseSprite].x = this.basePos.x - camPos.x;
                sLeaser.sprites[this.BaseSprite].y = this.basePos.y - camPos.y;
                Vector2 vector = Vector2.Lerp(this.lastHead, this.head, timeStacker);
                Vector2 vector2 = Vector3.Slerp(this.lastHeadDir, this.headDir, timeStacker);
                Vector2 vector3 = Vector2.Lerp(this.lastArmPos, this.armPos, timeStacker);
                Vector2 vector4 = Custom.InverseKinematic(this.basePos, vector3, this.armLength * 0.65f, this.armLength * 0.35f, this.flip);
                sLeaser.sprites[this.Arm1Sprite].x = this.basePos.x - camPos.x;
                sLeaser.sprites[this.Arm1Sprite].y = this.basePos.y - camPos.y;
                sLeaser.sprites[this.Arm1Sprite].scaleY = Vector2.Distance(this.basePos, vector4);
                sLeaser.sprites[this.Arm1Sprite].rotation = Custom.AimFromOneVectorToAnother(this.basePos, vector4);
                sLeaser.sprites[this.Arm2Sprite].x = vector4.x - camPos.x;
                sLeaser.sprites[this.Arm2Sprite].y = vector4.y - camPos.y;
                sLeaser.sprites[this.Arm2Sprite].scaleY = Vector2.Distance(vector4, vector3);
                sLeaser.sprites[this.Arm2Sprite].rotation = Custom.AimFromOneVectorToAnother(vector4, vector3);
                sLeaser.sprites[this.SocketSprite].x = vector3.x - camPos.x;
                sLeaser.sprites[this.SocketSprite].y = vector3.y - camPos.y;
                sLeaser.sprites[this.SocketSprite].rotation = Custom.VecToDeg(Vector3.Slerp(Custom.DirVec(vector4, vector3), Custom.DirVec(vector3, Vector2.Lerp(this.coord[0, 1], this.coord[0, 0], timeStacker)), 0.4f));
                Vector2 vector5 = Vector2.Lerp(this.basePos, vector4, 0.3f);
                Vector2 vector6 = Vector2.Lerp(vector4, vector3, 0.4f);
                sLeaser.sprites[this.Arm3Sprite].x = vector5.x - camPos.x;
                sLeaser.sprites[this.Arm3Sprite].y = vector5.y - camPos.y;
                sLeaser.sprites[this.Arm3Sprite].rotation = Custom.AimFromOneVectorToAnother(vector5, vector6);
                sLeaser.sprites[this.Arm4Sprite].x = vector6.x - camPos.x;
                sLeaser.sprites[this.Arm4Sprite].y = vector6.y - camPos.y;
                sLeaser.sprites[this.Arm4Sprite].rotation = Custom.AimFromOneVectorToAnother(vector5, vector6);
                vector5 += Custom.DirVec(this.basePos, vector4) * (this.armLength * 0.1f + 2f);
                sLeaser.sprites[this.Arm5Sprite].x = vector5.x - camPos.x;
                sLeaser.sprites[this.Arm5Sprite].y = vector5.y - camPos.y;
                sLeaser.sprites[this.Arm5Sprite].rotation = Custom.AimFromOneVectorToAnother(this.basePos, vector4);
                sLeaser.sprites[this.LampSprite].x = vector5.x - camPos.x;
                sLeaser.sprites[this.LampSprite].y = vector5.y - camPos.y;
                sLeaser.sprites[this.LampSprite].color = Color.Lerp(this.lampOffCol, this.lampColor, Mathf.Lerp(this.lastLampPower, this.lampPower, timeStacker) * Mathf.Pow(Random.value, 0.5f) * (0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(this.lastSinCounter, this.sinCounter, timeStacker) / 6f)));
                sLeaser.sprites[this.ArmJointSprite].x = vector4.x - camPos.x;
                sLeaser.sprites[this.ArmJointSprite].y = vector4.y - camPos.y;
                sLeaser.sprites[this.HeadSprite].x = vector.x - camPos.x;
                sLeaser.sprites[this.HeadSprite].y = vector.y - camPos.y;
                sLeaser.sprites[this.HeadSprite].rotation = Custom.VecToDeg(vector2);
                Vector2 p = vector3;
                for (int i = 0; i < this.coord.GetLength(0); i++)
                {
                    Vector2 vector7 = Vector2.Lerp(this.coord[i, 1], this.coord[i, 0], timeStacker);
                    sLeaser.sprites[this.CoordSprite(i)].x = vector7.x - camPos.x;
                    sLeaser.sprites[this.CoordSprite(i)].y = vector7.y - camPos.y;
                    sLeaser.sprites[this.CoordSprite(i)].rotation = Custom.AimFromOneVectorToAnother(p, vector7);
                    p = vector7;
                }
                if (base.slatedForDeletetion || this.room != rCam.room)
                {
                    sLeaser.CleanSpritesAndRemove();
                }
            }

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    sLeaser.sprites[i].color = palette.blackColor;
                }
                this.lampOffCol = Color.Lerp(palette.blackColor, new Color(1f, 1f, 1f), 0.15f);
            }

            public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
            {
                if (newContatiner == null)
                {
                    newContatiner = rCam.ReturnFContainer("Midground");
                }
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    sLeaser.sprites[i].RemoveFromContainer();
                    newContatiner.AddChild(sLeaser.sprites[i]);
                }
            }

            public Vector2 hoverPos;
            public MeadowCollectToken token;
            public Vector2[,] stalk;
            public Vector2 basePos;
            public Vector2 mainDir;
            public float flip;
            public Vector2 armPos;
            public Vector2 lastArmPos;
            public Vector2 armVel;
            public Vector2 armGetToPos;
            public Vector2 head;
            public Vector2 lastHead;
            public Vector2 headVel;
            public Vector2 headDir;
            public Vector2 lastHeadDir;
            private float headDist = 15f;
            public float armLength;
            private Vector2[,] coord;
            private float coordLength;
            private float coordSeg = 3f;
            private float[,] curveLerps;
            private float keepDistance;
            private float sinCounter;
            private float lastSinCounter;
            private float lampPower;
            private float lastLampPower;
            private SharedPhysics.TerrainCollisionData scratchTerrainCollisionData;
            public Color lampColor;
            private Color lampOffCol;
        }

        public class TokenSpark : CosmeticSprite
        {
            public TokenSpark(Vector2 pos, Vector2 vel, Color color, bool underWater)
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
                sLeaser.sprites[0] = new FSprite("pixel", true);
                sLeaser.sprites[0].color = this.color;
                sLeaser.sprites[0].anchorY = 0f;
                if (this.underWater)
                {
                    sLeaser.sprites[0].alpha = 0.5f;
                }
                this.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Foreground"));
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                Vector2 vector = Vector2.Lerp(this.lastPos, this.pos, timeStacker);
                Vector2 vector2 = Vector2.Lerp(this.lastLastPos, this.lastPos, timeStacker);
                sLeaser.sprites[0].x = vector.x - camPos.x;
                sLeaser.sprites[0].y = vector.y - camPos.y;
                sLeaser.sprites[0].scaleY = Vector2.Distance(vector, vector2) * Mathf.InverseLerp(0f, 0.5f, this.life);
                sLeaser.sprites[0].rotation = Custom.AimFromOneVectorToAnother(vector, vector2);
                sLeaser.sprites[0].isVisible = (Random.value < Mathf.InverseLerp(0f, 0.5f, this.life));
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            }

            public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
            }

            public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
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

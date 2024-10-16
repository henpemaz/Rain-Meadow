using System.IO;
using UnityEngine;
using RWCustom;
using System;
using HarmonyLib;

namespace RainMeadow
{
    public class AbstractMeadowGhost : AbstractMeadowCollectible
    {
        public int targetCount;
        public int currentCount;
        public AbstractMeadowGhost(World world, AbstractObjectType type, WorldCoordinate pos, EntityID ID) : base(world, type, pos, ID)
        {
            duration = 40 * 60; // a minute
            targetCount = 3;
        }

        public override void Realize()
        {
            base.Realize();
            if (this.realizedObject != null)
            {
                return;
            }
            if (type == RainMeadow.Ext_PhysicalObjectType.MeadowGhost)
            {
                this.realizedObject = new MeadowGhost(this);
            }
        }

        internal void Activated()
        {
            RainMeadow.Debug(online);
            NowCollected();
        }
    }

    internal class MeadowGhost : MeadowCollectible, IDrawable
    {
        Vector2 pos;
        float voffset = 40f;
        public AbstractMeadowGhost abstractGhost => this.abstractPhysicalObject as AbstractMeadowGhost;

        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);
            pos = this.firstChunk.pos + new Vector2(0f, voffset);
            Reset();
        }

        public MeadowGhost(AbstractMeadowGhost apo) : base(apo)
        {
            this.scale = 0.25f;
            this.lightSpriteScale = 2f;

            this.ncircles = abstractGhost.targetCount;
            this.ringOffsets = new Vector2[ncircles];
            var left = (-30f * (ncircles - 1)) / 2f;
            for (int i = 0; i < ncircles; i++)
            {
                ringOffsets[i] = new Vector2(left + i * 30f, 60f);
            }
            
            this.spine = new MeadowGhost.Part[this.spineSegments];
            for (int i = 0; i < this.spine.Length; i++)
            {
                this.spine[i] = new MeadowGhost.Part(this.scale);
            }
            this.legs = new MeadowGhost.Part[2, 3];
            for (int j = 0; j < this.legs.GetLength(0); j++)
            {
                for (int k = 0; k < this.legs.GetLength(1); k++)
                {
                    this.legs[j, k] = new MeadowGhost.Part(this.scale);
                }
            }
            this.LoadElement("ghostScales");
            this.LoadElement("ghostPlates");
            this.LoadElement("ghostBand");
            this.totalSprites = 1;
            this.totalSprites += 2 * ncircles;
            this.rags = new MeadowGhost.Rags(this, this.totalSprites);
            this.behindBodySprites = totalSprites + this.rags.totalSprites;
            this.totalSprites = this.behindBodySprites + this.totalStaticSprites;
            this.chains = new MeadowGhost.Chains(this, this.totalSprites);
            this.totalSprites += this.chains.totalSprites;
            this.sinBob = global::UnityEngine.Random.value;
            this.Reset();
        }

        public void Reset()
        {
            for (int i = 0; i < this.spine.Length; i++)
            {
                this.spine[i].pos = this.pos + Custom.RNV();
                this.spine[i].lastPos = this.spine[i].pos;
                this.spine[i].vel *= 0f;
            }
            for (int j = 0; j < this.legs.GetLength(0); j++)
            {
                for (int k = 0; k < this.legs.GetLength(1); k++)
                {
                    this.legs[j, k].pos = this.pos + Custom.RNV();
                    this.legs[j, k].lastPos = this.legs[j, k].pos;
                    this.legs[j, k].vel *= 0f;
                }
            }
            this.chains.Reset(this.pos);
            this.rags.Reset(this.pos);
            this.flip = this.defaultFlip;
            this.flipFrom = this.defaultFlip;
            this.flipTo = this.defaultFlip;
            this.flipProg = 1f;
            this.flipSpeed = 1f;
        }

        private void LoadElement(string elementName)
        {
            if (Futile.atlasManager.GetAtlasWithName(elementName) != null)
            {
                return;
            }
            string text = AssetManager.ResolveFilePath("Illustrations" + Path.DirectorySeparatorChar.ToString() + elementName + ".png");
            Texture2D texture2D = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            AssetManager.SafeWWWLoadTexture(ref texture2D, "file:///" + text, false, true);
            Futile.atlasManager.LoadAtlasFromTexture(elementName, texture2D, false);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            this.lastFadeOut = this.fadeOut;

            // logic

            // if not activated yet and is owner
            if (!abstractCollectible.collected && abstractCollectible.online != null && abstractCollectible.online.isMine)
            {
                // counting players nearby
                int count = 0;
                for (int i = 0; i < room.updateList.Count; i++)
                {
                    var uad = room.updateList[i];
                    if (uad is Creature c && c.room == this.room)
                    {
                        if ((c.firstChunk.pos - this.pos).sqrMagnitude < Math.Pow(500 - c.firstChunk.rad, 2))
                        {
                            count++;
                            RainMeadow.Trace($"found {c} at distance {(c.firstChunk.pos - this.pos).magnitude}");
                        }
                    }
                }
                abstractGhost.currentCount = count;
                RainMeadow.Trace($"counted {count}");
                // if enough players
                if (abstractGhost.currentCount >= abstractGhost.targetCount)
                {
                    // activate
                    abstractGhost.Activated();
                }
            }

            // if activated
            if (abstractCollectible.collected)
            {
                abstractGhost.currentCount = abstractGhost.targetCount;
            //      if not collected locally
                if (!abstractCollectible.collectedLocally)
                {
            //          if local player nearby
                    if (OnlineManager.lobby.gameMode.avatars[0].realizedCreature is Creature c)
                    {
                        if ((c.firstChunk.pos - this.pos).sqrMagnitude < Mathf.Pow(700 - c.firstChunk.rad, 2))
                        {
            //              collect
                            abstractCollectible.Collect();
            //              start animating
                            fadeOut = 0.01f;
                        }
                    }
                }
            }

            // ghostiness
            this.ghostinessGoal = 0.3f + 0.7f * (abstractGhost.currentCount / (float)abstractGhost.targetCount) - fadeOut;
            this.ghostiness = Mathf.Lerp(this.ghostiness, this.ghostinessGoal, 0.004f + fadeOut);
            this.room.game.cameras.Do(c => { if (c.room == this.room) c.ghostMode = this.ghostiness; });

            // if animating
            if (this.fadeOut > 0f)
            {
                this.fadeOut = Mathf.Min(1f, this.fadeOut + 0.0125f);
                if (this.fadeOut == 1f)
                {
                    RainMeadow.Debug("complete: ");
                    this.RemoveFromRoom();
                    return;
                }
            }

            // animation
            this.rags.Update();
            this.chains.Update();
            this.sinBob += 1f / Mathf.Lerp(140f, 210f, global::UnityEngine.Random.value);
            this.pos = this.firstChunk.pos + new Vector2(0f, voffset +  Mathf.Sin(this.sinBob * 3.1415927f * 2f) * 18f * this.scale);
            this.flipProg = Mathf.Min(1f, this.flipProg + this.flipSpeed);
            this.flip = Mathf.Lerp(this.flipFrom, this.flipTo, Custom.SCurve(this.flipProg, 0.7f));
            if (this.flipProg >= 1f && global::UnityEngine.Random.value < 0.1f)
            {
                this.flipFrom = this.flip;
                this.flipTo = Mathf.Clamp((this.flip + this.defaultFlip) / 2f + Mathf.Lerp(0.05f, 0.5f, Mathf.Pow(global::UnityEngine.Random.value, 2.5f)) * ((global::UnityEngine.Random.value < 0.5f) ? (-1f) : 1f), -1f, 1f);
                this.flipProg = 0f;
                this.flipSpeed = 1f / (Mathf.Lerp(30f, 220f, global::UnityEngine.Random.value) * Mathf.Abs(this.flipFrom - this.flipTo));
            }
            float num = 30f * this.scale;
            for (int j = 0; j < this.spine.Length; j++)
            {
                float num2 = (float)j / (float)(this.spine.Length - 1);
                Vector2 vector = Custom.FlattenVectorAlongAxis(Custom.DegToVec(Mathf.Lerp(180f, -90f, num2)), -15f, 1.3f) * Mathf.Lerp(100f, 40f, num2) * this.scale;
                vector.x *= this.flip;
                vector += this.pos;
                this.spine[j].vel *= this.airResistance;
                this.spine[j].Update();
                this.spine[j].vel += (vector - this.spine[j].pos) / 10f;
                if (j > 0)
                {
                    Vector2 vector2 = (this.spine[j].pos - this.spine[j - 1].pos).normalized;
                    float num3 = Vector2.Distance(this.spine[j].pos, this.spine[j - 1].pos);
                    float num4 = ((num3 < num && j == this.spineBendPoint) ? 0f : 0.5f);
                    this.spine[j].pos += vector2 * (num - num3) * num4;
                    this.spine[j].vel += vector2 * (num - num3) * num4;
                    this.spine[j - 1].pos -= vector2 * (num - num3) * num4;
                    this.spine[j - 1].vel -= vector2 * (num - num3) * num4;
                    if (j > 1)
                    {
                        vector2 = (this.spine[j].pos - this.spine[j - 2].pos).normalized;
                        this.spine[j].vel += vector2 * 0.2f;
                        this.spine[j - 2].vel -= vector2 * 0.2f;
                    }
                }
            }
            for (int k = 0; k < this.legs.GetLength(0); k++)
            {
                for (int l = 0; l < this.legs.GetLength(1); l++)
                {
                    Vector2 vector3;
                    if (l == 0)
                    {
                        vector3 = Vector2.Lerp(this.pos, this.spine[this.spineBendPoint - 3].pos, 0.5f) + new Vector2(Mathf.Lerp(110f, 50f, Mathf.Abs(this.flip)) * ((k == 0) ? (-1f) : 1f) + this.flip * 8f, 15f) * this.scale;
                    }
                    else if (l == 1)
                    {
                        vector3 = Vector2.Lerp(this.pos, this.spine[0].pos, 0.5f) + new Vector2(Mathf.Lerp(-70f, -30f, Mathf.Abs(this.flip)) * ((k == 0) ? (-1f) : 1f) - this.flip * 20f, -70f) * this.scale;
                    }
                    else
                    {
                        vector3 = this.spine[0].pos + new Vector2(Mathf.Lerp(-80f, -40f, Mathf.Abs(this.flip)) * ((k == 0) ? (-1f) : 1f), -90f) * this.scale;
                        vector3 = Vector2.Lerp(vector3, this.legs[k, 1].pos + new Vector2(-20f * ((k == 0) ? (-1f) : 1f), -10f) * this.scale, 0.5f);
                        this.legs[k, l].vel += Custom.DirVec(this.legs[k, 0].pos, this.legs[k, l].pos) * 2f * this.scale;
                    }
                    this.legs[k, l].vel *= this.airResistance;
                    this.legs[k, l].Update();
                    this.legs[k, l].vel += (vector3 - this.legs[k, l].pos) / 10f;
                }
                Vector2 vector4 = (this.legs[k, 0].pos - this.legs[k, 1].pos).normalized;
                float num5 = Vector2.Distance(this.legs[k, 0].pos, this.legs[k, 1].pos);
                float num6 = 210f * this.scale;
                this.legs[k, 0].pos += vector4 * (num6 - num5) * 0.5f;
                this.legs[k, 0].vel += vector4 * (num6 - num5) * 0.5f;
                this.legs[k, 1].pos -= vector4 * (num6 - num5) * 0.5f;
                this.legs[k, 1].vel -= vector4 * (num6 - num5) * 0.5f;
                vector4 = (this.legs[k, 0].pos - this.spine[0].pos).normalized;
                num5 = Vector2.Distance(this.legs[k, 0].pos, this.spine[0].pos);
                num6 = 120f * this.scale;
                this.legs[k, 0].pos += vector4 * (num6 - num5) * 0.5f;
                this.legs[k, 0].vel += vector4 * (num6 - num5) * 0.5f;
                this.spine[0].pos -= vector4 * (num6 - num5) * 0.5f;
                this.spine[0].vel -= vector4 * (num6 - num5) * 0.5f;
                vector4 = (this.legs[k, 1].pos - this.legs[k, 2].pos).normalized;
                num5 = Vector2.Distance(this.legs[k, 1].pos, this.legs[k, 2].pos);
                num6 = 40f * this.scale;
                this.legs[k, 1].pos += vector4 * (num6 - num5) * 0.15f;
                this.legs[k, 1].vel += vector4 * (num6 - num5) * 0.15f;
                this.legs[k, 2].pos -= vector4 * (num6 - num5) * 0.85f;
                this.legs[k, 2].vel -= vector4 * (num6 - num5) * 0.85f;
            }
        }

        public int LightSprite
        {
            get
            {
                return 0;
            }
        }

        int RingSprite(int index) => 1 + index;
        int CircleSprite(int index) => 1 + ncircles + index;

        public int BodyMeshSprite
        {
            get
            {
                return this.behindBodySprites;
            }
        }

        public int ButtockSprite(int side)
        {
            return this.behindBodySprites + 1 + side;
        }

        public int ThightSprite(int side)
        {
            return this.behindBodySprites + 3 + side;
        }

        public int LowerLegSprite(int side)
        {
            return this.behindBodySprites + 5 + side;
        }

        public int NeckConnectorSprite
        {
            get
            {
                return this.behindBodySprites + 7;
            }
        }

        public int HeadMeshSprite
        {
            get
            {
                return this.behindBodySprites + 8;
            }
        }

        public int DistortionSprite
        {
            get
            {
                return this.behindBodySprites + 9;
            }
        }

        public int FadeSprite
        {
            get
            {
                return this.behindBodySprites + 10;
            }
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[this.totalSprites];
            this.rags.InitiateSprites(sLeaser, rCam);
            this.chains.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites[this.LightSprite] = new FSprite("Futile_White", true);
            sLeaser.sprites[this.LightSprite].shader = rCam.game.rainWorld.Shaders["LightSource"];
            sLeaser.sprites[this.LightSprite].color = new Color(0.25882354f, 0.5137255f, 0.79607844f);
            sLeaser.sprites[this.LightSprite].isVisible = this.lightSpriteScale > 0f;
            sLeaser.sprites[this.DistortionSprite] = new FSprite("Futile_White", true);
            sLeaser.sprites[this.DistortionSprite].shader = rCam.game.rainWorld.Shaders["GhostDistortion"];
            sLeaser.sprites[this.BodyMeshSprite] = TriangleMesh.MakeLongMesh(this.spineBendPoint, false, true);
            sLeaser.sprites[this.HeadMeshSprite] = TriangleMesh.MakeLongMesh(this.spineSegments - this.spineBendPoint + this.snoutSegments, false, true, "ghostScales");
            sLeaser.sprites[this.HeadMeshSprite].shader = rCam.game.rainWorld.Shaders["GhostSkin"];
            sLeaser.sprites[this.NeckConnectorSprite] = new FSprite("Circle20", true);
            sLeaser.sprites[this.FadeSprite] = new FSprite("Futile_White", true);
            sLeaser.sprites[this.FadeSprite].scaleX = 87.5f;
            sLeaser.sprites[this.FadeSprite].scaleY = 50f;
            sLeaser.sprites[this.FadeSprite].x = rCam.game.rainWorld.screenSize.x / 2f;
            sLeaser.sprites[this.FadeSprite].y = rCam.game.rainWorld.screenSize.y / 2f;
            sLeaser.sprites[this.FadeSprite].isVisible = false;

            for (int i = 0; i < ncircles; i++)
            {
                sLeaser.sprites[RingSprite(i)] = new FSprite("FoodCircleA");
                sLeaser.sprites[CircleSprite(i)] = new FSprite("FoodCircleB");
            }

            for (int i = 0; i < this.legs.GetLength(0); i++)
            {
                sLeaser.sprites[this.ThightSprite(i)] = TriangleMesh.MakeLongMesh(this.thighSegments, false, true, "ghostBand");
                sLeaser.sprites[this.ThightSprite(i)].shader = rCam.game.rainWorld.Shaders["GhostSkin"];
                sLeaser.sprites[this.LowerLegSprite(i)] = TriangleMesh.MakeLongMesh(this.lowerLegSegments, false, true, "ghostPlates");
                sLeaser.sprites[this.LowerLegSprite(i)].shader = rCam.game.rainWorld.Shaders["GhostSkin"];
                sLeaser.sprites[this.ButtockSprite(i)] = new FSprite("Circle20", true);
            }
            this.AddToContainer(sLeaser, rCam, null);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("Items");
            }
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].RemoveFromContainer();
                if (i == this.DistortionSprite)
                {
                    rCam.ReturnFContainer("Bloom").AddChild(sLeaser.sprites[i]);
                }
                else if (i == this.LightSprite)
                {
                    rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[i]);
                }
                else if (i == this.FadeSprite)
                {
                    rCam.ReturnFContainer("Bloom").AddChild(sLeaser.sprites[i]);
                }
                else if (i <= CircleSprite(ncircles - 1))
                {
                    rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[i]);
                }
                else
                {
                    newContatiner.AddChild(sLeaser.sprites[i]);
                }
            }
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            float num = Mathf.Clamp((Mathf.Lerp(this.spine[this.spineBendPoint - 2].lastPos.x, this.spine[this.spineBendPoint - 2].pos.x, timeStacker) - Mathf.Lerp(this.spine[this.spineBendPoint + 2].lastPos.x, this.spine[this.spineBendPoint + 2].pos.x, timeStacker)) / (80f * this.scale), -1f, 1f);
            float num2 = 10f * this.scale;
            float num3 = 10f * this.scale;
            float num4 = Mathf.Lerp(this.lastFadeOut, this.fadeOut, timeStacker);
            sLeaser.sprites[this.FadeSprite].isVisible = num4 > 0f;
            if (num4 > 0f)
            {
                sLeaser.sprites[this.FadeSprite].alpha = Mathf.InverseLerp(0f, 0.7f, num4);
                float num5 = Custom.SCurve(Mathf.InverseLerp(0.5f, 1f, num4), 0.3f);
                sLeaser.sprites[this.FadeSprite].color = new Color(1f - num5, 1f - num5, 1f - num5);
            }
            this.rags.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            this.chains.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            Vector2 vector = Vector2.Lerp(this.spine[this.spine.Length - 1].lastPos, this.spine[this.spine.Length - 1].pos, timeStacker);
            Vector2 vector2 = Custom.DirVec(Vector2.Lerp(this.spine[this.spine.Length - 2].lastPos, this.spine[this.spine.Length - 2].pos, timeStacker), vector);
            vector += vector2 * 5f * this.scale;
            Vector2 vector3 = vector + vector2 * 190f * this.scale + Custom.PerpendicularVector(vector2) * 40f * this.scale * num;
            Vector2 vector4 = Vector2.Lerp(this.spine[0].lastPos, this.spine[0].pos, timeStacker);
            vector4 += Custom.DirVec(Vector2.Lerp(this.spine[1].lastPos, this.spine[1].pos, timeStacker), vector4);
            Vector2 vector5 = vector4;
            for (int i = 0; i < this.spineBendPoint; i++)
            {
                float num6 = (float)i / (float)(this.spineBendPoint - 1);
                Vector2 vector6 = Vector2.Lerp(this.spine[i].lastPos, this.spine[i].pos, timeStacker);
                float num7 = Mathf.Lerp(10f, Custom.LerpMap(num, -1f, 1f, 70f, 30f, 2f), Mathf.Sin(3.1415927f * Mathf.Pow(num6, 1.5f))) * this.scale;
                float num8 = Mathf.Lerp(10f, Custom.LerpMap(num, 1f, -1f, 70f, 30f, 2f), Mathf.Sin(3.1415927f * Mathf.Pow(num6, 1.5f))) * this.scale;
                Vector2 normalized = (vector4 - vector6).normalized;
                Vector2 vector7 = Custom.PerpendicularVector(normalized);
                float num9 = Vector2.Distance(vector4, vector6) / 5f;
                TriangleMesh? bodymesh = (sLeaser.sprites[this.BodyMeshSprite] as TriangleMesh);
                bodymesh.MoveVertice(i * 4, vector4 - normalized * num9 - vector7 * (num2 + num7) * 0.5f - camPos);
                bodymesh.MoveVertice(i * 4 + 1, vector4 - normalized * num9 + vector7 * (num3 + num8) * 0.5f - camPos);
                bodymesh.MoveVertice(i * 4 + 2, vector6 + normalized * num9 - vector7 * num7 - camPos);
                bodymesh.MoveVertice(i * 4 + 3, vector6 + normalized * num9 + vector7 * num8 - camPos);
                if (i == this.spineBendPoint - 2)
                {
                    vector5 = vector6;
                }
                vector4 = vector6;
                num2 = num7;
                num3 = num8;
            }
            Vector2 vector8 = Custom.DegToVec(180f - 90f * num);
            vector8.x = Mathf.Pow(Mathf.Abs(vector8.x), 8f) * Mathf.Sign(vector8.x);
            vector8 *= 40f * this.scale;
            vector8.y -= 7f * this.scale;
            Vector2 vector9 = (this.pos + new Vector2(0f, -170f) + vector + vector8 + Vector2.Lerp(this.spine[5].lastPos, this.spine[5].pos, timeStacker)) / 3f;
            FSprite distortionSprite = sLeaser.sprites[this.DistortionSprite];
            distortionSprite.x = vector9.x - camPos.x;
            distortionSprite.y = vector9.y - camPos.y;
            distortionSprite.scale = 933f * this.scale / 16f;
            FSprite lightSprite = sLeaser.sprites[this.LightSprite];
            lightSprite.x = vector9.x - camPos.x;
            lightSprite.y = vector9.y - camPos.y;
            lightSprite.scale = 500f * this.lightSpriteScale / 16f;

            for (int i = 0; i < ncircles; i++)
            {
                var cpos = vector9 + ringOffsets[i] - camPos;
                sLeaser.sprites[RingSprite(i)].SetPosition(cpos);
                sLeaser.sprites[CircleSprite(i)].SetPosition(cpos);
                sLeaser.sprites[CircleSprite(i)].isVisible = i < abstractGhost.currentCount;
            }

            vector4 = Vector2.Lerp(this.spine[this.spineBendPoint].lastPos, this.spine[this.spineBendPoint].pos, timeStacker);
            vector4 += Custom.DirVec(Vector2.Lerp(this.spine[this.spineBendPoint + 1].lastPos, this.spine[this.spineBendPoint + 1].pos, timeStacker), vector4);
            vector4 += vector8;
            for (int j = this.spineBendPoint; j < this.spineSegments + this.snoutSegments; j++)
            {
                float num10 = Mathf.InverseLerp((float)this.spineBendPoint, (float)(this.spineSegments + this.snoutSegments - 1), (float)j);
                Vector2 vector10;
                if (j < this.spineSegments)
                {
                    vector10 = Vector2.Lerp(this.spine[j].lastPos, this.spine[j].pos, timeStacker);
                }
                else
                {
                    vector10 = Custom.Bezier(vector, vector + vector2 * 60f * this.scale, vector3, vector + vector2 * 150f * this.scale, Mathf.InverseLerp((float)this.spineSegments, (float)(this.spineSegments + this.snoutSegments - 1), (float)j));
                }
                vector10 += vector8;
                if (j == this.spineBendPoint)
                {
                    FSprite necksprite = sLeaser.sprites[this.NeckConnectorSprite];
                    necksprite.x = (vector10.x + vector5.x) / 2f - camPos.x;
                    necksprite.y = (vector10.y + vector5.y) / 2f - camPos.y;
                    necksprite.rotation = Custom.AimFromOneVectorToAnother(vector5, vector10);
                    necksprite.scaleY = Vector2.Distance(vector5, vector10) * 1.6f / 20f;
                    necksprite.scaleX = this.scale * 1.6f;
                }
                float num11;
                float num12;
                if (num10 < 0.15f)
                {
                    num11 = 10f * this.scale;
                    num12 = 10f * this.scale;
                }
                else if (num10 < 0.4f)
                {
                    num11 = Mathf.Lerp(10f, 20f, Mathf.Sin(Custom.LerpMap(num10, 0.15f, 0.4f, 0f, 0.5f) * 3.1415927f)) * this.scale;
                    num12 = Mathf.Lerp(10f, 20f, Mathf.Sin(Custom.LerpMap(num10, 0.15f, 0.4f, 0f, 0.5f) * 3.1415927f)) * this.scale;
                }
                else
                {
                    num11 = this.SnoutContour(Mathf.InverseLerp(0.4f, 1f, num10), false, Mathf.Abs(num));
                    num12 = this.SnoutContour(Mathf.InverseLerp(0.4f, 1f, num10), false, Mathf.Abs(num));
                }
                Vector2 normalized2 = (vector4 - vector10).normalized;
                Vector2 vector11 = Custom.PerpendicularVector(normalized2);
                float num13 = Vector2.Distance(vector4, vector10) / 5f;
                int num14 = j - this.spineBendPoint;
                TriangleMesh? headmesh = (sLeaser.sprites[this.HeadMeshSprite] as TriangleMesh);
                headmesh.MoveVertice(num14 * 4, vector4 - normalized2 * num13 - vector11 * (num2 + num11) * 0.5f - camPos);
                headmesh.MoveVertice(num14 * 4 + 1, vector4 - normalized2 * num13 + vector11 * (num3 + num12) * 0.5f - camPos);
                headmesh.MoveVertice(num14 * 4 + 2, vector10 + normalized2 * num13 - vector11 * num11 - camPos);
                headmesh.MoveVertice(num14 * 4 + 3, vector10 + normalized2 * num13 + vector11 * num12 - camPos);
                vector4 = vector10;
                num2 = num11;
                num3 = num12;
            }
            float num15 = Custom.AimFromOneVectorToAnother(vector3, vector) / 360f;
            for (int k = 0; k < (sLeaser.sprites[this.HeadMeshSprite] as TriangleMesh).verticeColors.Length; k++)
            {
                float num16 = (float)k / (float)((sLeaser.sprites[this.HeadMeshSprite] as TriangleMesh).verticeColors.Length - 1);
                float num17;
                float num18;
                if (num16 < 0.15f)
                {
                    num17 = 10f * this.scale;
                    num18 = 10f * this.scale;
                }
                else if (num16 < 0.4f)
                {
                    num17 = Mathf.Lerp(10f, 20f, Mathf.Sin(Custom.LerpMap(num16, 0.15f, 0.4f, 0f, 0.5f) * 3.1415927f)) * this.scale;
                    num18 = Mathf.Lerp(10f, 20f, Mathf.Sin(Custom.LerpMap(num16, 0.15f, 0.4f, 0f, 0.5f) * 3.1415927f)) * this.scale;
                }
                else
                {
                    num17 = this.SnoutContour(Mathf.InverseLerp(0.4f, 1f, num16), false, Mathf.Abs(num));
                    num18 = this.SnoutContour(Mathf.InverseLerp(0.4f, 1f, num16), false, Mathf.Abs(num));
                }
                float num19 = (num17 + num18) / (2f * this.scale);
                (sLeaser.sprites[this.HeadMeshSprite] as TriangleMesh).verticeColors[k] = new Color(Mathf.InverseLerp(0.1f, 30f, num19), Mathf.InverseLerp(-1f, 1f, num), Mathf.InverseLerp(0.25f, 0.05f, num16), num15);
            }
            Vector2 vector12 = Vector2.Lerp(Vector2.Lerp(this.spine[0].lastPos, this.spine[0].pos, timeStacker), Vector2.Lerp(this.spine[1].lastPos, this.spine[1].pos, timeStacker), 0.5f);
            vector12 += Custom.DirVec(Vector2.Lerp(this.spine[2].lastPos, this.spine[2].pos, timeStacker), vector12) * 20f * this.scale;
            for (int l = 0; l < this.legs.GetLength(0); l++)
            {
                Vector2 vector13 = Vector2.Lerp(this.legs[l, 0].lastPos, this.legs[l, 0].pos, timeStacker);
                Vector2 vector14 = Vector2.Lerp(this.legs[l, 1].lastPos, this.legs[l, 1].pos, timeStacker);
                Vector2 vector15 = Vector2.Lerp(this.legs[l, 2].lastPos, this.legs[l, 2].pos, timeStacker);
                Vector2 vector16 = vector12 + Custom.DirVec(vector12, vector) * 5f * this.scale + Custom.DirVec(vector12, vector13) * 10f * this.scale;
                vector4 = vector16 + Custom.DirVec(vector13, vector16);
                FSprite buttocksprite = sLeaser.sprites[this.ButtockSprite(l)];
                buttocksprite.x = (vector12 + vector16).x / 2f - camPos.x;
                buttocksprite.y = (vector12 + vector16).y / 2f - camPos.y;
                buttocksprite.scaleX = this.scale;
                buttocksprite.rotation = Custom.AimFromOneVectorToAnother(vector16, Vector2.Lerp(spine[1].lastPos, spine[1].pos, timeStacker));
                buttocksprite.scaleY = Mathf.Max(this.scale / 2f, Vector2.Distance(vector16, Vector2.Lerp(spine[1].lastPos, spine[1].pos, timeStacker)) / 40f);
                TriangleMesh? tighmesh = (sLeaser.sprites[this.ThightSprite(l)] as TriangleMesh);
                for (int m = 0; m < this.thighSegments; m++)
                {
                    float num20 = Mathf.InverseLerp(0f, (float)(this.thighSegments - 1), (float)m);
                    Vector2 vector17 = Vector2.Lerp(vector16, vector13 + Custom.DirVec(vector16, vector13) * 10f * this.scale, num20);
                    float num21 = this.ThighContour(num20, l == 0);
                    float num22 = this.ThighContour(num20, l == 1);
                    Vector2 normalized3 = (vector4 - vector17).normalized;
                    Vector2 vector18 = Custom.PerpendicularVector(normalized3);
                    float num23 = Vector2.Distance(vector4, vector17) / 5f;
                    tighmesh.MoveVertice(m * 4, vector4 - normalized3 * num23 - vector18 * (num2 + num21) * 0.5f - camPos);
                    tighmesh.MoveVertice(m * 4 + 1, vector4 - normalized3 * num23 + vector18 * (num3 + num22) * 0.5f - camPos);
                    tighmesh.MoveVertice(m * 4 + 2, vector17 + normalized3 * num23 - vector18 * num21 - camPos);
                    tighmesh.MoveVertice(m * 4 + 3, vector17 + normalized3 * num23 + vector18 * num22 - camPos);
                    vector4 = vector17;
                    num2 = num21;
                    num3 = num22;
                }
                float num24 = Custom.AimFromOneVectorToAnother(vector16, vector13) / 360f;
                for (int n = 0; n < tighmesh.verticeColors.Length; n++)
                {
                    float num25 = (float)n / (float)(tighmesh.verticeColors.Length - 1);
                    tighmesh.verticeColors[n] = new Color(1f, Custom.LerpMap(num, -1f, 1f, 0.4f, 0.6f), ((double)num25 < 0.3 || num25 > 0.7f) ? 1f : 0f, num24);
                }
                vector4 = vector13 + Custom.DirVec(vector14, vector13);
                for (int num26 = 0; num26 < this.lowerLegSegments; num26++)
                {
                    float num27 = Mathf.InverseLerp(0f, (float)(this.lowerLegSegments - 1), (float)num26);
                    Vector2 vector19;
                    if (num27 < 0.8f)
                    {
                        vector19 = Vector2.Lerp(vector13, vector14, Mathf.InverseLerp(0f, 0.8f, num27));
                    }
                    else
                    {
                        vector19 = Vector2.Lerp(vector14, vector15, Mathf.InverseLerp(0.8f, 1f, num27));
                    }
                    float num28 = this.LowerLegContour(num27, l == 0, Mathf.Lerp(0.7f, num * ((l == 1) ? (-1f) : 1f), Mathf.Abs(num)));
                    float num29 = this.LowerLegContour(num27, l == 1, Mathf.Lerp(0.7f, num * ((l == 1) ? (-1f) : 1f), Mathf.Abs(num)));
                    Vector2 normalized4 = (vector4 - vector19).normalized;
                    Vector2 vector20 = Custom.PerpendicularVector(normalized4);
                    float num30 = Vector2.Distance(vector4, vector19) / 5f;
                    TriangleMesh? legmesh = (sLeaser.sprites[this.LowerLegSprite(l)] as TriangleMesh);
                    legmesh.MoveVertice(num26 * 4, vector4 - normalized4 * num30 - vector20 * (num2 + num28) * 0.5f - camPos);
                    legmesh.MoveVertice(num26 * 4 + 1, vector4 - normalized4 * num30 + vector20 * (num3 + num29) * 0.5f - camPos);
                    legmesh.MoveVertice(num26 * 4 + 2, vector19 + normalized4 * num30 - vector20 * num28 - camPos);
                    legmesh.MoveVertice(num26 * 4 + 3, vector19 + normalized4 * num30 + vector20 * num29 - camPos);
                    vector4 = vector19;
                    num2 = num28;
                    num3 = num29;
                }
                num24 = Custom.AimFromOneVectorToAnother(vector13, vector15) / 360f;
                for (int num31 = 0; num31 < (sLeaser.sprites[this.LowerLegSprite(l)] as TriangleMesh).verticeColors.Length; num31++)
                {
                    float num32 = (float)num31 / (float)((sLeaser.sprites[this.LowerLegSprite(l)] as TriangleMesh).verticeColors.Length - 1);
                    (sLeaser.sprites[this.LowerLegSprite(l)] as TriangleMesh).verticeColors[num31] = new Color(1f, Custom.LerpMap(num, -1f, 1f, 0.4f, 0.6f), Mathf.InverseLerp(0.25f, 0.05f, num32), num24);
                }
            }
            if (!sLeaser.deleteMeNextFrame && (base.slatedForDeletetion || this.room != rCam.room))
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }

        public float SnoutContour(float f, bool side, float sideView)
        {
            float num;
            if (f > 0.85f)
            {
                num = 0.2f + 0.8f * Mathf.Sin(Custom.LerpMap(f, 0.85f, 1f, 0.5f, 1f) * 3.1415927f);
            }
            else
            {
                num = Custom.LerpMap(f, 0f, 0.5f, Mathf.Lerp(Custom.LerpMap(f, 0f, 0.5f, 1.5f, 2f), 1f, sideView), 1f);
            }
            num *= Mathf.Lerp(1f, 0.3f, sideView * f);
            return num * 10f * this.scale;
        }

        public float ThighContour(float f, bool side)
        {
            float num;
            if (f < 0.3f)
            {
                num = 0.2f + 0.6f * Mathf.Sin(Custom.LerpMap(f, 0f, 0.3f, 0f, 0.5f) * 3.1415927f);
            }
            else if (side)
            {
                if (f < 0.85f)
                {
                    num = Custom.LerpMap(f, 0.3f, 0.85f, 0.8f, 1f, 0.5f);
                }
                else
                {
                    num = 0.2f + 0.8f * Custom.BackwardsSCurve(1f - Mathf.InverseLerp(0.85f, 1f, f), 0.3f);
                }
            }
            else if (f < 0.65f)
            {
                num = Custom.LerpMap(f, 0.3f, 0.65f, 0.8f, 1f, 0.5f);
            }
            else
            {
                num = Custom.LerpMap(f, 0.65f, 1f, 1f, 0.2f);
                num = Mathf.Max(num, 0.1f + 0.6f * Mathf.Sin(Custom.LerpMap(f, 0.85f, 1f, 0.5f, 1f) * 3.1415927f));
            }
            return num * 15f * this.scale;
        }

        public float LowerLegContour(float f, bool side, float flip)
        {
            float num = 0f;
            if (f < 0.1f)
            {
                num = 0.5f + 0.5f * Custom.BackwardsSCurve(Mathf.InverseLerp(0f, 0.1f, f), 0.3f);
            }
            else if (num < 0.8f)
            {
                num = Custom.LerpMap(f, 0.1f, 0.8f, 1f, 0.6f, 0.3f);
            }
            else
            {
                num = 0.6f;
            }
            if (side)
            {
                num = Mathf.Max(num, 0.5f + Mathf.Sin(Mathf.Pow(Mathf.InverseLerp(0f, 0.3f, f), 0.5f) * 3.1415927f));
            }
            else
            {
                num = Mathf.Max(num, Mathf.Sin(Mathf.Pow(Mathf.InverseLerp(0.2f, 0.5f, f), 0.6f) * 3.1415927f));
            }
            num += Mathf.Sin(Mathf.Pow(f, 0.5f) * 3.1415927f) * (side ? (-1f) : 1f) * flip;
            if (f > 0.85f)
            {
                if (side)
                {
                    num += Mathf.Sin(Mathf.InverseLerp(0.85f, 1f, f) * 3.1415927f) * 0.7f;
                }
                num *= 0.3f + 0.7f * Mathf.InverseLerp(1f, 0.94f, f);
            }
            return num * 10f * this.scale;
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            this.blackColor = palette.blackColor;
            sLeaser.sprites[this.NeckConnectorSprite].color = this.blackColor;
            sLeaser.sprites[this.ButtockSprite(0)].color = this.blackColor;
            sLeaser.sprites[this.ButtockSprite(1)].color = this.blackColor;
            TriangleMesh? triangleMesh1 = (sLeaser.sprites[this.BodyMeshSprite] as TriangleMesh);
            for (int i = 0; i < triangleMesh1.verticeColors.Length; i++)
            {
                triangleMesh1.verticeColors[i] = this.blackColor;
            }
            for (int j = 0; j < this.legs.GetLength(0); j++)
            {
                TriangleMesh? triangleMesh = (sLeaser.sprites[this.ThightSprite(j)] as TriangleMesh);
                for (int k = 0; k < triangleMesh.verticeColors.Length; k++)
                {
                    triangleMesh.verticeColors[k] = this.blackColor;
                }
            }
        }

        private float scale;

        private float lightSpriteScale;
        private int ncircles;
        private Vector2[] ringOffsets;

        public int totalStaticSprites = 11;

        public int totalSprites;

        public int behindBodySprites;

        public MeadowGhost.Part[] spine;

        public MeadowGhost.Part[,] legs;

        public int spineSegments = 11;

        public int snoutSegments = 20;

        public int spineBendPoint = 7;

        public int thighSegments = 7;

        public int lowerLegSegments = 17;

        public float flip;

        public float defaultFlip;

        public float flipFrom;

        public float flipTo;

        public float flipProg;

        public float flipSpeed;

        public float airResistance = 0.6f;

        public MeadowGhost.Rags rags;

        public MeadowGhost.Chains chains;

        public Color blackColor;

        public Color goldColor = new Color(0.5294118f, 0.3647059f, 0.18431373f);

        public float sinBob;

        public float fadeOut;

        public float lastFadeOut;
        private float ghostinessGoal;
        private float ghostiness;

        public class Part
        {
            public Part(float scale)
            {
                this.scale = scale;
            }

            public void Update()
            {
                this.lastPos = this.pos;
                this.pos += this.vel;
                this.vel += this.randomMovement * 1.4f * this.scale;
                this.randomMovement = Vector2.ClampMagnitude(this.randomMovement + Custom.RNV() * global::UnityEngine.Random.value * 0.1f, 1f);
            }

            public Vector2 pos;

            public Vector2 lastPos;

            public Vector2 vel;

            private Vector2 randomMovement;

            public float scale;
        }

        public class Rags
        {
            public Rags(MeadowGhost ghost, int firstSprite)
            {
                this.ghost = ghost;
                this.firstSprite = firstSprite;
                this.conRad = 30f * ghost.scale;
                int num = 6;
                this.segments = new Vector2[num][,];
                for (int i = 0; i < this.segments.Length; i++)
                {
                    this.segments[i] = new Vector2[global::UnityEngine.Random.Range(7, 27), 7];
                }
                this.totalSprites = this.segments.Length;
            }

            public void Reset(Vector2 resetPos)
            {
                for (int i = 0; i < this.segments.Length; i++)
                {
                    for (int j = 0; j < this.segments[i].GetLength(0); j++)
                    {
                        this.segments[i][j, 0] = resetPos + Custom.RNV();
                        this.segments[i][j, 1] = this.segments[i][j, 0];
                        this.segments[i][j, 2] *= 0f;
                    }
                }
            }

            public void Update()
            {
                for (int i = 0; i < this.segments.Length; i++)
                {
                    for (int j = 0; j < this.segments[i].GetLength(0); j++)
                    {
                        this.segments[i][j, 1] = this.segments[i][j, 0];
                        this.segments[i][j, 0] += this.segments[i][j, 2];
                        this.segments[i][j, 2] *= 0.999f;
                        this.segments[i][j, 2] += Custom.RNV() * 0.2f * this.ghost.scale;
                        this.segments[i][j, 5] = this.segments[i][j, 4];
                        this.segments[i][j, 4] = (this.segments[i][j, 4] + this.segments[i][j, 6] * 0.05f).normalized;
                        this.segments[i][j, 6] = (this.segments[i][j, 6] + Custom.RNV() * global::UnityEngine.Random.value * (this.segments[i][j, 2].magnitude / (this.ghost.scale * 3f))).normalized;
                    }
                    for (int k = 0; k < this.segments[i].GetLength(0); k++)
                    {
                        if (k > 0)
                        {
                            Vector2 vector = (this.segments[i][k, 0] - this.segments[i][k - 1, 0]).normalized;
                            float num = Vector2.Distance(this.segments[i][k, 0], this.segments[i][k - 1, 0]);
                            this.segments[i][k, 0] += vector * (this.conRad - num) * 0.5f;
                            this.segments[i][k, 2] += vector * (this.conRad - num) * 0.5f;
                            this.segments[i][k - 1, 0] -= vector * (this.conRad - num) * 0.5f;
                            this.segments[i][k - 1, 2] -= vector * (this.conRad - num) * 0.5f;
                            if (k > 1)
                            {
                                vector = (this.segments[i][k, 0] - this.segments[i][k - 2, 0]).normalized;
                                this.segments[i][k, 2] += vector * 0.2f;
                                this.segments[i][k - 2, 2] -= vector * 0.2f;
                            }
                            if (k < this.segments[i].GetLength(0) - 1)
                            {
                                this.segments[i][k, 4] = Vector3.Slerp(this.segments[i][k, 4], (this.segments[i][k - 1, 4] + this.segments[i][k + 1, 4]) / 2f, 0.05f);
                                this.segments[i][k, 6] = Vector3.Slerp(this.segments[i][k, 6], (this.segments[i][k - 1, 6] + this.segments[i][k + 1, 6]) / 2f, 0.05f);
                            }
                        }
                        else
                        {
                            this.segments[i][k, 0] = this.AttachPos(i, 1f);
                        }
                    }
                }
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                for (int i = 0; i < this.segments.Length; i++)
                {
                    sLeaser.sprites[this.firstSprite + i] = TriangleMesh.MakeLongMesh(this.segments[i].GetLength(0), false, true);
                    sLeaser.sprites[this.firstSprite + i].shader = rCam.room.game.rainWorld.Shaders["TentaclePlant"];
                    sLeaser.sprites[this.firstSprite + i].alpha = 0.3f + 0.7f * Mathf.InverseLerp(7f, 27f, (float)this.segments[i].GetLength(0));
                }
            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                for (int i = 0; i < this.segments.Length; i++)
                {
                    float num = 0f;
                    Vector2 vector = this.AttachPos(i, timeStacker);
                    float num2 = 0f;
                    TriangleMesh? triangleMesh = (sLeaser.sprites[this.firstSprite + i] as TriangleMesh);
                    for (int j = 0; j < this.segments[i].GetLength(0); j++)
                    {
                        Vector2 vector2 = Vector2.Lerp(this.segments[i][j, 1], this.segments[i][j, 0], timeStacker);
                        float num3 = 14f * this.ghost.scale * Vector3.Slerp(this.segments[i][j, 5], this.segments[i][j, 4], timeStacker).x;
                        Vector2 normalized = (vector - vector2).normalized;
                        Vector2 vector3 = Custom.PerpendicularVector(normalized);
                        float num4 = Vector2.Distance(vector, vector2) / 5f;
                        triangleMesh.MoveVertice(j * 4, vector - normalized * num4 - vector3 * (num3 + num) * 0.5f - camPos);
                        triangleMesh.MoveVertice(j * 4 + 1, vector - normalized * num4 + vector3 * (num3 + num) * 0.5f - camPos);
                        triangleMesh.MoveVertice(j * 4 + 2, vector2 + normalized * num4 - vector3 * num3 - camPos);
                        triangleMesh.MoveVertice(j * 4 + 3, vector2 + normalized * num4 + vector3 * num3 - camPos);
                        float num5 = 0.35f + 0.65f * Custom.BackwardsSCurve(Mathf.Pow(Mathf.Abs(Vector2.Dot(Vector3.Slerp(this.segments[i][j, 5], this.segments[i][j, 4], timeStacker), Custom.DegToVec(45f + Custom.VecToDeg(normalized)))), 2f), 0.5f);
                        triangleMesh.verticeColors[j * 4] = Color.Lerp(ghost.blackColor, ghost.goldColor, (num5 + num2) / 2f);
                        triangleMesh.verticeColors[j * 4 + 1] = Color.Lerp(ghost.blackColor, ghost.goldColor, (num5 + num2) / 2f);
                        triangleMesh.verticeColors[j * 4 + 2] = Color.Lerp(ghost.blackColor, ghost.goldColor, num5);
                        triangleMesh.verticeColors[j * 4 + 3] = Color.Lerp(ghost.blackColor, ghost.goldColor, num5);
                        vector = vector2;
                        num = num3;
                        num2 = num5;
                    }
                }
            }

            public Vector2 AttachPos(int rag, float timeStacker)
            {
                return Vector2.Lerp(this.ghost.spine[4].lastPos, this.ghost.spine[4].pos, timeStacker);
            }

            public MeadowGhost ghost;

            public int firstSprite;

            public int totalSprites;

            public Vector2[][,] segments;

            private float conRad;
        }

        public class Chains
        {
            public Chains(MeadowGhost ghost, int firstSprite)
            {
                this.ghost = ghost;
                this.firstSprite = firstSprite;
                int num = 2;
                this.segments = new Vector2[num][,];
                this.firstSpriteOfChains = new int[num];
                for (int i = 0; i < this.segments.Length; i++)
                {
                    this.segments[i] = new Vector2[27, 7];
                    this.firstSpriteOfChains[i] = this.totalSprites;
                    for (int j = 0; j < this.segments[i].GetLength(0); j++)
                    {
                        if (j % 3 < 2)
                        {
                            this.segments[i][j, 4] = new Vector2(19f, 0.2f);
                        }
                        else
                        {
                            this.segments[i][j, 4] = new Vector2(35f, 1f);
                        }
                        this.totalSprites += 2;
                    }
                }
            }

            public void Reset(Vector2 resetPos)
            {
                for (int i = 0; i < this.segments.Length; i++)
                {
                    for (int j = 0; j < this.segments[i].GetLength(0); j++)
                    {
                        this.segments[i][j, 0] = resetPos + Custom.RNV();
                        this.segments[i][j, 1] = this.segments[i][j, 0];
                        this.segments[i][j, 2] *= 0f;
                    }
                }
            }

            public void Update()
            {
                for (int i = 0; i < this.segments.Length; i++)
                {
                    for (int j = 0; j < this.segments[i].GetLength(0); j++)
                    {
                        this.segments[i][j, 5].y = this.segments[i][j, 5].x;
                        this.segments[i][j, 5].x += this.segments[i][j, 6].x;
                        this.segments[i][j, 6].x *= 0.99f;
                        if (global::UnityEngine.Random.value < 0.071428575f)
                        {
                            this.segments[i][j, 6].x += Mathf.Pow(global::UnityEngine.Random.value, 5f) * ((global::UnityEngine.Random.value < 0.5f) ? (-1f) : 1f) * this.segments[i][j, 2].magnitude / (15.5f * this.ghost.scale);
                        }
                        this.segments[i][j, 1] = this.segments[i][j, 0];
                        this.segments[i][j, 0] += this.segments[i][j, 2];
                        this.segments[i][j, 2] *= 0.999f;
                        this.segments[i][j, 2] += Custom.RNV() * 0.2f * this.ghost.scale;
                        this.segments[i][j, 2] = Vector2.Lerp(this.segments[i][j, 2], Custom.DirVec(this.segments[i][j, 0], this.ghost.spine[4].pos) * (this.segments[i][j, 2].magnitude + 3f * this.ghost.scale) * 0.5f, Custom.LerpMap(Vector2.Distance(this.segments[i][j, 0], this.ghost.spine[4].pos), 250f * this.ghost.scale, 600f * this.ghost.scale, 0f, 0.1f, 17f));
                    }
                    this.AttachChain(i);
                    for (int k = 1; k < this.segments[i].GetLength(0); k++)
                    {
                        Vector2 vector = (this.segments[i][k, 0] - this.segments[i][k - 1, 0]).normalized;
                        float num = Vector2.Distance(this.segments[i][k, 0], this.segments[i][k - 1, 0]);
                        float num2 = this.segments[i][k - 1, 4].y / (this.segments[i][k, 4].y + this.segments[i][k - 1, 4].y);
                        this.segments[i][k, 0] += vector * (this.segments[i][k, 4].x - num) * num2;
                        this.segments[i][k, 2] += vector * (this.segments[i][k, 4].x - num) * num2;
                        this.segments[i][k - 1, 0] -= vector * (this.segments[i][k, 4].x - num) * (1f - num2);
                        this.segments[i][k - 1, 2] -= vector * (this.segments[i][k, 4].x - num) * (1f - num2);
                    }
                    this.AttachChain(i);
                    for (int l = this.segments[i].GetLength(0) - 2; l >= 0; l--)
                    {
                        Vector2 vector = (this.segments[i][l, 0] - this.segments[i][l + 1, 0]).normalized;
                        float num = Vector2.Distance(this.segments[i][l, 0], this.segments[i][l + 1, 0]);
                        float num3 = this.segments[i][l + 1, 4].y / (this.segments[i][l, 4].y + this.segments[i][l + 1, 4].y);
                        this.segments[i][l, 0] += vector * (this.segments[i][l + 1, 4].x - num) * num3;
                        this.segments[i][l, 2] += vector * (this.segments[i][l + 1, 4].x - num) * num3;
                        this.segments[i][l + 1, 0] -= vector * (this.segments[i][l + 1, 4].x - num) * (1f - num3);
                        this.segments[i][l + 1, 2] -= vector * (this.segments[i][l + 1, 4].x - num) * (1f - num3);
                    }
                    this.AttachChain(i);
                }
            }

            private void AttachChain(int r)
            {
                Vector2 normalized = (this.segments[r][0, 0] - this.AttachPos(r, 1f)).normalized;
                float num = Vector2.Distance(this.segments[r][0, 0], this.AttachPos(r, 1f));
                this.segments[r][0, 0] += normalized * (this.segments[r][0, 4].x - num);
                this.segments[r][0, 2] += normalized * (this.segments[r][0, 4].x - num);
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                for (int i = 0; i < this.segments.Length; i++)
                {
                    for (int j = 0; j < this.segments[i].GetLength(0); j++)
                    {
                        if (this.segments[i][j, 4].y == 0.2f)
                        {
                            sLeaser.sprites[this.firstSprite + this.firstSpriteOfChains[i] + j * 2] = new FSprite("haloGlyph-1", true);
                            sLeaser.sprites[this.firstSprite + this.firstSpriteOfChains[i] + j * 2 + 1] = new FSprite("pixel", true);
                        }
                        else
                        {
                            sLeaser.sprites[this.firstSprite + this.firstSpriteOfChains[i] + j * 2] = new FSprite("ghostLink", true);
                            sLeaser.sprites[this.firstSprite + this.firstSpriteOfChains[i] + j * 2].anchorY = -0.6666667f;
                            sLeaser.sprites[this.firstSprite + this.firstSpriteOfChains[i] + j * 2 + 1] = new FSprite("ghostLink", true);
                            sLeaser.sprites[this.firstSprite + this.firstSpriteOfChains[i] + j * 2 + 1].anchorY = -0.6666667f;
                        }
                    }
                }
            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                for (int i = 0; i < this.segments.Length; i++)
                {
                    Vector2 vector = this.AttachPos(i, timeStacker);
                    for (int j = 0; j < this.segments[i].GetLength(0); j++)
                    {
                        Vector2 vector2 = Vector2.Lerp(this.segments[i][j, 1], this.segments[i][j, 0], timeStacker);
                        if (this.segments[i][j, 4].y == 0.2f)
                        {
                            sLeaser.sprites[this.firstSprite + this.firstSpriteOfChains[i] + j * 2].x = (vector2.x + vector.x) / 2f - camPos.x;
                            sLeaser.sprites[this.firstSprite + this.firstSpriteOfChains[i] + j * 2].y = (vector2.y + vector.y) / 2f - camPos.y;
                            sLeaser.sprites[this.firstSprite + this.firstSpriteOfChains[i] + j * 2 + 1].x = (vector2.x + vector.x) / 2f - camPos.x - 1f;
                            sLeaser.sprites[this.firstSprite + this.firstSpriteOfChains[i] + j * 2 + 1].y = (vector2.y + vector.y) / 2f - camPos.y + 1f;
                            sLeaser.sprites[this.firstSprite + this.firstSpriteOfChains[i] + j * 2].color = Color.Lerp(this.ghost.blackColor, this.ghost.goldColor, 0.65f);
                            sLeaser.sprites[this.firstSprite + this.firstSpriteOfChains[i] + j * 2 + 1].color = this.ghost.goldColor;
                        }
                        else
                        {
                            Vector2 vector3 = Custom.PerpendicularVector(vector, vector2);
                            float num = Mathf.Sin(Mathf.Lerp(this.segments[i][j, 5].y, this.segments[i][j, 5].x, timeStacker)) * 360f / 3.1415927f;
                            for (int k = 0; k < 2; k++)
                            {
                                sLeaser.sprites[this.firstSprite + this.firstSpriteOfChains[i] + j * 2 + k].x = vector2.x + vector3.x * (float)(-1 + k * 2) * this.ghost.scale * 0.9f - camPos.x;
                                sLeaser.sprites[this.firstSprite + this.firstSpriteOfChains[i] + j * 2 + k].y = vector2.y + vector3.y * (float)(-1 + k * 2) * this.ghost.scale * 0.9f - camPos.y;
                                sLeaser.sprites[this.firstSprite + this.firstSpriteOfChains[i] + j * 2 + k].rotation = Custom.AimFromOneVectorToAnother(vector2, vector);
                                sLeaser.sprites[this.firstSprite + this.firstSpriteOfChains[i] + j * 2 + k].scaleX = Mathf.Max(0.1f, Mathf.Abs(Custom.DegToVec(num).x));
                            }
                            float num2 = Mathf.Abs(Vector2.Dot(Custom.DegToVec(num), Custom.DirVec(vector, vector2)));
                            num2 = Custom.BackwardsSCurve(num2, 0.3f);
                            sLeaser.sprites[this.firstSprite + this.firstSpriteOfChains[i] + j * 2].color = Color.Lerp(this.ghost.blackColor, this.ghost.goldColor, 0.65f + 0.1f * Mathf.Sin(num2 * 3.1415927f * 2f));
                            sLeaser.sprites[this.firstSprite + this.firstSpriteOfChains[i] + j * 2 + 1].color = Color.Lerp(this.ghost.blackColor, this.ghost.goldColor, 0.1f + 0.9f * num2);
                        }
                        vector = vector2;
                    }
                }
            }

            public Vector2 AttachPos(int chain, float timeStacker)
            {
                return Vector2.Lerp(this.ghost.legs[chain, 2].lastPos, this.ghost.legs[chain, 2].pos, timeStacker);
            }

            public MeadowGhost ghost;

            public int firstSprite;

            public int totalSprites;

            public Vector2[][,] segments;

            public int[] firstSpriteOfChains;
        }
    }
}
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using static RainMeadow.RainMeadow;

namespace RainMeadow
{

    class SlugcatCape : CosmeticManager.IMeadowCosmetic
    {
        public PlayerGraphics playerGFX { get; private set; }

        public int totalSprites => 1;

        public CosmeticManager.ICosmeticSkin cloakColor;
        private SimpleSegment[,] segments;
        private const int size = 5;
        private const float targetLength = 50f;
        public int firstSpriteIndex;

        public SlugcatCape(GraphicsModule gfx, int firstSpriteIndex, CosmeticManager.ICosmeticSkin cloakColor)
        {
            if (gfx is not PlayerGraphics pgfx) throw new InvalidOperationException("only slugcats have capes");
            playerGFX = pgfx;
            this.segments = new SimpleSegment[size + 1, size + 1];
            this.firstSpriteIndex = firstSpriteIndex;
            this.cloakColor = cloakColor;
        }

        public void Reset()
        {
            for (int i = 0; i <= size; i++)
            {
                for (int j = 0; j <= size; j++)
                {
                    this.segments[j, i].Reset(playerGFX.owner.bodyChunks[0].pos);
                }
            }
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites[this.firstSpriteIndex] = TriangleMesh.MakeGridMesh("Futile_White", SlugcatCape.size);
            sLeaser.sprites[this.firstSpriteIndex].shader = rCam.game.rainWorld.Shaders["TemplarCloak"];
            if (cloakColor is CosmeticManager.CosmicCapeColor ccc)
            {
                ccc.shaderApplied = false;
            }
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Color customColor = Color.red;
            if (playerGFX.owner.abstractPhysicalObject.GetOnlineObject() is OnlinePhysicalObject ent)
            {
                if (ent.TryGetData<SlugcatCustomization>(out var s))
                {
                    customColor = s.customCosmeticColor;
                }
            }


            TriangleMesh triangleMesh = (sLeaser.sprites[this.firstSpriteIndex] as TriangleMesh)!;
            int num = 0;
            for (int i = 0; i <= SlugcatCape.size; i++)
            {
                for (int j = 0; j <= SlugcatCape.size; j++)
                {
                    triangleMesh.MoveVertice(num, this.segments[j, i].DrawPos(timeStacker) - camPos);
                    cloakColor.ApplyColor(triangleMesh, num, rCam, customColor);
                    ++num;
                }
            }
        }


        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            sLeaser.sprites[this.firstSpriteIndex].RemoveFromContainer();
            var background = rCam.ReturnFContainer("Background");
            if (background == newContatiner)
            {
                // looks better 75% of the time
                background = rCam.ReturnFContainer("BackgroundShortcuts");
            }
            background.AddChild(sLeaser.sprites[this.firstSpriteIndex]);

        }

        // Token: 0x060022B4 RID: 8884 RVA: 0x002B2484 File Offset: 0x002B0684
        private void ConnectEnd()
        {
            if ((ModManager.Watcher && playerGFX.player.isCamo))
            {
                return;
            }

            BodyChunk mainBodyChunk = playerGFX.player.mainBodyChunk;
            BodyChunk bodyChunk = playerGFX.player.bodyChunks[1];
            Vector2 normalized = GetBodyNormalized();
            Vector2 a = Custom.PerpendicularVector(normalized);
            for (int i = 0; i <= SlugcatCape.size; i++)
            {
                float d = (float)i / (float)SlugcatCape.size * 2f - 1f;
                ref SimpleSegment ptr = ref this.segments[i, 0];
                ptr.pos = mainBodyChunk.pos + (a * d * 3f) + Vector2.right * -playerGFX.player.flipDirection * 0.5f;
                ptr.vel = mainBodyChunk.vel;

                ref SimpleSegment ptr2 = ref this.segments[i, 1];
                ptr2.pos = mainBodyChunk.pos + normalized * 3f + a * d * 5f + Vector2.right * -playerGFX.player.flipDirection * 1.0f;
                ptr2.vel = mainBodyChunk.vel;
            }
        }

        // Token: 0x060022B5 RID: 8885 RVA: 0x002B25C8 File Offset: 0x002B07C8
        private void ConnectSegments(int x, int y, int otherX, int otherY, float targetDist, float massRatio)
        {
            ref SimpleSegment ptr = ref this.segments[x, y];
            ref SimpleSegment ptr2 = ref this.segments[otherX, otherY];
            Vector2 a = ptr2.pos - ptr.pos;
            float magnitude = a.magnitude;
            if (magnitude > targetDist)
            {
                Vector2 a2 = a / magnitude;
                float num = targetDist - magnitude;
                ptr.pos -= 0.45f * num * a2 * massRatio;
                ptr.vel -= 0.35f * num * a2 * massRatio;
                ptr2.pos += 0.45f * num * a2 * (1f - massRatio);
                ptr2.vel += 0.35f * num * a2 * (1f - massRatio);
            }
        }

        public Vector2 GetBodyNormalized()
        {
            BodyChunk mainBodyChunk = playerGFX.player.mainBodyChunk;
            Vector2 normalized = (playerGFX.player.bodyChunks[1].pos - mainBodyChunk.pos).normalized;
            if (normalized.x < 0.05f && (playerGFX.player.input[0].x == 0))
            {
                normalized.x = (float)playerGFX.player.flipDirection * 0.05f;

                // simplification of sin(acos(x)) 
                normalized.y = Mathf.Sqrt(1 - (normalized.x * normalized.x)) * Math.Sign(normalized.y);
            }

            return normalized;
        }
        public void Update()
        {
            float num = SlugcatCape.targetLength / (float)SlugcatCape.size;
            BodyChunk mainBodyChunk = playerGFX.player.mainBodyChunk;
            Vector2 normalized = GetBodyNormalized();
            Room room = playerGFX.player.room;
            for (int i = 0; i <= SlugcatCape.size; i++)
            {
                float targetDist = SlugcatCape.targetLength / (float)SlugcatCape.size * 0.5f;
                for (int j = 0; j <= SlugcatCape.size; j++)
                {
                    this.segments[j, i].lastPos = this.segments[j, i].pos;
                    this.segments[j, i].vel = Vector2.ClampMagnitude(this.segments[j, i].vel * 0.95f, 10f);
                    if (i > 0)
                    {
                        this.ConnectSegments(j, i, j, i - 1, num, 0.7f);
                    }
                    if (j > 0)
                    {
                        this.ConnectSegments(j, i, j - 1, i, targetDist, 0.5f);
                    }
                }
            }

            for (int k = 2; k <= SlugcatCape.size; k++)
            {
                float num2 = (float)k / (float)SlugcatCape.size;
                for (int l = 0; l <= SlugcatCape.size; l++)
                {
                    float num3 = (float)l / (float)SlugcatCape.size * 2f - 1f;
                    ref SimpleSegment ptr = ref this.segments[l, k];
                    ptr.vel.y = ptr.vel.y - 0.4f * playerGFX.player.EffectiveRoomGravity;
                    float num4 = 1f - 2f * num2;

                    if (room.waterObject is not null)
                    {
                        if (room.PointSubmerged(ptr.pos))
                        {
                            ptr.vel.x = ptr.vel.x * (1f - 0.75f * room.waterObject.viscosity);
                            if (ptr.vel.y > 0f)
                            {
                                ptr.vel.y = ptr.vel.y * (1f - 0.075f * room.waterObject.viscosity);
                            }
                            else
                            {
                                ptr.vel.y = ptr.vel.y * (1f - 0.15f * room.waterObject.viscosity);
                            }

                            ptr.vel.y += 0.45f + (0.2f * room.waterObject.viscosity);
                        }
                    }
                    if (num4 > 0f)
                    {
                        ptr.vel += Custom.PerpendicularVector(normalized) * num4 * num3 * 2.0f * (1f - 0.7f * Mathf.Abs(normalized.x));
                    }
                }
            }
            for (int m = 0; m <= SlugcatCape.size; m++)
            {
                float t = (float)m / (float)SlugcatCape.size;
                for (int n = 0; n <= SlugcatCape.size; n++)
                {
                    ref SimpleSegment ptr5 = ref this.segments[n, m];
                    ptr5.pos += ptr5.vel;
                    if (m > 2 && room.GetTile(ptr5.lastPos).Solid && Custom.DistLess(ptr5.lastPos, ptr5.pos, num * 4f))
                    {
                        float rad = Mathf.Lerp(3f, 1f, t);
                        SharedPhysics.TerrainCollisionData terrainCollisionData = new SharedPhysics.TerrainCollisionData(ptr5.pos, ptr5.lastPos, ptr5.vel, rad, default(IntVector2), true);
                        terrainCollisionData = SharedPhysics.HorizontalCollision(room, terrainCollisionData);
                        terrainCollisionData = SharedPhysics.VerticalCollision(room, terrainCollisionData);
                        ptr5.pos = terrainCollisionData.pos;
                        ptr5.vel = terrainCollisionData.vel;
                    }
                }
            }

            this.ConnectEnd();
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            throw new NotImplementedException();
        }
    }
}
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
    class MeadowScarf : CosmeticManager.IMeadowCosmetic
    {
        public override string ToString() => "scarf";
        public readonly int firstSprite;
        public int totalSprites => 2;
        public readonly CosmeticManager.ICosmeticSkin color;
        public readonly GraphicsModule baseModule;
        public MeadowScarf(GraphicsModule baseModule, int firstSprite, CosmeticManager.ICosmeticSkin color)
        {
            
            this.firstSprite = firstSprite;
            this.baseModule = baseModule;
            this.color = color;
            segments = new SimpleSegment[size, size];

            // todo cosmetic options??
            dangle_length = 90f;
            dangle_width = 7f; 

            if (baseModule is ScavengerGraphics)
            {
                neck_length = 3f;
                neck_width = 8f;
            }
            else
            {
                neck_length = 3f;
                neck_width = 16f;
            }
            
        }


        float dangle_length;
        float dangle_width;


        float neck_length;
        float neck_width;
        
        
        const int size = 6;
        private SimpleSegment[,] segments;

        public Vector2 HeadDir(float timeStacker) {
            switch (baseModule)
            {
                case PlayerGraphics playerGraphics:
                {
                    Vector2 dir = Vector2.Lerp( playerGraphics.player.bodyChunks[0].lastPos - playerGraphics.player.bodyChunks[1].lastPos,
                                                    playerGraphics.player.bodyChunks[0].pos - playerGraphics.player.bodyChunks[1].pos, 
                                                    timeStacker);
                    return dir.normalized;
                }

                case ScavengerGraphics scavGrphs:
                    return scavGrphs.HeadDir(timeStacker);
            }

            return Vector2.up;
        }

        public Vector2 NeckPos(float timeStacker) {
            switch (baseModule)
            {
                case PlayerGraphics playerGraphics:
                {
                    Vector2 headpos = Vector2.Lerp(playerGraphics.player.bodyChunks[0].lastPos, playerGraphics.player.bodyChunks[0].pos, timeStacker);
                    Vector2 bodypos = Vector2.Lerp(playerGraphics.player.bodyChunks[1].lastPos, playerGraphics.player.bodyChunks[1].pos, timeStacker);
                    return Vector2.LerpUnclamped(headpos, bodypos, 0.65f);
                }

                case ScavengerGraphics scavGrphs:
                {
                    Vector2 headpos = Vector2.Lerp(scavGrphs.scavenger.bodyChunks[2].lastPos, scavGrphs.scavenger.bodyChunks[2].pos, timeStacker);
                    Vector2 bodypos = Vector2.Lerp(scavGrphs.scavenger.bodyChunks[0].lastPos, scavGrphs.scavenger.bodyChunks[0].pos, timeStacker);
                    return Vector2.Lerp(headpos, bodypos, 0.4f);
                }
            }

            return baseModule.owner.bodyChunks[0].pos;
        }


        public float Flip(float timeStacker) {
            return baseModule switch
            {
                PlayerGraphics playerGraphics => Mathf.Lerp(playerGraphics.player.flipDirection, playerGraphics.player.lastFlipDirection, timeStacker),
                ScavengerGraphics scavGrphs => Mathf.Lerp(scavGrphs.flip, scavGrphs.lastFlip, timeStacker),
                _ => 0
            };
        }




        public void Update()
        {
            float segmentlength = dangle_length / size;
            float segmentwidth = dangle_width / size;

            Room room = baseModule.owner.room;
            if (room is null) return;

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    ref SimpleSegment segment = ref segments[i, j];
                    segment.lastPos = segment.pos;
                    segment.vel = Vector2.ClampMagnitude(segment.vel * 0.95f, 10f);
                    segment.vel.y -= 0.4f * room.gravity;

                    if (room.waterObject is not null)
                    {
                        if (room.PointSubmerged(segment.pos))
                        {
                            segment.vel.x = segment.vel.x * (1f - 0.75f * room.waterObject.viscosity);
                            if (segment.vel.y > 0f)
                            {
                                segment.vel.y = segment.vel.y * (1f - 0.075f * room.waterObject.viscosity);
                            }
                            else
                            {
                                segment.vel.y = segment.vel.y * (1f - 0.15f * room.waterObject.viscosity);
                            }

                            segment.vel.y += 0.45f + (0.2f * room.waterObject.viscosity);
                        }
                    }
                    
                    if (i > 0)
                    {
                        this.ConnectSegments(ref segment, ref segments[i - 1, j], segmentlength, 0.5f);
                    }
                    if (j > 0)
                    {
                        this.ConnectSegments(ref segment, ref segments[i, j - 1], segmentwidth, 0.9f);
                    }
                }
            }


            for (int i = 0; i < size; i++)
            {
                float segment_pos = ((float)(i / (size - 1)) - 0.5f);
                segment_pos *= dangle_width;


                ref SimpleSegment segment = ref segments[i, 0];
                Vector2 neck = NeckPos(1f);
                segment.pos.x = neck.x;
                segment.pos.y = neck.y + segment_pos;
            }
            
            
            for (int i = 0; i < size; i++)
            {
                for (int j = 1; j < size; j++)
                {
                    ref SimpleSegment segment = ref segments[i, j];
                    ref SimpleSegment back_segment = ref segments[i, j - 1];
                    Vector2 dir = (segment.pos - back_segment.pos).normalized;
                    if (dir.x < 0.05f)
                    {
                        dir.x = Mathf.Sign(Flip(0f)) * 0.05f;

                        // simplification of sin(acos(x)) 
                        dir.y = Mathf.Sqrt(1 - (dir.x * dir.x)) * Math.Sign(dir.y);
                    }
                    
                    segment.pos += segment.vel;
                    if (room.GetTile(segment.lastPos).Solid && Custom.DistLess(segment.lastPos, segment.pos, segmentlength * 4f))
                    {
                        float rad = Mathf.Lerp(3f, 1f, (float)j / ((float)size - 1f));
                        SharedPhysics.TerrainCollisionData terrainCollisionData = new SharedPhysics.TerrainCollisionData(segment.pos, segment.lastPos, segment.vel, rad, default(IntVector2), true);
                        terrainCollisionData = SharedPhysics.HorizontalCollision(room, terrainCollisionData);
                        terrainCollisionData = SharedPhysics.VerticalCollision(room, terrainCollisionData);
                        segment.pos = terrainCollisionData.pos;
                        segment.vel = terrainCollisionData.vel;
                    }
                }
            }
        }

        public void Reset()
        {
            for (int i = 0; i < size; i++)
			{
				for (int j = 0; j < size; j++)
				{
					this.segments[j, i].Reset(NeckPos(0f));
				}
			}
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites[firstSprite] = TriangleMesh.MakeGridMesh("Futile_White", size - 1); // scarf dangle
            sLeaser.sprites[firstSprite + 1] = TriangleMesh.MakeGridMesh("Futile_White", 1); // neck
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Color customColor = Color.red;
            if (baseModule.owner.abstractPhysicalObject.GetOnlineObject() is OnlinePhysicalObject ent)
            {
                if (ent.TryGetData<SlugcatCustomization>(out var s))
                {
                    customColor = s.customCosmeticColor;
                }
            }


            TriangleMesh dangle = (TriangleMesh)sLeaser.sprites[firstSprite];
            TriangleMesh neck = (TriangleMesh)sLeaser.sprites[firstSprite + 1];

            int dangle_index = 0;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    
                    dangle.MoveVertice(dangle_index, segments[i, j].DrawPos(timeStacker) - camPos);
                    color.ApplyColor(dangle, dangle_index, rCam, customColor);
                    ++dangle_index;
                }
            }

            int neck_index = 0;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    float segment_height = ((float)i - 0.5f) * neck_length;
                    float segment_width = ((float)j - 0.5f) * neck_width;
                    Vector2 posrelativetohead = new Vector2(segment_width, segment_height);
                    posrelativetohead = Custom.rotateVectorDeg(posrelativetohead, Custom.VecToDeg(HeadDir(timeStacker)));
                    Vector2 neckpos = NeckPos(timeStacker);
                    neck.MoveVertice(neck_index, posrelativetohead + neckpos - camPos);
                    color.ApplyColor(neck, neck_index, rCam, customColor);
                    ++neck_index;
                }
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {

        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer container)
        {
            if (container == null) container = rCam.ReturnFContainer("Midground");
            int lastSprite = firstSprite + totalSprites;
            for (int i = firstSprite; i < lastSprite; i++)
            {
                container.AddChild(sLeaser.sprites[i]);
            }
            
        }

		private void ConnectSegments(ref SimpleSegment A, ref SimpleSegment B, float targetDist, float massRatio)
		{
			Vector2 difference = B.pos - A.pos;
			float magnitude = difference.magnitude;
			if (magnitude > targetDist)
			{
				Vector2 a2 = difference / magnitude;
				float error = targetDist - magnitude;
				A.pos -= 0.45f * error * a2 * massRatio;
				A.vel -= 0.15f * error * a2 * massRatio;
				B.pos += 0.45f * error * a2 * (1f - massRatio);
				B.vel += 0.15f * error * a2 * (1f - massRatio);
			}
		}
    }
}
using RWCustom;
using System;
using UnityEngine;
using static RainMeadow.CosmeticManager;

namespace RainMeadow
{
    class MeadowScarf : CosmeticManager.IMeadowCosmetic
    {
        public override string ToString() => "scarf";
        public readonly int firstSprite;
        public int totalSprites => 3;
        public int DangleSprite => firstSprite;
        public int DangleSpriteSub => firstSprite + 2;
        public int NeckSprite => firstSprite + 1;
        public readonly CosmeticManager.ICosmeticSkin color;
        public readonly GraphicsModule baseModule;
        public MeadowScarf(GraphicsModule baseModule, int firstSprite, CosmeticManager.ICosmeticSkin color)
        {
            
            this.firstSprite = firstSprite;
            this.baseModule = baseModule;
            this.color = color;
            dangleBigSegments = new SimpleSegment[sizeDangle, sizeDangle];
            dangleSmallSegments = new SimpleSegment[sizeSmall, sizeSmall];

            // todo cosmetic options??
            dangle_length = 45f;
            dangle_width = 16f; 
            small_dangle_length = 25f;
            small_dangle_width = 10f; 

            if (baseModule is ScavengerGraphics)
            {
                neck_length = 4f;
                neck_width = 8f;
            }
            else
            {
                neck_length = 5f;
                neck_width = 16f;
            }
        }


        float small_dangle_length;
        float small_dangle_width;
        float dangle_length;
        float dangle_width;


        float neck_length;
        float neck_width;
        float neck_lenght_mult = 1;
        
        const float tip_fade = 0.85f;
        
        const int sizeDangle = 7;
        const int sizeSmall = 5;
        const int sizeNeck = 5;
        private SimpleSegment[,] dangleBigSegments;
        private SimpleSegment[,] dangleSmallSegments;

        public Vector2 HeadDir(float timeStacker) {
            switch (baseModule)
            {
                case PlayerGraphics playerGraphics:
                {
                    Vector2 head = Vector2.Lerp(
                        playerGraphics.drawPositions[0, 1], 
                        playerGraphics.drawPositions[0, 0], 
                        timeStacker
                    );
                    Vector2 toward = Vector2.Lerp(
                        playerGraphics.drawPositions[1, 1], 
                        playerGraphics.drawPositions[1, 0], 
                        timeStacker
                    );
                    return (head - toward).normalized;
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
                    Vector2 head = Vector2.Lerp(
                        playerGraphics.drawPositions[0, 1], 
                        playerGraphics.drawPositions[0, 0], 
                        timeStacker
                    );
                    Vector2 toward = Vector2.Lerp(
                        playerGraphics.drawPositions[1, 1], 
                        playerGraphics.drawPositions[1, 0], 
                        timeStacker
                    );
                    Vector2 body = new(
                        (toward.x * 2f + toward.x) / 3f,
                        (toward.y * 2f + toward.y) / 3f - (playerGraphics.player?.sleepCurlUp ?? 0) * 3f
                    );
                    return Vector2.LerpUnclamped(head, body, 
                        playerGraphics.player?.animation == Player.AnimationIndex.HangUnderVerticalBeam
                            ? 0.45f // exception here since the player's face hangs particularly low
                            : 0.25f);
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

        private void UpdateSegmentVelocity(Room room, ref SimpleSegment segment, float gravity = 0.4f)
        {
            segment.lastPos = segment.pos;
            segment.vel = Vector2.ClampMagnitude(segment.vel * 0.95f, 10f);
            segment.vel.y -= gravity * room.gravity;

            if (room.waterObject is not null)
            {
                if (room.PointSubmerged(segment.pos))
                {
                    segment.vel.x *= 1f - 0.75f * room.waterObject.viscosity;
                    if (segment.vel.y > 0f)
                    {
                        segment.vel.y *= 1f - 0.075f * room.waterObject.viscosity;
                    }
                    else
                    {
                        segment.vel.y *= 1f - 0.15f * room.waterObject.viscosity;
                    }

                    segment.vel.y += 0.45f + (0.2f * room.waterObject.viscosity);
                }
            }
        }
        private void UpdateSegmentVelocity(Room room, ref SimpleSegment[,] segments, float segmentlength, float segmentEndwidth,
            float gravity = 0.4f, float correction = 0.65f, float elasticity = 0.05f, 
            float spacing = 0.85f, float spacingCorrection = 0.35f, float spacingElasticity = 0.75f)
        {
            for (int i = 0; i < segments.GetLength(0); i++)
            {
                for (int j = 0; j < segments.GetLength(1); j++)
                {
                    ref SimpleSegment segment = ref segments[i, j];
                    UpdateSegmentVelocity(room, ref segment, gravity);

                    float dangle_knot_ratio = j / (segments.GetLength(1) - 1f);
                    float width = Mathf.Lerp(neck_length / segments.GetLength(0), segmentEndwidth, Mathf.Pow(dangle_knot_ratio * 1.25f, 2));
                    if (i > 0)
                    {
                        CosmeticManager.ConnectSegments(ref segment, ref segments[i - 1, j], 
                            width, 0.5f, correction, elasticity, 
                            spacing, spacingCorrection * Mathf.Sqrt(dangle_knot_ratio), spacingElasticity * Mathf.Sqrt(dangle_knot_ratio));
                        
                        // Force it to unfold
                        if (i > 1)
                        {
                            CosmeticManager.ConnectSegments(ref segment, ref segments[i - 2, j], 
                                width * 2, 0.5f, correction, elasticity, 
                                spacing, spacingCorrection * dangle_knot_ratio, spacingElasticity * dangle_knot_ratio);
                        }
                    }
                    if (j > 0)
                    {
                        CosmeticManager.ConnectSegments(ref segment, ref segments[i, j - 1], 
                            segmentlength, 0.9f, correction, elasticity);
                    }
                }
            }
        }
        private void ConnectToNeck(ref SimpleSegment[,] segments, float offset = 0f)
        {
            for (int i = 0; i < segments.GetLength(0); i++)
            {
                float segment_pos = ((float)i / (segments.GetLength(0) - 1)) - 0.5f;
                segment_pos *= neck_length;
                segment_pos += neck_width * offset;

                ref SimpleSegment segment = ref segments[i, 0];
                Vector2 posrelativetohead = new(segment_pos, neck_length * 0.5f);
                posrelativetohead = Custom.rotateVectorDeg(posrelativetohead, Custom.VecToDeg(HeadDir(1f)));
                Vector2 neck = NeckPos(1f);
                segment.pos = neck + posrelativetohead;
                segment.pos.y += 1;
                segment.vel = Vector2.zero;
            }
        }
        private void UpdateSegmentPosition(ref SimpleSegment segment)
        {
            segment.pos += segment.vel;
        }
        private void UpdateSegmentPosition(Room room, ref SimpleSegment[,] segments, float segmentlength)
        {
            for (int i = 0; i < segments.GetLength(0); i++)
            {
                for (int j = 1; j < segments.GetLength(1); j++)
                {
                    ref SimpleSegment segment = ref segments[i, j];
                    ref SimpleSegment back_segment = ref segments[i, j - 1];

                    UpdateSegmentPosition(ref segment);
                    
                    if (room.GetTile(segment.lastPos).Solid && Custom.DistLess(segment.lastPos, segment.pos, segmentlength * 4f))
                    {
                        float rad = Mathf.Lerp(3f, 1f, j / (segments.GetLength(1) - 1f));
                        SharedPhysics.TerrainCollisionData terrainCollisionData = new(segment.pos, segment.lastPos, segment.vel, rad, default, true);
                        terrainCollisionData = SharedPhysics.HorizontalCollision(room, terrainCollisionData);
                        terrainCollisionData = SharedPhysics.VerticalCollision(room, terrainCollisionData);
                        segment.pos = terrainCollisionData.pos;
                        segment.vel = terrainCollisionData.vel;
                    }
                }
            }
        }

        public void Update()
        {
            float bigDanglelength = dangle_length / sizeDangle;
            float bigDanglewidth = dangle_width / sizeDangle;
            float smallDanglelength = small_dangle_length / sizeSmall;
            float smallDanglewidth = small_dangle_width / sizeSmall;

            Room room = baseModule.owner.room;
            if (room is null) return;

            UpdateSegmentVelocity(room, ref dangleBigSegments, bigDanglelength, bigDanglewidth);
            UpdateSegmentVelocity(room, ref dangleSmallSegments, smallDanglelength, smallDanglewidth, gravity: 0.25f, elasticity:0.20f, spacingElasticity: 1.35f);

            ConnectToNeck(ref dangleBigSegments, 0.15f);
            ConnectToNeck(ref dangleSmallSegments, 0.35f);
            
            UpdateSegmentPosition(room, ref dangleBigSegments, bigDanglelength);
            UpdateSegmentPosition(room, ref dangleSmallSegments, smallDanglelength);
        }

        private void ResetDangle(ref SimpleSegment[,] dangle)
        {
            for (int i = 0; i < dangle.GetLength(0); i++)
			{
				for (int j = 0; j < dangle.GetLength(1); j++)
				{
					dangle[i, j].Reset(NeckPos(0f));
				}
			}
        }
        public void Reset()
        {
            ResetDangle(ref dangleBigSegments);
            ResetDangle(ref dangleSmallSegments);
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites[DangleSprite] = TriangleMesh.MakeGridMesh("Futile_White", sizeDangle - 1); // scarf dangle
            sLeaser.sprites[DangleSpriteSub] = TriangleMesh.MakeGridMesh("Futile_White", sizeSmall - 1); // small scarf dangle
            sLeaser.sprites[NeckSprite] = TriangleMesh.MakeGridMesh("Futile_White", sizeNeck - 1); // neck
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

            TriangleMesh dangle = (TriangleMesh)sLeaser.sprites[DangleSprite];
            TriangleMesh dangleSmall = (TriangleMesh)sLeaser.sprites[DangleSpriteSub];
            TriangleMesh neck = (TriangleMesh)sLeaser.sprites[NeckSprite];

            int dangle_index = 0;
            for (int i = 0; i < sizeDangle; i++)
            {
                for (int j = 0; j < sizeDangle; j++)
                {
                    dangle.MoveVertice(dangle_index, dangleBigSegments[i, j].DrawPos(timeStacker) - camPos);

                    float fade = 0f;
                    if (j > sizeDangle / 2) fade = Mathf.InverseLerp(sizeDangle / 2, sizeDangle - 1f, j) * tip_fade;

                    color.ApplyColor(dangle, dangle_index, rCam, customColor, fade);
                    ++dangle_index;
                }
            }

            int small_dangle_index = 0;
            for (int i = 0; i < sizeSmall; i++)
            {
                for (int j = 0; j < sizeSmall; j++)
                {
                    dangleSmall.MoveVertice(small_dangle_index, dangleSmallSegments[i, j].DrawPos(timeStacker) - camPos);

                    float fade = 0f;
                    if (j > 1) fade = Mathf.InverseLerp(1, sizeSmall - 1f, j) * tip_fade;

                    color.ApplyColor(dangleSmall, small_dangle_index, rCam, customColor, fade);
                    ++small_dangle_index;
                }
            }

            int neck_index = 0;
            for (int i = 0; i < sizeNeck; i++)
            {
                for (int j = 0; j < sizeNeck; j++)
                {
                    float segment_width = ((i / (sizeNeck - 1f)) - 0.5f) * neck_width * neck_lenght_mult;
                    float segment_height = ((j / (sizeNeck - 1f)) - 0.5f)  * neck_length;
                    
                    Vector2 neckpos = NeckPos(timeStacker);
                    Vector2 posrelativetohead = new(segment_width, segment_height);
                    posrelativetohead = Custom.rotateVectorDeg(posrelativetohead, Custom.VecToDeg(HeadDir(timeStacker)));

                    Vector2 additionalOffset = Vector2.up * Mathf.Abs(i - sizeNeck/2); // round shape
                    if (i == 0)
                        additionalOffset.x -= sizeNeck/2 - Mathf.Abs(j - sizeNeck/2);
                    else if (i == sizeNeck - 1)
                        additionalOffset.x += sizeNeck/2 - Mathf.Abs(j - sizeNeck/2);

                    neck.MoveVertice(neck_index, posrelativetohead + neckpos + additionalOffset - camPos);
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

        public void ApplyWind(Vector2 wind)
        {
            int clock = baseModule?.owner?.room.game.clock ?? 0;
            ApplySurfaceWind(ref dangleBigSegments, wind * 4f, 0.2f, clock);
            ApplySurfaceWind(ref dangleSmallSegments, wind * 8f, 0.45f, clock);
        }
    }
}
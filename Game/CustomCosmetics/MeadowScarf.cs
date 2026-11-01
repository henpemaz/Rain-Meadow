using RWCustom;
using System;
using UnityEngine;

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
            dangle_width = 25f; 
            small_dangle_length = 10f;
            small_dangle_width = 15f; 

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


        float small_dangle_length;
        float small_dangle_width;
        float dangle_length;
        float dangle_width;


        float neck_length;
        float neck_width;
        float neck_lenght_mult = 1;
        const float odd_dangle_ratio = 0.20f;
        const float neck_dangle_ratio = 0.20f;
        
        const float dangle_fade = 0.05f;
        const float small_dangle_fade = 0.10f;
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
        private void UpdateSegmentVelocity(Room room, ref SimpleSegment[,] segments, float segmentlength, float segmentwidth,
            float gravity = 0.4f, float correction = 0.65f, float elasticity = 0.05f, 
            float spacing = 0.85f, float spacingCorrection = 0.35f, float spacingElasticity = 0.75f)
        {
            for (int i = 0; i < segments.GetLength(0); i++)
            {
                for (int j = 0; j < segments.GetLength(1); j++)
                {
                    ref SimpleSegment segment = ref segments[i, j];
                    UpdateSegmentVelocity(room, ref segment, gravity);

                    float dangle_knot_ratio = Mathf.Lerp(neck_dangle_ratio, 1f, j / (segments.GetLength(1) - 1f));
                    if (i > 0)
                    {
                        ConnectSegments(ref segment, ref segments[i - 1, j], 
                            segmentwidth * dangle_knot_ratio, 
                            0.5f, correction, elasticity, 
                            spacing * dangle_knot_ratio, spacingCorrection * dangle_knot_ratio, spacingElasticity * dangle_knot_ratio);
                        
                        if (i > 1)
                        {
                            // force it to spread around instead on folding on itself
                            ConnectSegments(ref segment, ref segments[i - 2, j], 
                                segmentwidth * dangle_knot_ratio * 2, 
                                0.5f, correction, elasticity, 
                                spacing * dangle_knot_ratio, spacingCorrection * dangle_knot_ratio, spacingElasticity * dangle_knot_ratio);
                        }
                    }
                    if (j > 0)
                    {
                        ConnectSegments(ref segment, ref segments[i, j - 1], 
                            segmentlength * (j == segments.GetLength(1) - 1 && i%2 == 0 ? odd_dangle_ratio : 1),
                            0.9f, correction, elasticity);
                    }
                }
            }
        }
        private void ConnectToNeck(ref SimpleSegment[,] segments, float segmentwidth, float offset = 0f)
        {
            for (int i = 0; i < segments.GetLength(0); i++)
            {
                float segment_pos = ((float)i / (segments.GetLength(0) - 1)) - 0.5f;
                segment_pos *= segmentwidth * neck_dangle_ratio;
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
        private void UpdateSegmentPosition(ref SimpleSegment segment, ref SimpleSegment back_segment)
        {
            
            Vector2 dir = (segment.pos - back_segment.pos).normalized;
            if (dir.x < 0.05f)
            {
                dir.x = Mathf.Sign(Flip(0f)) * 0.05f;

                // simplification of sin(acos(x)) 
                dir.y = Mathf.Sqrt(1 - (dir.x * dir.x)) * Math.Sign(dir.y);
            }
            
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

                    UpdateSegmentPosition(ref segment, ref back_segment);
                    
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
            UpdateSegmentVelocity(room, ref dangleSmallSegments, smallDanglelength, smallDanglewidth, gravity: 0.25f, elasticity:0.15f, spacingElasticity: 1.25f);

            ConnectToNeck(ref dangleBigSegments, dangle_width, 0.25f);
            ConnectToNeck(ref dangleSmallSegments, small_dangle_width, 0.35f);
            
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
            sLeaser.sprites[NeckSprite] = TriangleMesh.MakeOneToOneLongMesh(sizeNeck, true); // neck
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

            HSLColor customColorHSL = customColor.ToHSL();
            Color fadeColor;
            if (customColorHSL.saturation < 0.4f)
                fadeColor = new HSLColor(customColorHSL.hue, 1f, customColorHSL.lightness).rgb; // fade to saturated color if desaturated
            else if (customColorHSL.lightness < 0.4f)
                fadeColor = Color.white; // fade to white if dark color
            else
                fadeColor = new(0.1f, 0.1f, 0.1f); // fade to black if bright color

            if (baseModule is PlayerGraphics)
                neck_lenght_mult = sLeaser.sprites[0].scaleX;
            else if (baseModule is ScavengerGraphics scavengerGraphics)
                neck_lenght_mult = sLeaser.sprites[scavengerGraphics.ChestSprite].scaleX;

            TriangleMesh dangle = (TriangleMesh)sLeaser.sprites[DangleSprite];
            TriangleMesh dangleSmall = (TriangleMesh)sLeaser.sprites[DangleSpriteSub];
            TriangleMesh neck = (TriangleMesh)sLeaser.sprites[NeckSprite];

            int dangle_index = 0;
            for (int i = 0; i < sizeDangle; i++)
            {
                for (int j = 0; j < sizeDangle; j++)
                {
                    dangle.MoveVertice(dangle_index, dangleBigSegments[i, j].DrawPos(timeStacker) - camPos);
                    color.ApplyColor(dangle, dangle_index, rCam, Color.Lerp(customColor, fadeColor, dangle_fade));
                    if (j > sizeDangle / 2)
                    {
                        float tipRatio = Mathf.InverseLerp(sizeDangle / 2, sizeDangle - 1f, j);
                        dangle.verticeColors[dangle_index] = Color.Lerp(dangle.verticeColors[dangle_index], fadeColor, Mathf.Pow(tipRatio, 3) * tip_fade);
                    }
                    ++dangle_index;
                }
            }

            int small_dangle_index = 0;
            for (int i = 0; i < sizeSmall; i++)
            {
                for (int j = 0; j < sizeSmall; j++)
                {
                    dangleSmall.MoveVertice(small_dangle_index, dangleSmallSegments[i, j].DrawPos(timeStacker) - camPos);
                    color.ApplyColor(dangleSmall, small_dangle_index, rCam, Color.Lerp(customColor, fadeColor, small_dangle_fade));
                    if (j > 0)
                    {
                        float tipRatio = Mathf.InverseLerp(0, sizeDangle - 1f, j);
                        dangleSmall.verticeColors[small_dangle_index] = Color.Lerp(dangle.verticeColors[small_dangle_index], fadeColor, tipRatio * tip_fade);
                    }
                    ++small_dangle_index;
                }
            }

            int neck_index = 0;
            for (int i = 0; i < sizeNeck; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    float segment_width = ((i / (sizeNeck - 1f)) - 0.5f) * neck_width * neck_lenght_mult;
                    float segment_height = (j - 0.5f) * neck_length;
                    Vector2 posrelativetohead = new(segment_width, segment_height);
                    posrelativetohead = Custom.rotateVectorDeg(posrelativetohead, Custom.VecToDeg(HeadDir(timeStacker)));
                    Vector2 neckpos = NeckPos(timeStacker);
                    neck.MoveVertice(neck_index, posrelativetohead + neckpos - camPos + Vector2.up * Mathf.Abs(i + 1 - sizeNeck/2));
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
            // putting some randomness to the wind
            int clock = baseModule?.owner?.room.game.clock ?? 0;
            // if (clock % 5 != 0) return;
            for (int i = 1; i < sizeDangle; i++)
            {
                for (int j = 0; j < sizeDangle; j++)
                {
                    int randomInt = clock + i + j;
                    // if (randomInt % 8 == 0) continue;
                    wind.x *= 0.75f + Mathf.Sin(randomInt) * 0.25f;
                    wind.y *= 0.75f + Mathf.Cos(randomInt) * 0.25f;
                    CosmeticManager.ApplyWindEffect(ref dangleBigSegments[i, j].vel, 8f * wind / (sizeDangle - j), 0.2f);
                }
            }

            for (int i = 1; i < sizeSmall; i++)
            {
                for (int j = 0; j < sizeSmall; j++)
                {
                    int randomInt = clock + i + j;
                    // if (randomInt % 4 == 0) continue;
                    wind.x *= 0.75f + Mathf.Cos(randomInt) * 0.25f;
                    wind.y *= 0.75f + Mathf.Sin(randomInt) * 0.25f;
                    CosmeticManager.ApplyWindEffect(ref dangleSmallSegments[i, j].vel, 4f * wind / (sizeSmall - j), 0.5f);
                }
            }
        }

		private void ConnectSegments(ref SimpleSegment A, ref SimpleSegment B, float targetDist, float massRatio, float correction = 0.45f, float elasticity = 0.15f, float spacing = 0f, float spacingCorrection = 0f, float spacingElasticity = 0f)
		{
			Vector2 difference = B.pos - A.pos;
			float magnitude = difference.magnitude;
			if (magnitude > targetDist)
			{
                Vector2 normalizedDiff = difference / magnitude;
                float error = targetDist - magnitude;
				A.pos -= correction * error * normalizedDiff * massRatio;
				A.vel -= elasticity * error * normalizedDiff * massRatio;
				B.pos += correction * error * normalizedDiff * (1f - massRatio);
				B.vel += elasticity * error * normalizedDiff * (1f - massRatio);
			}
            else if (magnitude > 0.1f && magnitude < targetDist * spacing) // magnitude need a minimum so it doesn't get send to Mars or smt
            {
                Vector2 normalizedDiff = difference / magnitude;
                float error = (targetDist * spacing) - magnitude;
				A.pos -= spacingCorrection * error * normalizedDiff * massRatio;
				A.vel -= spacingElasticity * error * normalizedDiff * massRatio;
				B.pos += spacingCorrection * error * normalizedDiff * (1f - massRatio);
				B.vel += spacingElasticity * error * normalizedDiff * (1f - massRatio);
            }
		}
    }
}
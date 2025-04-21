using System.Runtime.CompilerServices;
using RWCustom;
using UnityEngine;
using System.Net;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow {
    static class CapeManager {
        const string capes_txt = "https://raw.githubusercontent.com/invalidunits/MeadowCosmetics/refs/heads/master/capes.txt";
        static public void FetchCapes() {
            try {
                RainMeadow.DebugMe();
                using (WebClient client = new WebClient ()) 
                {
                    string capes = client.DownloadString(capes_txt);
                    entries.Clear();
                    
                    foreach(string line in capes.Split('\n')) {
                        RainMeadow.Debug(line);
                         // Skip the header or empty lines
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("steamid64"))
                            continue;

                        // Split the line into parts
                        string[] parts = line.Split(new[] { ',' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        RainMeadow.Debug(parts);
                        if (parts.Length < 2) continue;

                        string steamId64 = parts[0].Trim();
                        string colorPart = parts[1].Split(';')[0].Trim(); // Extract only the color part
                        string color = colorPart.Trim('(', ')'); // Remove parentheses around the color

                        // Check if the color is a list of floats (RGB)
                        Color rgbColor = Color.red;
                        if (color.Contains(","))
                        {
                            string[] rgbParts = color.Split(',');
                            if (rgbParts.Length == 3 &&
                                float.TryParse(rgbParts[0].Trim(), out float r) &&
                                float.TryParse(rgbParts[1].Trim(), out float g) &&
                                float.TryParse(rgbParts[2].Trim(), out float b))
                            {
                                rgbColor = new Color(r, g, b);
                            }
                        } else {
                            if (color == "sgold") {
                                rgbColor = RainWorld.SaturatedGold;
                            }
                        }

                        // Add the parsed entry to the list
                        entries.Add(new CapeEntry(ulong.Parse(steamId64), rgbColor));
                    }

                }
            } catch (Exception except) {
                RainMeadow.Error(except);
            }

        }


        public class CapeEntry {
            public ulong steamID;
            public Color color; 

            public CapeEntry(ulong steamID, Color color) {
                RainMeadow.Debug($"Cape Entry {steamID}, {color}");
                this.steamID = steamID;
                this.color = color;

            }
        }
        private static List<CapeEntry> entries = new();

        static public Color? HasCape(MeadowPlayerId player) {
            if (player is SteamMatchmakingManager.SteamPlayerId steamid) {
                CapeEntry? entry = entries.Where(x => steamid.oid.GetSteamID64() == x.steamID).FirstOrDefault();
                if (entry is not null) {
                    return entry.color;
                }
            } 
            return null;
        } 
        
    }

    class SlugcatCape {
        public static ConditionalWeakTable<PlayerGraphics, SlugcatCape> cloaked_slugcats = new();  
        public PlayerGraphics playerGFX {  get; private set; }
        public Color cloakColor;
        private SimpleSegment[,] segments;           
        private const int size = 5;
        private const float targetLength = 50f;
        public const int totalSprites = 1;
        private readonly int firstSpriteIndex;

        public SlugcatCape(PlayerGraphics gfx, int firstSpriteIndex, Color cloakColor) {
            cloaked_slugcats.Add(gfx, this);
            this.segments = new SimpleSegment[size + 1, size + 1];
            this.firstSpriteIndex = firstSpriteIndex; 
            this.cloakColor = cloakColor;
            this.playerGFX = gfx;
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
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            TriangleMesh triangleMesh = (sLeaser.sprites[this.firstSpriteIndex] as TriangleMesh)!;
            int num = 0;
            for (int i = 0; i <= SlugcatCape.size; i++)
            {
                for (int j = 0; j <= SlugcatCape.size; j++)
                {
                    triangleMesh.MoveVertice(num++, this.segments[j, i].DrawPos(timeStacker) - camPos);
                }
            }
            num = 0;
            for (int k = 0; k <= SlugcatCape.size; k++)
            {
                for (int l = 0; l <= SlugcatCape.size; l++)
                {
                    Color color = this.cloakColor;
                    Vector2 p = triangleMesh.vertices[l + Mathf.Max(k - 1, 0) * (SlugcatCape.size + 1)];
                    Vector2 p2 = triangleMesh.vertices[l + Mathf.Min(k + 1, SlugcatCape.size) * (SlugcatCape.size + 1)];
                    Vector2 vector = Custom.DirVec(p, p2) * 5f;
                    color.a = 1.0f;
                    triangleMesh.verticeColors[num++] = this.cloakColor;
                }
            }
        }


    public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) {
        sLeaser.sprites[this.firstSpriteIndex].RemoveFromContainer();
        rCam.ReturnFContainer("Background").AddChild(sLeaser.sprites[this.firstSpriteIndex]);
        
    }

        // Token: 0x060022B4 RID: 8884 RVA: 0x002B2484 File Offset: 0x002B0684
        private void ConnectEnd()
        {
            BodyChunk mainBodyChunk = playerGFX.player.mainBodyChunk;
            BodyChunk bodyChunk = playerGFX.player.bodyChunks[1];
            Vector2 normalized = (bodyChunk.pos - mainBodyChunk.pos).normalized;
            Vector2 a = Custom.PerpendicularVector(normalized);
            for (int i = 0; i <= SlugcatCape.size; i++)
            {
                float d = (float)i / (float)SlugcatCape.size * 2f - 1f;
                ref SimpleSegment ptr = ref this.segments[i, 0];
                ptr.pos = bodyChunk.pos + a * d * 3f;
                ptr.vel = bodyChunk.vel;
            }
            for (int j = 0; j <= SlugcatCape.size; j++)
            {
                float d2 = (float)j / (float)SlugcatCape.size * 2f - 1f;
                ref SimpleSegment ptr2 = ref this.segments[j, 1];
                ptr2.pos = mainBodyChunk.pos + normalized * 3f + a * d2 * 5f;
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

        public void Update()
        {
            float num = SlugcatCape.targetLength / (float)SlugcatCape.size;
            BodyChunk mainBodyChunk = playerGFX.player.mainBodyChunk;
            Vector2 normalized = (playerGFX.player.bodyChunks[1].pos - mainBodyChunk.pos).normalized;
            if (normalized.x < 0.05f) {
                normalized.x = (float)playerGFX.player.flipDirection*0.05f;
            }

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
            this.ConnectEnd();
            for (int k = 2; k <= SlugcatCape.size; k++)
            {
                float num2 = (float)k / (float)SlugcatCape.size;
                for (int l = 0; l <= SlugcatCape.size; l++)
                {
                    float num3 = (float)l / (float)SlugcatCape.size * 2f - 1f;
                    ref SimpleSegment ptr = ref this.segments[l, k];
                    ptr.vel.y = ptr.vel.y - 0.4f;
                    float num4 = 1f - 2f * num2;
                    if (num4 > 0f)
                    {
                        ptr.vel += -normalized * num4;
                        ptr.vel.x = ptr.vel.x + num4 * num3 * 3f * (1f - 0.5f * Mathf.Abs(normalized.x));
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
        }
    }
}

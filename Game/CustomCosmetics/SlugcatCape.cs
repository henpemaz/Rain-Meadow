using System.Runtime.CompilerServices;
using RWCustom;
using UnityEngine;
using System.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;

namespace RainMeadow
{
    static class CapeManager
    {
        const string capes_latest_commit = "https://github.com/invalidunits/MeadowCosmetics16.git/info/refs?service=git-upload-pack";
        static string getRemoteLatestCommit() 
        {
            using (WebClient client = new WebClient())
            {
                string response = client.DownloadString(capes_latest_commit);

                // Split by lines
                var lines = response.Split('\n');

                foreach (var line in lines)
                {
                    if (line.Contains("refs/heads/master"))
                    {
                        RainMeadow.Debug(line);
                        // Line format: <length><sha1> refs/heads/master
                        // Strip off the first 4 chars (pkt-line length)
                        string trimmed = line.Length > 4 ? line.Substring(4) : line;
                        string[] parts = trimmed.Split(' ');
                        if (parts.Length > 0)
                        {
                            RainMeadow.Debug($"recieved the hash latest commit: {parts[0]}");
                            return parts[0];
                        }
                    }
                }

                throw new Exception("We couldn't find the remote hash");
            }
        }
        

        const string capes_remote_txt = "https://raw.githubusercontent.com/invalidunits/MeadowCosmetics16/refs/heads/master/capes.txt";
        static public void FetchCapes()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    var cape_hash_file = Path.Combine(ModManager.GetModById("henpemaz_rainmeadow").path, "capes_hash.txt");
                    var capes_txt = Path.Combine(ModManager.GetModById("henpemaz_rainmeadow").path, "capes.txt");

                    // Download remote commit hash.
                    string commithash = getRemoteLatestCommit();

                    // Read local commit hash from file.
                    string commithashlocal = string.Empty;
                    if (File.Exists(cape_hash_file))
                    {
                        using (FileStream stream = File.OpenRead(cape_hash_file))
                        using (StreamReader reader = new(stream))
                        {
                            commithashlocal = reader.ReadLine();
                        }
                    }


                    // Only download the new capes when we hashes don't match.
                    if (commithash != commithashlocal)
                    {
                        RainMeadow.Debug("Local hash doesn't match, downloading remote.");
                        using (FileStream stream = File.Create(cape_hash_file))
                        using (StreamWriter writer = new(stream))
                        {
                            writer.WriteLine(commithash);
                        }
                        string response = client.DownloadString(capes_remote_txt);
                        using (FileStream stream = File.Create(capes_txt))
                        using (StreamWriter writer = new(stream))
                        {
                            writer.WriteLine(response);
                        }
                    }

                    entries.Clear();
                    // process capes from file.
                    using (FileStream capefile = File.OpenRead(capes_txt))
                    using (StreamReader capestream = new StreamReader(capefile))
                    {
                        while (true)
                        {
                            var line = capestream.ReadLine();
                            if (line == null) break;

                            // Skip the header or empty lines
                            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("steamid64"))
                                continue;

                            // Split the line into parts
                            string[] parts = line.Split(new[] { ',' }, 2, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length < 2) continue;

                            string HashedsteamId64 = parts[0].Trim();
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
                            }
                            else
                            {
                                if (color == "sgold")
                                {
                                    rgbColor = RainWorld.SaturatedGold;
                                }
                            }


                            // Add the parsed entry to the list
                            if (!entries.ContainsKey(HashedsteamId64)) entries.Add(HashedsteamId64, rgbColor);
                        }
                    }
                }
            }
            catch (Exception except)
            {
                RainMeadow.Error(except);
            }

        }


        private static Dictionary<string, Color> entries = new();
        private static ConditionalWeakTable<MeadowPlayerId, object> cape_cache = new();

        static public Color? HasCape(MeadowPlayerId player)
        {
            if (cape_cache.TryGetValue(player, out var entry) && entry is not null) return (Color)entry;
            if (player is SteamMatchmakingManager.SteamPlayerId steamid)
            {
                ulong steamID = steamid.oid.GetSteamID64();
                SHA256 Sha = SHA256.Create();
                var hashed_cape_str = System.Convert.ToBase64String(Sha.ComputeHash(Encoding.ASCII.GetBytes(steamID.ToString())));

                if (entries.TryGetValue(hashed_cape_str, out Color found_entry))
                {
                    cape_cache.Add(player, found_entry);
                    return found_entry;
                }
            }

            if (player is LANMatchmakingManager.LANPlayerId lanPlayer)
            {
                if (lanPlayer.name == "goldcape") return RainWorld.SaturatedGold;
                if (lanPlayer.name == "redcape") return Color.red;
                if (lanPlayer.name == "bluecape") return Color.blue;
            }


            return null;
        }

    }

    class SlugcatCape
    {
        public static ConditionalWeakTable<PlayerGraphics, SlugcatCape> cloaked_slugcats = new();
        public PlayerGraphics playerGFX { get; private set; }
        public Color cloakColor;
        private SimpleSegment[,] segments;
        private const int size = 5;
        private const float targetLength = 50f;
        public const int totalSprites = 1;
        public int firstSpriteIndex;

        public SlugcatCape(PlayerGraphics gfx, int firstSpriteIndex, Color cloakColor)
        {
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
            if (ModManager.Watcher && playerGFX.player.isCamo)
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
                ptr2.pos = mainBodyChunk.pos + normalized * 3f + a * d * 5f + Vector2.right*-playerGFX.player.flipDirection*1.0f;
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
                normalized.y = Mathf.Sqrt(1 - (normalized.x*normalized.x))*Math.Sign(normalized.y); 
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
                    ptr.vel.y = ptr.vel.y - 0.4f * room.gravity;
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
    }
}

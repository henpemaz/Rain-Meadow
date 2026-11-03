using RWCustom;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow
{
    public static class CosmeticManager
    {
        public static void ApplyWindEffect(ref Vector2 vel, Vector2 wind, float force)
        {
            float magnitude = wind.magnitude;
            if (magnitude < 0.01) return; // wind too slow
            if (Vector2.Dot(vel, wind) * (1f / magnitude) > magnitude) return; // vel projeted is greater than wind

            vel = Vector2.Lerp(vel, wind, force);
        }
        public static void ApplySurfaceWind(ref SimpleSegment[,] segments, Vector2 wind, float force, int clock = 0)
        {
            for (int i = 1; i < segments.GetLength(0) - 1; i++)
            {
                for (int j = 1; j < segments.GetLength(1) - 1; j++)
                {
                    int randomInt = clock + i + j;
                    wind.x *= 0.75f + Mathf.Sin(randomInt) * 0.25f;
                    wind.y *= 0.75f + Mathf.Cos(randomInt) * 0.25f;
                    
                    Vector2 windXAxis = wind.ProjectOn(segments[i - 1, j].pos - segments[i + 1, j].pos);
                    Vector2 windYAxis = wind.ProjectOn(segments[i, j - 1].pos - segments[i, j + 1].pos);
                    ApplyWindEffect(ref segments[i, j].vel, windXAxis, force);
                    ApplyWindEffect(ref segments[i, j].vel, windYAxis, force);
                }
            }
        }
        public static void ConnectSegments(ref SimpleSegment A, ref SimpleSegment B, 
            float targetDist, float massRatio, 
            float correction = 0.45f, float elasticity = 0.15f, 
            float spacing = 0f, float spacingCorrection = 0f, float spacingElasticity = 0f)
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
            else if (magnitude < targetDist * spacing)
            {
                Vector2 normalizedDiff = difference.normalized; // puts 0 if the vector is too tiny
                float error = (targetDist * spacing) - magnitude;
				A.pos -= spacingCorrection * error * normalizedDiff * massRatio;
				A.vel -= spacingElasticity * error * normalizedDiff * massRatio;
				B.pos += spacingCorrection * error * normalizedDiff * (1f - massRatio);
				B.vel += spacingElasticity * error * normalizedDiff * (1f - massRatio);
            }
		}

        public interface IMeadowCosmetic : IDrawable
        {
            int totalSprites { get; }
            void Update();
            void Reset();
            void ApplyWind(Vector2 wind);
        }

        public interface ICosmeticSkin
        {
            bool UsesCustomColor { get; }
            public void ApplyColor(TriangleMesh mesh, int vertex, RoomCamera rCam, Color customColor);
        }

        public class RainbowCapeColor : ICosmeticSkin
        {
            public bool UsesCustomColor => false;
            public void ApplyColor(TriangleMesh mesh, int vertex, RoomCamera rCam, Color customColor)
            {
                mesh.verticeColors[vertex] = Color.HSVToRGB(Time.time * 0.1f % 1f, 1f, 1f); ;
            }

            public override string ToString() => "rainbow";
        }

        public class CosmicCapeColor : ICosmeticSkin
        {
            public bool UsesCustomColor => true;
            public List<WeakReference> shaderApplied = new();
            public void ApplyColor(TriangleMesh mesh, int vertex, RoomCamera rCam, Color customColor)
            {
                if (!shaderApplied.Any(x => x.Target == mesh))
                {
                    var nightsky = rCam?.game.rainWorld.Shaders["RM_NightSkySkin"];
                    mesh.shader = nightsky;
                    RainMeadow.OnPopulateRenderLayer.Get(mesh).onEvent += (FFacetNode node) =>
                    {
                        node._renderLayer._material.SetTexture("_RM_NightSky", RainMeadow.nightsky);
                    };
                    shaderApplied.Add(new WeakReference(mesh));
                }

                mesh.verticeColors[vertex] = customColor;
            }

            public override string ToString() => "cosmic";
        }

        public class SolidCapeColor : ICosmeticSkin
        {
            public bool UsesCustomColor => true;
            public SolidCapeColor()
            {
            }
            public void ApplyColor(TriangleMesh mesh, int vertex, RoomCamera rCam, Color customColor)
            {
                mesh.verticeColors[vertex] = customColor;
            }

            public override string ToString() => "solid";
        }

        public class FadeGradientCapeColor : ICosmeticSkin
        {
            public static Color GetFadeColor(Color baseColor)
            {
                HSLColor baseColorHSL = baseColor.ToHSL();
                if (baseColorHSL.saturation < 0.5f) // fade to saturated color if desaturated
                    return new HSLColor(
                        baseColorHSL.hue, 
                        1f, 
                        Mathf.Pow(baseColorHSL.lightness, baseColorHSL.lightness > 0.5f ? 2 : 0.5f)
                    ).rgb;

                else if (baseColorHSL.lightness < 0.5f) // fade to white if dark color
                    return Color.white;

                else // fade to black if bright color
                    return new(0.1f, 0.1f, 0.1f);
            }

            public bool UsesCustomColor => true;
            public FadeGradientCapeColor()
            {
            }
            public void ApplyColor(TriangleMesh mesh, int vertex, RoomCamera rCam, Color customColor)
            {
                mesh.verticeColors[vertex] = customColor;
            }

            public void ApplyFade(TriangleMesh mesh, int vertex, float fade)
                => ApplyFade(mesh, vertex, GetFadeColor(mesh.verticeColors[vertex]), fade);
            public void ApplyFade(TriangleMesh mesh, int vertex, Color fadeColor, float fade)
            {
                mesh.verticeColors[vertex] = Color.Lerp(mesh.verticeColors[vertex], fadeColor, fade);
            }

            public override string ToString() => "fade gradient";
        }


        const string capes_latest_commit = "https://github.com/invalidunits/MeadowCosmetics16.git/info/refs?service=git-upload-pack";
        static async Task<string> FetchLatestCommit()
        {
            using (WebClient client = new WebClient())
            {
                string response = await client.DownloadStringTaskAsync(capes_latest_commit);

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
        static async public void FetchCosmetics()
        {
            try
            {
                var cape_hash_file = Path.Combine(ModManager.GetModById("henpemaz_rainmeadow").path, "capes_hash.txt");
                var capes_txt = Path.Combine(ModManager.GetModById("henpemaz_rainmeadow").path, "capes.txt");

                using (WebClient client = new WebClient())
                {
                    string commithash = await FetchLatestCommit();  

                    // Read local commit hash from file.
                    string commithashlocal = string.Empty;
                    if (File.Exists(cape_hash_file)) commithashlocal = File.ReadAllText(cape_hash_file);

                    // Only download the new capes when the hashes don't match.
                    if (commithash != commithashlocal)
                    {
                        RainMeadow.Debug("Local hash doesn't match, downloading remote.");
                        using (FileStream stream = File.Create(cape_hash_file))
                        using (StreamWriter writer = new(stream))
                        {
                            writer.Write(commithash);
                        }

                        string response = client.DownloadString(capes_remote_txt);
                        using (FileStream stream = File.Create(capes_txt))
                        using (StreamWriter writer = new(stream))
                        {
                            writer.WriteLine(response);
                        }
                    }
                }
            }
            catch (Exception except)
            {
                RainMeadow.Error($"Failed to fetch cosmetics: {except}");
            }
        }

        private static Dictionary<string, List<string>> entries = new();
        static public void ParseAvailableCosmetics()
        {
            entries.Clear();
            var capes_txt = Path.Combine(ModManager.GetModById("henpemaz_rainmeadow").path, "capes.txt");
            RainMeadow.DebugMe();

            // process capes from file.
            using (FileStream capefile = File.OpenRead(capes_txt))
            using (StreamReader capestream = new StreamReader(capefile))
            {
                while (true)
                {
                    var line = capestream.ReadLine();
                    RainMeadow.Debug(line);
                    if (line == null) break;

                    // Skip the header or empty lines
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("steamid64"))
                        continue;

                    // Split the line into parts
                    string[] parts = line.Split(';')[0].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) continue;

                    string HashedsteamId64 = parts[0].Trim();
                    RainMeadow.Debug(HashedsteamId64);
                    List<string> cosmetics = [];
                    cosmetics.AddRange(parts.Skip(1).Select(x => x.Trim()));

                    // Add the parsed entry to the list
                    if (cosmetics.Count > 0 && !entries.ContainsKey(HashedsteamId64))
                    {
                        entries.Add(HashedsteamId64, cosmetics);
                    }
                }
            }
        }

        static public IMeadowCosmetic? ParseCosmetic(string cosmetic, GraphicsModule baseModule, int firstSprite, ICosmeticSkin color)
        {
            return cosmetic switch
            {
                "cape" => new SlugcatCape(baseModule, firstSprite, color),
                "scarf" => new MeadowScarf(baseModule, firstSprite, color),
                _ => null
            };
        }


        public static ICosmeticSkin? ParseCosmeticSkin(string color)
        {
            return color switch
            {
                "rainbow" => new RainbowCapeColor(),
                "cosmic" => new CosmicCapeColor(),
                "solid" => new SolidCapeColor(),
                "fade gradient" => new FadeGradientCapeColor(),
                _ => null,
            };
        }

        static IReadOnlyList<string> emptyList = [];
        static IReadOnlyList<string> allCosmetics = ["cape", "scarf"];
        private static ConditionalWeakTable<MeadowPlayerId, IReadOnlyList<string>> cape_cache = new();
        static public IReadOnlyList<string> AvailableCosmetics(MeadowPlayerId player)
        {
            if (cape_cache.TryGetValue(player, out var entry) && entry is not null) return entry;
            if (player is SteamMatchmakingManager.SteamPlayerId steamid)
            {
                ulong steamID = steamid.oid.GetSteamID64();
                SHA256 Sha = SHA256.Create();
                var hashed_cape_str = System.Convert.ToBase64String(Sha.ComputeHash(Encoding.ASCII.GetBytes(steamID.ToString())));
                RainMeadow.Debug(hashed_cape_str);

                if (entries.TryGetValue(hashed_cape_str, out List<string> found_entry))
                {
                    cape_cache.Add(player, found_entry);
                    return found_entry;
                }
            }

            cape_cache.Add(player, emptyList);
            return emptyList;
        }

        private static ConditionalWeakTable<MeadowPlayerId, IReadOnlyList<string>> available_skins_cache = new();
        static public IReadOnlyList<string> AvailableCosmeticSkins(MeadowPlayerId player)
        {
            if (available_skins_cache.TryGetValue(player, out var ret)) return ret;

            var skins = new List<string>();
            if (player == OnlineManager.mePlayer.id? MatchmakingManager.instances.Values.Any(x => x.IsDev(player)) : MatchmakingManager.currentInstance.IsDev(player)) skins.Add("cosmic");
            if (SpecialEvents.AprilFoolsEvent.IsActive)
            {
                skins.Add("rainbow");
            }

            skins.Add("solid");
            skins.Add("fade gradient");
            available_skins_cache.Add(player, skins);
            return skins;
        }



        public static void RefreshGraphicalModule(PhysicalObject obj)
        {
            if (obj is not null && obj.graphicsModule is GraphicsModule module && obj.room is Room r)
            {
                r.drawableObjects.Remove(module);
                obj.graphicsModule = null;
                obj.InitiateGraphicsModule();
                for (int k = 0; k < r.game.cameras.Length; k++)
                {
                    if (r.game.cameras[k].room == r)
                    {
                        r.game.cameras[k].ReplaceDrawable(module, obj.graphicsModule);
                    }
                }

                for (int i = 0; i < 40; i++)
                {
                    obj.graphicsModule?.Update();
                }
            }
        }
    }
}

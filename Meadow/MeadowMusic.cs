using Music;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public partial class MeadowMusic
    {
        public static void EnableMusic()
        {
            On.RainWorldGame.ctor += GameCtorPatch;
            On.RainWorldGame.RawUpdate += RawUpdatePatch;
            On.RegionGate.Update += GateUpdatePatch;
            On.VirtualMicrophone.NewRoom += NewRoomPatch;
        }

        private const int waitSecs = 5;
        private static bool filesChecked = false;
        private static bool gateOpen = false;
        private static readonly Dictionary<string, string[]> ambientDict = new();
        private static readonly Dictionary<string, VibeZone[]> vibeZonesDict = new();
        private static Dictionary<int, ActiveZone> activeZonesDict = null;
        private static string[] ambienceSongArray = null;
        private static float time = 0f;
        private static bool timerStopped = true;
        private static ActiveZone? activeZone = null;

        private struct VibeZone
        {
            public VibeZone(string room, float radius, string songName)
            {
                this.room = room;
                this.radius = radius;
                this.songName = songName;
            }

            public string room;
            public float radius;
            public string songName;
        }

        private struct ActiveZone
        {
            public ActiveZone(float radius, string songName)
            {
                this.radius = radius;
                this.songName = songName;
            }

            public float radius;
            public string songName;
        }

        private static void GameCtorPatch(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig.Invoke(self, manager);

            if (!filesChecked)
            {
                string[] dirs = AssetManager.ListDirectory("world", true, true);
                foreach (string dir in dirs)
                {
                    string regName = GetFolderName(dir).ToUpper();
                    string path = dir + Path.DirectorySeparatorChar + "playlist.txt";
                    if (regName.Length == 2 && File.Exists(path) && !ambientDict.ContainsKey(regName))
                    {
                        string[] lines = File.ReadAllLines(path);
                        foreach (string line in lines)
                        {
                            Debug.Log("Meadow Music:  Registered song " + line + " in " + dir);
                        }
                        ambientDict.Add(regName, lines);
                    }
                    path = dir + Path.DirectorySeparatorChar + "vibe_zones.txt";
                    if (File.Exists(path) && !vibeZonesDict.ContainsKey(regName))
                    {
                        string[] lines = File.ReadAllLines(path);
                        VibeZone[] zones = new VibeZone[lines.Length];
                        for (int i = 0; i < lines.Length; i++)
                        {
                            string[] arr = lines[i].Split(',');
                            zones[i] = new VibeZone(arr[0], float.Parse(arr[1]), arr[2]);
                        }
                        vibeZonesDict.Add(regName, zones);
                    }
                }

                filesChecked = true;
                AnalyzeRegion(self.world);
                time = 0f;
                timerStopped = true;
            }
        }

        private static void RawUpdatePatch(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            orig.Invoke(self, dt);
            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MeadowGameMode) return;
            if (!timerStopped) time += dt;

            MusicPlayer musicPlayer = self.manager.musicPlayer;

            if (musicPlayer != null && musicPlayer.song == null && self.world.rainCycle.RainApproaching > 0.5f)
            {
                timerStopped = false;
                if (time > waitSecs)
                {
                    if (activeZone == null)
                    {
                        if (ambienceSongArray != null)
                        {
                            Debug.Log("Meadow Music:  Playing ambient song");
                            Song song = new(musicPlayer, ambienceSongArray[(int)Random.Range(0f, ambienceSongArray.Length - 0.1f)], MusicPlayer.MusicContext.StoryMode)
                            {
                                playWhenReady = true,
                                volume = 1,
                                fadeInTime = 40f
                            };
                            musicPlayer.song = song;
                        }
                        //Nitpick: if we are outside a vibe zone and the current region has no ambience list, this results in exiting this if-stack with no song playing
                        //Therefore these checks will be repeated for every waitSecs seconds
                    }
                    else
                    {
                        Debug.Log("Meadow Music:  Playing vibe song...");
                        Song song = new Song(musicPlayer, ((ActiveZone)activeZone).songName, MusicPlayer.MusicContext.StoryMode)
                        {
                            playWhenReady = true,
                            volume = 1,
                            fadeInTime = 40f
                        };
                        musicPlayer.song = song;
                    }
                }
            }
            else
            {
                time = 0f;
                timerStopped = true;
            }
        }

        private static void GateUpdatePatch(On.RegionGate.orig_Update orig, RegionGate self, bool eu)
        {
            orig.Invoke(self, eu);

            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MeadowGameMode) return;

            //TODO: incompatible with unintended methods to switch regions such as Warp Mod
            if (!gateOpen && self.mode == RegionGate.Mode.MiddleOpen)
            {
                AnalyzeRegion(self.room.world);
                gateOpen = true;
            }
            else if (gateOpen && self.mode != RegionGate.Mode.MiddleOpen) gateOpen = false;
        }

        private static void NewRoomPatch(On.VirtualMicrophone.orig_NewRoom orig, VirtualMicrophone self, Room room)
        {
            orig.Invoke(self, room);

            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MeadowGameMode) return;

            MusicPlayer musicPlayer = room.game.manager.musicPlayer;

            if (musicPlayer != null && musicPlayer.song != null && activeZonesDict != null)
            {
                int[] rooms = activeZonesDict.Keys.ToArray();
                float[] dists = new float[rooms.Length];
                for (int i = 0; i < rooms.Length; i++)
                {
                    Vector2 v1 = room.world.RoomToWorldPos(Vector2.zero, room.abstractRoom.index);
                    Vector2 v2 = room.world.RoomToWorldPos(Vector2.zero, rooms[i]);
                    dists[i] = Mathf.Abs(Vector2.Distance(v1, v2));
                }
                float minDist = Mathf.Min(dists);
                int closestVibe = rooms[dists.ToList().IndexOf(minDist)];
                ActiveZone az = activeZonesDict[closestVibe];
                if (musicPlayer.song.name == az.songName && minDist > az.radius)
                {
                    Debug.Log("Meadow Music:  Fading echo song...");
                    musicPlayer.song.FadeOut(40f);
                    activeZone = null;
                }
                else if (musicPlayer.song.name != az.songName && minDist < az.radius)
                {
                    Debug.Log("Meadow Music:  Fading ambience song...");
                    musicPlayer.song.FadeOut(40f);
                    activeZone = az;
                }
                else if (minDist < az.radius)
                {
                    musicPlayer.song.volume = (1 - minDist) / (2 * az.radius);
                }
            }
        }

        private static string GetFolderName(string path)
        {
            string[] arr = path.Split(Path.DirectorySeparatorChar);
            return arr[arr.Length - 1];
        }

        private static void AnalyzeRegion(World world)
        {
            VibeZone[] vzArray;
            activeZonesDict = null;
            if (vibeZonesDict.TryGetValue(world.region.name, out vzArray))
            {
                activeZonesDict = new Dictionary<int, ActiveZone>();
                foreach (VibeZone vz in vzArray)
                {
                    foreach (AbstractRoom room in world.abstractRooms)
                    {
                        if (room.name == vz.room)
                        {
                            activeZonesDict.Add(room.index, new ActiveZone(vz.radius, vz.songName));
                            break;
                        }
                    }
                }
                if (activeZonesDict.Count == 0) activeZonesDict = null;
            }
            if (ambientDict.TryGetValue(world.region.name, out string[] songArr)) ambienceSongArray = songArr;
            else ambienceSongArray = null;
        }
    }
}
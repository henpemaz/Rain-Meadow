using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Music;
using System.Linq;
using RWCustom;

namespace RainMeadow
{
    public partial class MeadowMusic
    {
        public static void EnableMusic()
        {
            On.RainWorldGame.ctor += GameCtorPatch;
            On.RainWorldGame.RawUpdate += RawUpdatePatch;
            On.OverWorld.WorldLoaded += WorldLoadedPatch;
            On.VirtualMicrophone.NewRoom += NewRoomPatch;
        }

        const int waitSecs = 5;

        static bool filesChecked = false;

        static readonly Dictionary<string, string[]> ambientDict = new();
        static readonly Dictionary<string, VibeZone[]> vibeZonesDict = new();

        internal static Dictionary<int, VibeZone> activeZonesDict = null;
        static string[] ambienceSongArray = null;

        static float time = 0f;
        static bool timerStopped = true;

        static VibeZone? activeZone = null;

        static float? vibeIntensity = null;
        static float? vibePan = null;

        internal struct VibeZone
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

        static void GameCtorPatch(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig.Invoke(self, manager);

            //Arena mode, etc... won't have Meadow Music, so no point checking the files
            if (!self.IsStorySession) return;

            if (!filesChecked)
            {
                string[] dirs = AssetManager.ListDirectory("world", true, true);
                foreach (string dir in dirs)
                {
                    string regName = new DirectoryInfo(dir).Name.ToUpper();
                    string path = dir + Path.DirectorySeparatorChar + "playlist.txt";
                    if (regName.Length == 2 && File.Exists(path) && !ambientDict.ContainsKey(regName))
                    {
                        string[] lines = File.ReadAllLines(path).Where(l => l != string.Empty).ToArray();
                        foreach (string line in lines)
                        {
                            RainMeadow.Debug("Meadow Music:  Registered song " + line + " in " + dir);
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
            }
            AnalyzeRegion(self.world);
            time = 0f;
            timerStopped = true;
        }

        static void RawUpdatePatch(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
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
                            RainMeadow.Debug("Meadow Music:  Playing ambient song");
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
                        RainMeadow.Debug("Meadow Music:  Playing vibe song...");
                        Song song = new Song(musicPlayer, ((VibeZone)activeZone).songName, MusicPlayer.MusicContext.StoryMode)
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

        static void WorldLoadedPatch(On.OverWorld.orig_WorldLoaded orig, OverWorld self)
        {
            orig.Invoke(self);

            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MeadowGameMode) return;

            AnalyzeRegion(self.activeWorld);
        }

        static void NewRoomPatch(On.VirtualMicrophone.orig_NewRoom orig, VirtualMicrophone self, Room room)
        {
            orig.Invoke(self, room);

            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MeadowGameMode) return;

            MusicPlayer musicPlayer = room.game.manager.musicPlayer;

            if (musicPlayer != null && musicPlayer.song != null && activeZonesDict != null)
            {
                //activezonedict has the room ids of each vibe zone's room as keys
                int[] rooms = activeZonesDict.Keys.ToArray();
                float minDist = float.MaxValue;
                int closestVibe = -1;
                //find the closest one
                for (int i = 0; i < rooms.Length; i++)
                {
                    //yoink the coordinates of the player's current room
                    Vector2 v1 = room.world.RoomToWorldPos(Vector2.zero, room.abstractRoom.index);
                    //yoink the coordinates of the vibe zone room
                    Vector2 v2 = room.world.RoomToWorldPos(Vector2.zero, rooms[i]);
                    //calculate the flat distance between these two vectors
                    var dist = (v2 - v1).magnitude;
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestVibe = rooms[i];
                    }
                }
                //and just grab its corresponding vibezone from the dict
                VibeZone az = activeZonesDict[closestVibe];
                //if this active zone's song is currently playing, and we are beyond the zone's radius
                if (musicPlayer.song.name == az.songName && minDist > az.radius)
                {
                    RainMeadow.Debug("Meadow Music:  Fading echo song...");
                    musicPlayer.song.FadeOut(40f);
                    activeZone = null;
                    vibeIntensity = null;
                    vibePan = null;
                }
                //if this active zone's song is not currently playing, and we are within its radius
                else if (musicPlayer.song.name != az.songName && minDist < az.radius)
                {
                    RainMeadow.Debug("Meadow Music:  Fading ambience song...");
                    musicPlayer.song.FadeOut(40f);
                    activeZone = az;
                }
                //if this active zone's song is currently playing (we can assume this at this point) and are within its radius
                else if (minDist < az.radius)
                {
                    vibeIntensity = Custom.LerpMap(minDist, az.radius, 0, 0.5f, 1);
                    vibePan = Vector2.Dot((room.world.RoomToWorldPos(Vector2.zero, rooms[closestVibe]) - room.world.RoomToWorldPos(Vector2.zero, room.abstractRoom.index)).normalized, Vector2.right);
                    musicPlayer.song.baseVolume = Custom.LerpMap(minDist, az.radius, 0, 0.5f, 1) * 0.3f;
                }
            }
        }

        static void AnalyzeRegion(World world)
        {
            RainMeadow.Debug("Meadow Music:  Analyzing " + world.name);
            VibeZone[] vzArray;
            activeZonesDict = null;
            if (vibeZonesDict.TryGetValue(world.region.name, out vzArray))
            {
                RainMeadow.Debug("Meadow Music:  found zones " + vzArray.Length);
                activeZonesDict = new Dictionary<int, VibeZone>();
                foreach(VibeZone vz in vzArray)
                {
                    foreach (AbstractRoom room in world.abstractRooms)
                    {
                        RainMeadow.Debug("Meadow Music:  looking for room " + vz.room);
                        if (room.name == vz.room)
                        {
                            RainMeadow.Debug("Meadow Music:  found hub " + room.name);
                            activeZonesDict.Add(room.index, vz);
                            break;
                        }
                    }
                }
                if (activeZonesDict.Count == 0)
                {
                    RainMeadow.Debug("Meadow Music:  no hubs found");
                    activeZonesDict = null;
                }
            }
            if (ambientDict.TryGetValue(world.region.name, out string[] songArr))
            {
                RainMeadow.Debug("Meadow Music:  ambiences loaded");
                ambienceSongArray = songArr;
            }
            else
            {
                RainMeadow.Debug("Meadow Music:  no ambiences for region");
                ambienceSongArray = null;
            }
        }
    }
}
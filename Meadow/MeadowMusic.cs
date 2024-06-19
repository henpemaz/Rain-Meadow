using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Music;
using System.Linq;
using RWCustom;
using IL.MoreSlugcats;
using System;
using IL;

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
        internal static readonly Dictionary<string, VibeZone[]> vibeZonesDict = new();

        internal static Dictionary<int, VibeZone> activeZonesDict = null;
        static string[] ambienceSongArray = null;

        static int[] shufflequeue = new int[0];
        static int shuffleindex = int.MaxValue - 69;

        static float time = 0f;
        static bool timerStopped = true;

        static VibeZone? activeZone = null;
        public static bool AllowPlopping;

        public static float? vibeIntensity = null;
        public static float? vibePan = null;
        static bool UpdateIntensity;

        internal struct VibeZone
        {
            public VibeZone(string room, float radius, float minradius, string sampleUsed)
            {
                this.room = room;
                this.radius = radius;
                this.minradius = minradius;
                this.sampleUsed = sampleUsed;
            }

            public string room;
            public float radius;
            public float minradius;
            public string sampleUsed;
        }
        static VibeZone az = new VibeZone();
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
                            zones[i] = new VibeZone(arr[0], float.Parse(arr[1]), float.Parse(arr[2]), arr[3]);
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
        static public float plopIntensity;
        static void RawUpdatePatch(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            orig.Invoke(self, dt);
            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MeadowGameMode) return;
            if (!timerStopped) time += dt;

            MusicPlayer musicPlayer = self.manager.musicPlayer;

            if (UpdateIntensity)
            {
                vibePan = Vector2.Dot((RoomImIn.world.RoomToWorldPos(Vector2.zero, closestVibe) - RoomImIn.world.RoomToWorldPos(Vector2.zero, RoomImIn.abstractRoom.index)).normalized, Vector2.right);

                //RainMeadow.Debug("IsFased");
                Vector2 LOL = self.world.RoomToWorldPos(self.world.GetAbstractRoom(closestVibe).size.ToVector2() * 10f, closestVibe);
                Vector2 lol = self.world.RoomToWorldPos(MyGuyMic.listenerPoint, RoomImIn.abstractRoom.index);

                //RainMeadow.Debug("IsZased");
                
                float vibeIntensityTarget = 
                             Mathf.Pow(Mathf.InverseLerp(az.radius, az.minradius, Vector2.Distance(lol, LOL)), 1.425f)
                           * Custom.LerpMap((float)DegreesOfAwayness, 0f, 3f, 1f, 0.15f)
                           //* Custom.LerpMap((float)DegreesOfAwayness, 1f, 3f, 0.6f, 0.15f)
                           * ((RoomImIn.abstractRoom.layer == self.world.GetAbstractRoom(closestVibe).layer) ? 1f : 0.75f);

                //RainMeadow.Debug("IsBased");

                //float floattargetplop = Mathf.Pow((float)MeadowMusic.vibeIntensity, 1.6f) * 0.5f;
                vibeIntensityTarget = Custom.LerpAndTick(vibeIntensity == null ? 0 : (float)vibeIntensity, vibeIntensityTarget, 0.025f, 0.002f);
                vibeIntensity = vibeIntensityTarget;

                AllowPlopping = vibeIntensity.Value >= 0.2f;

                if (musicPlayer != null && musicPlayer.song != null)
                {
                    if ((float)vibeIntensity > 0.9f)
                    {
                        musicPlayer.song.baseVolume = 0f;
                    }
                    else
                    {
                        musicPlayer.song.baseVolume = Mathf.Pow(1f - (float)vibeIntensity, 2f) * 0.3f;
                    }
                }
                
                RainMeadow.Debug("IsMased");
            }
            if (musicPlayer != null && musicPlayer.song == null && self.world.rainCycle.RainApproaching > 0.5f)
            {
                timerStopped = false;
                if (time > waitSecs)
                {
                    RainMeadow.Debug("But here's the overthetimer");
                    //if (activeZone == null)
                    //{
                        if (ambienceSongArray != null)
                        {
                            RainMeadow.Debug("Meadow Music:  Playing ambient song");
                            Song song = new(musicPlayer, ambienceSongArray[NewSong()], MusicPlayer.MusicContext.StoryMode)
                            {
                                playWhenReady = true,
                                volume = 1,
                                fadeInTime = 1f
                            };
                            musicPlayer.song = song;
                        }
                        //Nitpick: if we are outside a vibe zone and the current region has no ambience list, this results in exiting this if-stack with no song playing
                        //Therefore these checks will be repeated for every waitSecs seconds
                    //}
                    //else
                    //{
                    //    RainMeadow.Debug("Meadow Music:  Playing vibe song...");
                    //    Song song = new Song(musicPlayer, ((VibeZone)activeZone).songName, MusicPlayer.MusicContext.StoryMode)
                    //    {
                    //        playWhenReady = true,
                    //        volume = 1,
                    //        fadeInTime = 40f
                    //    };
                    //    musicPlayer.song = song;
                    //}
                }
                //RainMeadow.Debug("AAAAAAAAAAAAa");
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
        static int closestVibe;
        static Room? RoomImIn;
        static int DegreesOfAwayness;
        static int CalculateDegreesOfAwayness(AbstractRoom testRoom)
        {
            var vibeRoom = testRoom.world.GetAbstractRoom(az.room);
            if (vibeRoom == null) return -1;

            if (testRoom.index == vibeRoom.index)
            {
                return 0;
            }
            int num = 100;
            for (int i = 0; i < vibeRoom.connections.Length; i++)
            {
                if (vibeRoom.connections[i] == testRoom.index)
                {
                    return 1;
                }
                if (vibeRoom.connections[i] > -1)
                {
                    AbstractRoom abstractRoom = testRoom.world.GetAbstractRoom(vibeRoom.connections[i]);
                    for (int j = 0; j < abstractRoom.connections.Length; j++)
                    {
                        if (abstractRoom.connections[j] == testRoom.index)
                        {
                            num = Math.Min(num, 2);
                            break;
                        }
                        if (abstractRoom.connections[j] > -1)
                        {
                            AbstractRoom abstractRoom2 = testRoom.world.GetAbstractRoom(abstractRoom.connections[j]);
                            for (int k = 0; k < abstractRoom2.connections.Length; k++)
                            {
                                if (abstractRoom2.connections[k] == testRoom.index)
                                {
                                    num = Math.Min(num, 3);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            if (num > 3)
            {
                return -1;
            }
            return num;
        }
        static VirtualMicrophone MyGuyMic;
        static void NewRoomPatch(On.VirtualMicrophone.orig_NewRoom orig, VirtualMicrophone self, Room room)
        {
            orig.Invoke(self, room);

            MyGuyMic = self;
            RoomImIn = room;

            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MeadowGameMode) return;

            MusicPlayer musicPlayer = room.game.manager.musicPlayer;

            if (musicPlayer != null && musicPlayer.song != null && activeZonesDict != null)
            {
                //activezonedict has the room ids of each vibe zone's room as keys
                int[] rooms = activeZonesDict.Keys.ToArray(); //why does it have to be the keys? can't this just be a list and have the id defined in class vibezone?
                float minDist = float.MaxValue;
                closestVibe = -1;
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
                az = activeZonesDict[closestVibe];
                //if this active zone's song is currently playing, and we are beyond the zone's radius
                //if (musicPlayer.song.name == az.songName && minDist > az.radius)
                //{
                //    RainMeadow.Debug("Meadow Music:  Fading echo song...");
                //    musicPlayer.song.FadeOut(40f);
                //    activeZone = null;
                //    vibeIntensity = null;
                //    vibePan = null;
                //}
                ////if this active zone's song is not currently playing, and we are within its radius
                //else if (musicPlayer.song.name != az.songName && minDist < az.radius)
                if (minDist > az.radius)
                {
                    //RainMeadow.Debug("Meadow Music:  Fading echo song...");
                    //musicPlayer.song.FadeOut(40f);
                    //activeZone = null;
                    vibeIntensity = 0f;
                    musicPlayer.song.baseVolume = 0.3f;
                    AllowPlopping = false;
                    UpdateIntensity = false;
                    //vibePan = null;
                }
                //if this active zone's song is currently playing (we can assume this at this point) and are within its radius
                else if (minDist < az.radius)
                {
                    UpdateIntensity = true;
                    AllowPlopping = true;
                    //vibePan = Vector2.Dot((room.world.RoomToWorldPos(Vector2.zero, rooms[closestVibe]) - room.world.RoomToWorldPos(Vector2.zero, room.abstractRoom.index)).normalized, Vector2.right);
                    //activeZone = az;

                    RainMeadow.Debug($"So we've decided the thing is now: {vibeIntensity}, {vibeIntensity}");
                }
            }
            RainMeadow.Debug($":D :D  Yuri  {closestVibe}");
            DegreesOfAwayness = CalculateDegreesOfAwayness(room.abstractRoom);
        }
        static int NewSong()
        {
            RainMeadow.Debug("calling all hoes");
            if (shuffleindex + 1 >= shufflequeue.Length)
            {
                RainMeadow.Debug("calling all of them hoes");
                ShuffleSongs();
            }
            else
            {
                shuffleindex++;
                RainMeadow.Debug("New number" + shuffleindex);
            }
            RainMeadow.Debug("From this it " + shuffleindex);
            RainMeadow.Debug("I'm calling the number " + shufflequeue[shuffleindex]);
            return shufflequeue[shuffleindex];
        }
        static void ShuffleSongs()
        {
            shuffleindex = 0;
            bool isfirstshuffle = shufflequeue.Length == 0;
            int shufflelastbuffle = 0;
            if (!isfirstshuffle) shufflelastbuffle = shufflequeue[shufflequeue.Length - 1];
            shufflequeue = new int[ambienceSongArray.Length];
            List<int> Hah = new List<int>();
            for (int i = 0; i < ambienceSongArray.Length; i++) { Hah.Add(i); }
            if (!isfirstshuffle) Hah.Remove(shufflelastbuffle);
            int j = 0;
            while (j < shufflequeue.Length) 
            {
                int RandomInt = UnityEngine.Random.Range(0, Hah.Count);
                shufflequeue[j] = Hah[RandomInt];
                Hah.RemoveAt(RandomInt);
                if (j == 0 && !isfirstshuffle) Hah.Add( shufflelastbuffle ); 
                j++;
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
                ShuffleSongs();
            }
            else
            {
                RainMeadow.Debug("Meadow Music:  no ambiences for region");
                ambienceSongArray = null;
            }
        }
    }
}
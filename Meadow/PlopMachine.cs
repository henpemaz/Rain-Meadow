using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace RainMeadow
{
    public class PlopMachine
    {
        public void OnEnable()
        {
            On.RainWorldGame.Update += RainWorldGame_Update; //actually usefull
            On.PlayerGraphics.DrawSprites += hehedrawsprites;
            On.RainWorldGame.ctor += RainWorldGame_ctor; //actually usefull 
        }
        readonly float magicnumber = 1.0594630776202568303519954093385f;
        int CurrentKey = 0;
        int QueuedModulation = 0;
        int debugtimer = 0;

        readonly string[,] notesinkey =
        {
            {"Gb", "Ab", "Bb", "B" , "Db", "Eb", "F" , "Gb"}, //Gb  (the same as F#) 
            {"Db", "Eb", "F" , "Gb", "Ab", "Bb", "C" , "Db"}, //Db 
            {"Ab", "Bb", "C" , "Db", "Eb", "F" , "G" , "Ab"}, //Ab
            {"Eb", "F" , "G" , "Ab", "Bb", "C" , "D" , "Eb"}, //Eb
            {"Bb", "C" , "D" , "Eb", "F" , "G" , "A" , "Bb"}, //Bb
            {"F" , "G" , "A" , "Bb", "C" , "D" , "E" , "F" }, //F
            {"C" , "D" , "E" , "F" , "G" , "A" , "B" , "C" }, //C
            {"G" , "A" , "B" , "C" , "D" , "E" , "F#", "G" }, //G
            {"D" , "E" , "F#", "G" , "A" , "B" , "C#", "D" }, //D
            {"A" , "B" , "C#", "D" , "E" , "F#", "G" , "A" }, //A
            {"E" , "F#", "G#", "A" , "B" , "C#", "D#", "E" }, //E
            {"B" , "C#", "D#", "E" , "F#", "G#", "A#", "B" }, //B
            {"F#", "G#", "A#", "B" , "C#", "D#", "F" , "F#"}, //F#   (the same as Gb)
        };
        public readonly int[,] intsinkey =
        {
            { 6  , 8  , 10 , 11 , 1  , 3  , 5  , 6  }, //Gb  (the same as F#) 
            { 1  , 3  , 5  , 6  , 8  , 10 , 0  , 1  }, //Db 
            { 8  , 10 , 0  , 1  , 3  , 5  , 7  , 8  }, //Ab
            { 3  , 5  , 7  , 8  , 10 , 0  , 2  , 3  }, //Eb
            { 10 , 0  , 2  , 3  , 5  , 7  , 9  , 10 }, //Bb
            { 5  , 7  , 9  , 10 , 0  , 2  , 4  , 5  }, //F
            { 0  , 2  , 4  , 5  , 7  , 9  , 11 , 0  }, //C
            { 7  , 9  , 11 , 0  , 2  , 4  , 6  , 7  }, //G
            { 2  , 4  , 6  , 7  , 9  , 11 , 1  , 2  }, //D
            { 9  , 11 , 1  , 2  , 4  , 6  , 8  , 9  }, //A
            { 4  , 6  , 8  , 9  , 11 , 1  , 3  , 4  }, //E
            { 11 , 1  , 3  , 4  , 6  , 8  , 10 , 11 }, //B
            { 6  , 8  , 10 , 11 , 1  , 3  , 5  , 6  } //F#   (the same as Gb)
        };
        //major key, goes root +2 +2 +1 +2 +2 +2  

        //so the minor version of all of these are as follows:
        public readonly int[,] intsinmkey =
        {
            { 6  , 8  , 9  , 11 , 1  , 2  , 4  , 6  }, //Gb m (the same as F#) 
            { 1  , 3  , 4  , 6  , 8  , 9  , 11 , 1  }, //Db m
            { 8  , 10 , 11 , 1  , 3  , 4  , 6  , 8  }, //Ab m
            { 3  , 5  , 6  , 8  , 10 , 11 , 1  , 3  }, //Eb m
            { 10 , 0  , 1  , 3  , 5  , 6  , 8  , 10 }, //Bb m
            { 5  , 7  , 8  , 10 , 0  , 1  , 3  , 5  }, //F  m
            { 0  , 2  , 3  , 5  , 7  , 8  , 10 , 0  }, //C  m
            { 7  , 9  , 10 , 0  , 2  , 3  , 5  , 7  }, //G  m
            { 2  , 4  , 5  , 7  , 9  , 10 , 0  , 2  }, //D  m
            { 9  , 11 , 0  , 2  , 4  , 5  , 7  , 9  }, //A  m
            { 4  , 6  , 7  , 9  , 11 , 0  , 2  , 4  }, //E  m
            { 11 , 1  , 2  , 4  , 6  , 7  , 9  , 11 }, //B  m
            { 6  , 8  , 9  , 11 , 1  , 2  , 4  , 6  } //F# m  (the same as Gb)
        };

        //https://muted.io/major-minor-scales/#minor-scales

        //todo: make a switch thing that makes the notes into the ints immediatelly

        /* the switch statement
        int transposition = 0;
        switch (NoteNow)
        {
            case "C":
                transposition = 0;
                break;
        
            case "C#" or "Db":
                transposition = 1;
                break;
        
            case "D":
                transposition = 2;
                break;
        
            case "D#" or "Eb":
                transposition = 3;
                break;
        
            case "E":
                transposition = 4;
                break;
        
            case "F":
                transposition = 5;
                break;
        
            case "F#" or "Gb":
                transposition = 6;
                break;
        
            case "G":
                transposition = 7;
                break;
        
            case "G#" or "Ab":
                transposition = 8;
                break;
        
            case "A":
                transposition = 9;
                break;
        
            case "A#" or "Bb":
                transposition = 10;
                break;
        
            case "B":
                transposition = 11;
                break;
        }
        */

        bool inmajorscale = true;

        bool EntryRequest = true;

        bool entrychord = false;
        bool entryriff = false;

        bool playingchord = false;
        bool playingriff = false;

        string chordnotes = "yosup";
        string chordleadups = "yosup";

        string riffline = "the command line yo like [pow][pow]";
        string riffleadups = "just the name of the chord";

        int chordstopwatch = 0;
        string chordqueuedentry = "yeah";

        bool inwaitmode = false;
        int riffstopwatch = 0;
        string UpcomingEntry = "Balaboo";         //important to set a first one
        string[]? theline;
        int riffindex;
        int rifflength;
        string? riffcurrentvar;
        bool islooping;
        int tilestasked;
        int loopcountdown;
        float riffd;
        int upcomingdelay;

        bool weplipping = true;

        //readonly bool firstchordevahhhhhhasbeenplayedd = false;

        int agora = 1;
        float phob;

        //string teststring = "hehe";
        string? CurrentRegion;

        //float distancetovibeepicentre = 0;

        //float intensity = 1.0f;

        //AudioReverbPreset[] thepresets = [AudioReverbPreset.Off, AudioReverbPreset.Generic, AudioReverbPreset.PaddedCell, AudioReverbPreset.Room, AudioReverbPreset.Bathroom, AudioReverbPreset.Livingroom, AudioReverbPreset.Stoneroom, AudioReverbPreset.Auditorium, AudioReverbPreset.Concerthall, AudioReverbPreset.Cave, AudioReverbPreset.Arena, AudioReverbPreset.Hangar, AudioReverbPreset.CarpetedHallway, AudioReverbPreset.Hallway, AudioReverbPreset.StoneCorridor, AudioReverbPreset.Alley, AudioReverbPreset.Forest, AudioReverbPreset.City, AudioReverbPreset.Mountains, AudioReverbPreset.Quarry, AudioReverbPreset.Plain, AudioReverbPreset.ParkingLot, AudioReverbPreset.SewerPipe, AudioReverbPreset.Underwater, AudioReverbPreset.Drugged, AudioReverbPreset.Dizzy, AudioReverbPreset.Psychotic, AudioReverbPreset.User];
        /*
        float IntikusdecayHFRatio = 0.5f;
        float IntikusdecayTime = 1f;
        float Intikusdensity = 1f;
        float Intikusdiffusion = 1f;
        float IntikusdryLevel = 1f;
        float IntikushfReference = 1f;
        float IntikuslfReference = 1f;
        float IntikusreflectionsDelay = 1f;
        float IntikusreflectionsLevel = 1f;
        float IntikusreverbDelay = 1f;
        float IntikusreverbLevel = 1f;
        float IntikusreverbPreset = 1f;
        float Intikusroom = 1f;
        float IntikusroomHF = 1f;
        float IntikusroomLF = 1f;
        */
        float[] RangeAdjs = new float[13];
        //[0.1f, 0.2f, 0.5f, 1f, 2f, 5f, 10f, 50f, 100f, 500f, 1000f, 2000f, 5000f];
        //float[] ack = [IntikusdecayHFRatio, IntikusdecayTime, Intikusdensity, Intikusdiffusion, IntikusdryLevel, IntikushfReference, IntikuslfReference, IntikusreflectionsDelay, IntikusreflectionsLevel, IntikusreverbDelay, IntikusreverbLevel, IntikusreverbPreset, Intikusroom, IntikusroomHF, IntikusroomLF]

        public static class EnumExt_AudioFilters
        {
#pragma warning disable 0649
            public static RoomSettings.RoomEffect.Type? AudioFiltersReverb;
#pragma warning restore 0649
        }


        bool fileshavebeenchecked = false;
        string[][]? ChordInfos;
        static readonly Dictionary<string, VibeZone[]> vibeZonesDict = new();
        struct VibeZone
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
        private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig.Invoke(self, manager);
            try
            {
                if (!fileshavebeenchecked)
                {
                    RainMeadow.Debug("Checking files");
                    string[] mydirs = AssetManager.ListDirectory("soundeffects", false, true);
                    RainMeadow.Debug("Printing all directories in soundeffects");
                    foreach (string dir in mydirs)
                    {
                        string filename = GetFolderName(dir);
                        RainMeadow.Debug(filename + " Is one of the things it sees, straight from " + dir);
                        if (filename == "!entries.txt")
                        {
                            RainMeadow.Debug("The file exists actually");
                            string[] lines = File.ReadAllLines(dir);

                            RainMeadow.Debug("it has read all its lines");
                            List<string[]> listtho = new List<string[]>();
                            foreach (string line in lines)
                            {
                                string[] chord = line.Split(new char[] { '$' });
                                RainMeadow.Debug("Plopmachine:  Registered Entry: " + line + " in ");
                                listtho.Add(chord);
                            }
                            RainMeadow.Debug("it has added the thongs");
                            ChordInfos = listtho.ToArray();
                        }
                    }
                    RainMeadow.Debug("Yo it's done with sfx");
                    string[] dirs = AssetManager.ListDirectory("2ndworld", true, true);
                    foreach (string dir in dirs)
                    {
                        string regName = GetFolderName(dir).ToUpper();
                        string path = dir + Path.DirectorySeparatorChar + "vibe_zones.txt";
                        if (File.Exists(path) && !vibeZonesDict.ContainsKey(regName))
                        {
                            RainMeadow.Debug($"It found the path for vibezone {regName} tho");
                            string[] lines = File.ReadAllLines(path);
                            VibeZone[] zones = new VibeZone[lines.Length];
                            for (int i = 0; i < lines.Length; i++)
                            {
                                string[] arr = lines[i].Split(',');
                                zones[i] = new VibeZone(arr[0], float.Parse(arr[1]), arr[2]);
                                RainMeadow.Debug($"{arr[0]}, {arr[1]}, {arr[2]}");
                            }
                            vibeZonesDict.Add(regName, zones);
                        }
                    }
                    RainMeadow.Debug("Yo wassup it went past worlds");
                    fileshavebeenchecked = true;
                }
            }
            catch (Exception e)
            {
                RainMeadow.Debug(e);
                //throw;
            }
        }
        static string GetFolderName(string path)
        {
            string[] arr = path.Split(Path.DirectorySeparatorChar);
            return arr[arr.Length - 1];
        }
        public void hehedrawsprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            //orig(self, sLeaser, timeStacker, rCam, timeStacker, camPos);
            orig(self, sLeaser, rCam, timeStacker, camPos);
            Color camocoloryo = rCam.PixelColorAtCoordinate(self.player.mainBodyChunk.pos);
            //RainMeadow.Debug($"So the color at here issss {color}");
            //Color mycolor = sLeaser[self.startSprite].color;
            foreach (var sprite in sLeaser.sprites)
            {
                //sprite.color = camocoloryo;
                sprite.color = mycolor;
            }
        }

        /*
            //On Initialize()
            IDetour hookTestMethodA = new Hook(
                    typeof(Class Of Property).GetProperty("property without get_", BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                    typeof(Class where hook is located).GetMethod("name of the methodHK", BindingFlags.Static | BindingFlags.Public)
                );
            //On Static hook class
                public delegate Type_of_property orig_nameOfProperty(Class self);
                
                public static Type_of_property get_PropertyHK(orig_nameOfProperty orig_nameOfProperty, Class self)
                {
                return orig_nameOfProperty(self);
                }
        */

        //public delegate int orig_lengthSamples(int self);
        //
        //    public static int get_CreateHK(orig_lengthSamples orig_lengthSamples, AudioClip self)
        //    {
        //        
        //        return orig_lengthSamples(self);
        //    }


        public int IndexTOCKInt(int index)
        {
            int treatedkey = CurrentKey + 6;
            int[,] thescale;
            if (inmajorscale)
            {
                thescale = intsinkey;
            }
            else
            {
                thescale = intsinmkey;
            }
            int integer = thescale[treatedkey, index - 1];
            return integer;
        }
        private SoundID[] SampDict(string length)
        {
            //RainMeadow.Debug($"It's trying to get length {length}");
            SoundID[] library = new SoundID[7]; //to do:  make better
            string acronym = CurrentRegion.ToUpper();
            VibeZone[] newthings;
            bool diditwork = vibeZonesDict.TryGetValue(acronym, out newthings);
            //we retrieve a newthings array (one of many vibezones)
            //RainMeadow.Debug("C" + diditwork);
            if (!diditwork) { RainMeadow.Debug("itdidn'twork"); return null; }
            VibeZone newthing = newthings[0]; //TEMP DUMMY FOR UNTIL HELLOTHERE'S REQUIUM
            //and pick the one that is closer
            //RainMeadow.Debug("d");
            string patch = newthing.songName;
            //RainMeadow.Debug(patch);
            //switch (acronym)
            //{
            //    case "su" or "hi":
            //        patch = "Trisaw";
            //        break;
            //
            //    case "gw" or "sh":
            //        patch = "Bell";
            //        break;
            //
            //    case "ss" or "sb" or "sl":
            //        patch = "Litri";
            //        break;
            //
            //    case "cc" or "si":
            //        patch = "Sine";
            //        break;
            //
            //    case "ds" or "lf" or "uw":
            //        patch = "Clar";
            //        break;
            //    default:
            //        patch = "Trisaw";
            //        break;
            //}
            //cc(chimney cannopy)
            //ds(drainage system)
            //gw(garbage wastes)
            //hi(industrial complex)
            //lf(farm array)
            //sb("sb subterranian")
            //sh(shadow)
            //si(sky place)
            //sl(shoreline)
            //ss(fivebepples)
            //su(outskirts)
            //uw(underhang and wall)

            switch (length)
            {
                case "L":
                    switch (patch)
                    {
                        case "Trisaw":
                            //string[] keycodeNames = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" };
                            library[0] = C1LongTrisaw;
                            library[1] = C2LongTrisaw;
                            library[2] = C3LongTrisaw;
                            library[3] = C4LongTrisaw;
                            library[4] = C5LongTrisaw;
                            library[5] = C6LongTrisaw;
                            library[6] = C7LongTrisaw;
                            break;

                        case "Clar":
                            library[0] = C1LongClar;
                            library[1] = C2LongClar;
                            library[2] = C3LongClar;
                            library[3] = C4LongClar;
                            library[4] = C5LongClar;
                            library[5] = C6LongClar;
                            library[6] = C7LongClar;
                            break;

                        case "Litri":
                            library[0] = C1LongLitri;
                            library[1] = C2LongLitri;
                            library[2] = C3LongLitri;
                            library[3] = C4LongLitri;
                            library[4] = C5LongLitri;
                            library[5] = C6LongLitri;
                            library[6] = C7LongLitri;
                            break;

                        case "Sine":
                            library[0] = C1LongSine;
                            library[1] = C2LongSine;
                            library[2] = C3LongSine;
                            library[3] = C4LongSine;
                            library[4] = C5LongSine;
                            library[5] = C6LongSine;
                            library[6] = C7LongSine;
                            break;

                        case "Bell":
                            library[0] = C1LongBell;
                            library[1] = C2LongBell;
                            library[2] = C3LongBell;
                            library[3] = C4LongBell;
                            library[4] = C5LongBell;
                            library[5] = C6LongBell;
                            library[6] = C7LongBell;
                            break;
                    }
                    break;
                case "M":
                    switch (patch)
                    {
                        case "Trisaw":
                            library[0] = C1MediumTrisaw;
                            library[1] = C2MediumTrisaw;
                            library[2] = C3MediumTrisaw;
                            library[3] = C4MediumTrisaw;
                            library[4] = C5MediumTrisaw;
                            library[5] = C6MediumTrisaw;
                            library[6] = C7MediumTrisaw;
                            break;

                        case "Clar":
                            library[0] = C1MediumClar;
                            library[1] = C2MediumClar;
                            library[2] = C3MediumClar;
                            library[3] = C4MediumClar;
                            library[4] = C5MediumClar;
                            library[5] = C6MediumClar;
                            library[6] = C7MediumClar;
                            break;

                        case "Litri":
                            library[0] = C1MediumLitri;
                            library[1] = C2MediumLitri;
                            library[2] = C3MediumLitri;
                            library[3] = C4MediumLitri;
                            library[4] = C5MediumLitri;
                            library[5] = C6MediumLitri;
                            library[6] = C7MediumLitri;
                            break;

                        case "Sine":
                            library[0] = C1MediumSine;
                            library[1] = C2MediumSine;
                            library[2] = C3MediumSine;
                            library[3] = C4MediumSine;
                            library[4] = C5MediumSine;
                            library[5] = C6MediumSine;
                            library[6] = C7MediumSine;
                            break;

                        case "Bell":
                            library[0] = C1MediumBell;
                            library[1] = C2MediumBell;
                            library[2] = C3MediumBell;
                            library[3] = C4MediumBell;
                            library[4] = C5MediumBell;
                            library[5] = C6MediumBell;
                            library[6] = C7MediumBell;
                            break;

                    }
                    break;
                case "S":
                    switch (patch)
                    {
                        case "Trisaw":
                            library[0] = C1ShortTrisaw;
                            library[1] = C2ShortTrisaw;
                            library[2] = C3ShortTrisaw;
                            library[3] = C4ShortTrisaw;
                            library[4] = C5ShortTrisaw;
                            library[5] = C6ShortTrisaw;
                            library[6] = C7ShortTrisaw;
                            break;

                        case "Clar":
                            library[0] = C1ShortClar;
                            library[1] = C2ShortClar;
                            library[2] = C3ShortClar;
                            library[3] = C4ShortClar;
                            library[4] = C5ShortClar;
                            library[5] = C6ShortClar;
                            library[6] = C7ShortClar;
                            break;

                        case "Litri":
                            library[0] = C1ShortLitri;
                            library[1] = C2ShortLitri;
                            library[2] = C3ShortLitri;
                            library[3] = C4ShortLitri;
                            library[4] = C5ShortLitri;
                            library[5] = C6ShortLitri;
                            library[6] = C7ShortLitri;
                            break;

                        case "Sine":
                            library[0] = C1ShortSine;
                            library[1] = C2ShortSine;
                            library[2] = C3ShortSine;
                            library[3] = C4ShortSine;
                            library[4] = C5ShortSine;
                            library[5] = C6ShortSine;
                            library[6] = C7ShortSine;
                            break;

                        case "Bell":
                            library[0] = C1ShortBell;
                            library[1] = C2ShortBell;
                            library[2] = C3ShortBell;
                            library[3] = C4ShortBell;
                            library[4] = C5ShortBell;
                            library[5] = C6ShortBell;
                            library[6] = C7ShortBell;
                            break;

                    }
                    break;
            }
            return library;
        }
        private int Peeps(int low, int high)
        {

            int tlow = Peep(low);
            int thigh = Peep(high);

            if (tlow == thigh) { thigh++; }

            int lol = UnityEngine.Random.Range(tlow, thigh);

            return lol;
        }
        private int Peep(int value)  //marked for die
        {

            if (agora <= 1) { phob = 1; }
            if (agora > 1) { phob = (float)(Mathf.Log((float)(agora * 0.8)) / 4.5 + 1); }

            //RainMeadow.Debug(phob);
            float fvalue = value;
            float avalue = fvalue / phob;

            string st1 = avalue.ToString();
            //RainMeadow.Debug($"{st1}, Peep");

            //RainMeadow.Debug(st1);
            int PointPos = st1.IndexOf('.');

            //RainMeadow.Debug($"PointPos, Funny");
            if (PointPos == -1) { st1 += ".00000"; }
            else
            {
                string lettersafterpoint = st1.Substring(PointPos);
                int lettersamount = lettersafterpoint.Length - 1;

                switch (lettersamount)
                {
                    case 4:
                        st1 += "0";
                        break;
                    case 3:
                        st1 += "00";
                        break;
                    case 2:
                        st1 += "000";
                        break;
                    case 1:
                        RainMeadow.Debug("what");
                        st1 += "00000";
                        break;
                    default:
                        break;
                }

                //RainMeadow.Debug($"{st1}, Peep 2");
            }

            string[] parts = st1.Split('.');
            int former = int.Parse(parts[0]);
            string latter = parts[1].Substring(0, 5);
            int latterint = int.Parse(latter);

            //int dicedint = UnityEngine.Random.Range(0, 100000);
            int dicedint = UnityEngine.Random.Range(0, 100000);

            //1.99999 latter
            //  44246 diced
            if (latterint > dicedint) { former++; }
            return former;

        }
        private void Plop(string input, VirtualMicrophone mic)
        {
            RainMeadow.Debug("It plays the plop " + input);
            string[] parts = input.Split('-');

            string slib = parts[0]; //either L for Long, M for Medium, or S for Short
            int oct = int.Parse(parts[1]);
            int ind;
            bool intiseasy = int.TryParse(parts[2], out ind);
            if (weplipping) if (intiseasy) if (3 == UnityEngine.Random.Range(1, 4)) Dust.Add(input);

            //RainMeadow.Debug($"So the string is {s}, which counts as {parts.Length} amounts of parts. {slib}, {oct}, {ind}");

            SoundID[] slopb = SampDict(slib);

            //SoundID sampleused = slopb[oct]; //one octave higher
            SoundID sampleused = slopb[oct - 1];
            //RainMeadow.Debug("Octave integer " + oct + ". sampleused: " + sampleused);
            //RainMeadow.Debug($"It uses the sample {sampleused}");
            int extratranspose = 0;
            if (!intiseasy)
            {
                string appends = parts[2].Substring(1);
                foreach (char letter in appends)
                {
                    switch (letter)
                    {
                        case 'b':
                            extratranspose--;
                            break;
                        case '#':
                            extratranspose++;
                            break;
                    }
                }

                ind = int.Parse(parts[2].Substring(0, 1));
            }

            int transposition = IndexTOCKInt(ind);
            if (!intiseasy)
            {
                transposition += extratranspose;
                if (transposition > 11) { transposition -= 12; }
                if (transposition < 0) { transposition += 12; }
            }

            float speeed = 1;

            speeed *= Mathf.Pow(magicnumber, transposition);

            // get intensity and turn that into too 
            // (which will also be reverb effect here then)

            float humanizingrandomnessinvelocitylol = UnityEngine.Random.Range(360, 1001) / 1000f;
            PlayThing(sampleused, humanizingrandomnessinvelocitylol, speeed, mic);

        }
        private void Plip(string input, float velocity, VirtualMicrophone mic)
        {
            //hellllllllllllllllllllllllllllllllllllllllllllllllllllllllllllllllllllllll
            myblue += velocity * 0.5f;
            string s = input.ToString();

            string[] parts = s.Split('-');

            string slib = parts[0]; //either L for Long, M for Medium, or S for Short
            int oct = int.Parse(parts[1]);
            int ind;
            bool intiseasy = int.TryParse(parts[2], out ind);
            int extratranspose = 0;
            if (!intiseasy)
            {
                string appends = parts[2].Substring(1);
                foreach (char letter in appends)
                {
                    switch (letter)
                    {
                        case 'b':
                            extratranspose--;
                            break;
                        case '#':
                            extratranspose++;
                            break;
                    }
                }

                ind = int.Parse(parts[2].Substring(0, 1));
            }
            SoundID[] slopb = SampDict(slib);
            SoundID sampleused = slopb[oct - 1];
            int transposition = IndexTOCKInt(ind);
            if (!intiseasy)
            {
                transposition += extratranspose;
                if (transposition > 11) { transposition -= 12; }
                if (transposition < 0) { transposition += 12; }
            }
            float speeed = 1 * Mathf.Pow(magicnumber, transposition);
            PlayThing(sampleused, velocity, speeed, mic);

        }
        private void PushModulation()
        {
            int dicedsign = UnityEngine.Random.Range(-5, 3);
            if (dicedsign <= 0) dicedsign = -1; else dicedsign = 1; //6/8th chance to go downwards, unstable by choice
            int dicedint = UnityEngine.Random.Range(0, 777) / (Math.Abs(QueuedModulation) + 1);
            int deadint = CurrentKey; //RainMeadow.Debug thing
            if (dicedint <= 77 && dicedint > 44) CurrentKey += 1 * dicedsign;
            if (dicedint <= 44 && dicedint > 7) CurrentKey += 2 * dicedsign;
            if (dicedint <= 7) CurrentKey += 3 * dicedsign;

            if (UnityEngine.Random.Range(0, 101) < 4)
            {
                if (inmajorscale)
                {
                    inmajorscale = false;
                    RainMeadow.Debug("Minor scale now");
                    //weplipping = false;
                }
                else
                {
                    inmajorscale = true;
                    RainMeadow.Debug("Major scale now");
                    //weplipping = true;
                }
            }


            RainMeadow.Debug($"The chance rolled {dicedint}, modified by {QueuedModulation}, it goes to {dicedsign}. So it was {deadint} and now is {CurrentKey}");
            QueuedModulation = 0;

            //CurrentKey += QueuedModulation;

            switch (CurrentKey)
            {
                case -7:
                    CurrentKey = 5;
                    break;
                case -8:
                    CurrentKey = 4;
                    break;
                case -9:
                    CurrentKey = 3;
                    break;
                case 7:
                    CurrentKey = -5;
                    break;
                case 8:
                    CurrentKey = -4;
                    break;
                case 9:
                    CurrentKey = -3;
                    break;
                default:
                    break;
            }
            ChitChat.Analyze(this); //if made into a 12 tone temprement, maybe analyze will  be readjusted to remove duplicates, or muting the duplicates, or other stuff with them
        }
        private void PlayEntry(VirtualMicrophone mic)
        {
            //RainMeadow.Debug($"yo sup dude,{EntryRequest} {UpcomingEntry} {chordqueuedentry} {entrychord} {entryriff} {playingchord} {playingriff}");
            if (EntryRequest == true)
                for (int i = 0; i < ChordInfos.GetLength(0); i++)
                {
                    //RainMeadow.Debug($"Nuclear {UpcomingEntry} vs Coughing {ChordInfos[i, 0]}... Round {i}, begin!");
                    if (UpcomingEntry == ChordInfos[i][0])
                    {
                        RainMeadow.Debug($"So it's gonna start a {UpcomingEntry}, with the key {CurrentKey}");
                        //RainMeadow.Debug($"{ChordInfos[i, 0]},{ChordInfos[i, 1]},{ChordInfos[i, 2]},{ChordInfos[i, 3]}");

                        switch (ChordInfos[i][1])
                        {
                            case "Chord":
                                //sowhatwasthechorddude = UpcomingEntry;
                                chordnotes = ChordInfos[i][2];
                                chordleadups = ChordInfos[i][3];
                                entrychord = true;
                                break;
                            case "Riff":
                                riffline = ChordInfos[i][2];
                                riffleadups = ChordInfos[i][3];
                                //RainMeadow.Debug(ChordInfos[i, 3] +" " + riffleadups);
                                entryriff = true;
                                break;
                        }
                    }
                }
            //RainMeadow.Debug("Done with entryrequest");
            if (EntryRequest == true && entrychord == true)
            {
                //playing a chord
                //RainMeadow.Debug("Starts the chord: " + UpcomingEntry + " " + chordnotes + "    and leadup: " + chordleadups);
                myred += 2f;
                PushModulation();
                ChitChat.Wipe(this);
                ChitChat.RandomMode();
                EntryRequest = false;
                entrychord = false;
                string[] inst = chordnotes.Split(',');

                string chord = inst[0];
                string bass = inst[1];

                string[] notes = chord.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < notes.Length; i++)
                {
                    Plop(notes[i], mic);
                    if (UnityEngine.Random.Range(0, 100) > 69) ChitChat.Add(notes[i].Substring(2), this);
                    //RainMeadow.Debug($"It is playing the Notes?{chord},{notes.Length},{i}, {notes[i]}... {RainMeadow.Debugtimer}");    
                }
                //RainMeadow.Debug($"done playing them???{EntryRequest}");                                  !!!!!!!!!!
                //string[] bassnotes = bass.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string[] bassnotes = bass.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int sowhichoneisitboss = UnityEngine.Random.Range(0, bassnotes.Length);

                Plop(bassnotes[sowhichoneisitboss], mic); //THIS is where it fucked up, which was because it had a space before the comma
                //RainMeadow.Debug("And i played a Bass note");


                //all notes have been played, moving onto liason

                string[] leadups = chordleadups.Split('|');
                int butwhatnowboss = UnityEngine.Random.Range(0, leadups.Length);
                string leadup = leadups[butwhatnowboss];

                string[] leadupinfo = leadup.Split(',');

                chordqueuedentry = leadupinfo[0];
                int low = int.Parse(leadupinfo[1]);
                int high = int.Parse(leadupinfo[2]);
                string liaisonnotes = leadupinfo[3];
                QueuedModulation = int.Parse(leadupinfo[4]);

                int madeupchordcountdown = Peeps(low, high);

                chordstopwatch = madeupchordcountdown * 4;
                if (liaisonnotes == "0")
                {
                    //hehe
                }
                else
                {
                    string[] thebabes = liaisonnotes.Split(' ');
                    foreach (string s in thebabes)
                    {
                        ChitChat.Add(s, this);
                    }
                }
                playingchord = true;
                //RainMeadow.Debug($"Info given of: Timer: {low} {high}, {chordstopwatch}, And times: {Ltime1}, {Ltime2}, {Ltime3}, and Key {CurrentKey} of chord (put another name here)... {RainMeadow.Debugtimer}");
            }
            //RainMeadow.Debug("Done with entrychord");

            if (playingchord == true)
            {
                //RainMeadow.Debug("It's playing liaisonnotes" + $": {Lslot1}, {Ltime1} {Lnote1}. {Lslot2}, {Ltime2} {Lnote2}. {Lslot3}, {Ltime3} {Lnote3}. ");

                if (chordstopwatch == 0)
                {
                    EntryRequest = true;
                    UpcomingEntry = chordqueuedentry;
                    //RainMeadow.Debug($"{UpcomingEntry} will play");       
                    playingchord = false;
                }
                else
                {
                    ChitChat.Update(mic, this);
                    chordstopwatch--;
                }
            }
            //RainMeadow.Debug("Done with Playingchord");

            if (EntryRequest == true && entryriff == true)
            {
                EntryRequest = false;
                entryriff = false;
                PushModulation();
                theline = riffline.Split(',');
                riffindex = 0;
                rifflength = theline.Length;
                playingriff = true;
            }

            if (playingriff)
            {
                if (inwaitmode)
                {
                    riffstopwatch--;//just to double check but 0 is the same as 1, you're delaying it whatever
                    if (riffstopwatch <= 0) inwaitmode = false; // :3
                }
                else
                {
                    if (riffindex < rifflength)
                    {
                        myred += 0.075f;
                        //if (pushingindex) { riffindex = queuedindex; pushingindex = false; }
                        //RainMeadow.Debug("Started they thing");
                        //RainMeadow.Debug("hullo");
                        //randomise it, if it's an array, then also remove extras if else
                        //RainMeadow.Debug($"{riffindex}, {rifflength}, {riffcurrentvar}, {theline}");
                        //RainMeadow.Debug(splitvar[0]);
                        //RainMeadow.Debug(splitvar.Length);
                        riffcurrentvar = theline[riffindex];
                        RainMeadow.Debug("Currently treating " + riffindex + ". With currentvar: " + riffcurrentvar);
                        string[] splitvar = riffcurrentvar.Split(' ');
                        int whichofthese = UnityEngine.Random.Range(0, splitvar.Length);
                        string treatedvar = splitvar[whichofthese];

                        //RainMeadow.Debug("hello");
                        //testing if it's just a number
                        bool umitsanumber = int.TryParse(treatedvar, out int intivarp);
                        if (umitsanumber)
                        {
                            riffstopwatch = intivarp;
                            inwaitmode = true;
                        }
                        else
                        {
                            RainMeadow.Debug(treatedvar);
                            if (treatedvar.Contains("loop"))
                            {
                                RainMeadow.Debug("Matched it as a loop");
                                if (islooping)
                                {
                                    loopcountdown--;
                                    if (loopcountdown > 0)
                                    {
                                        //queuedindex = riffindex - tilestasked;
                                        //pushingindex = true;
                                        riffindex -= tilestasked + 1;
                                        RainMeadow.Debug($"Went backwards {tilestasked} to {riffindex}");
                                    }
                                    if (loopcountdown <= 0)
                                    {
                                        islooping = false;
                                    }
                                    RainMeadow.Debug("Done with islooping, looping countdown is " + loopcountdown);
                                }
                                //finish the timeloop by not doin anythin
                                else
                                {
                                    //start the timeloop of the things
                                    string[] Supdude = treatedvar.Split(new string[] { "loop" }, StringSplitOptions.None);

                                    tilestasked = int.Parse(Supdude[0]);
                                    loopcountdown = int.Parse(Supdude[1]);
                                    islooping = true;
                                    riffindex -= tilestasked + 1; //the extra 1 is to compensate for riffindex being ++;'d at the end, it goes 5 backwards FROM this one, 1 will be 1 back
                                    RainMeadow.Debug($"He thinks he's {riffindex}, {tilestasked}");
                                }
                            }
                            if (treatedvar.Contains("d"))
                            {
                                char lollollol = treatedvar[1];

                                switch (lollollol)
                                {
                                    case '=':
                                        riffd = float.Parse(treatedvar.Substring(2));
                                        break;
                                    case '+':
                                        riffd += float.Parse(treatedvar.Substring(2));
                                        break;
                                    case '-':
                                        riffd -= float.Parse(treatedvar.Substring(2));
                                        if (riffd < 0)
                                            riffd = 0;
                                        break;
                                    case '*':
                                        //hehehehe hellothere fuck uuu >:))))))
                                        riffd *= float.Parse(treatedvar.Substring(2));
                                        break;
                                    case '/':
                                        if (riffd != 0 || float.Parse(treatedvar.Substring(2)) != 0.0f)
                                            riffd /= float.Parse(treatedvar.Substring(2));
                                        break;
                                    default:
                                        break;
                                }
                                riffstopwatch = (int)Math.Round(riffd, 0);
                                RainMeadow.Debug($"Matched it as a Delta, waiting for {riffd}, {riffstopwatch}");
                                inwaitmode = true;
                            }

                            if (treatedvar.Contains("!"))
                            {
                                RainMeadow.Debug("Matched it as a chorder, the leadups are");
                                EntryRequest = true;
                                //RainMeadow.Debug(riffleadups);
                                string[] leadups = riffleadups.Split('|');
                                RainMeadow.Debug("Splits it up");
                                //for (int i = 0; i < leadups.Length - 1; i++)
                                //{
                                //    RainMeadow.Debug(leadups[i]);
                                //}
                                int butwhatnowboss = UnityEngine.Random.Range(0, leadups.Length);
                                //RainMeadow.Debug("Picks a random one");
                                string leadup = leadups[butwhatnowboss];
                                //RainMeadow.Debug("Picks " + leadup);
                                UpcomingEntry = leadup;
                                //RainMeadow.Debug(riffleadups + " " + leadups + " "+ butwhatnowboss + " " + leadup + " " + UpcomingEntry);
                            }
                            if (treatedvar.Contains("L-") || treatedvar.Contains("M-") || treatedvar.Contains("S-"))
                            {
                                RainMeadow.Debug("Matched it as a noter");
                                //will assume its a note for now
                                treatedvar = treatedvar.ToString();
                                Plop(treatedvar, mic);
                            }
                            if (treatedvar.Contains("D-"))
                            {

                                RainMeadow.Debug("Matched it as a Dynamic noter");
                                var riffnextvar = theline[riffindex + 1];
                                RainMeadow.Debug("Predicting future index to be " + riffindex + "+1. With thenextvar being: " + riffnextvar);
                                string[] splitnextvar = riffnextvar.Split(' ');
                                int whichofthesenexts = UnityEngine.Random.Range(0, splitnextvar.Length);
                                string treatednextvar = splitnextvar[whichofthesenexts];

                                int intinextvarp;
                                bool umnextanumber = int.TryParse(treatednextvar, out intinextvarp);
                                if (umnextanumber == true)
                                {
                                    upcomingdelay = intinextvarp;
                                }
                                else
                                {
                                    if (treatednextvar.Contains("d"))
                                    {
                                        {
                                            char lollollol = treatednextvar[1];
                                            float dummyriffd = riffd;
                                            switch (lollollol)
                                            {
                                                case '=':
                                                    dummyriffd = float.Parse(treatednextvar.Substring(2));
                                                    break;
                                                case '+':
                                                    dummyriffd += float.Parse(treatednextvar.Substring(2));
                                                    break;
                                                case '-':
                                                    dummyriffd -= float.Parse(treatednextvar.Substring(2));
                                                    if (dummyriffd < 0)
                                                        dummyriffd = 0;
                                                    break;
                                                case '*':
                                                    //hehehehe hellothere fuck uuu >:))))))
                                                    dummyriffd *= float.Parse(treatednextvar.Substring(2));
                                                    break;
                                                case '/':
                                                    if (dummyriffd != 0 || float.Parse(treatednextvar.Substring(2)) != 0.0f)
                                                        dummyriffd /= float.Parse(treatednextvar.Substring(2));
                                                    break;
                                                default:
                                                    break;
                                            }
                                            upcomingdelay = (int)Math.Round((double)dummyriffd, 0);
                                            RainMeadow.Debug($"Matched it as a Delta, waiting for {riffd}, {riffstopwatch}");
                                        }
                                    }
                                }
                                treatedvar = treatedvar.Substring(1);
                                int currentsounds = mic.soundObjects.Count;
                                RainMeadow.Debug("I have calculated upcomingdelay to be " + upcomingdelay + " and the amount of currently to be " + currentsounds);
                                if (upcomingdelay < 3.5f || currentsounds > 22)//either 3 or 2
                                {
                                    treatedvar = "S" + treatedvar;
                                }
                                else if (upcomingdelay >= 3 && upcomingdelay < 55 || currentsounds > 17) //either 75 or 40, or 55
                                {
                                    treatedvar = "M" + treatedvar;
                                }
                                else //(upcomingdelay >= 75)
                                {
                                    treatedvar = "L" + treatedvar;
                                }
                                //RainMeadow.Debug(treatedvar);
                                Plop(treatedvar, mic);
                            }
                        }
                        //RainMeadow.Debug("HEY THIS ONE DOES THE THING IT*S COOL");
                        riffindex++;
                    }
                    else
                    {
                        playingriff = false;
                        //RainMeadow.Debug("it is OVER");
                    }
                }
            }
        }
        private void PlayThing(SoundID Note, float velocity, float speed, VirtualMicrophone virtualMicrophone)
        {

            //virtualMicrophone.PlaySound(Note, 0f, intensity*0.5f, speed);

            float pan = (float)(UnityEngine.Random.Range(-350, 351) / 1000f);
            float vol = velocity * 0.5f;
            float pitch = speed;

            //RainMeadow.Debug($"Trying to play a {Note}");
            try
            {
                if (virtualMicrophone.visualize)
                {
                    virtualMicrophone.Log(Note);
                }

                if (!virtualMicrophone.AllowSound(Note))
                {
                    RainMeadow.Debug($"Too many sounds playing, denying a {Note}");
                    return;
                }
                SoundLoader.SoundData soundData = virtualMicrophone.GetSoundData(Note, -1);
                if (virtualMicrophone.SoundClipReady(soundData))
                {

                    var thissound = new VirtualMicrophone.DisembodiedSound(virtualMicrophone, soundData, pan, vol, pitch, false, 0);

                    /*
                    var reverb = thissound.gameObject.AddComponent<AudioReverbFilter>();
                    reverb.reverbPreset = thepresets[lel];


                    //reverb.room             = 10000*((float)Math.Pow(intensity, 0.75)-1);
                    //reverb.reflectionsLevel = 10000*((float)Math.Pow(intensity, 0.75)-1);
                    //reverb.dryLevel         = 10000*((float)Math.Pow((-intensity+1.0), 0.75)-1);
                    
                    reverb.decayHFRatio         = revbvalues[0];
                    reverb.decayTime            = revbvalues[1];
                    reverb.density              = revbvalues[2];
                    reverb.diffusion            = revbvalues[3];
                    reverb.dryLevel             = revbvalues[4];
                    reverb.hfReference          = revbvalues[5];
                    reverb.lfReference          = revbvalues[6];
                    reverb.reflectionsDelay     = revbvalues[7];
                    reverb.reflectionsLevel     = revbvalues[8];
                    reverb.reverbDelay          = revbvalues[9];
                    reverb.reverbLevel          = revbvalues[10];
                    reverb.reverbPreset         = AudioReverbPreset.User;
                    reverb.room                 = revbvalues[12];
                    reverb.roomHF               = revbvalues[13];
                    reverb.roomLF               = revbvalues[14];
                    */


                    //RainMeadow.Debug(10000*((float)Math.Pow(intensity, 0.75)-1));
                    //RainMeadow.Debug(10000*((float)Math.Pow((-intensity+1.0), 0.75)-1));


                    //var delay = thissound.gameObject.AddComponent<AudioEchoFilter>();


                    virtualMicrophone.soundObjects.Add(thissound);
                }
                else
                {
                    RainMeadow.Debug($"Soundclip not ready");
                    return;
                }

                if (RainWorld.ShowLogs)
                {
                    //RainMeadow.Debug($"the note that played: {Note} at {speed}");
                }
            }
            catch (Exception e)
            {
                RainMeadow.Debug($"Log {e}");
            }

        }
        struct Flutter
        {
            public Flutter(string originalnote, string[] fluttertail, int stopwatch, int step, float basetime, float propacceleration)
            {
                this.originalnote = originalnote; //for safekeeping
                this.fluttertail = fluttertail; //the path that will follow
                this.stopwatch = stopwatch;
                this.step = step; //how many steps through the fluttertail has been taken (though zero isn't a good one)
                this.basetime = basetime;
                this.propacceleration = propacceleration;
            }
            public string originalnote; //just for safekeeping aye?
            public string[] fluttertail;//First in array is always nothing,   ...  first one in array is the one you start at
            public int stopwatch;
            public int step; //starts at 0,  ...     starts at -1
            public float basetime;
            public float propacceleration; //will multiply basetime by this
        }
        public static class Dust
        {
            static List<Flutter> flutters = new List<Flutter>();
            static List<int> slatedfordeletion = new List<int>();
            public static void Update(VirtualMicrophone mic, PlopMachine plopmachine)
            {
                if (flutters.Count != 0)
                {
                    for (int i = 0; i < flutters.Count; i++)
                    {
                        Flutter flutter = flutters[i];
                        if (flutter.stopwatch > 0)
                        {
                            int onelessnumbah = flutter.stopwatch - 1;
                            flutters[i] = new Flutter(flutter.originalnote, flutter.fluttertail, onelessnumbah, flutter.step, flutter.basetime, flutter.propacceleration);
                        }
                        else
                        {
                            if (mic.soundObjects.Count < 21)
                            {
                                flutter.step++;
                                plopmachine.Plip(flutter.fluttertail[flutter.step], 0.6f * Mathf.Lerp(1, 0, (float)flutter.step / flutter.fluttertail.Length), mic);
                                flutter.stopwatch = (int)flutter.basetime;
                                flutter.basetime *= flutter.propacceleration;
                                if (flutter.step == flutter.fluttertail.Length - 1) { slatedfordeletion.Add(i); }
                            } //this is lovely actually! Flutter is only *after* the sounds have played
                            else
                            {
                                RainMeadow.Debug($"Waiting on a note {flutter.originalnote}, has the index: {i}, with there being {mic.soundObjects.Count} amount of sounds currently occuptying");
                                flutter.stopwatch = (int)flutter.basetime * 2;
                                flutter.basetime *= 1.12f;
                            }
                            flutters[i] = new Flutter(flutter.originalnote, flutter.fluttertail, flutter.stopwatch, flutter.step, flutter.basetime, flutter.propacceleration);
                        }
                    }
                    if (slatedfordeletion.Count != 0)
                    {
                        slatedfordeletion.Reverse();
                        foreach (int deletion in slatedfordeletion)
                        {
                            RainMeadow.Debug("Does delete " + deletion);
                            flutters.RemoveAt(deletion);
                        }
                        slatedfordeletion.Clear();
                    }
                }
            }
            public static void Add(string note)
            {
                int TailLength = UnityEngine.Random.Range(3, 8);
                string[] notetail = new string[TailLength];

                string[] parts = note.Split('-');

                string slib = parts[0]; //useless
                int oct = int.Parse(parts[1]);
                int ind = int.Parse(parts[2]);

                for (int i = 0; i < TailLength; i++)
                {
                    ind += UnityEngine.Random.Range(-3, 3);

                    //while (1 > ind || ind > 7)
                    //{
                    if (ind > 7)
                    {
                        ind -= 7;
                        oct++;
                    }
                    if (ind < 1)
                    {
                        ind += 7;
                        oct--;
                    }
                    //}
                    if (oct < 1) oct++;
                    if (oct > 7) oct--;
                    string construction = "S-" + Convert.ToString(oct) + "-" + Convert.ToString(ind);
                    notetail[i] = construction;
                }

                //string[] notetail = { "S-4-5", "S-4-6", "S-4-7", "S-5-1" };

                int randomint = UnityEngine.Random.Range(12, 32); //6, 22
                float haha = UnityEngine.Random.Range(75, 135) / 100f;
                RainMeadow.Debug(haha);
                Flutter helo = new Flutter(note, notetail, randomint, -1, randomint, haha);
                flutters.Add(helo);
            }
        }
        struct Liaison
        {
            public Liaison(string note, int stopwatch, int yoffset)
            {
                this.note = note;
                this.stopwatch = stopwatch;
                this.yoffset = yoffset;
            }
            public string note;
            public int stopwatch;
            public int yoffset;
        }
        public static class ChitChat
        {
            static List<Liaison> LiaisonList = new List<Liaison>(); //list of the Liaison(s) currently playing
            static int[] liaisonrace = new int[0]; //arp pitch sorted array that will be remade with the analyze function
            static public bool isindividualistic = false; //setting for whether it'll treat things as individualistic
            static bool upperswitch = true;
            static int Idletimer = 0;
            static int uniqueyoffset = 0;
            public enum Arpmode
            {
                upwards,
                downwards,
                switchwards,
                randomset
            }
            static public Arpmode arpingmode = Arpmode.upwards;
            static int arpstep = 0;
            static public bool arpgoingupwards = false;
            static int arpcountdown = 20;
            static List<int> randomsetsacrificeboard = new List<int>();
            //static List<string> nameshaha = [];


            //public Liaison slot3; //public Liaison slot4; //public Liaison slot5; //public Liaison slot6; //public Liaison slot7; //public Liaison slot8;

            public static void Update(VirtualMicrophone mic, PlopMachine plopmachine)
            {
                Idletimer++;
                //if (LiaisonList.Count == 1) isindividualistic = true; //until a thing can grow horns on its own, it should stay like this... but then what if it could? What if a note had a chance to spawn others that fitted to it? Check that
                //this is also now also decided in Add function, instead of making it a wholey other thing, becaaaaause i'm lazy... why? because this doesn't hold the door open for ^^^this expansion
                if (isindividualistic) //upperswitch is true here
                {
                    Anarchy(mic, plopmachine);
                    upperswitch = true;
                }
                else //shall be false here
                {
                    if (upperswitch)
                    {
                        Analyze(plopmachine);
                        upperswitch = false;
                    }
                    arpcountdown--;
                    if (arpcountdown <= 0)
                    {
                        if (LiaisonList.Count != 0) //bruh why should it ever be less than zero lmao (that's the joke here)
                        {
                            plopmachine.mygreen += 0.3f;
                            arpcountdown = (int)(Math.Cos(Mathf.PerlinNoise(Idletimer / 400f, 7f + Idletimer / 1600f) * Math.PI) * 27 + 47);
                            arpcountdown = plopmachine.Peep(arpcountdown);
                            CollectiveArpStep(mic, plopmachine);
                        }
                    }
                }
            }

            private static void Anarchy(VirtualMicrophone mic, PlopMachine plopmachine)
            {
                for (int i = 0; i < LiaisonList.Count; i++)
                {
                    Liaison liaison = LiaisonList[i];
                    if (liaison.stopwatch > 0)
                    {
                        liaison.stopwatch--;
                        LiaisonList[i] = new Liaison(liaison.note, liaison.stopwatch, liaison.yoffset);
                    }
                    else
                    {
                        plopmachine.mygreen += 0.2f;
                        plopmachine.Plop(liaison.note, mic);
                        int moneydym1 = (int)(Math.Cos(Mathf.PerlinNoise(Idletimer / 400f, liaison.yoffset + Idletimer / 1600f) * Math.PI) * 69 + 107);
                        moneydym1 = plopmachine.Peep(moneydym1);
                        LiaisonList[i] = new Liaison(liaison.note, moneydym1, liaison.yoffset);
                        RainMeadow.Debug($"We're so here, playing a {liaison.note}, and their {liaison.yoffset} makes a delay of {moneydym1}");
                    }
                }
            }
            public static void Analyze(PlopMachine plopmachine)
            {
                List<int> LiaisonsFreqNumbs = new List<int>();
                int index = 0;
                foreach (Liaison heyo in LiaisonList)
                {
                    string[] hey = heyo.note.Substring(2).Split('-');//maybe this'll fuck up in the future :3
                    int freqnumb = int.Parse(hey[0]) * 100 + plopmachine.IndexTOCKInt(int.Parse(hey[1]));
                    LiaisonsFreqNumbs.Add(freqnumb);
                    //RainMeadow.Debug($"Car warranty {freqnumb}, It's index in the LiaisonList: {index}");
                    index++;
                }//there's ceraintly a better and less costly ways of going about but :PPPPPP

                int[] LiaisonIndexArrayThatllBeSwayed = new int[index];
                for (int i = 0; i < index; i++)
                    LiaisonIndexArrayThatllBeSwayed[i] = i;

                int[] LiaisonsFreqNumbsArray = LiaisonsFreqNumbs.ToArray();
                Array.Sort(LiaisonsFreqNumbsArray, LiaisonIndexArrayThatllBeSwayed);
                liaisonrace = LiaisonIndexArrayThatllBeSwayed;

            }
            public static void PrintRace()
            {
                RainMeadow.Debug("Liaisonrace being printed individually from left to right. The number is the index, the latter is what it represents.");
                RainMeadow.Debug("Remember that the sequence they're PRINTED in is the order of the liaisonrace, NOT the index shown(as that is just the pointer)");
                foreach (int i in liaisonrace)
                {
                    RainMeadow.Debug(i + " " + LiaisonList[i].note);
                }
            }
            public static void Add(string note, PlopMachine plopmachine)
            {
                //RainMeadow.Debug("SOthestart of add");
                string[] anotherhighernotesparts = note.Split('-');
                //RainMeadow.Debug(anotherhighernotesparts[0] + " " + anotherhighernotesparts[1]);
                int octave = int.Parse(anotherhighernotesparts[0]);
                int randomint = UnityEngine.Random.Range(octave, octave + 4);
                if (2 <= randomint && randomint <= 4)
                {
                    RainMeadow.Debug("Made it a shimmer power too");
                    string shimmernote = "M-" + Convert.ToString(octave + 1) + "-" + anotherhighernotesparts[1];  //reminder that i want this to be dynamic
                    uniqueyoffset += 10;
                    Liaison sup = new Liaison(shimmernote, plopmachine.Peeps(30, 80), uniqueyoffset);
                    LiaisonList.Add(sup);
                };
                bool willadd = true;
                string mynote = "M-" + note;
                uniqueyoffset += 10;
                Liaison helo = new Liaison(mynote, plopmachine.Peeps(30, 80), uniqueyoffset);

                //checks there's no duplicates, doesn't add if so
                foreach (Liaison thing in LiaisonList)
                {
                    if (thing.note == mynote) willadd = false;
                }

                if (willadd) LiaisonList.Add(helo);


                if (LiaisonList.Count < 3)
                {
                    isindividualistic = true;
                }
                else
                {
                    isindividualistic = UnityEngine.Random.Range(0, 100) > 44;
                }
                if (!isindividualistic) { Analyze(plopmachine); }
                //if (!isindividualistic) { RainMeadow.Debug($"Added {mynote}, a {helo.note} with analysis"); } else { RainMeadow.Debug($"Added {mynote}, a {helo.note} without analysis"); }
            }
            public static void Wipe(PlopMachine plopmachine)
            {
                LiaisonList.Clear();
                arpstep = 0;
                randomsetsacrificeboard.Clear();
                if (!isindividualistic) { Analyze(plopmachine); }
            }
            public static void RandomMode()
            {
                //switch statement that takes a number and changes
                //arpingmode to be: "upwards" "downwards" "switchwards" "random" "randomset"
                int sowhichoneboss = UnityEngine.Random.Range(0, 4);
                arpingmode = (Arpmode)sowhichoneboss;
            }
            public static void CollectiveArpStep(VirtualMicrophone mic, PlopMachine plopMachine)
            {
                //uses the logic of the mode selected by the string of arpingmode to *arp* between the notes,
                //plays the note that it's selected, then selects the next one
                //so, arpstep is the current step in the arpegio, normal
                //liaisonrace interprets the number and gives back another number which is the index of the note in liaison
                //when picking an index from the liaisonlist, it returns a Liaison, that liaison has got properties, one of them is its note, which'll be a string.

                //RainMeadow.Debug($"it plays a thing at uhhhh   {arpstep},,,,   then it's the index  {liaisonrace[arpstep]}.. .  ..  {LiaisonList[liaisonrace[arpstep]].note}");
                plopMachine.Plop(LiaisonList[liaisonrace[arpstep]].note, mic); //so it plays the previous one

                switch (arpingmode)
                {
                    case Arpmode.upwards:
                        arpstep++;
                        if (arpstep >= LiaisonList.Count) { arpstep = 0; }
                        break;

                    case Arpmode.downwards:
                        arpstep--;
                        if (arpstep < 0) { arpstep = LiaisonList.Count - 1; }
                        break;

                    case Arpmode.switchwards:
                        if (arpgoingupwards)
                        {
                            arpstep++;
                            if (arpstep >= LiaisonList.Count)
                            { //will have already played the top one, so back down it go
                                arpstep = LiaisonList.Count - 2;
                                arpgoingupwards = false;
                            }
                        }
                        else
                        {
                            arpstep--;
                            if (arpstep < 0)
                            {
                                arpstep = 1;
                                arpgoingupwards = true;
                            }
                        }
                        break;

                    case Arpmode.randomset:
                        if (randomsetsacrificeboard.Count == 0)
                        {
                            foreach (int i in liaisonrace)
                            {
                                randomsetsacrificeboard.Add(i);
                            }
                        }
                        int thesacrifice = UnityEngine.Random.Range(0, randomsetsacrificeboard.Count);
                        arpstep = liaisonrace[randomsetsacrificeboard[thesacrifice]];
                        randomsetsacrificeboard.RemoveAt(thesacrifice);
                        break;
                }
            }
        }
        //
        //With the help of henpe, i was able to register soundID's and have plopmachine playing in game. 
        //though he had one gripe: how arrhythmical it was. which pointed me towards
        //which begged the question, how quick will it be? this made the idea: dynamic progression. sometimes ambience-y, sometimes chord-y
        //the amount of features this idea way spawned has had me pseudocoding for 6 hours, it adds features to everything

        //So, after showing it of to henpemaz, they were all nice and chill but then they had their gripes as well,
        //apperantly it was quite the pointer to another idea i had had for a while but putting off: 
        //
        //Quantization update.
        //
        //well, so what henp's big gripe then was that there was randomness, and not the good kind. good kinds of randomness being:
        //The chords that are played
        //
        //uh,
        //yeah no just the notes
        //maybe the liaison picks? oh yeah no definetly the type of liaison that is being played (which is also something to take note of)
        //and also, the chord modulations, that's not too bad to have be random
        //and also the switch between major and minor
        //
        //everything else that is random, are the delays, ergo, the tempo,
        //and everything of that will have to be reworked, because that was the gripe, arythmetic sounds like too ass,
        //even though my lazy argument against it was that "it's like rain in the forest, and conversations are chittering randomly, 2.5 rule after all"
        //the rebuttal: "does rain sound like music"
        //(which by my definition yes fuck u henp lololol)
        //still, the point is that music most often follows a quantized grid that a bpm decides
        //and that the current production line has clockwatches set at a wholey anarchistic vibe
        //this is not the machine way.
        //we want the machine way.
        //.
        //(reminder that i want the thing to be able to switch between the machine and nature way)
        //
        //so lets ask ourselves the question:
        //what shall we doa bout it
        //
        //
        //
        //
        //          ----Fichtean Update. QUANTIZATION UPDATE.-----
        //
        //
        //with this quantization comes liveliness, which is a thing i want to make
        //
        //because with every bach piece, there's like, two different vibes to follow an intensity curve, a Fichtean curve apperantly, lets say calm v intense
        //but here, it's actually more like: ambiancy v chordy
        //ya see where i'm going, well, i don't 
        //those are the ends of a spectrum though - 0 to 1 - and the value inbetween them will be a slowly moving perlin noise value
        //all the different properties and stuff shall lerp this value in a respective way.
        //by the end of this, there shall be no more need for the liaisoninfo's between chords; as the delaytime, the liaisons, and the modulation is taken care of somewhere else
        //
        //
        //---ambiancy---
        //chords    :less frequent to make it more drawn out
        //liaisons  :also less frequent, but more of them, and with more complex arping
        //flutter   :longer tail, but (takes more time in between) steps, also more chance of them happening
        //
        //----chordy----.
        //chords    :more frequent,, bigger chance of sequencing..               like  [bam, bam, 0, bam, 0, 0, 0], [bam bam bam bam light-bam,,,,[bam],,,,,]
        //liaisons  :more frequent but smaller quantity, and simpler
        //flutter   :shorter tail, fast steps, less chance of them happening 
        //
        //
        //flutter *is* triggered by liaisons and chord notes the same, aye? so...
        //
        //
        //lets discuss the extra features:
        //
        //---Chords---
        //
        //so, there's this whole nother thing that i have elected to ignore for myself and i'm happy i still remember it now.
        //because, well, now it is.
        //
        //the scary thing about ambiency chords is the fear that there won't really be enough liaisons to carry it through the big empty pit
        //pluss, you can't really let too many notes by default in, because it'll overwhelm when you play chordie
        //even so, what if there's too few notes to be liaisoned over? what if you should want six notes when you only have four to choose:
        //
        //and thus i introduce: 
        //class LiaisonMagazineCreator
        //
        //the purpose of this creature will be that it'll make an array with a fitting amount of liaisons for the current livelyness
        //when there's too few liaisons, i want it to find out what note would also work in here, and create that.
        //      how? idk, well, easy but then a bit complicated cuz i want livelyness to have a say in which notes are preferred in created liaisons
        //      the more lively, the more chance it'll create new *off* notes
        //      it does a thing
        //      until theres's as many notes as requested, it'll try creating notes until it's made
        //the consequence of it will also be that we won't be needing to specify liaisons in the chord graph
        //
        //so, what playing a chord currently does is this:                 i can make it do this:
        //
        //takes the chord notes, splits them into notes and basses    |
        //plays all the notes (also adds some to chitchat)            |     adds all notes to chitchat
        //plays one bass                                              |
        //gets one of the leadups                                     |     gets just the name of one of the leadups, queues it up
        //starts dissecting leadup:                                   |
        //Lowest wait,                                                |     handled by below
        //Highest wait,                                               |     handled by below
        //modulation.                                                 |     handled by below
        //Plugs and utilises                                          |     handled by below
        //finds Extra Liaisons and adds them to chitchat              |     no need, the extras are figured by liaisonmagazinecreator
        //
        //
        //
        //
        //i want 0 - 1,  ambience - chord-y,  6 - 2 liaison
        //notes are entered to this thing as an oct-ind
        //
        //NoteMagazine.InNoteList = List<string> 
        //NoteMagazine.OutNoteList = List<string> 
        //bool hasdecidedamount
        //int decidedamount
        //
        //
        //NoteMagazine.Add(string)
        //  (this will add a note to the InNotelist)
        //
        //NoteMagazine.AddLoad(string)
        //  (this will add a note to the OutNoteList)
        //
        //NoteMagazine.Fester() (it is time to create the outnotes.) (very expensive here)
        //  if (hasdecidedamount){decidedamount = (roundddown)lerp(6.5,2,livingly) }
        //  while (OutNoteList.count() < decidedamount)
        //  {
        //      bool gonnadomultiple;
        //      if (InNoteList.count() = 1) { gonnadomultiple = false }
        //      else { gonnadomultiple = (Random(0,2) == 1) }
        //      
        //      if (gonnadomultiple) NoteMagazine.Grows();
        //      else NoteMagazine.Grow();
        //  }
        //    
        //  NoteMagazine.Push()
        //  
        //NoteMagazine.Grow(), 
        //  my baby boy
        //  you shall take 
        //  a nicenote, from innotelist
        //  store its octave, have it's ind
        //  input the octaveless thing as a key to the dictionary
        //  have the dictionary give you back a relative string.  (technically two, but you biasly get rid of one of them, and have picked one of the notes in the string)
        //  get the othernote it's looking for by refrencing nicenote, adding and compensating
        //  see if the note already exists in OutNoteList, if not, addload it 
        //  see if the note already exists in InNoteList, if not, add it 
        //
        //NoteMagazine.Grows(), 
        //  my baby boy
        //  you shall recieve
        //  two nicenotes, from innotelist
        //  count their octs*8 + inds*1, let the lowest be the rootnote, latter be the relnote
        //  the rootnote shall be 1-x, the relnote can vary in octave from the first, so it shall store how far away from the first it is, and say, well, 2-y, or 1-(x+4)
        //  
        //  input the octaveless things as a key to the dictionary
        //  have the dictionary give you back a relative string.  (technically two, but you biasly get rid of one of them, and have picked one of the notes in the string)
        //  get the othernote it's looking for by refrencing nicenote, adding and compensating
        //  see if the notes already exists in OutNoteList, addload the ones that don't
        //  see if the notes already exists in InNoteList, add the ones that don't
        //
        //NoteMagazine.Push()
        //  Takes all the notes and sends them over to ChitChat
        //  clears slotted innotes beforeso
        //  clears slooted outnotes as well
        //
        //this calls for a dictionary,like list.
        //all notes, and all two-notes, has an array of two strings: one for ambient easynote, one for chord-y dissonantnote.
        //when given this key^^^, it returns the array of two strings,
        //depending on livelyness, it'll be biased towards picking its thing (((-cos(livelyness*pi))^0.52)/2+0.5
        //
        //
        //
        //
        //
        //
        //
        //---time to speak wait and delay
        //:maxwait~=32bars
        //
        //the idea that popped up is:
        //chord exhaustion: the more chords that are played, the longer they'll wait for the next set
        //so, when a chord is played, it'll increase chordexhaustion(float).
        //
        //big brainwave,playing chords in: 
        //ambiancy will increment chordexhaustion a lot      /  //////un-exhausts slowly     0.975
        //chordy   will increment chordexhaustion a little   /  //////un-exhausts rapidly    0.935
        //
        //while playing chords, there should be a chance it'll ignore it's exhaustion, so that it can play stuff sequencially
        //only a human power(no special treatment): chords played in sequence still increment chordexhaustion
        //once it's started waiting, it shant stop
        //
        //
        //so, how does this *realllllly* work. how will it be inserted into the other code and shit
        //well, ok, lets not discuss how it's inserted, but still how it works or smth
        //
        //so we have the variable "livelyness" or smth, 0 is ambiancy, 1 is chordy, slow, fast
        //
        //we want to make it so that when livelyness is higher, chords play more frequently, and may have a higher chance of sequencing, yet even the highest one has 
        //      so, when it's low,  chords   play     unfrequently          sometimes with      sequences      but even the sequences are slow
        //      when it's high,   chords     play   more often       though   with breaks and that            sequences make big pauses too
        //
        //
        //0 = ambiency,  1 = chordy
        //
        //every chord is independant, funnily enough, though present is made up of past.
        //the present made would be made up of. chordexhaustion
        //
        //
        //sequencedchord = false
        //everytime the chord is played it does a 
        //      chordexhaustion^2 + chordexhaustion + (lerp(0.5, 3, livelyness))
        //the sequencing code::
        //            
        //      i want the chance for it to be a sequenced chord to be dependant on two things:
        //          1: Livelyness, the bigger, the more chance
        //          2: chordexhaustion, the more, the less chacne
        //      and so i roll a number between 0-1.000 = feelingtopolay:
        //          threshholdtoplay = (0.9-(livelyness*0.9)) + chordexaustion*0.08
        //          if feelingtoplay > (thing(threshholdtoplay)), sequencedchord = true
        //            
        //      if (sequencedchord)
        //      1 = (rounded down) random(100, 500-(int)(livelyness*375))/100
        //      { thedelay = wait.bar(1); }
        //      else:
        //      the delay should be made into NOT the chordexhaustion
        //      chordexhaustion should be rounded into an int, then the delay shall be made
        //      the delay = wait.bar(int (int)chordexhaustion)
        //
        //at the update()
        //      chordexhaustion is proportionally reduced by *= lerp(0.975, 0.925, livelyness)
        //      chordtimer--;
        //
        //--------
        //
        //Liaisons:
        //
        //There's many ideas that popped up suddenly, all features that would exist to be complementing a long wait
        //Well, when they're implemented, they would just *be* more prevalent during long waits(ambiances).
        //
        //Most of these ideas bear to modify fundamental blocks of ChitChat
        //
        //  idea    :   having some liaisons evolve (change note),
        //                  or transposing key while waiting, or min/maj switch
        //  idea    :   chance of many liaisons being occationally strummed (lightly)
        //  idea    :   having the ChitChat arpmode change over time too
        //  idea    :   having ChitChat arps and strums interlink, so that a strum can change the arpmode, and have
        //              more flare too: that it pauses arp a bit before and after.
        //  idea    :   having the notes by ChitChat be played in creciendoes and the opposite              requires Plop update, if you still want flutter. and also a bit wacky
        //  idea    :   make more ChitChat arpmodes, some of which can play multiple notes and also wait    uuuu
        //  idea    :   add functionality in "collectivearpstep" to process and play multiple notes         oooh
        //
        //
        //so lets go through and think, what to set up for these things?
        //
        //
        //so, the first one has two different things cuz it's targetting two different areas, so we're dealing with two different places
        //
        //lets deal with what's affecting everything first! The key modulation / minmaj switch
        //So, when would this happen? After a while? When's a while? What's time?
        //We need to set a Chordstopwatch- actually, i just reazied everything i've used that counts down to 0 is a *timer*, not a stopwatch
        //still, for here, it would actually be nice to have a chordstopwatch (something that counts upwards) resetted to 0.
        //this could be called on wipe()
        //
        //we want to make it so that the more time(stopwatch) that goes, the more likely it is that the all get shifted. (want it to be most probably happen at 16bars(still a 1/4ths roll of it happening at all)
        //  a "shift" can be three things: either a modulation, a minmaj switch, or a ,,, transposition?
        //  a "shift" can result in three things happening: either a modulation (minmaj is in here), or a transposition
        //      if it gets a modulation, it calls Pushmodulation() like, 3 times. the modulation is permanent, as the key will simply be used by the next chord (don't gotta cuz pushmodulation does that)
        //      if it gets a transposition, every note in LiaisonList gets a flat or a sharp. this is temporary, as the liaisons are wipe();d by the next chord (must Analyze())
        //  whatever happens, log the change
        //      log the modulation by,, urm, referencing CurrentKey (I should probably just rework pushmodulation)
        //
        //oh, and when things get shifted, reset the timer so that it doesn't do it *many times*  
        //      have a chance(smaller chance the bigger stopwatch was) to make a new bar-timer which when elapsed, reverts the change
        //          if there was a modulation, add the inverse of the steps taken.
        //          if there was a transposition, SubString(0,3) all liaisonnotes (cut off flats sharps)
        //
        //
        //so, the evolving notes: (lol this do be collective thinking)
        //lets picture a four noted upwards arpegio
        //we want to make it so that all notes have a chance to somehow shift one up/downwards
        //      the chance any note will shift increases the longer it's been since the last shift (did i meantion we want a shiftstopwatch?, --reset to zero-- shiftstopwatch = debugtimer.)
        //          
        //      this chance shall be rolled after playing a note (it is much harder to modify the arp before, as it might break together by trying to analyze() it)
        //      the chance is, like, random(debugtimer-shiftstopwatch, debugtimer-shiftstopwatch+7000) > 7000+shiftstopwatch
        //      
        //      if it hits, ok. hopefully we know what index it is in the liaisonlist, cuz we gonna modify it to go either up or down    
        //          
        //          there's a hypothetical universe where a note shall is less likely to shift if already shifted. in that universe, it's a wheelspin with paid entry to who gets shifted.
        //          in that universe i'd require liaison to have a number to be resetted, "votes". and  the whole thing to count "totalvotes" 
        //          every time a note is played, we start the mechanism.
        //          we add the time it had waited (shh) to their "votes" and "totalvotes"
        //          roll (random(0, totalvotes) < their.votes), if less: {modify their.note; subtract their.votes from totalvotes; reset their.votes}
        //
        //this also points towards a hot new "ChitChat.Modify(int whichone)" method
        //
        //
        //
        //so, strums
        //well, 1 or 2 delay between strummed notes, somethighn like that 
        //ok so how does this really work
        //well, it'd be a new thing in ChitChat
        //ChitChat.strum();
        //      we want to make a function that, when called, will take all the notes in the chitchat, and play them in order like how you strum a guitar
        //      well, how you'd strum a guitar would be a bit different, aye? i mean, you can't strum a note that's right next to each other on a guitar.
        //      hmmm, lets not think that far, lets just play all the notes either upwards or downwards
        //      
        //      this'll need var
        //      int "strumindex", which will be used to pick from liaisonrace, and in/decremented every turn it plays a note
        //      string "strumdirection", to say it goes upwards or downwards.    i want the string picked to  be the opposite direction of what it is currently
        //      int strumtimer, which will be set when a note is played, and used to wait for a bit. It will be 1-3, perlinnoising to be em 
        //      bool strumming, true when it's strumming
        //      bool strumqueued, 
        //      int strumqueuetimer
        //
        //      so when this bool is false when this method is called, it'll: switch it on, find out what direction to play, set strumindex what's fitting (0 or max)... then do the same thing as if it was true once, (so set the delay as well)
        //
        //      i'll work out the *chance* if a strum happening later for now lets just say we queue it up manually
        //      
        //      this thing is just a *liaison thing*, only called within a liaison, in chitchat, nah it's in chitchat.update() it shall be called
        //      notes shall arp every eight(triplet) notes. 
        //          
        //          
        //      so, playing a note shall have a chance (that increases over time since chordstart) to queue up a strum
        //      {
        //          strumming = true;
        //          strumphase = strumphases.queued;
        //          strumqueuetimer = wait.bar(1,2)) 
        //          analyse what arpegiationmode is happening, and pick the opposite
        //              if arpegiation = upwards:  strumdirection = downwards..
        //              if it's swapwards, check if it was going upwards/downwards, pick downwards/upwards.. ChitChat.arpgoingupwards
        //              if random, pick a random direction
        //          depending on strumdirection, strumindex will be: "upwards" = 0, "downwards" = (liaisonlist.count-1) //(cuz 4 and then 3 is maxindex)
        //      }
        //
        //strumphases strumphase;
        //enum strumphases
        //{
        //  queued,
        //  playing,
        //  epilogue
        //}
        //
        //ChitChat.strum();
        //  switch (strumphase)
        //        case (strumphases.queued)
        //            strumqueuetimer--;
        //            if strumqueuetimer has reached zero: strum is no longer queued, it will start playing (strumplaying/strumstrumming = true, strumqueued = false, strumtimer = 0)
        //            if strumtimer = 0, {strumphase++; strumtimer = 0}
        //        
        //        case (strumphases.playing)
        //            if (strumtimer > 0)
        //                strumtimer--;
        //            else
        //                playsound liaisonrace(strumindex) (medium sound)
        //                strumtimer = int slowperlinnoise(1,3) 
        //                strumindex = (depending on direction, -- or ++)
        //                if strumindex is less than 0, or more than (liaisonlist.count-1)
        //                {
        //                    strumphase++;
        //                    strumsepiloguetimer = wait.untilbar();
        //                }
        //        case (strumphases.epilogue)
        //            if strumsepiloguetimer > 0
        //                strumsepiloguetimer--;   //maybe in the future we can do some BONUS DUCKS
        //            else
        //                strumming = false
        //
        //
        //
        //
        //in update()
        //  is arping
        //      if strumming
        //        chitchat.strum();
        //      else
        //      { do normal liaisoning things }
        //
        //
        //
        //still kinda wanna do a "multiplenote" rework on arps
        //      
        //
        //
        //
        //
        //
        //
        //ok just realized that quantizing stuff makes leeway for eazy drum,,,
        //
        //
        //
        //
        //
        //
        //-------
        //
        //Fetter:
        //
        //Fetter will be 32ths, but can transform into until16T()'s
        //
        //
        //sixtyfourthtriplet as real fast, 2
        //thirdysecond as a bit fast,      3
        //thirtysecondtriplet as normal,   4
        //sixteenth as really slow,        6
        //sixteenthtriplet is really slow  8
        //
        //int randomint = UnityEngine.Random.Range(12, 32); //6, 22
        //though this was the status quo beforeso, maybe i should reconsider, well then, maybe put like two steps above
        //
        //HIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII
        //thirtysecondtriplet as real fast, 4
        //sixteenth as a bit fast,          6
        //sixteenthtriplet as normal,       8
        //eight as really slow,             12
        //eigthtriplet is really slow       16
        //
        //
        //-----ambiancy-----
        //flutter   :longer tail, but (takes more time in between) steps, also more chance of them happening
        //----chordy----.
        //flutter   :shorter tail, fast steps, less chance of them happening 
        //
        //
        //so, how will we rework flutters to make this into smth
        //
        //    public Flutter(string originalnote, string[] fluttertail, int stopwatch, int step, float basetime, float propacceleration)
        //    {
        //        this.originalnote = originalnote; //for safekeeping
        //        this.fluttertail = fluttertail; //the path that will follow
        //        this.stopwatch = stopwatch;
        //        this.step = step; //how many steps through the fluttertail has been taken (though zero isn't a good one)
        //        this.basetime = basetime;
        //        this.propacceleration = propacceleration;
        //    }
        //    public string originalnote; //just for safekeeping aye?
        //    public string[] fluttertail;//First in array is always nothing,   ...  first one in array is the one you start at  // GOTTEN ANOTHER ONE
        //    public int stopwatch;                                                             // IS GOTTEN PUTTING BASE THROUGH A METHOD 
        //    public int step; //starts at 0,  ...     starts at -1                           //
        //    public float basetime;                                                          //IS NOT THE DELAYVALUE 
        //    public float propacceleration; //will multiply basetime by this                 //NO LONGER
        //
        //
        //the things we are modifying now is NOT originalnote, nor step, but we are 
        //fundimentally(LOLLL) altering how propacceleration is interacting with basetime, and thus setting the time for stopwatch, but eh
        //that it decided when the thing is built, first and foremost
        //and, well, then when stopwatch = basetime    *=  propacceleration
        //
        //
        //
        //
        //but whatever lets just start getting to the meat of it,, ,how is the time so different
        //well, we need to figure out how to make it be a quantized amount, so 
        //
        //if we go from the HIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII, we'll say that the first is a 1, and last is a 0 (chordy, ambienty)
        //and the current livelyness just makes the FALLOFFpoint from them,, as in it'll turn through a lerp,, (Fichtean, 0, 1, 0.3, 0.7) 
        //this will then be like,,,, added a number on top of every time,, (that's what propacceleration now becomes lol) ((no longer prop))
        //but then the big elephant: what  basenumber  constitutes  what  wait?
        //well, here=
        //
        //0-0.11    = 0
        //0.11-0.28 = 0.2
        //0.28-0.72 = 0.4
        //0.72-0.89 = 0.6
        //0.89-1    = 0.8
        //
        //
        //and thus the delaytimer generated is a method that takes in the thing and returns the other thing, as in
        //
        //private int flutternumber(float base)
        //{
        //      if (base <= 0.11f)                 { wait.until(thirtysecondtriplet, 1) }
        //      if (base > 0.11f && base <= 0.28f) { wait.until(sixteenth, 1) }
        //      if (base > 0.28f && base <= 0.72f) { wait.until(sixteenthtriplet, 1) }
        //      if (base > 0.72f && base <= 0.89f) { wait.until(eight, 1) }
        //      if (base > 0.89f)                  { wait.until(eigthtriplet, 1) }
        //}        
        //so basetime becomes that,, and propacceleration is rolled as a 
        //randomint(-22, 23) / 1000      0.022 
        //which is what the thing is added with every time
        //
        //
        //
        //uggghhhh the length of the tail,, Taillengthhhh  
        //randomint(8-(int)lichtean*6,16-(int)lichtean*8) 
        //
        //
        //
        //and uGHHHHHHH the chance of them... appppearrrrinnnngggg
        //
        //ummmmmmmm
        //if (weplipping) if (intiseasy) if (3 == UnityEngine.Random.Range(1, 4)) Dust.Add(input)
        //is the previous standard thing, but like,,,, 0 is ambient, 1 is chordy
        //
        //soooooooooo
        //if (weplipping) if (intiseasy) if (35-(Fichtean*25)<UnityEngine.Random.Range(0, 101))
        //
        //
        //i'm going to wake up to this mess of a pseudocode and want to die... future me's problem
        //
        //
        //
        //
        //
        //
        //let's discuss frequency : waits : delays
        //https://external-content.duckduckgo.com/iu/?u=http%3A%2F%2Fwww.songsofthecosmos.com%2Fimages%2Fexamples_of_triplets.jpg&f=1&nofb=1&ipt=0ba2b02132091e1f9db7a10d94a80c0f8a4f7af1fb63eeaeb6fce07be880b472&ipo=images
        //      the actual quantisation update 
        //
        //
        //                            a bar = 24*4 = 96
        //                           a half = 24*2 = 48
        //        a quartertriplet = 96/3 = 24*4/3 = 32
        //                           a quarternote = 24
        //                          a eighttriplet = 16
        //                                am eight = 12
        //                      a sixteenthtriplet = 8
        //                             a sixteenth = 6
        //                   a thirtysecondtriplet = 4
        //                          a thirtysecond = 3
        //                    a sixtyfourthtriplet = 2
        //          a hundredandtwentyeighttriplet = 1     //lmao
        //remember that when you have to wait for any of these, you'll need to subtract 1 from it, because you're using that 1 at the time of starting the wait.
        //
        //
        //
        //the universal clock: well, we'll calculate everything out of a bar, cuz triplets are smth
        //a clock will go around from being something, downwards to close to 0, but never 0.
        //imagine a timer with a 1 second timer, which takes 1 seconds of steps at a time.
        //if the clock was able to "count" 0, it would be alternating between 1 and 0, making it a 2 second loop
        //if it was only 0, that's dumb as shit and recursive, when would it loop? it would need to revert back  to -1? 
        //see, again, imagine the 1 second timer, but as a clock or eggwatch. when it has done one rotation, exactly when it's done it goes back around to 1
        //00:00 o' clock is the same as 24:00 o' clock. 
        //so what i'm saying is, we will reach 0, but that doesn't mean we count it, aka: When we reach 0, it loops-around/reverts-back-to-max *at the same time*
        //
        //
        //aka, every second: it decrements, 
        //      if it reached 0, reset to max
        //      
        //another way to think of it is like that annoying friend that halters at 1.
        //    "3.... 2.... 1.... (1....) 1.... 1.... 1...."
        //everytime he would've reached 0, he resets to 1, effectivly starting *a* 1 second loop.
        //like, it's the same with the other ones.
        //      "4... 3... 2... 1... 2... 1... (2... 1... )2... 1... " 2 second loop
        //      "4... 3... 2... 1... (4... 3... 2... 1...) 4... 3... 2... 1... " 4 BEAT loop,,  the nice segway
        // 
        // 
        //so lets talk about how to go about the quantization, the grid.
        //because i'm so fucking smart, i've made it so that every *beat* is comprised of 24 segments, which makes a bar(4 beats) divisable by three, and all that
        //what i want to figure out now would be 
        //      what is readposition (spoiler: it's where we are in a bar) ((read position as in: "This is the position we read whether there's a note or a wait"))
        //      how will the creature know where we are in a bar? (what's its current delta in relation to the bar's start)
        //      how can we know how far away from... a quarter, the current readposition is
        //
        //
        //a bar is 96 segments
        // 
        //lets find out where we are in that bar as an example at first, well, since we'll usually use the bartime or smth.
        //anyhoo 
        //yeah
        //
        //so, there's a debugtimer int var that increments every rwupdate(), that can be the universal timer the barstopwatch shall calculate from.
        //bartimer = 96 - (debugtimer % 96) 
        //
        //so, explenation=
        //debutimer = thing go upwards every frame
        //"%" will.. "the modulo operation returns the remainder or signed remainder of a division"
        //      here i'm using it so that every 96FFs, it resets back down to 0
        //i am subtracting 96 by this.
        //this will make bartimer be a timer that goes from 96 down to 1 and back around.
        // 
        //when debugtimer%96 = 0, that's the start of a bar. 
        //bartimer will be 96 then, which is the amount of segments from and with *now* to the next bar
        // 
        //ok, tragic turn of the fates, i don' really need a master *timer* or smth like that. you can calculate the distance "to the next one" with the same formula.
        //
        //
        //bar               timer = 96 - (debugtimer % 96) 
        //half               timer = 48 - (debugtimer % 48) 
        //quartertriplet      timer = 32 - (debugtimer % 32) 
        //quarternote          timer = 24 - (debugtimer % 24) 
        //eighttriplet          timer = 16 - (debugtimer % 16) 
        //eight                  timer = 12 - (debugtimer % 12) 
        //sixteenthtriplet        timer = 8  - (debugtimer % 8) 
        //sixteenth                timer = 6  - (debugtimer % 6) 
        //thirtysecondtriplet       timer = 4  - (debugtimer % 4) 
        //thirtysecond               timer = 3  - (debugtimer % 3) 
        //sixtyfourthtriplet          timer = 2  - (debugtimer % 2) 
        //hundredandtwentyeighttriplet timer = 1  - (debugtimer % 1)
        //
        //
        //then, lets discuss how one of these will be translated into a wait
        //      when i play a quarter note each bar, i am waiting for three quarters
        //      so if a queuetimer is set for 24FF when the note is played (and doesn't decrement it at the same update), and it'll play the next note when timer reaches 0, it wont be waiting a quarter note, it'll wait a quarternote + 1FF
        //      this can be mitigated by setting the wait to 23FF, or checking when it's 1 instead
        //      i will mitigate it by setting the wait to 23FF instead. (I will have all wait be 1FF less than total), because that's how notation works
        //      in a coding sense, every method call is a 1/4*24 th note, and decrementing a timer starts only after that. so a wait.bar is really a note, and 24*4-1 waits, and we are calculating on a grid of 24*4 then.
        //
        //wait.bar    just an int
        //wait.bar(1, 2)  ((useless, you could just time it outside) PSYCHE, not useless cuz a bar is 23FF really, this method would do a subtraction after finding it out)
        //wait.untilbar();  a method that returns an int
        //
        //so, i'm considering just making it implicit that uhhh, 
        //waiting multiple of a note always means you want to be *in line* with that note.
        //and through that, i wanna make a feat where like, wait.bar(3) means waiting for the bar that's 3 ahead (pluss your own)
        //meaning that you wanna wait 2 whole bars, + 1 wackybar.. (-1 FF for the time taken to consider this)
        //
        //
        //
        //wait.abar            public  var int frames for a bar, 96
        //wait.leftofbar();    private method that returns: int frames of wait after now until the next bar. (96 - (debugtimer % 96) - 1)
        //wait.untilbar(2);    public  method that returns: int frames of wait after now until the bar 2 after this one. (calculates untilbar + (int input-1) * abar)
        //wait.untilbar(1, 3); public  method that returns: the untilbar(of), a randomint between the two inputs.
        //
        //
        //public class wait
        //
        //
        //
        //      public int bar = 96  
        //      
        //      private int leftofbar()
        //          return ( abar - ( debugtimer % abar ) - 1);
        //      
        //      public int untilbar(int bars)
        //      {
        //          return (leftofbar() + (bars-1)*abar );
        //      }
        //
        //
        //
        //ok actually this is quite universal actually
        //
        //      Dictionary<string, int> waittypes = new();
        //      waittypes.Add("bar", 96)                         
        //      waittypes.Add("half", 48)    
        //      waittypes.Add("quarterT", 32)
        //      waittypes.Add("quarternote", 24)
        //      waittypes.Add("eightT", 16)
        //      waittypes.Add("eight", 12)
        //      waittypes.Add("sixteenthT", 8 )
        //      waittypes.Add("sixteenth", 6 )
        //      waittypes.Add("thirtysecondT", 4 )
        //      waittypes.Add("thirtysecond", 3 )
        //      waittypes.Add("sixtyfourthT", 2 )
        //      waittypes.Add("hundredandtwentyeightT", 1 )
        //
        //      private int leftof(string waittype)
        //      {
        //          return ( waittypes(waitttype) - (debugtimer % waittypes(waittype) ) - 1);
        //      }
        //
        //      public int until(string waittype, int waits)
        //      {
        //          if (wait >= 0) wait = 1;
        //          return (leftof(waittype) + (waits-1)*waittypes(waittype) );
        //      }
        //
        //      public int until(string waittype, int waitlow, int waithigh)
        //      {
        //          int thewait = unityengine.random.range(waitlow, waithigh+1)
        //          return (leftof(waittype) + (thewait-1)*waittypes(waittype) );
        //      }
        //
        //
        //



        Color mycolor = new(0f, 0f, 1f, 1f);
        float myred;
        float mygreen;
        float myblue;
        /*
        these lines i write to
        bring equality, and for
        my love of haikus
        */
        private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            debugtimer++;
            var mic = self.cameras[0].virtualMicrophone;
            CurrentRegion = self.world.region.name;
            CurrentRegion ??= "sl";
            PlayEntry(mic);
            Dust.Update(mic, this);
            //RainMeadow.Debug(MeadowMusic.vibeIntensity ??= 231);
            //RainMeadow.Debug(MeadowMusic.vibePan ??= 20);

            myred *= 0.97f;
            mygreen *= 0.963f;
            myblue *= 0.94f;
            if (myred == 0f && mygreen == 0f && myblue == 0f)
            {
                myred = 0.06f;
            }
            mycolor = new(myred, mygreen, myblue, 1f);

        }

        public static readonly SoundID HelloC = new SoundID("HelloC", register: true);
        public static readonly SoundID HelloD = new SoundID("HelloD", register: true);
        public static readonly SoundID HelloE = new SoundID("HelloE", register: true);
        public static readonly SoundID HelloF = new SoundID("HelloF", register: true);
        public static readonly SoundID HelloG = new SoundID("HelloG", register: true);
        public static readonly SoundID HelloA = new SoundID("HelloA", register: true);
        public static readonly SoundID HelloB = new SoundID("HelloB", register: true);
        public static readonly SoundID C1LongSine = new SoundID("C1LongSine", register: true);
        public static readonly SoundID C2LongSine = new SoundID("C2LongSine", register: true);
        public static readonly SoundID C3LongSine = new SoundID("C3LongSine", register: true);
        public static readonly SoundID C4LongSine = new SoundID("C4LongSine", register: true);
        public static readonly SoundID C5LongSine = new SoundID("C5LongSine", register: true);
        public static readonly SoundID C6LongSine = new SoundID("C6LongSine", register: true);
        public static readonly SoundID C7LongSine = new SoundID("C7LongSine", register: true);
        public static readonly SoundID C1MediumSine = new SoundID("C1MediumSine", register: true);
        public static readonly SoundID C2MediumSine = new SoundID("C2MediumSine", register: true);
        public static readonly SoundID C3MediumSine = new SoundID("C3MediumSine", register: true);
        public static readonly SoundID C4MediumSine = new SoundID("C4MediumSine", register: true);
        public static readonly SoundID C5MediumSine = new SoundID("C5MediumSine", register: true);
        public static readonly SoundID C6MediumSine = new SoundID("C6MediumSine", register: true);
        public static readonly SoundID C7MediumSine = new SoundID("C7MediumSine", register: true);
        public static readonly SoundID C1ShortSine = new SoundID("C1ShortSine", register: true);
        public static readonly SoundID C2ShortSine = new SoundID("C2ShortSine", register: true);
        public static readonly SoundID C3ShortSine = new SoundID("C3ShortSine", register: true);
        public static readonly SoundID C4ShortSine = new SoundID("C4ShortSine", register: true);
        public static readonly SoundID C5ShortSine = new SoundID("C5ShortSine", register: true);
        public static readonly SoundID C6ShortSine = new SoundID("C6ShortSine", register: true);
        public static readonly SoundID C7ShortSine = new SoundID("C7ShortSine", register: true);
        public static readonly SoundID C1LongLitri = new SoundID("C1LongLitri", register: true);
        public static readonly SoundID C2LongLitri = new SoundID("C2LongLitri", register: true);
        public static readonly SoundID C3LongLitri = new SoundID("C3LongLitri", register: true);
        public static readonly SoundID C4LongLitri = new SoundID("C4LongLitri", register: true);
        public static readonly SoundID C5LongLitri = new SoundID("C5LongLitri", register: true);
        public static readonly SoundID C6LongLitri = new SoundID("C6LongLitri", register: true);
        public static readonly SoundID C7LongLitri = new SoundID("C7LongLitri", register: true);
        public static readonly SoundID C1MediumLitri = new SoundID("C1MediumLitri", register: true);
        public static readonly SoundID C2MediumLitri = new SoundID("C2MediumLitri", register: true);
        public static readonly SoundID C3MediumLitri = new SoundID("C3MediumLitri", register: true);
        public static readonly SoundID C4MediumLitri = new SoundID("C4MediumLitri", register: true);
        public static readonly SoundID C5MediumLitri = new SoundID("C5MediumLitri", register: true);
        public static readonly SoundID C6MediumLitri = new SoundID("C6MediumLitri", register: true);
        public static readonly SoundID C7MediumLitri = new SoundID("C7MediumLitri", register: true);
        public static readonly SoundID C1ShortLitri = new SoundID("C1ShortLitri", register: true);
        public static readonly SoundID C2ShortLitri = new SoundID("C2ShortLitri", register: true);
        public static readonly SoundID C3ShortLitri = new SoundID("C3ShortLitri", register: true);
        public static readonly SoundID C4ShortLitri = new SoundID("C4ShortLitri", register: true);
        public static readonly SoundID C5ShortLitri = new SoundID("C5ShortLitri", register: true);
        public static readonly SoundID C6ShortLitri = new SoundID("C6ShortLitri", register: true);
        public static readonly SoundID C7ShortLitri = new SoundID("C7ShortLitri", register: true);
        public static readonly SoundID C1LongBell = new SoundID("C1LongBell", register: true);
        public static readonly SoundID C2LongBell = new SoundID("C2LongBell", register: true);
        public static readonly SoundID C3LongBell = new SoundID("C3LongBell", register: true);
        public static readonly SoundID C4LongBell = new SoundID("C4LongBell", register: true);
        public static readonly SoundID C5LongBell = new SoundID("C5LongBell", register: true);
        public static readonly SoundID C6LongBell = new SoundID("C6LongBell", register: true);
        public static readonly SoundID C7LongBell = new SoundID("C7LongBell", register: true);
        public static readonly SoundID C1MediumBell = new SoundID("C1MediumBell", register: true);
        public static readonly SoundID C2MediumBell = new SoundID("C2MediumBell", register: true);
        public static readonly SoundID C3MediumBell = new SoundID("C3MediumBell", register: true);
        public static readonly SoundID C4MediumBell = new SoundID("C4MediumBell", register: true);
        public static readonly SoundID C5MediumBell = new SoundID("C5MediumBell", register: true);
        public static readonly SoundID C6MediumBell = new SoundID("C6MediumBell", register: true);
        public static readonly SoundID C7MediumBell = new SoundID("C7MediumBell", register: true);
        public static readonly SoundID C1ShortBell = new SoundID("C1ShortBell", register: true);
        public static readonly SoundID C2ShortBell = new SoundID("C2ShortBell", register: true);
        public static readonly SoundID C3ShortBell = new SoundID("C3ShortBell", register: true);
        public static readonly SoundID C4ShortBell = new SoundID("C4ShortBell", register: true);
        public static readonly SoundID C5ShortBell = new SoundID("C5ShortBell", register: true);
        public static readonly SoundID C6ShortBell = new SoundID("C6ShortBell", register: true);
        public static readonly SoundID C7ShortBell = new SoundID("C7ShortBell", register: true);
        public static readonly SoundID C1LongClar = new SoundID("C1LongClar", register: true);
        public static readonly SoundID C2LongClar = new SoundID("C2LongClar", register: true);
        public static readonly SoundID C3LongClar = new SoundID("C3LongClar", register: true);
        public static readonly SoundID C4LongClar = new SoundID("C4LongClar", register: true);
        public static readonly SoundID C5LongClar = new SoundID("C5LongClar", register: true);
        public static readonly SoundID C6LongClar = new SoundID("C6LongClar", register: true);
        public static readonly SoundID C7LongClar = new SoundID("C7LongClar", register: true);
        public static readonly SoundID C1MediumClar = new SoundID("C1MediumClar", register: true);
        public static readonly SoundID C2MediumClar = new SoundID("C2MediumClar", register: true);
        public static readonly SoundID C3MediumClar = new SoundID("C3MediumClar", register: true);
        public static readonly SoundID C4MediumClar = new SoundID("C4MediumClar", register: true);
        public static readonly SoundID C5MediumClar = new SoundID("C5MediumClar", register: true);
        public static readonly SoundID C6MediumClar = new SoundID("C6MediumClar", register: true);
        public static readonly SoundID C7MediumClar = new SoundID("C7MediumClar", register: true);
        public static readonly SoundID C1ShortClar = new SoundID("C1ShortClar", register: true);
        public static readonly SoundID C2ShortClar = new SoundID("C2ShortClar", register: true);
        public static readonly SoundID C3ShortClar = new SoundID("C3ShortClar", register: true);
        public static readonly SoundID C4ShortClar = new SoundID("C4ShortClar", register: true);
        public static readonly SoundID C5ShortClar = new SoundID("C5ShortClar", register: true);
        public static readonly SoundID C6ShortClar = new SoundID("C6ShortClar", register: true);
        public static readonly SoundID C7ShortClar = new SoundID("C7ShortClar", register: true);
        public static readonly SoundID C1LongTrisaw = new SoundID("C1LongTrisaw", register: true);
        public static readonly SoundID C2LongTrisaw = new SoundID("C2LongTrisaw", register: true);
        public static readonly SoundID C3LongTrisaw = new SoundID("C3LongTrisaw", register: true);
        public static readonly SoundID C4LongTrisaw = new SoundID("C4LongTrisaw", register: true);
        public static readonly SoundID C5LongTrisaw = new SoundID("C5LongTrisaw", register: true);
        public static readonly SoundID C6LongTrisaw = new SoundID("C6LongTrisaw", register: true);
        public static readonly SoundID C7LongTrisaw = new SoundID("C7LongTrisaw", register: true);
        public static readonly SoundID C1MediumTrisaw = new SoundID("C1MediumTrisaw", register: true);
        public static readonly SoundID C2MediumTrisaw = new SoundID("C2MediumTrisaw", register: true);
        public static readonly SoundID C3MediumTrisaw = new SoundID("C3MediumTrisaw", register: true);
        public static readonly SoundID C4MediumTrisaw = new SoundID("C4MediumTrisaw", register: true);
        public static readonly SoundID C5MediumTrisaw = new SoundID("C5MediumTrisaw", register: true);
        public static readonly SoundID C6MediumTrisaw = new SoundID("C6MediumTrisaw", register: true);
        public static readonly SoundID C7MediumTrisaw = new SoundID("C7MediumTrisaw", register: true);
        public static readonly SoundID C1ShortTrisaw = new SoundID("C1ShortTrisaw", register: true);
        public static readonly SoundID C2ShortTrisaw = new SoundID("C2ShortTrisaw", register: true);
        public static readonly SoundID C3ShortTrisaw = new SoundID("C3ShortTrisaw", register: true);
        public static readonly SoundID C4ShortTrisaw = new SoundID("C4ShortTrisaw", register: true);
        public static readonly SoundID C5ShortTrisaw = new SoundID("C5ShortTrisaw", register: true);
        public static readonly SoundID C6ShortTrisaw = new SoundID("C6ShortTrisaw", register: true);
        public static readonly SoundID C7ShortTrisaw = new SoundID("C7ShortTrisaw", register: true);
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using HarmonyLib;
using Steamworks;
using static RainMeadow.PlopMachine;

namespace RainMeadow
{
    public class PlopMachine
    {
        public void OnEnable()
        {
            On.RainWorldGame.Update += RainWorldGame_Update; //actually usefull
            //On.PlayerGraphics.DrawSprites += hehedrawsprites;
            On.RainWorldGame.ctor += RainWorldGame_ctor; //actually usefull 
            On.AmbientSoundPlayer.TryInitiation += AmbientSoundPlayer_TryInitiation;
        }

        private void AmbientSoundPlayer_TryInitiation(On.AmbientSoundPlayer.orig_TryInitiation orig, AmbientSoundPlayer self)
        {
            //fuckoff
        }

        readonly float magicnumber = 1.0594630776202568303519954093385f;
        int CurrentKey = 0;
        int QueuedModulation = 0;
        int debugstopwatch = 0;
        /*
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
        */
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

        bool playingchord = false;

        string chordnotes = "yosup";
        string chordleadups = "yosup";

        int chordtimer = 0;

        string UpcomingEntry = "Balaboo";         //important to set a first one

        //bool weplipping = true;
        //stopwatches go upwards
        //timers go downwards

        float fichtean;
        float chordexhaustion = 1f;
        int chordsequenceiteration;
        // int chordwait;


        int agora = 1;
        float phob;

        //string teststring = "hehe";
        string CurrentRegion; //?

        //float distancetovibeepicentre = 0;

        //float intensity = 1.0f;

        bool fileshavebeenchecked = false;
        string[][] ChordInfos; //?

        private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig.Invoke(self, manager);
            RainMeadow.Debug("   ##                         ");
            RainMeadow.Debug("   ##    ###                  ");
            RainMeadow.Debug(" .###%######             #####");
            RainMeadow.Debug("#-##=.=####             ##### ");
            RainMeadow.Debug("#==#########         #######  ");
            RainMeadow.Debug("############       ########   ");
            RainMeadow.Debug("##########################    ");
            RainMeadow.Debug(" ########################     ");
            RainMeadow.Debug("  #####################       ");
            RainMeadow.Debug("   ##################         ");
            RainMeadow.Debug("  ################            ");
            RainMeadow.Debug("  ###    ###                  ");
            RainMeadow.Debug("   ##      ####               ");
            RainMeadow.Debug("   ###       ##               ");
            try
            {
                if (!fileshavebeenchecked)
                {
                    StartthefuckingWaitDict();
                    NoteMagazine.fuckinginitthatdictlineagebitch();
                    RainMeadow.Debug("Checking files");
                    string[] mydirs = AssetManager.ListDirectory("soundeffects", false, true);
                    RainMeadow.Debug("Printing all directories in soundeffects");
                    foreach (string dir in mydirs)
                    {
                        string filename = GetFolderName(dir);
                        //RainMeadow.Debug(filename + " Is one of the things it sees, straight from " + dir);
                        if (filename == "!Entries.txt")
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
            int[,] thescale = inmajorscale ? intsinkey : intsinmkey;
            int integer = thescale[treatedkey, index - 1];
            return integer;
        }
        private SoundID[] SampDict(string length)
        {
            RainMeadow.Debug($"It's trying to get length {length}");
            SoundID[] library = new SoundID[7]; //to do:  make better
            string acronym = CurrentRegion.ToUpper();
            MeadowMusic.VibeZone[] newthings;
            bool diditwork = MeadowMusic.vibeZonesDict.TryGetValue(acronym, out newthings);
            //we retrieve a newthings array (one of many vibezones)
            RainMeadow.Debug("C" + diditwork);
            RainMeadow.Debug(Newtonsoft.Json.JsonConvert.SerializeObject(newthings));
            if (!diditwork) { RainMeadow.Debug("itdidn'twork"); return null; }
            MeadowMusic.VibeZone newthing = newthings[0]; //TEMP DUMMY FOR UNTIL HELLOTHERE'S REQUIUM
                                                          //and pick the one that is closer
            RainMeadow.Debug("d");
            string patch = newthing.sampleUsed;
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
                            //here's how henp did it string[] keycodeNames = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" };
                            //SoundID[] library2 = { C1LongTrisaw, C2LongTrisaw, C3LongTrisaw, C4LongTrisaw, C5LongTrisaw, C6LongTrisaw, C7LongTrisaw };
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
        private void Plop(string input, VirtualMicrophone mic)
        {
            RainMeadow.Debug("It plays the plop " + input);
            string[] parts = input.Split('-');

            //Dust.Add(input, this); haltered
            string slib = parts[0]; //either L for Long, M for Medium, or S for Short
            int oct = int.Parse(parts[1]);
            int ind;
            bool intiseasy = int.TryParse(parts[2], out ind);
            //RainMeadow.Debug($"So the string is {s}, which counts as {parts.Length} amounts of parts. {slib}, {oct}, {ind}");

            SoundID[] slopb = SampDict(slib);
            SoundID sampleused = slopb[oct - 1];
            RainMeadow.Debug("Octave integer " + oct + ". sampleused: " + sampleused);
            RainMeadow.Debug($"It uses the sample {sampleused}");
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
            //if (!intiseasy)
            //{
            //    transposition += extratranspose;
            //    if (transposition > 11) { transposition -= 12; }
            //    if (transposition < 0) { transposition += 12; }
            //}

            transposition += extratranspose; //If to the power is smart(can take negative numbers), this can work

            float speeed = 1;

            speeed *= Mathf.Pow(magicnumber, transposition);

            // get intensity and turn that into too 
            // (which will also be reverb effect here then)

            float humanizingrandomnessinvelocitylol = UnityEngine.Random.Range(360, 1001) / 1000f;
            PlayThing(sampleused, humanizingrandomnessinvelocitylol, speeed, mic);

        }
        private void PushKeyModulation(int diff)
        {
            CurrentKey += diff;
            while (CurrentKey < -6 ||  CurrentKey > 6)
            {
                if (CurrentKey < 1) CurrentKey += 12;
                else CurrentKey -= 12;
            }
        }
        private void PushScaleModulation(bool majorscaling)
        {
            inmajorscale = majorscaling;
        }
        private void InfluenceModulation()
        {
            int dicedsign = UnityEngine.Random.Range(-5, 3);
            if (dicedsign <= 0) dicedsign = -1; else dicedsign = 1; //6/8th chance to go downwards, unstable by choice
            int dicedint = UnityEngine.Random.Range(0, 777) / (Math.Abs(QueuedModulation) + 1);
            int deadint = CurrentKey; //Debug thing
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
            if (EntryRequest)
            {
                EntryRequest = false;
                for (int i = 0; i < ChordInfos.GetLength(0); i++)
                {
                    //RainMeadow.Debug($"Nuclear {UpcomingEntry} vs Coughing {ChordInfos[i, 0]}... Round {i}, begin!");
                    if (UpcomingEntry == ChordInfos[i][0])
                    {
                        RainMeadow.Debug($"So it's gonna start a {UpcomingEntry}, with the key {CurrentKey}");
                        chordnotes = ChordInfos[i][1];
                        chordleadups = ChordInfos[i][2];
                    }
                }
                myred += 2f;
                InfluenceModulation(); //this'll be modified to take in int
                ChitChat.Wipe(this);
                string[] inst = chordnotes.Split(',');
                string chord = inst[0];
                string bass = inst[1];
                string[] notes = chord.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < notes.Length; i++)
                {
                    Plop(notes[i], mic);
                    NoteMagazine.AddSeed(notes[i].Substring(2));
                }
                string[] bassnotes = bass.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int sowhichoneisitboss = UnityEngine.Random.Range(0, bassnotes.Length);
                Plop(bassnotes[sowhichoneisitboss], mic); 


                //all notes have been played, moving onto leadup
                string[] leadups = chordleadups.Split('|');
                int butwhatnowboss = UnityEngine.Random.Range(0, leadups.Length);
                UpcomingEntry = leadups[butwhatnowboss];

                float feelingtoplay = UnityEngine.Random.Range(0, 1001) / 1000f;
                float threshholdtoplay = 0.77f - (fichtean * 0.48f) + chordexhaustion * 0.08f + chordsequenceiteration * Mathf.Lerp(0.225f, 0.125f, fichtean); //will be tweaked probs
                bool chordissequenced = feelingtoplay > threshholdtoplay;
                RainMeadow.Debug($"Chord is getting played with a {threshholdtoplay} threshhold");
                if (chordissequenced)
                {
                    chordexhaustion = (chordexhaustion * 1.325f) + Mathf.Lerp(1.75f, 0.55f, fichtean);
                    RainMeadow.Debug("Chord sequenced their thing:" + chordexhaustion);
                    float sequencedwait = UnityEngine.Random.Range(100, 500 - (int)(fichtean * 375)) / 100f;
                    chordtimer = Wait.Until("half", (int)sequencedwait, debugstopwatch);
                    chordsequenceiteration++;
                }
                else
                {
                    chordexhaustion = (chordexhaustion * 2) + Mathf.Lerp(1.5f, 0.5f, fichtean);
                    //RainMeadow.Debug("Chord waited a bar " + (int)chordexhaustion);
                    //RainMeadow.Debug("Chord waited a bar " + Wait.Until("bar", (int)chordexhaustion, debugstopwatch));
                    RainMeadow.Debug("New Chord Played");
                    chordtimer = Wait.Until("bar", (int)chordexhaustion, debugstopwatch);
                    chordsequenceiteration = 0;
                }
                NoteMagazine.Fester(this);
                playingchord = true;
            }
            //RainMeadow.Debug("Done with entrychord");

            if (playingchord == true) //holding
            {
                if (chordtimer <= 0)
                {
                    //RainMeadow.Debug($"{UpcomingEntry} will play");       
                    playingchord = false;
                    EntryRequest = true;
                }
                else
                {
                    ChitChat.Update(mic, this);
                    chordexhaustion *= Mathf.Lerp(0.9975f, 0.9925f, fichtean);
                    chordtimer--;
                }
            }           
        }
        private void PlayThing(SoundID Note, float velocity, float speed, VirtualMicrophone virtualMicrophone)
        {
            //virtualMicrophone.PlaySound(Note, 0f, intensity*0.5f, speed);

            //float pan = (float)(UnityEngine.Random.Range(-350, 351) / 1000f);
            float pan = MeadowMusic.vibePan == null ? 0 : (float)MeadowMusic.vibePan * (Mathf.Pow((float)MeadowMusic.vibeIntensity.Value*0.7f+0.125f, 1.65f));
            //float vol = velocity * 0.5f;
            float vol = MeadowMusic.vibeIntensity == null ? 0 : Mathf.Pow((float)MeadowMusic.vibeIntensity, 1.65f) * 0.5f * velocity;
            //float vol = MeadowMusic.plopIntensity * velocity;
            float pitch = speed;
            RainMeadow. Debug($"Trying to play a {Note}, at {vol} volume, with {pan} pan");
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
        struct Liaison
        {
            public Liaison(string note, int stopwatch, bool[] pattern, int patternindex, string period)
            {
                this.note = note;
                this.stopwatch = stopwatch;
                this.pattern = pattern;
                this.patternindex = patternindex;
                this.period = period;
            }
            public string note;
            public int stopwatch;
            public bool[] pattern;
            public int patternindex;
            public string period;
        }
        public static class ChitChat
        {
            static List<Liaison> LiaisonList = new List<Liaison>(); //list of the Liaison(s) currently playing
            static int[] liaisonrace = new int[0]; //arp pitch sorted array that will be remade with the analyze function
            static public bool isindividualistic = false; //setting for whether it'll treat things as individualistic
            static bool upperswitch = true;
            //is NOT the bar timer, that would be plopmachine.debugstopwatch.  This shall be used as a ,,, relatuve thing=????
            public enum Arpmode
            {
                upwards,
                downwards,
                switchwards,
                randomwards,
                inwards,
                outwards
            }
            static public Arpmode arpingmode = Arpmode.upwards;
            static int arpstep = 0;
            static public bool arpgoingupwards = false;
            static int arptimer = 20;

            static public bool[] arppattern = new bool[4];
            //static bool[] arppattern = [true, true, true, true, true, false, true, false, false, true, true, false];
            //static bool[] arppattern = [true, true, true, true, true, true, false, false, true, false, false];
            static int arppatternindex = 0;

            static public double arpcounterstopwatch;
            static int arpcurrentfreq;
            static public int arpbufferfreq;

            static List<int> randomsetsacrificeboard = new List<int>();
            static bool arpmiddlenoteistop;
            static int arpindexabovemidline;
            static int arpindexbelowmidline;
            //static List<string> nameshaha = [];
            
            static int TensionStopwatch; //this will be reset on wipe, and be the strain until a modulation or strum  //tension is chordstopwatch essentially 
            //depending on how i wanna do the random, like if i wanna do it like cookie clicker
            static bool ismodulation; //0 will make it break like a modulation, 1 will like a transposition.  random
            static bool hasbroken; //will be set to true when breaks. reset to false at wipe, and by undo
            static int differencebuffer;
            static bool hasswitchedscales;
            static int BreakUndoStopwatch; //will start to be counted when tension has broken
            static float evolvestopwatch;
            private static void Break(PlopMachine plopmachine)
            {
                hasbroken = true;
                ismodulation = 0 == UnityEngine.Random.Range(0, 2);
                if (ismodulation)
                {//modulation
                    int keybefore = plopmachine.CurrentKey;
                    bool scalebefore = plopmachine.inmajorscale;
                    plopmachine.QueuedModulation = 2;
                    plopmachine.InfluenceModulation();
                    plopmachine.QueuedModulation = 2;
                    plopmachine.InfluenceModulation();
                    plopmachine.QueuedModulation = 2;
                    plopmachine.InfluenceModulation();
                    differencebuffer = plopmachine.CurrentKey - keybefore;
                    hasswitchedscales = scalebefore == plopmachine.inmajorscale;
                }
                else
                {//transposition
                    differencebuffer = UnityEngine.Random.Range(-2, 3);
                    while (differencebuffer == 0) { differencebuffer = UnityEngine.Random.Range(-2, 3); }

                    string appendedaccidentals = "";
                    switch (differencebuffer)
                    {
                        case 2:
                            appendedaccidentals = "##";
                            break;
                        case 1:
                            appendedaccidentals = "#";
                            break;
                        case -1:
                            appendedaccidentals = "b";
                            break;
                        case -2:
                            appendedaccidentals = "bb";
                            break;
                    }

                    for (int i = 0; i < LiaisonList.Count; i++)
                    {
                        Liaison liaison = LiaisonList[i];
                        string newnote = string.Concat(liaison.note, appendedaccidentals);
                        LiaisonList[i] = new Liaison(newnote, liaison.stopwatch, liaison.pattern, liaison.patternindex, liaison.period);
                    }
                }
            }
            private static void UndoBreak(PlopMachine plopMachine)
            {
                if (ismodulation)
                {//modulation
                    plopMachine.PushKeyModulation(-differencebuffer);
                    if (hasswitchedscales)
                    {
                        plopMachine.PushScaleModulation(!plopMachine.inmajorscale);
                    }
                }
                else
                {
                    for (int i = 0; i < LiaisonList.Count; i++)
                    {
                        Liaison liaison = LiaisonList[i];
                        string unduednote = liaison.note.Substring(0, 5);
                        LiaisonList[i] = new Liaison(unduednote, liaison.stopwatch, liaison.pattern, liaison.patternindex, liaison.period);
                    }
                }
            }
            public static void Update(VirtualMicrophone mic, PlopMachine plopmachine)
            {
                TensionStopwatch++;//if i add it here every time, well, then, reminder that the stopwatch starts on 1, since a wipe and the start of liaisoning for the next chord happens at the same time.... well... ig that's the nature of doing a ++; at the start of ever
                                   //if (LiaisonList.Count == 1) isindividualistic = true; //until a thing can grow horns on its own, it should stay like this... but then what if it could? What if a note had a chance to spawn others that fitted to it? Check that
                                   //this is also now also decided in Add function, instead of making it a wholey other thing, becaaaaause i'm lazy... why? because this doesn't hold the door open for ^^^this expansion
                evolvestopwatch += plopmachine.fichtean*9 + 2.5f;
                if (UnityEngine.Random.Range(0, 3000000) + TensionStopwatch*4 > 3000000 && !hasbroken) //RTYU                           //temporary, it's gonna be a chance activation later. olololooooooool lol ooooooo lo  lol   looo 
                {
                    TensionStopwatch = 0;
                    Break(plopmachine);
                }

                if (hasbroken)
                {
                    BreakUndoStopwatch++;
                    if (UnityEngine.Random.Range(0, 150001) + BreakUndoStopwatch*3 > 150000)//RTYU 120
                    {
                        if (UnityEngine.Random.Range(0, 12) + (int)((1 - plopmachine.fichtean) * 4) <= 4)
                        {
                            RainMeadow.Debug("UNDID A BREAK BUT GOOD");
                            UndoBreak(plopmachine);
                        }
                        BreakUndoStopwatch = 0;
                    }
                }

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

                    if (isstrumming)
                    {
                        Strum(mic, plopmachine);
                    }
                    else
                    {
                        if (arptimer <= 0)
                        {
                            if (LiaisonList.Count != 0) //bruh why should it ever be less than zero lmao (that's the joke here)
                            { 
                                if (arppattern[arppatternindex]) CollectiveArpStep(mic, plopmachine);
                                plopmachine.mygreen += 0.3f;
                                arpcounterstopwatch += plopmachine.fichtean * 12 + 4;
                                //arpcurrentfreq = (int)(Mathf.PerlinNoise((float)arpcounterstopwatch / 1000f, (float)arpcounterstopwatch / 4000f) * 5);

                                int waitnumber = arpbufferfreq switch { 0 => 4, 1 => 6, 2 => 8, 3 => 12, 4 => 16, _ => 16, };
                                arpcurrentfreq = (int)(Mathf.PerlinNoise((float)arpcounterstopwatch / 1000f, (float)arpcounterstopwatch / 4000f) * 5);
                                if (arpbufferfreq != arpcurrentfreq && plopmachine.chordtimer < 96)
                                {
                                    if (arpbufferfreq > arpcurrentfreq) 
                                    {
                                        waitnumber /= 2;
                                        if (arppattern[arppatternindex]) CollectiveArpStep(mic, plopmachine);
                                    }
                                    else 
                                    { 
                                        //if (plopmachine.chordtimer == 48-1) waitnumber /= 2; 
                                        //so this shall be remade if (plopmachine.chordtimer < 48-1) waitnumber *= 2; //doesn't work artistically
                                    }
                                }
                                //PLUSS ONE because its' fucking... because this one plays it at the exact same time??? And goes downward for some reason??? Oh wait it's because it starts HERE. At THIS MOMENT, if it was a wait, there would be 24 until the next.
                                //RainMeadow.Debug("OK SO THE NEXT ONE IS " + arptimer);
                                arptimer = Wait.Until($"1/{waitnumber}", 1, plopmachine.debugstopwatch);

                                if (UnityEngine.Random.Range(0, 150000) + TensionStopwatch*12 > 150000) //RTYU            this is strum activationcode  //temp, will share with other. I decide now that if it's strummed, it'll roll a chance to break, but reset the "stopwatch" both use, tension   
                                {
                                    isstrumming = true;
                                    strumphase = Strumphases.queued;
                                    strumqueuetimer = Wait.Untils("half", 1, 3, plopmachine.debugstopwatch);
                                    if (UnityEngine.Random.Range(0, 1001) < 69) //good, the break will happen before the strum.
                                    {
                                        Break(plopmachine);
                                    }
                                    TensionStopwatch = 0;
                                }
                                arppatternindex = arppatternindex + 1 < arppattern.Length ? arppatternindex + 1 : 0;
                            }
                        }
                        else
                        {
                            arptimer--;
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
                        LiaisonList[i] = new Liaison(liaison.note, liaison.stopwatch, liaison.pattern, liaison.patternindex, liaison.period);
                    }
                    else
                    {
                        plopmachine.mygreen += 0.2f;
                        //RainMeadow.Debug("Playing a note from Chitchat.Anarchy");
                        if (liaison.pattern[liaison.patternindex])
                        {
                            bool nextnoteexists = liaison.patternindex + 1 < liaison.pattern.Length ? liaison.pattern[liaison.patternindex + 1] : liaison.pattern[0];
                            if (nextnoteexists) plopmachine.Plop($"S-{liaison.note.Substring(2, 3)}", mic);
                            else plopmachine.Plop(liaison.note, mic);
                            //string lol = "";
                            //foreach(bool thing in liaison.pattern)
                            //{
                            //    lol += thing;
                            //}
                            //RainMeadow.Debug("haha "+ lol);
                        }
                        int liaisonwait = Wait.Until(liaison.period, 1, plopmachine.debugstopwatch);
                        int lolol = liaison.patternindex + 1;
                        if (lolol >= liaison.pattern.Length) lolol = 0; 
                        LiaisonList[i] = new Liaison(liaison.note, liaisonwait, liaison.pattern, lolol, liaison.period);
                        CheckThisLiaisonOutDude(i, plopmachine);
                        //RainMeadow.Debug($"We're so here, playing a {liaison.note}, and their {liaison.yoffset} makes a delay of {moneydym1}");
                    }
                }
            }
            public static void Analyze(PlopMachine plopmachine)
            {
                List<int> LiaisonsFreqNumbs = new();
                int index = 0;
                //List<string> CopyCheckerColonD = new();
                foreach (Liaison heyo in LiaisonList)
                {
                    //bool isthisunique = true;
                    //foreach (string s in CopyCheckerColonD)
                    //{
                    //    if (s == heyo.note) isthisunique = false; break;
                    //}
                    //if (isthisunique){
                    //    CopyCheckerColonD.Add(heyo.note); }
                    string[] hey = heyo.note.Substring(2).Split('-');//maybe this'll fuck up in the future :3 yeah it fucks up now, it doesn't account for string
                    //fixed the fuck tho
                    bool intiseasy = int.TryParse(hey[1], out int ind);
                    int extratranspose = 0;
                    if (!intiseasy)
                    {
                        string accidentals = hey[1].Substring(1);
                        foreach (char accidental in accidentals)
                        {
                            switch (accidental)
                            {
                                case 'b':
                                    extratranspose--;
                                    break;
                                case '#':
                                    extratranspose++;
                                    break;
                            }
                        }
                        ind = int.Parse(hey[1].Substring(0, 1));
                    }
                    int transposition = plopmachine.IndexTOCKInt(ind);
                        
                    int freqnumb = int.Parse(hey[0]) * 12 + transposition + extratranspose;
                    LiaisonsFreqNumbs.Add(freqnumb);
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
            public static void Instantiate(PlopMachine plopMachine)
            {
                if (LiaisonList.Count < 3) { isindividualistic = true; RainMeadow.Debug("SO its normally individual"); }
                else
                {
                    RainMeadow.Debug("YO");
                    /*
                    if (!isindividualistic)
                    {
                        //isindividualistic = UnityEngine.Random.Range(0, 100) < 2+(int)(plopmachine.fichtean*6); 
                        //i hate individualism now (for a moment)
                    } 
                    else { isindividualistic = UnityEngine.Random.Range(0, 100) > 34 + (int)(plopMachine.fichtean * 26); }
                    */
                    isindividualistic = false;
                }
                if (!isindividualistic) { Analyze(plopMachine); }

                if (UnityEngine.Random.Range(0, 2) == 1) RandomMode();
                arpingmode = Arpmode.upwards; //FOR TESTING, REMOVE AFTERWARDS
                arppatternindex = 0;
                
                //
                //
                //A "Proper" Arpegiation is one where
                //The divisions of wait % the length of arppattern = 0
                //AND
                //The amount of True's % the amounts of waits per bar = 0
                //
                //
                //
                //
                //
                //
                //Maybe the arppatternindex shouldn't be reset to zero if the arpmode isn't changed.
                //I.E. It's only when the arpmode changes that you reset arppatternindex.
                //here it should generate arppattern
                //what it base its generation upon is weird
                // in the far future it should make the patterns for anarchy here
            }
            public static void Add(string note, PlopMachine plopmachine)
            {
                //RainMeadow.Debug($"SOthestart of Chitchat.add, when i'm trying to input a {note}");
                string[] anotherhighernotesparts = note.Split('-');
                //RainMeadow.Debug(anotherhighernotesparts[0] + " " + anotherhighernotesparts[1]);
                int octave = int.Parse(anotherhighernotesparts[0]);

                bool willadd = true;
                string mynote = "M-" + note;
                //int thing = (int)(plopmachine.fichtean * 5); //was 16 - 4 here
                int thing = arpbufferfreq; //is 4 - 16 here
                string period;
                switch (thing)
                {
                    case 0: period = "1/8"; break;
                    case 1: period = "1/12"; break;
                    case 2: period = "1/16"; break;
                    case 3: period = "1/24";break;
                    case 4: period = "1/32";break;
                    default:period = "1/32";break;
                }
                int liaisonwait = Wait.Until(period, 1, plopmachine.debugstopwatch);
                //int amountoftimes = UnityEngine.Random.Range(8 - (int)(plopmachine.fichtean * 3), 29 - (int)(plopmachine.fichtean * 15));
                int amountoftimes = UnityEngine.Random.Range(8 - (arpbufferfreq/5 * 3), 23 - ((int)(arpbufferfreq/2)* 3));
                bool[] mama = new bool[amountoftimes];
                for (int i = 0; i < amountoftimes; i++)
                {
                    mama[i] = UnityEngine.Random.Range(4, ((5-arpbufferfreq)*2) + 72) > 66;
                }

                Liaison helo = new Liaison(mynote, liaisonwait, mama, UnityEngine.Random.Range(0, amountoftimes), period);

                //checks there's no duplicates, doesn't add if so
                foreach (Liaison thisliaison in LiaisonList)
                {
                    if (thisliaison.note == mynote) willadd = false;
                }

                if (willadd) { LiaisonList.Add(helo); }
                //if (!isindividualistic) { RainMeadow.Debug($"Added {mynote}, a {helo.note} with analysis"); } else { RainMeadow.Debug($"Added {mynote}, a {helo.note} without analysis"); }
            }
            private static void CheckThisLiaisonOutDude(int indexofwhereitathomie, PlopMachine plopmachine)
            {
                //the liaison modifier code, this shall be called when the note has a chance to be FREAKED up dude (changed)
                //ok i had a bit of confusion about why i decided so and i found this line in the pseudo: this chance shall be rolled after playing a note (it is much harder to modify the arp before, as it might break together by trying to analyze() it)
                //so an y ways,,
                //bool hi = UnityEngine.Random.Range(plopmachine.debugstopwatch - evolvestopwatch, plopmachine.debugstopwatch - evolvestopwatch + 7000) > 7000 + evolvestopwatch;
                //what the fuck is this pseudocode ok
                //fuck this lets just have it be  a normal random shit and have that be fucked up or smth
                Liaison liaison = LiaisonList[indexofwhereitathomie];

                bool itwillevolve = UnityEngine.Random.Range(0, 800) + (int)evolvestopwatch > 1200; //RTYU
                if (Input.GetKey("7")) { itwillevolve = true; }
                //RainMeadow.Debug("Hi it is using this thing " + itwillevolve + " " + evolvestopwatch);
                if (itwillevolve)
                {
                    evolvestopwatch = 0;
                    RainMeadow.Debug("Evolves " + liaison.note);

                    string[] parts = liaison.note.Split('-');
                    //ok this REALLY should be reworked to pick the note NEXT to that guy
                    //      what the fuck do you mean, past me????

                    int oct = int.Parse(parts[1]);
                    bool intiseasy = int.TryParse(parts[2], out int ind);
                    string accidentals = "";
                    if (!intiseasy)
                    {
                        ind = int.Parse(parts[2].Substring(0, 1));
                        accidentals = parts[2].Substring(1);
                    }
                    int attempts = 0;
                    bool willmodify;
                    do
                    {
                        willmodify = true; //copied straight from the best coder on earth, me, when wriding Add()
                        int modifying = UnityEngine.Random.Range(-2, 1);
                        if (modifying > -1) modifying++;
                        if (modifying is (-2) or 2) if (UnityEngine.Random.Range(0, 2) == 1) modifying /= 2;

                        ind += modifying;

                        if (ind > 7) { ind -= 7; oct++; }
                        if (ind < 1) { ind += 7; oct--; }

                        if (oct < 1) oct++;
                        if (oct > 7) oct--;
                        string construction;
                        if (intiseasy) construction = "M-" + Convert.ToString(oct) + "-" + Convert.ToString(ind);
                        else construction = "M-" + Convert.ToString(oct) + "-" + Convert.ToString(ind) + accidentals;
                        liaison.note = construction;

                        foreach (Liaison thing in LiaisonList)
                        {
                            //RainMeadow.Debug($"Trying to Fuck {thing.note} {construction}");
                            if (thing.note == construction) willmodify = false;
                        }
                        attempts++;
                    } while (!willmodify && attempts < 4);
                    if (attempts >= 4) { RainMeadow.Debug("Oh no can't fuck with it"); }
                    else
                    {
                        LiaisonList[indexofwhereitathomie] = liaison;
                        Analyze(plopmachine);
                        RainMeadow.Debug("To " + liaison.note);
                    }
                }
            }
            public static void Wipe(PlopMachine plopmachine)
            {
                LiaisonList.Clear();
                arpstep = 0;
                TensionStopwatch = 0;
                hasbroken = false;
                isstrumming = false;
                BreakUndoStopwatch = 0;
                randomsetsacrificeboard.Clear();
                arpbufferfreq = (int)(Mathf.PerlinNoise((float)arpcounterstopwatch / 1000f, (float)arpcounterstopwatch / 4000f) * 5);
                arpbufferfreq = 3; //TESTING 
                if (!isindividualistic) { Analyze(plopmachine); }
            }
            public static void RandomMode()
            {
                //switch statement that takes a number and changes
                //arpingmode to be: "upwards" "downwards" "switchwards" "randomwards" "inwards" "outwards"
                int sowhichoneboss = UnityEngine.Random.Range(0, 6); //Holy fucking shit this has been only playing the last two for months.
                arpingmode = (Arpmode)sowhichoneboss;
                RainMeadow.Debug("The arping mode has been chosen to be " + arpingmode);
                arpmiddlenoteistop = UnityEngine.Random.Range(0, 2) == 1;
                
                float pseudoarpstep = LiaisonList.Count / 2;
                arpindexabovemidline = (int)Math.Ceiling(pseudoarpstep) - 1;
                arpindexbelowmidline = (int)Math.Floor(pseudoarpstep) - 1;
                //OKAY SO SIDE NOTE TO MYSELF, always call randommode AFTER you've found out how many things there are in here
            }
            public static void CollectiveArpStep(VirtualMicrophone mic, PlopMachine plopmachine)
            {
                //uses the logic of the mode selected by the string of arpingmode to *arp* between the notes,
                //plays the note that it's selected, then selects the next one
                //so, arpstep is the current step in the arpegio, normal
                //liaisonrace interprets the number and gives back another number which is the index of the note in liaison
                //when picking an index from the liaisonlist, it returns a Liaison, that liaison has got properties, one of them is its note, which'll be a string.

                //RainMeadow.Debug($"it plays a thing at uhhhh   {arpstep},,,,   then it's the index  {liaisonrace[arpstep]}.. .  ..  {LiaisonList[liaisonrace[arpstep]].note}");

                //public static string CollectiveArpStep(VirtualMicrophone mic, PlopMachine plopmachine, bool returnnoteinstead = false)
                //if (returnnoteinstead) return LiaisonList[liaisonrace[arpstep]].note; 
                //RainMeadow.Debug("Playing a Plop from Chitchat.CollectiveArpStep, the " + arpstep);
                plopmachine.Plop(LiaisonList[liaisonrace[arpstep]].note, mic); //so it plays the previous one
                CheckThisLiaisonOutDude(liaisonrace[arpstep], plopmachine);

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

                    case Arpmode.randomwards:
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

                    case Arpmode.inwards:
                        int lookoutfor;
                        if (arpgoingupwards)
                        {
                            arpstep++;
                            if (arpmiddlenoteistop) lookoutfor = arpindexabovemidline;
                            else lookoutfor = arpindexbelowmidline;

                            if (arpstep >= lookoutfor)
                            {
                                arpgoingupwards = false;
                                arpstep = LiaisonList.Count - 1;
                            }
                        }
                        else
                        {
                            arpstep--;
                            if (arpmiddlenoteistop) lookoutfor = arpindexbelowmidline;
                            else lookoutfor = arpindexabovemidline;
                            if (arpstep <= lookoutfor)
                            {
                                arpgoingupwards = true;
                                arpstep = 0;
                            }
                        }
                        break;

                    case Arpmode.outwards:
                        if (arpgoingupwards)
                        {
                            arpstep++;
                            if (arpstep >= LiaisonList.Count)
                            { //this fucks up and returns a negative value  if  liaisonlist.count = 1
                                //float pseudoarpstep = LiaisonList.Count / 2;
                                if (arpmiddlenoteistop)
                                    arpstep = arpindexbelowmidline;//(int)Math.Floor(pseudoarpstep) - 1;
                                else
                                    arpstep = arpindexabovemidline;// (int)Math.Ceiling(pseudoarpstep) - 1;
                                arpgoingupwards = false;
                            }
                        }
                        else
                        {
                            arpstep--;
                            if (arpstep < 0)
                            {
                                if (arpmiddlenoteistop) arpstep = arpindexabovemidline;
                                else arpstep = arpindexbelowmidline;
                                arpgoingupwards = true;
                            }
                        }
                        break;
                }
            }

            static int strumindex;
            static bool strumdirectionupwards; //true = upwards. false = downwards
            static bool isstrumming;
            static int strumqueuetimer = 20;
            static int strumtimer;
            static int strumepiloguetimer;
            static Strumphases strumphase;
            enum Strumphases
            {
                queued,
                playing,
                epilogue
            }
            private static void Strum(VirtualMicrophone mic, PlopMachine plopmachine)
            {
                switch (strumphase)
                {
                    case Strumphases.queued:
                        strumqueuetimer--;
                        //if strumqueuetimer has reached zero: strum is no longer queued, it will start playing (strumplaying/strumstrumming = true, strumqueued = false, strumtimer = 0)
                        if (strumqueuetimer <= 0)
                        {
                            switch (arpingmode)
                            {
                                case Arpmode.upwards:
                                    strumdirectionupwards = false;
                                    break;

                                case Arpmode.downwards:
                                    strumdirectionupwards = true;
                                    break;

                                case Arpmode.switchwards:
                                    if (arpgoingupwards) strumdirectionupwards = false;
                                    else strumdirectionupwards = true;
                                    break;

                                case Arpmode.randomwards:
                                    strumdirectionupwards = UnityEngine.Random.Range(0, 2) == 0;
                                    break;

                                case Arpmode.outwards: //because this one feels like it's missing
                                    if (arpmiddlenoteistop) strumdirectionupwards = true;
                                    else strumdirectionupwards = false;
                                    break;

                                case Arpmode.inwards: //because this one feels like it's cutting off 
                                    if (arpmiddlenoteistop) strumdirectionupwards = false;
                                    else strumdirectionupwards = true;
                                    break;

                                default:
                                    strumdirectionupwards = true;
                                    break;
                            }
                            //depending on strumdirection, strumindex will be: "upwards" = 0, "downwards" = (liaisonlist.count-1) //(cuz if there's 4 things, then 3 is maxindex)
                            if (strumdirectionupwards) strumindex = 0;
                            else strumindex = LiaisonList.Count - 1;

                            strumphase++;
                            strumtimer = 0;
                        }
                        break;

                    case Strumphases.playing:
                        if (strumtimer > 0)
                        {
                            strumtimer--;
                        }
                        else
                        {
                            //RainMeadow.Debug("It's the funny it's so playing :))))" + strumindex +"   ummm"+ liaisonrace[strumindex] +"   ummmm"+ LiaisonList[liaisonrace[strumindex]]);
                            plopmachine.Plop(LiaisonList[liaisonrace[strumindex]].note, mic);
                            strumtimer = (int)(plopmachine.fichtean * 4);  //which is essentially //perlinnoise(1, 4) (1, 2, 3)
                            if (strumdirectionupwards) { strumindex++; }
                            else { strumindex--; }

                            if (strumindex < 0 || strumindex > LiaisonList.Count - 1)
                            {
                                strumphase++;
                                strumepiloguetimer = Wait.Until("bar", 0, plopmachine.debugstopwatch);
                            }
                        }
                        break;

                    case Strumphases.epilogue:
                        if (strumepiloguetimer > 0)
                        {
                            strumepiloguetimer--;   //maybe in the future we can do some BONUS STRUMS
                        }
                        else
                        {
                            isstrumming = false;
                        }
                        break;
                }
            }

        }
        public static class NoteMagazine
        {
            static List<string> InNoteList = new(); //actually filled with "3-3" too,, 
            static List<string> OutNoteList = new(); //filled with "3-1"
            static bool hasdecidedamount = false;
            static int decidedamount;
            static int
                triedamounts;
            static readonly Dictionary<string, string> SoloLineageDict = new(); //one at a time kid
            static readonly Dictionary<string, string> DuoLineageDict = new(); //thanks dad it's time for duo
            public static void fuckinginitthatdictlineagebitch()
            {
                SoloLineageDict.Add("fuckineedtofindouthowtowritethishere", "Ambientynote|Chordy Notes");
                SoloLineageDict.Add("4-1", "3-5 4-1 4-3 4-5|3-6 4-2 4-4");
                SoloLineageDict.Add("4-2", "3-6 4-2 4-5 4-6|3-4 4-1 4-3 4-4");
                SoloLineageDict.Add("4-3", "3-6 4-2 4-3 4-6 5-1|3-7 4-3 4-4 4-7");
                SoloLineageDict.Add("4-4", "3-5 4-1 4-4 4-5 5-3|3-5 4-2 4-5 5-2");
                SoloLineageDict.Add("4-5", "4-1 4-5 5-1 5-3 5-5|4-2 4-6 4-7 5-3 5-6");
                SoloLineageDict.Add("4-6", "4-6 5-1 5-2 5-3 5-6 4-2|4-3 4-5 5-2 5-4");
                SoloLineageDict.Add("4-7", "3-3 3-7 4-1 4-5 4-7|3-6 4-2 4-4 4-5");

                //yeahhh and then the second one !
                DuoLineageDict.Add("timeforsecondof painyo", "yeah|yeah");
                //DuoLineageDict.Add("4-1 4-5", "4-4 4-1 4-5|4-6 5-3 4-1");
                DuoLineageDict.Add("4-1 4-2", "3-5 4-1 4-2 4-3 4-6 5-2|3-7 4-1 4-2 4-4 4-6 5-3");
                DuoLineageDict.Add("4-1 4-3", "3-6 4-1 4-3 4-5 5-2|3-6 4-4 4-5 4-6");
                DuoLineageDict.Add("4-1 4-4", "3-5 4-1 4-4 4-6 5-3|4-2 4-5 4-6 5-2");
                DuoLineageDict.Add("4-1 4-5", "3-5 4-1 4-5 5-1 5-3|4-2 4-4 4-5 4-7 5-2");
                DuoLineageDict.Add("4-1 4-6", "3-5 4-2 4-5 5-1 5-5|3-5 4-2 4-5 5-1");
                DuoLineageDict.Add("4-1 4-7", "4-1 4-5 4-7 5-2|4-2|4-5 4-7 5-1 5-2");
                DuoLineageDict.Add("4-2 4-3", "3-6 4-2 4-3 4-6 5-2|3-2 4-2 4-4 4-5 5-2");
                DuoLineageDict.Add("4-2 4-4", "3-6 4-2 4-4 5-4|3-7 4-3 4-7 5-1");
                DuoLineageDict.Add("4-2 4-5", "4-2 4-5 4-7 5-2|3-7 4-6 5-3 5-5");
                DuoLineageDict.Add("4-2 4-6", "3-5 4-2 4-5 4-6 5-2|3-6 4-3 4-7 5-3");
                DuoLineageDict.Add("4-2 4-7", "3-5 4-2 4-3 4-7|4-3 4-5 5-1 5-5");
                DuoLineageDict.Add("4-3 4-4", "3-6 4-3 4-4 4-6 5-3|3-7 4-5 5-1 5-4");
                DuoLineageDict.Add("4-3 4-5", "3-5 4-3 4-5 5-3 5-7|4-1 4-2 4-4 4-6 5-2");
                DuoLineageDict.Add("4-3 4-6", "4-1 4-3 4-6 5-5|3-5 4-1 4-6 5-1");
                DuoLineageDict.Add("4-3 4-7", "3-6 4-3 4-7 5-3|4-4 4-6 5-1 5-3 5-5");
                DuoLineageDict.Add("4-4 4-5", "3-5 4-1 4-4 4-5 5-6|4-2 4-6 5-1 5-2");
                DuoLineageDict.Add("4-4 4-6", "3-5 4-1 4-4 4-6 5-3 5-6|3-6 4-4 4-6 5-1 5-4 5-7");
                DuoLineageDict.Add("4-4 4-7", "3-5 4-4 4-6 4-7 5-3|3-7 4-3 4-5 5-4");
                DuoLineageDict.Add("4-5 4-6", "3-6 4-5 4-6 5-2 5-6|4-1 4-5 4-7 5-5");
                DuoLineageDict.Add("4-5 4-7", "3-5 4-1 4-5 4-7 5-5|3-4 3-7 4-4 4-6 5-4");
                DuoLineageDict.Add("4-6 4-7", "3-5 4-2 4-6 4-7 5-3 5-5|3-4 3-7 4-3 4-5 5-1 5-4");
            }
            //don't be scared of the lowest notes, oct 1 will be rolled up.
            //i'll use octave 4 as the baseline
            //index is index
            //
            //static string[] keycodeNames = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" };

            //alright so, the thing creature here is then,,,,
            //This includes an inputted string, the Innote,  and gives back two sets of "children" for lack of a better word.
            //First set is the ambiency set, second the chordy. 
            //This dict shall be used by Grow

            //static Dictionary<int, Dictionary<int, string>> LOLTHISWORKS = new();
            //imagine opening a book and it just having more books in it
            //maybe this is a quicker thing, i'll have to ask hellothere(an actual programmer)

            public static void AddSeed(string Note) { InNoteList.Add(Note); }
            public static void Fester(PlopMachine plopmachine)
            {//(it is time to create the outnotes.) (very expensive here)
                if (!hasdecidedamount) { decidedamount = (int)Mathf.Lerp(6.5f, 2f, plopmachine.fichtean); hasdecidedamount = true; }
                RainMeadow.Debug("Amount of things it's wanting" + decidedamount);
                while (OutNoteList.Count < decidedamount && triedamounts < 10)
                {
                    Grows(plopmachine);
                    triedamounts++;
                    //RainMeadow.Debug("Attempt " + triedamounts);
                }
                triedamounts = 0;
                Push(plopmachine);
            }
            //----------------------------------------------------------------------------------------------------
            private static void Grows(PlopMachine plopmachine)
            {
                string Note1 = InNoteList[UnityEngine.Random.Range(0, InNoteList.Count)];
                string Note2 = InNoteList[UnityEngine.Random.Range(0, InNoteList.Count)];

                string[] Break1 = Note1.Split('-');
                int Octave1 = int.Parse(Break1[0]);
                bool HasNoExtras1 = int.TryParse(Break1[1], out int Index1);
                string Extras1 = "";
                if (!HasNoExtras1)
                {
                    Index1 = int.Parse(Break1[1].Substring(0, 1));
                    Extras1 = Break1[1].Substring(1);
                }
                string[] Break2 = Note2.Split('-');
                int Octave2 = int.Parse(Break2[0]);
                bool HasNoExtras2 = int.TryParse(Break2[1], out int Index2);
                string Extras2 = "";
                if (!HasNoExtras2)
                {
                    Index2 = int.Parse(Break2[1].Substring(0, 1));
                    Extras2 = Break2[1].Substring(1);
                }

                bool TheyTheSame = true;
                if (Octave1 == Octave2) TheyTheSame = Index1 == Index2;
                string LowNote;
                string HighNote;
                string HighExtras;
                int HighFourDelta;
                if (Index1 > Index2 || Octave1 > Octave2)
                {
                    HighNote = $"4-{Index1}";
                    HighExtras = Extras1;
                    LowNote = $"4-{Index2}";
                    HighFourDelta = Octave1 - 4;
                }
                else
                {
                    HighNote = $"4-{Index2}";
                    HighExtras = Extras2;
                    LowNote = $"4-{Index1}";
                    HighFourDelta = Octave2 - 4;
                }
                string NoteValue;
                bool TheOneThatDiscards;
                if (TheyTheSame) 
                {
                    if (UnityEngine.Random.Range(0, 3) == 0) { TheOneThatDiscards = SoloLineageDict.TryGetValue(LowNote, out NoteValue); }
                    else { TheOneThatDiscards = SoloLineageDict.TryGetValue(HighNote, out NoteValue); }
                }
                else
                {
                    string NoteKey = $"{LowNote} {HighNote}";
                    TheOneThatDiscards = DuoLineageDict.TryGetValue(NoteKey, out NoteValue);
                }
                _ = TheOneThatDiscards;
                string[] heavenorhell = NoteValue.Split('|');
                float bias = Mathf.Pow(-Mathf.Cos(plopmachine.fichtean * Mathf.PI), 0.52f) / 2 + 0.5f;
                float doesthechurchallowit = UnityEngine.Random.Range(0, 100001) / 100000f;
                int martinlutherking;
                if (bias > doesthechurchallowit) { martinlutherking = 1; } else { martinlutherking = 0; }
                string whichonewillyouchoose = heavenorhell[martinlutherking];
                string[] thebegotten = whichonewillyouchoose.Split(' ');
                string theone = thebegotten[UnityEngine.Random.Range(0, thebegotten.Length)];
                string[] FinalNoteParts = theone.Split('-');

                int FinalOct = int.Parse(FinalNoteParts[0]) + HighFourDelta;
                if (FinalOct > 7) { FinalOct = UnityEngine.Random.Range(3, 7); }
                string FinalNote = $"{FinalOct}-{FinalNoteParts[1]}{HighExtras}";

                bool existsinhere = false;
                //RainMeadow.Debug("Pushing a " + FinalNote+"note");
                foreach (string seed in InNoteList)
                { if (FinalNote == seed) existsinhere = true; }
                if (!existsinhere) InNoteList.Add(FinalNote);
                existsinhere = false;
                foreach (string bullet in OutNoteList)
                { if (FinalNote == bullet) existsinhere = true; }
                if (!existsinhere) OutNoteList.Add(FinalNote);
            }
            public static void Push(PlopMachine plopmachine)
            {
                //chitchat.add takes only in "3-2" thing, not "S-3-1"
                foreach (string bullet in OutNoteList)
                {
                    ChitChat.Add(bullet, plopmachine);
                    //RainMeadow.Debug($"Pushed a {bullet}Thing");
                }
                ChitChat.Instantiate(plopmachine);
                InNoteList.Clear();
                OutNoteList.Clear();
                hasdecidedamount = false;
            }
        }
        //
        //Oh and after all this:
        //  Destruction Update (Destroying the last note to play the newest one)
        //which'll entail: tagging sounds, then picking which ones to destroy, calling to destroy them sometime(maybe when new one is played)
        //I am ideaing to tag 'em with L, M, and S notes, Destroying S then M never L.
        //whatever,

        static Dictionary<string, int> WaitDict = new();
        public void StartthefuckingWaitDict()
        {
            ChitChat.arppattern = new bool[4];
            ChitChat.arppattern[0] = true;
            ChitChat.arppattern[1] = true;
            ChitChat.arppattern[2] = true;
            ChitChat.arppattern[3] = false;

            WaitDict.Add("bar", 96);
            WaitDict.Add("half", 48);
            WaitDict.Add("quarterT", 32);
            WaitDict.Add("quarter", 24);
            WaitDict.Add("eightT", 16);
            WaitDict.Add("eight", 12);
            WaitDict.Add("sixteenthT", 8);
            WaitDict.Add("sixteenth", 6);
            WaitDict.Add("thirtysecondT", 4);
            WaitDict.Add("thirtysecond", 3);
            WaitDict.Add("sixtyfourthT", 2);
            WaitDict.Add("hundredandtwentyeightT", 1);

            WaitDict.Add("1", 96);
            WaitDict.Add("1/2", 48);
            WaitDict.Add("1/3", 32);
            WaitDict.Add("1/4T", 32);
            WaitDict.Add("1/4", 24);
            WaitDict.Add("1/6", 16);
            WaitDict.Add("1/8T", 16);
            WaitDict.Add("1/8", 12);
            WaitDict.Add("1/12", 8);
            WaitDict.Add("1/16", 6);
            WaitDict.Add("1/24", 4);
            WaitDict.Add("1/32", 3);
            WaitDict.Add("1/64T", 2);
            WaitDict.Add("1/48", 2);
            WaitDict.Add("1/128T", 1);
            WaitDict.Add("1/96", 1);

            WaitDict.Add("defult", 24); //this is definetly not how to go about it but whatevs henp can correct me later lol
        }
        public class Wait
        {
            private static int Leftof(string waittype, int atthistimeofday) //localized entirely within your kitchen
            {
                bool diditgetit = WaitDict.TryGetValue(waittype, out int waitvalue);
                if (diditgetit) { return waitvalue - (atthistimeofday % waitvalue) - 1; }
                else { return 24 - (atthistimeofday % 24) - 1; }
            }
            public static int Until(string waittype, int waits, int atthistimeofyear) //atthistimeofyear = debugstopwatch
            {
                if (waits <= 0) waits = 1;
                bool isvalidname = WaitDict.TryGetValue(waittype, out int waitvalue);
                if (!isvalidname) { waitvalue = 24; RainMeadow.Debug("INVALID NAME: " + waittype); }
                //RainMeadow.Debug($"{waits} is the amount, {waitvalue} is the type time, {waittype} is the name, {waits-1 * waitvalue}");
                return Leftof(waittype, atthistimeofyear) + ((waits - 1) * waitvalue);
            }
            public static int Untils(string waittype, int mininclusive, int maxinclusive, int atthistimeofyear)
            {
                int thewait = UnityEngine.Random.Range(mininclusive, maxinclusive + 1);
                bool isvalidname = WaitDict.TryGetValue(waittype, out int waitvalue);
                if (!isvalidname) { waitvalue = 24; }
                return Leftof(waittype, atthistimeofyear) + ((thewait - 1) * waitvalue);
            }
        }

        Color mycolor = new(0f, 0f, 1f, 1f);
        float myred;
        float mygreen;
        float myblue;
        /*
        these lines i write to
        bring equality, and for
        my love of haikus
        */
        //public void hehedrawsprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        //{
        //    //orig(self, sLeaser, timeStacker, rCam, timeStacker, camPos);
        //    orig(self, sLeaser, rCam, timeStacker, camPos);
        //    Color camocoloryo = rCam.PixelColorAtCoordinate(self.player.mainBodyChunk.pos);
        //    //RainMeadow.Debug($"So the color at here issss {color}");
        //    //Color mycolor = sLeaser[self.startSprite].color;
        //    foreach (var sprite in sLeaser.sprites)
        //    {
        //        //sprite.color = camocoloryo;
        //        sprite.color = mycolor;
        //    }
        //}
        bool switchbetweentwonumbers;
        bool switchbetweentwoothernumbers = true;
        bool yoyo;
        bool yoyo2;
        bool yoyo3;
        int theothernumber = 112222;
        static int SimulationNumber;
        float thenumber = 0.05f;
        private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            debugstopwatch++;
            var mic = self.cameras[0].virtualMicrophone;
            CurrentRegion = self.world.region.name;
            CurrentRegion ??= "sl";

            fichtean = Mathf.PerlinNoise(debugstopwatch / 1000f, debugstopwatch / 4000f);
            //fichtean = thenumber;
            //RainMeadow.Debug("This Goes Hard");
            //RainMeadow.Debug("Fichtean: " + fichtean + " Yeah");
            //RainMeadow.Debug("Chordexhaustion: " + chordexhaustion + " Yeah");
            if (MeadowMusic.AllowPlopping) PlayEntry(mic);
            /*
            string hallo = "    ";
            hallo += chordtimer.ToString();
            hallo += "    ";
            hallo += Wait.Until("1", 1, debugstopwatch).ToString();
            hallo += "    ";
            hallo += Wait.Until("1/2", 1, debugstopwatch).ToString();
            hallo += "    ";
            hallo += Wait.Until("1/3", 1, debugstopwatch).ToString();
            hallo += "    ";
            hallo += Wait.Until("1/4", 1, debugstopwatch).ToString();
            hallo += "    ";
            hallo += Wait.Until("1/6", 1, debugstopwatch).ToString();
            hallo += "    ";
            hallo += Wait.Until("1/8", 1, debugstopwatch).ToString(); 
            hallo += "    ";
            RainMeadow.Debug(hallo);
            RainMeadow.Debug(ChitChat.arpbufferfreq + " " + (int)(Mathf.PerlinNoise((float)ChitChat.arpcounterstopwatch / 1000f, (float)ChitChat.arpcounterstopwatch / 4000f) * 5));
            */
            //if (debugstopwatch < 300) RainMeadow.Debug("It's just waiting");
            //Dust.Update(mic, this);
            //RainMeadow.Debug("Amount of sounds currently in da works: " + mic.soundObjects.Count);

            //if (Wait.Until("bar", 1, debugstopwatch) == 1) { Plip("L-5-1", mic); }
            //if (Wait.Until("quarter", 1, debugstopwatch) == 23) //it's not 1,  it wouldn't start at 0, 0 is set so that the very next one is the action, max-1 is the start
            //{ 
            //    if (Wait.Until("bar", 1, debugstopwatch) == 95)
            //    
            //                Plop("S-6-1", mic); 
            //    else 
            //                Plop("S-5-1", mic); 
            //    
            //    //RainMeadow.Debug("Fourth"); 
            //}
            if (Wait.Until("eight", 1, debugstopwatch) == theothernumber)
            {
                if (Wait.Until("quarter", 1, debugstopwatch) == 23)
                {
                    PlayThing(HiHat, 0.5f / 2, 1, mic);
                    if (Wait.Until("bar", 1, debugstopwatch) == 95)
                    {
                        PlayThing(Kick, 0.7f / 2, 1, mic);
                    }
                    else
                    {
                        if (Wait.Until("half", 1, debugstopwatch) == 47)
                        {
                            PlayThing(Snare, 0.7f / 2, 1, mic);
                        }
                    }
                    if (Wait.Until("half", 1, debugstopwatch) == 47)
                    {
                        PlayThing(HiHat, 0.27f / 2, 1, mic);
                    }
                    else
                    {
                        PlayThing(HiHat, 0.5f / 2, 1, mic);
                    }
                }
                else
                {
                    PlayThing(HiHat, 0.12f / 2, 1, mic);
                }
            }
            

            //RainMeadow.Debug(MeadowMusic.vibeIntensity ??= 231);
            //RainMeadow.Debug(MeadowMusic.vibePan ??= 20);

            //myred *= 0.97f;
            //mygreen *= 0.963f;
            //myblue *= 0.94f;
            //if (myred == 0f && mygreen == 0f && myblue == 0f)
            //{
            //    myred = 0.06f;
            //}
            //mycolor = new(myred, mygreen, myblue, 1f);
            if (Input.GetKey("6") && !yoyo3)
            {
                SimulationNumber++;
            }
            yoyo3 = Input.GetKey("6");


            if (Input.GetKey("9") && !yoyo)
            {
                if (switchbetweentwonumbers)
                {
                    thenumber -= 0.05f;
                    if (thenumber < 0)
                    {
                        thenumber = 0.0f;
                        switchbetweentwonumbers = false;
                    } 
                }
                else
                {
                    thenumber += 0.05f;
                    if (thenumber > 1)
                    {
                        thenumber = 1.0f;
                        switchbetweentwonumbers = true;
                    }
                }
                RainMeadow.Debug("Fichtean is now " + thenumber);
            }
            yoyo = Input.GetKey("9");

            if (Input.GetKey("8") && !yoyo2)
            {
                if (switchbetweentwoothernumbers)
                {
                    theothernumber = 11;
                    switchbetweentwoothernumbers = false;
                    RainMeadow.Debug("Drums activated");
                }
                else
                {
                    theothernumber = 111222;
                    switchbetweentwoothernumbers = true;
                    RainMeadow.Debug("Drums deactivated");
                }
            }
            yoyo2 = Input.GetKey("8");
        }

        public static readonly SoundID Kick = new SoundID("Kick", register: true);
        public static readonly SoundID Snare = new SoundID("Snare", register: true);
        public static readonly SoundID HiHat = new SoundID("HiHat", register: true);
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
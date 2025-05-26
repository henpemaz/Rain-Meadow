using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

namespace RainMeadow
{
    public class PlopMachine
    {
        public void OnEnable()
        {
            On.RainWorldGame.Update += RainWorldGame_Update; //actually usefull
            On.VirtualMicrophone.DrawUpdate += VirtualMicrophone_DrawUpdate;
            On.RainWorldGame.ctor += RainWorldGame_ctor; //actually usefull 
        }

        private void VirtualMicrophone_DrawUpdate(On.VirtualMicrophone.orig_DrawUpdate orig, VirtualMicrophone self, float timeStacker, float timeSpeed)
        {
            orig.Invoke(self, timeStacker, timeSpeed);
            WetLoop?.Update(timeStacker, timeSpeed);
        }

        //SoundId to self if you ever need it, i have gathered wisdom throughout this journey: Processmanager.Preswitchmainprocess calls soundloader.releaseallunityaudio

        /*
        readonly string[,] notesinkey = //kept for the sake of remembering what chords have what notes
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

        int debugstopwatch = 0;

        int CurrentKey = 0;
        bool inmajorscale = true;

        bool EntryRequest = true;
        bool playingchord = false;

        int chordtimer = 0;

        string NextChord = "Balaboo";         //important to set a first one

        float fichtean;
        float chordexhaustion = 1f;
        int chordsequenceiteration;

        public static int agora = 0;
        float currentAgora = 0f;

        public static string? CurrentRegion = "sl"; 

        bool fileshavebeenchecked = false;
        string[][]? ChordInfos;

        PlopWetTrack? WetLoop;
        static DisembodiedLoopEmitter? WetController;

        private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig.Invoke(self, manager);
            try
            {
                if (!fileshavebeenchecked)
                {
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
                    RainMeadow.Debug("Checking files");
                    DrumMachine.StartthefuckingWaitDicthehe();
                    string[] mydirs = AssetManager.ListDirectory("soundeffects", false, true);
                    RainMeadow.Debug("Printing all directories in soundeffects");
                    foreach (string dir in mydirs)
                    {
                        string[] arr = dir.Split(Path.DirectorySeparatorChar);
                        string filename = arr[arr.Length - 1];
                        if (filename.ToLower() == "!entries.txt")
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
                RainMeadow.Error(e);
                //throw;
            }

            WetController ??= new DisembodiedLoopEmitter(MeadowMusic.defaultMusicVolume * MeadowMusic.defaultPlopVolume, 1, 0); // RainMeadow.Debug("Created wetcontroller");

            if (WetLoop == null) 
            {
                var mic = self.cameras[0].virtualMicrophone;
                SoundLoader.SoundData sounddata = mic.GetSoundData(twentysecsilence, -1);
                WetLoop = new PlopWetTrack(mic, sounddata, WetController, 0, MeadowMusic.defaultMusicVolume * MeadowMusic.defaultPlopVolume, 1, false);
                //WE'RE SO FUCKING BACK
                RainMeadow.Debug("Created wetloop");
            }
            else
            {
                var mic = self.cameras[0].virtualMicrophone;
                WetLoop.mic = mic;
            }
        }
        public int IndexTOCKInt(int index)
        {
            int treatedkey = CurrentKey + 6;
            int[,] thescale = inmajorscale ? intsinkey : intsinmkey;
            //RainMeadow.Debug(index + "   " + inmajorscale);
            int integer = thescale[treatedkey, index - 1];
            return integer;
        }
        private void Plop(string input)
        {
            //RainMeadow.Debug("Input " + input);
            string[] parts = input.Split('-');
            string length = parts[0];
            int oct = int.Parse(parts[1]);
            bool intiseasy = int.TryParse(parts[2], out int ind);
            int extratranspose = 0;
            if (!intiseasy)
            {
                ind = int.Parse(parts[2].Substring(0, 1));
                string appends = parts[2].Substring(1);
                foreach (char letter in appends) { extratranspose = letter switch { 'b' => extratranspose--, '#' => extratranspose++, _ => extratranspose }; }
            }
            int transposition = IndexTOCKInt(ind);
            
            transposition += extratranspose; //If to the power is smart(can take negative numbers), this can work

            float humanizingrandomnessinvelocitylol = UnityEngine.Random.Range(0.3f, 1f);
            float humanizingrandomnesspanlol = UnityEngine.Random.Range(-0.18f, 0.18f);

            WetLoop?.WetPlop(length, oct, transposition, humanizingrandomnessinvelocitylol, humanizingrandomnesspanlol);
        }
        private void Plop(Liaison input)
        {
            int transposition = IndexTOCKInt(input.index) + input.accidental;
            float hvel = UnityEngine.Random.Range(0.3f, 1f);
            float hpan = UnityEngine.Random.Range(-0.18f, 0.18f);
            WetLoop?.WetPlop("M", input.octave, transposition, hvel, hpan);
        }
        private void InfluenceModulation(int Strength)
        {
            int dicedsign = UnityEngine.Random.Range(-5, 3);
            dicedsign = (dicedsign <= 0) ? -1 : 1; //6/8th chance to go downwards, unstable by choice
            int dicedint = UnityEngine.Random.Range(0, 777) / (Math.Abs(Strength) + 1);
            if (dicedint <= 7)          CurrentKey += 3 * dicedsign;
            else if (dicedint <= 44)    CurrentKey += 2 * dicedsign;
            else if (dicedint <= 77)    CurrentKey += 1 * dicedsign;

            if (UnityEngine.Random.Range(0, 101) < 4) { inmajorscale = !inmajorscale; }

            //RainMeadow.Debug($"The chance rolled {dicedint}, modified by {QueuedModulation}, it goes to {dicedsign}. So it was {deadint} and now is {CurrentKey}");

            while (CurrentKey < -6 || CurrentKey > 6) CurrentKey -= 12 * Math.Sign(CurrentKey);
            ChitChat.Analyze(this); 
        }
        private void PlayEntry()
        {
            if (EntryRequest)
            {
                //RainMeadow.Debug("Playing New Chord");
                EntryRequest = false;
                string[] entry = ChordInfos.First(l => l[0] == NextChord);
                string chordnotes = entry[1];
                string chordleadups = entry[2];
                ChitChat.Wipe();
                InfluenceModulation(2); 
                string[] inst = chordnotes.Split(',');
                string[] notes = inst[0].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string[] bassnotes = inst[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < notes.Length; i++)
                {
                    Plop("L-" + notes[i]);
                    NoteMagazine.AddSeed(notes[i]);
                }
                int sowhichoneisitboss = UnityEngine.Random.Range(0, bassnotes.Length);
                Plop("L-" + bassnotes[sowhichoneisitboss]); 

                //all notes have been played, moving onto leadup
                string[] leadups = chordleadups.Split('|');
                int butwhatnowboss = UnityEngine.Random.Range(0, leadups.Length);
                NextChord = leadups[butwhatnowboss];

                float feelingtoplay = UnityEngine.Random.Range(0, 1001) / 1000f;
                float threshholdtoplay = 0.77f - (fichtean * 0.48f) + chordexhaustion * 0.08f + chordsequenceiteration * Mathf.Lerp(0.225f, 0.125f, fichtean); //will be tweaked probs
                bool chordissequenced = feelingtoplay > threshholdtoplay;
                if (chordissequenced)
                {
                    chordexhaustion = (chordexhaustion * 1.325f) + Mathf.Lerp(1.75f, 0.55f, fichtean);
                    //RainMeadow.Debug("Chord sequenced their thing:" + chordexhaustion);
                    float sequencedwait = UnityEngine.Random.Range(100, 500 - (int)(fichtean * 375)) / 100f;
                    chordtimer = Wait.Until("half", (int)sequencedwait, debugstopwatch);
                    chordsequenceiteration++;
                }
                else
                {
                    chordexhaustion = (chordexhaustion * 2) + Mathf.Lerp(1.5f, 0.5f, fichtean);
                    chordtimer = Wait.Until("bar", (int)chordexhaustion, debugstopwatch);
                    chordsequenceiteration = 0;
                }
                NoteMagazine.Fester(this);
                playingchord = true;
            }

            if (playingchord) //holding
            {
                ChitChat.Update(this);
                if (chordtimer <= 0)
                {
                    //RainMeadow.Debug($"{NextChord} will play");       
                    playingchord = false;
                    EntryRequest = true;
                }
                else
                {
                    chordexhaustion *= Mathf.Lerp(0.9975f, 0.9925f, fichtean);
                    chordtimer--;
                }
            }    
        }
        private void PlayThing(SoundID SoundId, float velocity, float speed, VirtualMicrophone virtualMicrophone)
        {
            if (MeadowMusic.vibeIntensity == null || MeadowMusic.vibeIntensity.Value == 0f || velocity == 0f)
            {
                /*
                if (RainWorld.ShowLogs) 
                {
                    if (MeadowMusic.vibeIntensity == null) RainMeadow.Debug("VibeIntensity is undefinedwon't bother playing thing");
                    else if (MeadowMusic.vibeIntensity.Value == 0f ) RainMeadow.Debug("VibeIntensity is 0, won't bother playing thing");
                    else RainMeadow.Debug("Inputted velocity is 0, won't bother playing thing");
                }
                */
                return;
            }
            float vol = Mathf.Pow(MeadowMusic.vibeIntensity.Value, 1.65f) * MeadowMusic.defaultPlopVolume * MeadowMusic.defaultMusicVolume * velocity * 0.8f; 
            float pan = (MeadowMusic.vibePan ?? 0f) * Mathf.Pow((1f-MeadowMusic.vibeIntensity.Value) * 0.7f + 0.125f, 1.65f);
            try
            {
                if (virtualMicrophone.visualize) virtualMicrophone.Log(SoundId);
                if (!virtualMicrophone.AllowSound(SoundId)) { RainMeadow.Debug($"Too many sounds playing, denying a {SoundId}"); return; }
                
                SoundLoader.SoundData soundData = virtualMicrophone.GetSoundData(SoundId, -1);
                if (virtualMicrophone.SoundClipReady(soundData))
                {
                    VirtualMicrophone.DisembodiedSound thissound = new(virtualMicrophone, soundData, pan, vol, speed, false, 3);
                    thissound.audioSource.volume = Mathf.Clamp01(Mathf.Pow(vol * thissound.soundData.vol * thissound.mic.volumeGroups[thissound.volumeGroup] * thissound.mic.camera.game.rainWorld.options.musicVolume, thissound.mic.soundLoader.volumeExponent));
                    virtualMicrophone.soundObjects.Add(thissound);
                }
                else
                {
                    RainMeadow.Debug($"Soundclip not ready");
                    return;
                }

                if (RainWorld.ShowLogs)
                {
                    //RainMeadow.Debug($"the note that played: {SoundId} at {speed}");
                }
            }
            catch (Exception e)
            {
                RainMeadow.Debug($"Log {e}");
            }
        }
        public struct Liaison
        {
            public Liaison(int octave, int index, int accidental)
            {
                this.octave = octave;
                this.index = index;
                this.accidental = accidental;
            }
            public Liaison(string note)
            {
                string[] Hahaha = note.Split('-');
                bool intiseasy = int.TryParse(Hahaha[1], out int ind);
                int extratranspose = 0;
                if (!intiseasy)
                {
                    ind = int.Parse(Hahaha[1].Substring(0, 1));
                    string accidentals = Hahaha[1].Substring(1);
                    foreach (char accidental in accidentals) { extratranspose += accidental switch { 'b' => -1, '#' => 1, _ => 0 }; }
                }
                octave = int.Parse(Hahaha[0]);
                index = ind;
                accidental = extratranspose;
            }
            public int octave;
            public int index;
            public int accidental;
            public readonly string Note
            {
                get
                {
                    string danote = $"{octave}-{index}";
                    for (int i = 0; i < Mathf.Abs(accidental); i++) 
                    { 
                        danote.Append((accidental < 0) ? 'b' : '#'); 
                    }
                    return danote;
                }
            }
        }
        public static class ChitChat
        {
            static List<Liaison> LiaisonList = new(); //list of the Liaison(s) currently playing
            static int[] liaisonrace = new int[0]; //arp pitch sorted array that will be remade with the analyze function
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

            static public bool[] steppattern = new bool[4] { true, true, true, false };
            static int steppatternindex = 0;

            static public double arpcounterstopwatch;
            static int arprate;

            static int halfway;
            static List<int> randomsetsacrificeboard = new();
            
            static int TensionStopwatch; //this will be reset on wipe, and be the strain until a modulation or strum  //tension is chordstopwatch essentially 
            static bool ismodulation;
            static bool hasbroken;
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
                    plopmachine.InfluenceModulation(7);
                    differencebuffer = plopmachine.CurrentKey - keybefore;
                    hasswitchedscales = scalebefore == plopmachine.inmajorscale;
                }
                else
                {//transposition
                    do { differencebuffer = UnityEngine.Random.Range(-2, 3); } while (differencebuffer == 0);

                    for (int i = 0; i < LiaisonList.Count; i++)
                    {
                        Liaison NewNote = LiaisonList[i];
                        NewNote.accidental += differencebuffer;
                        LiaisonList[i] = NewNote;
                    }
                }
            }
            private static void UndoBreak(PlopMachine plopMachine)
            {
                if (ismodulation)
                {//modulation
                    plopMachine.CurrentKey -= differencebuffer;
                    while (Math.Abs(plopMachine.CurrentKey) < 6) plopMachine.CurrentKey += (plopMachine.CurrentKey < 1) ? 12 : -12;
                    if (hasswitchedscales) plopMachine.inmajorscale = !plopMachine.inmajorscale;
                }
                else
                {
                    for (int i = 0; i < LiaisonList.Count; i++)
                    {
                        Liaison NewNote = LiaisonList[i];
                        NewNote.accidental = 0;
                        LiaisonList[i] = NewNote;
                    }
                }
            }
            public static void Update(PlopMachine plopmachine)
            {
                TensionStopwatch++;   //below might be outdated
                                     //if i add it here every time, well, then, reminder that the stopwatch starts on 1, since a wipe and the start of liaisoning for the next chord happens at the same time.... well... ig that's the nature of doing a ++; at the start of ever
                                    //if (LiaisonList.Count == 1) isindividualistic = true; //until a thing can grow horns on its own, it should stay like this... but then what if it could? What if a note had a chance to spawn others that fitted to it? Check that
                                   //this is also now also decided in Add function, instead of making it a wholey other thing, becaaaaause i'm lazy... why? because this doesn't hold the door open for ^^^this expansion
                evolvestopwatch += plopmachine.fichtean*9 + 2.5f;
                
                if (UnityEngine.Random.Range(0f, 1f) + TensionStopwatch*0.00000125f > 1 && !hasbroken) 
                {
                    TensionStopwatch = 0;
                    Break(plopmachine);
                }

                if (hasbroken)
                {
                    BreakUndoStopwatch++;
                    if (UnityEngine.Random.Range(0f, 1f) + BreakUndoStopwatch*0.00002f > 1)
                    {
                        if ((UnityEngine.Random.Range(0, 12) + (int)((1 - plopmachine.fichtean) * 4)) <= 4)
                        {
                            //RainMeadow.Debug("UNDID A BREAK BUT GOOD");
                            UndoBreak(plopmachine);
                        }
                        BreakUndoStopwatch = 0;
                    }
                }

                if (strumphase != 0)
                {
                    Strum(plopmachine);
                }
                else
                {
                    if (arptimer <= 0)
                    {
                        if (LiaisonList.Count != 0) 
                        {
                            if (steppattern[steppatternindex]) CollectiveArpStep(plopmachine); 
                            int waitnumber = arprate switch { 0 => 4, 1 => 6, 2 => 8, 3 => 12, 4 => 16, _ => 16, };
                            arpcounterstopwatch += plopmachine.fichtean * (arprate + 4);
                            int arpcurrentfreq = (int)(Mathf.PerlinNoise((float)arpcounterstopwatch / 1000f, (float)arpcounterstopwatch / 4000f) * 5);
                            if (arprate != arpcurrentfreq && plopmachine.chordtimer < 96)
                            {
                                if (arprate > arpcurrentfreq)
                                {
                                    waitnumber /= 2;
                                    if (steppattern[steppatternindex]) CollectiveArpStep(plopmachine);
                                }
                            }
                            arptimer = Wait.Until($"1/{waitnumber}", 1, plopmachine.debugstopwatch);

                            if (UnityEngine.Random.Range(0f, 1f) + TensionStopwatch*0.00008 > 1) //RTYU            this is strum activationcode  //temp, will share with other. I decide now that if it's strummed, it'll roll a chance to break, but reset the "stopwatch" both use, for tension   
                            {
                                strumphase = Strumphases.queued;
                                strumtimer = Wait.Untils("half", 1, 3, plopmachine.debugstopwatch);
                                if (UnityEngine.Random.Range(0, 1001) < 69) Break(plopmachine); 
                                    //good, the break will happen before the strum.
                                TensionStopwatch = 0;
                            }
                            steppatternindex = steppatternindex + 1 < steppattern.Length ? steppatternindex + 1 : 0;
                        }
                    }
                    else
                    {
                        arptimer--;
                    }
                }
            }
            public static void Analyze(PlopMachine plopmachine)
            {
                List<int> LiaisonsFreqNumbs = new();
                foreach (Liaison Note in LiaisonList)
                {
                    //RainMeadow.Debug("We testing " + Note);
                    int freqnumb =  12 * Note.octave + plopmachine.IndexTOCKInt(Note.index) + Note.accidental;
                    LiaisonsFreqNumbs.Add(freqnumb);
                }
                int[] Staircase = new int[LiaisonList.Count];
                for (int i = 0; i < LiaisonList.Count; i++) Staircase[i] = i;
                Array.Sort(LiaisonsFreqNumbs.ToArray(), Staircase);
                liaisonrace = Staircase; //Fucked up staircase
            }
            public static void PrintRace()
            {
                RainMeadow.Debug("Liaisonrace being printed individually from left to right. The number is the index, the latter is what it represents.");
                RainMeadow.Debug("Remember that the sequence they're PRINTED in is the order of the liaisonrace, NOT the index shown(as that is just the pointer)");
                foreach (int i in liaisonrace) RainMeadow.Debug(i + " " + LiaisonList[i].Note);
            }
            public static void Instantiate(PlopMachine plopMachine)
            {
                Analyze(plopMachine);

                if (UnityEngine.Random.Range(0, 2) == 1) RandomMode();
                steppatternindex = 0;

                //bogus code to make a new step sequence sometimes    //good enough
                if (UnityEngine.Random.Range(0, 100) < 10f + plopMachine.fichtean * 15f)
                {
                    //RainMeadow.Debug("Time to change the stepsequence, yup");
                    List<bool> steppatternlist = new() { true };
                    bool satisfied = false;

                    while (!satisfied)
                    {
                        if (steppatternlist.Count == 1 && !steppattern.Contains(false) && UnityEngine.Random.Range(0, 3) != 0)
                        {
                            satisfied = true;
                            continue;
                        }

                        bool addnewone = UnityEngine.Random.Range(0, 100) > 5f * steppatternlist.Count * (plopMachine.fichtean+0.2) + 5f;

                        if (addnewone)
                        {
                            steppatternlist.Add(UnityEngine.Random.Range(0, 100) > 10f+plopMachine.fichtean*15f);
                        }
                        else
                        {
                            satisfied = true;
                        }

                    }
                    var newarray = steppatternlist.ToArray();

                    steppattern = newarray;
                    //RainMeadow.Debug("Yeah so the steppattern is apperantly" + Newtonsoft.Json.JsonConvert.SerializeObject(steppattern));
                }
            }
            public static void Add(Liaison NL)
            {
                if (LiaisonList.Exists(L => L.octave == NL.octave && L.index == NL.index)) return;
                //string period = "1/" + arprate switch { 0 => "8", 1 => "12", 2 => "16", 3 => "24", 4 => "32", _ => "32" };
                //int liaisonwait = Wait.Until(period, 1, plopmachine.debugstopwatch);
                //int amountoftimes = Mathf.Clamp(UnityEngine.Random.Range(8 - (arprate / 5 * 3), 23 - (3 * (arprate / 2))), 0, 23);
                //
                //bool[] mama = new bool[amountoftimes];
                //for (int i = 0; i < amountoftimes; i++)
                //{
                //    mama[i] = UnityEngine.Random.Range(4, ((5-arprate)*2) + 72) > 66;
                //}
                //Liaison NewLiaison = new Liaison($"M-{note}", liaisonwait, mama, UnityEngine.Random.Range(0, amountoftimes), period);
                LiaisonList.Add(NL);
            }
            private static void CheckThisLiaisonOutDude(int indexofwhereitathomie, PlopMachine plopmachine)
            {
                Liaison liaison = LiaisonList[indexofwhereitathomie];

                bool itwillevolve = UnityEngine.Random.Range(0, 800) + (int)evolvestopwatch > 1200; //Values to be joar-tweaked
                if (itwillevolve)
                {
                    evolvestopwatch = 0;
                    //RainMeadow.Debug("Evolves " + liaison.Note);
                    int oct = liaison.octave;
                    int ind = liaison.index;
                    for (int i = 0; i < 4; i++) 
                    {
                        int modifying = UnityEngine.Random.Range(-2, 2);
                        if (modifying > -1) modifying++;
                        if (modifying == -2 || modifying == 2) if (UnityEngine.Random.Range(0, 2) == 1) modifying /= 2;

                        ind += modifying; 
                        if (ind > 7) { ind -= 7; oct++; }
                        if (ind < 1) { ind += 7; oct--; }
                        Mathf.Clamp(oct, 1, 7);
                        if (LiaisonList.Exists(l => l.index != ind && l.octave != oct)) 
                        {
                            LiaisonList[indexofwhereitathomie] = new Liaison(oct, ind, liaison.accidental);
                            Analyze(plopmachine);
                            break;
                        }
                    }
                }
            }
            public static void Wipe()
            {
                LiaisonList.Clear();
                arpstep = 0;
                TensionStopwatch = 0;
                hasbroken = false;
                strumphase = 0;
                BreakUndoStopwatch = 0;
                randomsetsacrificeboard.Clear();
                arprate = (int)(Mathf.PerlinNoise((float)arpcounterstopwatch / 1000f, (float)arpcounterstopwatch / 4000f) * 5);
                liaisonrace = new int[0];
            }
            public static void RandomMode()
            {
                //arpingmode to be: "upwards" "downwards" "switchwards" "randomwards" "inwards" "outwards"
                int sowhichoneboss = UnityEngine.Random.Range(0, 6); 
                arpingmode = (Arpmode)sowhichoneboss;
                bool arpmiddlenoteistop = UnityEngine.Random.Range(0, 2) == 1; //RainMeadow.Debug("We're now arping " + arpingmode);
                halfway = (LiaisonList.Count / 2) - (arpmiddlenoteistop ? ((LiaisonList.Count + 1) % 2) : 1);
                //OKAY SO SIDE NOTE TO MYSELF, always call randommode AFTER you've found out how many liaisons are listed
                arpgoingupwards = UnityEngine.Random.Range(0, 2) == 1;
                arpstep = arpingmode switch
                {
                    Arpmode.upwards => 0,
                    Arpmode.downwards => LiaisonList.Count - 1,
                    Arpmode.switchwards => arpgoingupwards ? LiaisonList.Count - 1 : 0,
                    Arpmode.randomwards => 0,
                    Arpmode.inwards => arpgoingupwards ? 0 : LiaisonList.Count - 1,
                    Arpmode.outwards => arpgoingupwards ? 0 : LiaisonList.Count - 1,
                    _ => 0
                };
            }
            public static void CollectiveArpStep(PlopMachine plopmachine)
            {
                //RainMeadow.Debug(LiaisonList.Count + "   " + arpstep + "    " + arpingmode + "   " + halfway + "   " + arpgoingupwards);
                plopmachine.Plop(LiaisonList[liaisonrace[arpstep]]); //so it plays the arp it *leaves*
                CheckThisLiaisonOutDude(liaisonrace[arpstep], plopmachine);

                switch (arpingmode)
                {
                    case Arpmode.upwards:
                        arpstep = ((arpstep + 1) < LiaisonList.Count) ? (arpstep + 1) : 0;
                        break;

                    case Arpmode.downwards:
                        arpstep = ((arpstep - 1) < 0) ? (LiaisonList.Count - 1) : (arpstep - 1);
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
                            foreach (int i in liaisonrace) randomsetsacrificeboard.Add(i);
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
                            lookoutfor = halfway + 1;
                            if (arpstep >= lookoutfor)
                            {
                                arpgoingupwards = false;
                                arpstep = LiaisonList.Count - 1;
                            }
                        }
                        else
                        {
                            arpstep--;
                            lookoutfor = halfway;
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
                            { 
                                arpstep = halfway;
                                arpgoingupwards = false;
                            }
                        }
                        else
                        {
                            arpstep--;
                            if (arpstep < 0)
                            {
                                arpstep = halfway + 1;
                                arpgoingupwards = true;
                            }
                        }
                        break;
                }
            }

            static Strumphases strumphase;
            enum Strumphases
            {
                notplaying,
                queued,
                playing,
                epilogue,
            }
            static int strumindex;
            static bool strumdirectionupwards; //true = upwards. false = downwards
            static int strumtimer = 20;
            private static void Strum(PlopMachine plopmachine)
            {
                switch (strumphase)
                {
                    case Strumphases.queued:
                        strumtimer--;
                        //if strumqueuetimer has reached zero: strum is no longer queued, it will start playing next tick (strumplaying/strumstrumming = true, strumqueued = false, strumtimer = 0)
                        if (strumtimer <= 0)
                        {
                            strumdirectionupwards = arpingmode switch
                            {
                                Arpmode.upwards => false,
                                Arpmode.downwards => true,
                                Arpmode.switchwards => !arpgoingupwards,
                                Arpmode.randomwards => UnityEngine.Random.Range(0, 2) == 0,
                                Arpmode.inwards => arpgoingupwards,
                                Arpmode.outwards => !arpgoingupwards,
                                _ => true
                            };
                            //depending on strumdirection, strumindex will be: "upwards" = 0, "downwards" = (liaisonlist.count-1) //(cuz if there's 4 things, then 3 is maxindex)
                            strumindex = strumdirectionupwards ? 0 : LiaisonList.Count - 1;
                            strumphase++;
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
                            plopmachine.Plop(LiaisonList[liaisonrace[strumindex]]);
                            strumtimer = (int)(plopmachine.fichtean * 4);  //which is essentially //perlinnoise(1, 4) (1, 2, 3)
                            if (strumdirectionupwards) { strumindex++; }
                            else { strumindex--; }

                            if (strumindex < 0 || strumindex > LiaisonList.Count - 1)
                            {
                                strumphase++;
                                strumtimer = Wait.Until("bar", 0, plopmachine.debugstopwatch);
                            }
                        }
                        break;

                    case Strumphases.epilogue:
                        if (strumtimer > 0)
                        {
                            strumtimer--;   //maybe in the future we can do some BONUS STRUMS  <-- but not now :P
                        }
                        else
                        {
                            strumphase = 0;
                        }
                        break;
                }
            }
        }

        public static class NoteMagazine
        {
            static List<Liaison> InNoteList = new(); 
            static List<Liaison> OutNoteList = new(); 
            //one at a time kid
            static readonly Dictionary<int, string> SoloLineageDict = new()
            {
                //{"fuckineedtofindouthowtowritethishere", "Ambientynote|Chordy Notes"},//SoloLineageDict.Add("fuckineedtofindouthowtowritethishere", "Ambientynote|Chordy Notes");
                {1, "-5 =1 =3 =5|-6 =2 =4"},                            //SoloLineageDict.Add("4-1", "3-5 4-1 4-3 4-5|3-6 4-2 4-4");
                {2, "-6 =2 =5 =6|-4 =1 =3 =4"},                         //SoloLineageDict.Add("4-2", "3-6 4-2 4-5 4-6|3-4 4-1 4-3 4-4");
                {3, "-6 =2 =3 =6 +1|-7 =3 =4 =7"},                      //SoloLineageDict.Add("4-3", "3-6 4-2 4-3 4-6 5-1|3-7 4-3 4-4 4-7");
                {4, "-5 =1 =4 =5 +3|-5 =2 =5 +2"},                      //SoloLineageDict.Add("4-4", "3-5 4-1 4-4 4-5 5-3|3-5 4-2 4-5 5-2");
                {5, "=1 =5 +1 +3 +5|=2 =6 =7 +3 +6"},                   //SoloLineageDict.Add("4-5", "4-1 4-5 5-1 5-3 5-5|4-2 4-6 4-7 5-3 5-6");
                {6, "=6 +1 +2 +3 +6 =2|=3 =5 +2 +4"},                   //SoloLineageDict.Add("4-6", "4-6 5-1 5-2 5-3 5-6 4-2|4-3 4-5 5-2 5-4");
                {7, "-3 -7 =1 =5 =7|-6 =2 =4 =5" }                      //SoloLineageDict.Add("4-7", "3-3 3-7 4-1 4-5 4-7|3-6 4-2 4-4 4-5");
            };
            //thanks dad it's time for duo
            static readonly Dictionary<int, string> DuoLineageDict = new()
            {
                //{"timeforsecondof painyo", "yeah|yeah"},              //DuoLineageDict.Add("timeforsecondof painyo", "yeah|yeah");
                {(6*1) + 2, "-5 =1 =2 =3 =6 +2|-7 =1 =2 =4 =6 +3"},     //DuoLineageDict.Add("4-1 4-2", "3-5 4-1 4-2 4-3 4-6 5-2|3-7 4-1 4-2 4-4 4-6 5-3");
                {(6*1) + 3, "-6 =1 =3 =5 +2|-6 =4 =5 =6"},              //DuoLineageDict.Add("4-1 4-3", "3-6 4-1 4-3 4-5 5-2|3-6 4-4 4-5 4-6");
                {(6*1) + 4, "-5 =1 =4 =6 +3|=2 =5 =6 +2"},              //DuoLineageDict.Add("4-1 4-4", "3-5 4-1 4-4 4-6 5-3|4-2 4-5 4-6 5-2");
                {(6*1) + 5, "-5 =1 =5 +1 +3|=2 =4 =5 =7 +2"},           //DuoLineageDict.Add("4-1 4-5", "3-5 4-1 4-5 5-1 5-3|4-2 4-4 4-5 4-7 5-2");
                {(6*1) + 6, "-5 =2 =5 +1 +5|-5 =2 =5 +1"},              //DuoLineageDict.Add("4-1 4-6", "3-5 4-2 4-5 5-1 5-5|3-5 4-2 4-5 5-1");
                {(6*1) + 7, "=1 =5 =7 +2|=2|=5 =7 +1 +2"},              //DuoLineageDict.Add("4-1 4-7", "4-1 4-5 4-7 5-2|4-2|4-5 4-7 5-1 5-2");
                {(6*2) + 3, "-6 =2 =3 =6 +2|-2 =2 =4 =5 +2"},           //DuoLineageDict.Add("4-2 4-3", "3-6 4-2 4-3 4-6 5-2|3-2 4-2 4-4 4-5 5-2");
                {(6*2) + 4, "-6 =2 =4 +4|-7 =3 =7 +1"},                 //DuoLineageDict.Add("4-2 4-4", "3-6 4-2 4-4 5-4|3-7 4-3 4-7 5-1");
                {(6*2) + 5, "=2 =5 =7 +2|-7 =6 +3 +5"},                 //DuoLineageDict.Add("4-2 4-5", "4-2 4-5 4-7 5-2|3-7 4-6 5-3 5-5");
                {(6*2) + 6, "-5 =2 =5 =6 +2|-6 =3 =7 +3"},              //DuoLineageDict.Add("4-2 4-6", "3-5 4-2 4-5 4-6 5-2|3-6 4-3 4-7 5-3");
                {(6*2) + 7, "-5 =2 =3 =7|=3 =5 +1 +5"},                 //DuoLineageDict.Add("4-2 4-7", "3-5 4-2 4-3 4-7|4-3 4-5 5-1 5-5");
                {(6*3) + 4, "-6 =3 =4 =6 +3|-7 =5 +1 +4"},              //DuoLineageDict.Add("4-3 4-4", "3-6 4-3 4-4 4-6 5-3|3-7 4-5 5-1 5-4");
                {(6*3) + 5, "-5 =3 =5 +3 +7|=1 =2 =4 =6 +2"},           //DuoLineageDict.Add("4-3 4-5", "3-5 4-3 4-5 5-3 5-7|4-1 4-2 4-4 4-6 5-2");
                {(6*3) + 6, "=1 =3 =6 +5|-5 =1 =6 +1"},                 //DuoLineageDict.Add("4-3 4-6", "4-1 4-3 4-6 5-5|3-5 4-1 4-6 5-1");
                {(6*3) + 7, "-6 =3 =7 +3|=4 =6 +1 +3 +5"},              //DuoLineageDict.Add("4-3 4-7", "3-6 4-3 4-7 5-3|4-4 4-6 5-1 5-3 5-5");
                {(6*4) + 5, "-5 =1 =4 =5 +6|=2 =6 +1 +2"},              //DuoLineageDict.Add("4-4 4-5", "3-5 4-1 4-4 4-5 5-6|4-2 4-6 5-1 5-2");
                {(6*4) + 6, "-5 =1 =4 =6 +3 +6|-6 =4 =6 +1 +4 +7"},     //DuoLineageDict.Add("4-4 4-6", "3-5 4-1 4-4 4-6 5-3 5-6|3-6 4-4 4-6 5-1 5-4 5-7");
                {(6*4) + 7, "-5 =4 =6 =7 +3|-7 =3 =5 +4"},              //DuoLineageDict.Add("4-4 4-7", "3-5 4-4 4-6 4-7 5-3|3-7 4-3 4-5 5-4");
                {(6*5) + 6, "-6 =5 =6 +2 +6|=1 =5 =7 +5"},              //DuoLineageDict.Add("4-5 4-6", "3-6 4-5 4-6 5-2 5-6|4-1 4-5 4-7 5-5");
                {(6*5) + 7, "-5 =1 =5 =7 +5|-4 -7 =4 =6 +4"},           //DuoLineageDict.Add("4-5 4-7", "3-5 4-1 4-5 4-7 5-5|3-4 3-7 4-4 4-6 5-4");
                {(6*6) + 7, "-5 =2 =6 =7 +3 +5|-4 -7 =3 =5 +1 +4"}      //DuoLineageDict.Add("4-6 4-7", "3-5 4-2 4-6 4-7 5-3 5-5|3-4 3-7 4-3 4-5 5-1 5-4");
            };                                                          
            public static void AddSeed(string Note) 
            { 
                InNoteList.Add(new Liaison(Note)); 
            }
            public static void Fester(PlopMachine plopmachine)
            {
                int decidedamount = (int)Mathf.Lerp(7.25f, 2.75f, plopmachine.fichtean); 

                for (int tries = 0; tries < 10; tries++)
                {
                    Grows(plopmachine);
                    if (OutNoteList.Count > decidedamount) break;
                }

                foreach (Liaison bullet in OutNoteList)
                {
                    ChitChat.Add(bullet);
                    //RainMeadow.Debug($"Pushed a {bullet}Thing");
                }
                ChitChat.Instantiate(plopmachine);
                InNoteList.Clear();
                OutNoteList.Clear();
            }
            private static void Grows(PlopMachine plopmachine)
            {
                Liaison Note1 = InNoteList[UnityEngine.Random.Range(0, InNoteList.Count)];
                Liaison Note2 = InNoteList[UnityEngine.Random.Range(0, InNoteList.Count)];

                bool FirstUpper = Note1.index > Note2.index;
                int LowNote = FirstUpper ? Note2.index : Note1.index;
                int HighNote = FirstUpper ? Note1.index : Note2.index;
                int HighExtras = FirstUpper ? Note1.accidental : Note2.accidental;
                int HighOctave = FirstUpper ? Note1.octave : Note2.octave;
                string NoteValue;

                if (Note1.index == Note2.index) {
                    _ = SoloLineageDict.TryGetValue((UnityEngine.Random.Range(0, 3) == 0) ? LowNote : HighNote, out NoteValue);
                }
                else {
                    _ = DuoLineageDict.TryGetValue((6 * LowNote) + HighNote, out NoteValue);
                }
                float bias = Mathf.Pow(-Mathf.Cos(plopmachine.fichtean * Mathf.PI), 0.52f) / 2 + 0.5f;
                float does_the_church_allow_it = UnityEngine.Random.Range(1f, 0f);
                int MartinLutherKing = (bias > does_the_church_allow_it) ? 1 : 0;
                string[] heaven_or_hell = NoteValue.Split('|');
                string which_one_will_you_choose = heaven_or_hell[MartinLutherKing];
                string[] the_begotten = which_one_will_you_choose.Split(' ');
                string the_One = the_begotten[UnityEngine.Random.Range(0, the_begotten.Length)];
                //added "clarity"... god really was with me when i made this

                int FinalOct = HighOctave + (int)(the_One[0] switch { '-' => -1, '=' => 0, '+' => 1, _ => 0 });
                if (FinalOct > 7) FinalOct = UnityEngine.Random.Range(3, 7);
                Liaison FinalNote = new(FinalOct, the_One[1] - '0', HighExtras);

                if (!InNoteList.Contains(FinalNote)) InNoteList.Add(FinalNote);
                if (!OutNoteList.Contains(FinalNote)) OutNoteList.Add(FinalNote);
            }
        }

        public static class DrumMachine
        {
            private static float linearToDb(float linear) { return Mathf.Pow(linear / 20f, 10); }
            private static float dbToLinear(float db) { return Mathf.Log(db) * 20f; }
            //private static float velocityToDb(float velocity) { return } 
            enum DrumGender
            {
                None,
                Kick,
                Snare,
                HiHat,
                Perc
            }
            struct Fill
            {
                public Fill(float velocity, string pausefor, int rests, float chancetoplay = 1)
                {
                    //if (velocity < 0f || velocity > 1f) velocity = dbToLinear(velocity);
                    this.velocity = velocity;
                    restvalue = pausefor;
                    restcount = rests;
                    chance = chancetoplay;
                }
                public float velocity;
                public string restvalue;
                public int restcount;
                public float chance;
            }
            struct Track
            {
                public Track(Fill[] sequence, SoundID sample, DrumGender inttrack)
                {
                    this.sequence = sequence;
                    this.sample = sample;
                    track = inttrack;
                    timer = 1;
                    sequenceindex = 0;
                }

                public Track(Fill[] sequence)
                {
                    this.sequence = sequence;
                    sample = SoundID.None;
                    track = DrumGender.None;
                    timer = 1;
                    sequenceindex = 0;
                }

                public Fill[] sequence;
                public SoundID sample;
                public DrumGender track;
                public int timer;
                public int sequenceindex;
                public void Reset()
                {
                    sequenceindex = 0;
                    timer = 0;
                }
                public Fill? Update(PlopMachine plopMachine)
                {
                    if (timer <= 0)
                    {
                        Fill filltoplay = sequence[sequenceindex];
                        timer = Wait.Until(filltoplay.restvalue, filltoplay.restcount, plopMachine.debugstopwatch);
                        //RainMeadow.Debug(timer + "  " + hittoplay.waiting + "  " + hittoplay.waiters + "  " +  plopMachine.debugstopwatch + "    " + plopMachine.debugstopwatch % WaitDict["1/8"] + "    " + plopMachine.debugstopwatch % WaitDict["1"]);
                        sequenceindex = (sequenceindex == sequence.Length - 1) ? 0 : sequenceindex + 1;
                        return filltoplay;
                    }
                    else
                    {
                        timer--;
                    }
                    return null;
                }
            }

            public static void Update(VirtualMicrophone mic, PlopMachine plopMachine)
            {
                for (int i = 0; i < tracks.Count; i++)
                {
                    Track track = tracks[i];
                    Fill? step = track.Update(plopMachine);
                    tracks[i] = track; 
                    if (step != null && step.Value.velocity != 0)
                    {
                        if (step.Value.chance != 1f && step.Value.chance < UnityEngine.Random.Range(0f, 1f)) continue;
                        PlayDrum(track.track, step.Value.velocity, mic, plopMachine);
                    }
                }
                //impulse from a main loop will trigger a random fill of x length
                //impulse from a main thingy will trigger every loop to reset.
            }
            static float? velOfLastOpenHat;
            private static void PlayDrum(DrumGender gender, float vel, VirtualMicrophone mic, PlopMachine plopMachine)
            {
                SoundID sample;
                float trackvol = 0;
                float speed = 1f;

                switch (gender)
                {
                    case DrumGender.None:
                        trackvol = 0f;
                        sample = Perc1;
                        break;

                    case DrumGender.Kick:
                        trackvol = Mathf.Clamp01(plopMachine.currentAgora * 0.4f - 0.5f);
                        sample = Kick;
                        break;

                    case DrumGender.Snare:
                        trackvol = Mathf.Clamp01(plopMachine.currentAgora * 0.5f - 1f);
                        sample = Snare;
                        break;

                    case DrumGender.HiHat:
                        trackvol = Mathf.Clamp01(plopMachine.currentAgora * 0.35f - 1f);

                        if (velOfLastOpenHat.HasValue)
                        {
                            if (mic.soundObjects.FirstOrDefault(c => c.soundData.soundID == OpenHat) is VirtualMicrophone.DisembodiedSound thingy) thingy.Destroy();
                            vel = velOfLastOpenHat.Value;
                            velOfLastOpenHat = null;
                        }
                        sample = HiHat;
                        if (plopMachine.currentAgora > 4 && UnityEngine.Random.Range(0f, 1f) < Mathf.Clamp01(plopMachine.currentAgora * 0.5f - 2.4f) * 0.05f)
                        {
                            sample = OpenHat;
                            velOfLastOpenHat = vel;
                        }
                        break;

                    case DrumGender.Perc:
                        trackvol = Mathf.Clamp01(plopMachine.currentAgora * 0.27f - 1.6f);
                        sample = Perc1;
                        int index = UnityEngine.Random.Range(0, 8) switch { 0 => 1, 1 => 1, 2 => 1, 3 => 1, 4 => 2, 5 => 3, 6 => 5, 7 => 7, _ => 7 };
                        int semitone = plopMachine.IndexTOCKInt(index);
                        speed *= Mathf.Pow(2, semitone / 12f);
                        break;

                    default:
                        trackvol = 0f;
                        sample = Perc1;
                        break;
                }
                plopMachine.PlayThing(sample, vel * trackvol, speed, mic);
            }


            static List<Track> tracks = new();
            public static void StartthefuckingWaitDicthehe()
            {
                tracks.Add(new Track (new Fill[] {
                    new(1f, "1/4", 3),
                    new(0.4f, "1/4", 1, 0.6f) }, 
                Kick, DrumGender.Kick));

                tracks.Add(new Track(new Fill[] {
                    new(0f, "1/2", 1), 
                    new(0.85f, "1/2", 1) }, 
                Snare, DrumGender.Snare));

                tracks.Add(new Track(new Fill[] {
                    new(0.3f, "1/8", 1),
                    new(0.55f, "1/8", 1, 0.8f)},
                HiHat, DrumGender.HiHat));

                tracks.Add(new Track(new Fill[] {
                    new(0.69f, "1/16", 6), 
                    new(0.53f, "1/16", 3, 0.3f) }, 
                Perc1, DrumGender.Perc));
            }
        }

        static Dictionary<string, int> WaitDict = new()
        {
            {"bar", 96}, {"half", 48},{"quarter", 24},{"eight", 12},{"sixteenth", 6},{"thirtysecond", 3},
            {"quarterT", 32},{"eightT", 16}, {"sixteenthT", 8}, {"thirtysecondT", 4},{"sixtyfourthT", 2},
            {"hundredandtwentyeightT", 1}, {"twobars", 192}, {"threebars", 288}, {"barbar", 384},
             {"2", 192},{"1",   96}, {"1/2",  48},{"1/4",  24}, {"1/8",  12}, {"1/16", 6}, {"1/32",   3},
             {"2/3",64},{"1/3", 32}, {"1/6",  16},{"1/12",  8}, {"1/24",  4}, {"1/48", 2}, {"1/96",   1},
            {"2/4T",64},{"1/4T",32}, {"1/8T", 16},{"1/16T", 8}, {"1/32T", 4}, {"1/64T",2}, {"1/128T", 1}, 
        };

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


        private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);

            try // music code still has some instabilities
            {
                //if is meadowgamemode if not return lmao
                if (OnlineManager.lobby?.gameMode is MeadowGameMode)
                {
                    var mic = self.cameras[0].virtualMicrophone;
                    CurrentRegion = self.world?.region?.name ?? "sl";
                    currentAgora = RWCustom.Custom.LerpAndTick(currentAgora, agora, 0.008f, 0.0005f);
                    if (MeadowMusic.AllowPlopping)
                    {
                        debugstopwatch++;
                        float x = Mathf.PerlinNoise(debugstopwatch / 1000f, debugstopwatch / 4000f);
                        fichtean = (Mathf.Pow(x, 1 / (currentAgora / 2 + 1)) + x) / 2;
                        PlayEntry();
                        DrumMachine.Update(mic, this);
                    }

                    if (Input.GetKey("f") && !ol2)
                    {
                        //RainMeadow.Debug("Manually fading out song");
                        //self.manager.musicPlayer.song.FadeOut(30f);
                    }
                    ol2 = Input.GetKey("f");


                    if (Input.GetKey("e") && !ol1)
                    {
                        agora++;
                    }
                    ol1 = Input.GetKey("e");
                    if (Input.GetKey("q") && !ol3)
                    {
                        agora--;
                    }
                    ol3 = Input.GetKey("q");
                }
            }
            catch (Exception e)
            {
                RainMeadow.Error(e); // log but don't throw
            }
        }

        public static readonly SoundID twentysecsilence = new SoundID("twentysecsilence", register: true);
        public static readonly SoundID Kick  = new SoundID("Kick", register: true);
        public static readonly SoundID Snare = new SoundID("Snare", register: true);
        public static readonly SoundID HiHat = new SoundID("HiHat", register: true);
        public static readonly SoundID OpenHat = new SoundID("OpenHat", register: true);
        public static readonly SoundID Perc1 = new SoundID("Perc1", register: true);
    }
}
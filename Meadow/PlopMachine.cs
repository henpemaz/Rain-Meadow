using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

        string chordnotes = "yosup";
        string chordleadups = "yosup";

        int chordtimer = 0;

        string NextChord = "Balaboo";         //important to set a first one

        float fichtean;
        float chordexhaustion = 1f;
        int chordsequenceiteration;

        public static int agora = 0;
        float currentagora = 0f;

        public static string? CurrentRegion = "sl"; 

        bool fileshavebeenchecked = false;
        string[][]? ChordInfos;

        DisembodiedWetLoop? WetLoop;
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

                    StartthefuckingWaitDict();
                    NoteMagazine.Fuckinginitthatdictlineagebitch();
                    RainMeadow.Debug("Checking files");
                    string[] mydirs = AssetManager.ListDirectory("soundeffects", false, true);
                    RainMeadow.Debug("Printing all directories in soundeffects");
                    foreach (string dir in mydirs)
                    {
                        string[] arr = dir.Split(Path.DirectorySeparatorChar);
                        string filename = arr[arr.Length - 1];
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
                RainMeadow.Error(e);
                //throw;
            }

            WetController ??= new DisembodiedLoopEmitter(MeadowMusic.defaultMusicVolume * MeadowMusic.defaultPlopVolume, 1, 0); // RainMeadow.Debug("Created wetcontroller");

            if (WetLoop == null) 
            {
                var mic = self.cameras[0].virtualMicrophone;
                SoundLoader.SoundData sounddata = mic.GetSoundData(twentysecsilence, -1);
                WetLoop = new DisembodiedWetLoop(mic, sounddata, WetController, 0, MeadowMusic.defaultMusicVolume * MeadowMusic.defaultPlopVolume, 1, false);
                //WE'RE SO FUCKING BACK
                RainMeadow.Debug("Created wetloop");
            }
        }
        public int IndexTOCKInt(int index)
        {
            int treatedkey = CurrentKey + 6;
            int[,] thescale = inmajorscale ? intsinkey : intsinmkey;
            int integer = thescale[treatedkey, index - 1];
            return integer;
        }
        private void Plop(string input)
        {
            string[] parts = input.Split('-');
            string length = parts[0];
            int oct = int.Parse(parts[1]);
            bool intiseasy = int.TryParse(parts[2], out int ind);
            int extratranspose = 0;
            if (!intiseasy)
            {
                string appends = parts[2].Substring(1);
                foreach (char letter in appends) { extratranspose = letter switch { 'b' => extratranspose--, '#' => extratranspose, _ => extratranspose }; }
                ind = int.Parse(parts[2].Substring(0, 1));
            }
            int transposition = IndexTOCKInt(ind);
            
            transposition += extratranspose; //If to the power is smart(can take negative numbers), this can work

            float humanizingrandomnessinvelocitylol = UnityEngine.Random.Range(0.3f, 1f);
            float humanizingrandomnesspanlol = UnityEngine.Random.Range(-0.12f, 0.12f);

            WetLoop.WetPlop(length, oct, transposition, humanizingrandomnessinvelocitylol, humanizingrandomnesspanlol);
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

            while (CurrentKey < -6 || CurrentKey > 6)
            {
                if (CurrentKey < 1) CurrentKey += 12;
                else CurrentKey -= 12;
            }
            ChitChat.Analyze(this); 
        }
        private void PlayEntry()
        {
            if (EntryRequest)
            {
                //RainMeadow.Debug("Playing New Chord");
                EntryRequest = false;
                string[] entry = ChordInfos.First(l => l[0] == NextChord);
                chordnotes = entry[1];
                chordleadups = entry[2];
                ChitChat.Wipe(this);
                InfluenceModulation(2); 
                string[] inst = chordnotes.Split(',');
                string[] notes = inst[0].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string[] bassnotes = inst[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < notes.Length; i++)
                {
                    Plop(notes[i]);
                    NoteMagazine.AddSeed(notes[i].Substring(2));
                }
                int sowhichoneisitboss = UnityEngine.Random.Range(0, bassnotes.Length);
                Plop(bassnotes[sowhichoneisitboss]); 

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
            float vol = Mathf.Pow(MeadowMusic.vibeIntensity.Value, 1.65f) * MeadowMusic.defaultPlopVolume * MeadowMusic.defaultMusicVolume * velocity; 
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

            static public bool[] steppattern = new bool[4];
            static int steppatternindex = 0;

            static public double arpcounterstopwatch;
            static int arprate;
            
            static List<int> randomsetsacrificeboard = new List<int>();
            static bool arpmiddlenoteistop;
            
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
                    string appendedaccidentals = differencebuffer switch { 2 => "##", 1 => "#", -1 => "b", -2 => "bb", _ => "" };

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

                    plopMachine.CurrentKey -= differencebuffer;
                    while (Math.Abs(plopMachine.CurrentKey) < 6) plopMachine.CurrentKey += (plopMachine.CurrentKey < 1) ? 12 : -12;
                    if (hasswitchedscales) plopMachine.inmajorscale = !plopMachine.inmajorscale;
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
            public static void Update(PlopMachine plopmachine)
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
                            //RainMeadow.Debug("UNDID A BREAK BUT GOOD");
                            UndoBreak(plopmachine);
                        }
                        BreakUndoStopwatch = 0;
                    }
                }

                if (isindividualistic) //upperswitch is true here
                {
                    Anarchy(plopmachine);
                    upperswitch = true;
                }
                else //shall be false here
                {

                    if (upperswitch)
                    {
                        Analyze(plopmachine);
                        upperswitch = false;
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
                                arpcounterstopwatch += plopmachine.fichtean * 12 + 4;
                                int waitnumber = arprate switch { 0 => 4, 1 => 6, 2 => 8, 3 => 12, 4 => 16, _ => 16, };
                                int arpcurrentfreq = (int)(Mathf.PerlinNoise((float)arpcounterstopwatch / 1000f, (float)arpcounterstopwatch / 4000f) * 5);
                                if (arprate != arpcurrentfreq && plopmachine.chordtimer < 96)
                                {
                                    if (arprate > arpcurrentfreq)
                                    {
                                        waitnumber /= 2;
                                        if (steppattern[steppatternindex]) CollectiveArpStep(plopmachine);
                                    }
                                    else
                                    {
                                        //if (plopmachine.chordtimer == 48-1) waitnumber /= 2; 
                                        //so this shall be remade if (plopmachine.chordtimer < 48-1) waitnumber *= 2; //doesn't work artistically
                                    }
                                }
                                //(note from self i'm keeping btecause funny (it's about timer)PLUSS ONE because its' fucking... because this one plays it at the exact same time??? And goes downward for some reason??? Oh wait it's because it starts HERE. At THIS MOMENT, if it was a wait, there would be 24 until the next.
                                arptimer = Wait.Until($"1/{waitnumber}", 1, plopmachine.debugstopwatch);

                                if (UnityEngine.Random.Range(0, 150000) + TensionStopwatch*12 > 150000) //RTYU            this is strum activationcode  //temp, will share with other. I decide now that if it's strummed, it'll roll a chance to break, but reset the "stopwatch" both use, tension   
                                {
                                    strumphase = Strumphases.queued;
                                    strumtimer = Wait.Untils("half", 1, 3, plopmachine.debugstopwatch);
                                    if (UnityEngine.Random.Range(0, 1001) < 69) //good, the break will happen before the strum.
                                    {
                                        Break(plopmachine);
                                    }
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
            }
            private static void Anarchy(PlopMachine plopmachine)
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
                        //RainMeadow.Debug("Playing a note from Chitchat.Anarchy");
                        if (liaison.pattern[liaison.patternindex])
                        {
                            bool nextnoteexists = liaison.patternindex + 1 < liaison.pattern.Length ? liaison.pattern[liaison.patternindex + 1] : liaison.pattern[0];
                            if (nextnoteexists) plopmachine.Plop($"S-{liaison.note.Substring(2, 3)}");
                            else plopmachine.Plop(liaison.note);
                        }
                        int liaisonwait = Wait.Until(liaison.period, 1, plopmachine.debugstopwatch);
                        int lolol = liaison.patternindex + 1;
                        if (lolol >= liaison.pattern.Length) lolol = 0; 
                        LiaisonList[i] = new Liaison(liaison.note, liaisonwait, liaison.pattern, lolol, liaison.period);
                        CheckThisLiaisonOutDude(i, plopmachine);
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
                    string[] hey = heyo.note.Substring(2).Split('-');
                    bool intiseasy = int.TryParse(hey[1], out int ind);
                    int extratranspose = 0;
                    if (!intiseasy)
                    {
                        string accidentals = hey[1].Substring(1);
                        foreach (char accidental in accidentals)
                        {
                            extratranspose += accidental switch { 'b' => -1, '#' => 1, _ => 0 };
                        }
                        ind = int.Parse(hey[1].Substring(0, 1));
                    }
                    int transposition = plopmachine.IndexTOCKInt(ind);
                        
                    int freqnumb = int.Parse(hey[0]) * 12 + transposition + extratranspose;
                    LiaisonsFreqNumbs.Add(freqnumb);
                    index++;
                }//there's ceraintly a better and less costly ways of going about but :PPPPPP

                int[] LiaisonIndexArrayThatllBeSwayed = new int[index];
                for (int i = 0; i < index; i++) LiaisonIndexArrayThatllBeSwayed[i] = i;
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
                if (LiaisonList.Count < 3) 
                { 
                    isindividualistic = true; 
                    //If it's less than three, some arpegiation patterns will break, we therefore do anarchic.
                }
                else
                {
                    //RainMeadow.Debug("YO");
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
                //arpingmode = Arpmode.upwards; //FOR TESTING, REMOVE AFTERWARDS
                steppatternindex = 0;

                //bogus code to make a new step sequence sometimes
                if (UnityEngine.Random.Range(0, 100) < 10f + plopMachine.fichtean * 15f)
                {
                    //RainMeadow.Debug("Time to change the stepsequence, yup");
                    List<bool> steppatternlist = new() { true };
                    bool satisfied = false;

                    while (!satisfied)
                    {
                        if (steppatternlist.Count == 1)
                        {
                            if (!steppattern.Contains(false))
                            {
                                if (UnityEngine.Random.Range(0, 3) != 0)
                                {
                                    satisfied = true;
                                    continue;
                                }
                            }
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
            public static void Add(string note, PlopMachine plopmachine)
            {
                if (LiaisonList.Exists(L => L.note == note.Substring(2))) return;
                string period = "1/" + arprate switch { 0 => "8", 1 => "12", 2 => "16", 3 => "24", 4 => "32", _ => "32" };
                int liaisonwait = Wait.Until(period, 1, plopmachine.debugstopwatch);
                int amountoftimes = Mathf.Clamp(UnityEngine.Random.Range(8 - (arprate / 5 * 3), 23 - (3 * (arprate / 2))), 0, 23);
                
                bool[] mama = new bool[amountoftimes];
                for (int i = 0; i < amountoftimes; i++)
                {
                    mama[i] = UnityEngine.Random.Range(4, ((5-arprate)*2) + 72) > 66;
                }

                Liaison NewLiaison = new($"M-{note}", liaisonwait, mama, UnityEngine.Random.Range(0, amountoftimes), period);
                LiaisonList.Add(NewLiaison);
            }
            private static void CheckThisLiaisonOutDude(int indexofwhereitathomie, PlopMachine plopmachine)
            {
                Liaison liaison = LiaisonList[indexofwhereitathomie];

                bool itwillevolve = UnityEngine.Random.Range(0, 800) + (int)evolvestopwatch > 1200; //Values to be joar-tweaked
                if (itwillevolve)
                {
                    evolvestopwatch = 0;
                    //RainMeadow.Debug("Evolves " + liaison.note);

                    string[] parts = liaison.note.Split('-');

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
                        willmodify = true; //copied straight from the best coder on earth, me, when wriding Add()  <<-- with all of it's wrongs apperantly
                        int modifying = UnityEngine.Random.Range(-2, 1);
                        if (modifying > -1) modifying++;
                        if (modifying == -2 || modifying == 2) if (UnityEngine.Random.Range(0, 2) == 1) modifying /= 2;

                        ind += modifying;

                        if (ind > 7) { ind -= 7; oct++; }
                        if (ind < 1) { ind += 7; oct--; }

                        if (oct < 1) oct++;
                        if (oct > 7) oct--;
                        liaison.note = $"M-{oct}-{ind}{accidentals}"; // string construction = "M-" + Convert.ToString(oct) + "-" + Convert.ToString(ind) + accidentals;
                        willmodify = LiaisonList.Exists(l => l.note == liaison.note);
                        attempts++;
                    } while (!willmodify && attempts < 4);

                    if (attempts >= 4) 
                    { 
                        //RainMeadow.Debug("Oh no can't fuck with it"); 
                    }
                    else
                    {
                        LiaisonList[indexofwhereitathomie] = liaison;
                        Analyze(plopmachine);
                        //RainMeadow.Debug("To " + liaison.note);
                    }
                }
            }
            public static void Wipe(PlopMachine plopmachine)
            {
                LiaisonList.Clear();
                arpstep = 0;
                TensionStopwatch = 0;
                hasbroken = false;
                strumphase = 0;
                BreakUndoStopwatch = 0;
                randomsetsacrificeboard.Clear();
                arprate = (int)(Mathf.PerlinNoise((float)arpcounterstopwatch / 1000f, (float)arpcounterstopwatch / 4000f) * 5);
                if (!isindividualistic) { Analyze(plopmachine); }
            }
            public static void RandomMode()
            {
                //arpingmode to be: "upwards" "downwards" "switchwards" "randomwards" "inwards" "outwards"
                int sowhichoneboss = UnityEngine.Random.Range(0, 6); 
                arpingmode = (Arpmode)sowhichoneboss;
                arpmiddlenoteistop = UnityEngine.Random.Range(0, 2) == 1;
                //RainMeadow.Debug("We're now arping " + arpingmode);
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
                //RainMeadow.Debug(arpstep + "    " + arpingmode + "   " + arpmiddlenoteistop + "   " + arpgoingupwards);
                plopmachine.Plop(LiaisonList[liaisonrace[arpstep]].note); //so it plays the arp it *leaves*
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
                            lookoutfor = (LiaisonList.Count / 2) - (arpmiddlenoteistop ? ((LiaisonList.Count + 1) % 2) : 1)+1 ;
                            if (arpstep >= lookoutfor)
                            {
                                arpgoingupwards = false;
                                arpstep = LiaisonList.Count - 1;
                            }
                        }
                        else
                        {
                            arpstep--;
                            lookoutfor = ((LiaisonList.Count / 2) - (arpmiddlenoteistop ? ((LiaisonList.Count + 1) % 2) : 1));
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
                                arpstep = (LiaisonList.Count / 2) - (arpmiddlenoteistop ? ((LiaisonList.Count + 1) % 2) : 1);
                                arpgoingupwards = false;
                            }
                        }
                        else
                        {
                            arpstep--;
                            if (arpstep < 0)
                            {
                                arpstep = ((LiaisonList.Count / 2) - (arpmiddlenoteistop ? ((LiaisonList.Count + 1) % 2) : 1)) + 1;
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
                            plopmachine.Plop(LiaisonList[liaisonrace[strumindex]].note);
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
            static List<string> InNoteList = new(); 
            static List<string> OutNoteList = new(); 
            static bool hasdecidedamount = false;
            static int decidedamount;
            static int triedamounts;
            static readonly Dictionary<string, string> SoloLineageDict = new(); //one at a time kid
            static readonly Dictionary<string, string> DuoLineageDict = new(); //thanks dad it's time for duo
            public static void Fuckinginitthatdictlineagebitch()
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

            public static void AddSeed(string Note) { InNoteList.Add(Note); }
            public static void Fester(PlopMachine plopmachine)
            {
                if (!hasdecidedamount) { decidedamount = (int)Mathf.Lerp(6.5f, 2f, plopmachine.fichtean); hasdecidedamount = true; }
                decidedamount = 7;
                while (OutNoteList.Count < decidedamount && triedamounts < 10)
                {
                    Grows(plopmachine);
                    triedamounts++;
                    //RainMeadow.Debug("Attempt " + triedamounts);
                }
                triedamounts = 0;
                Push(plopmachine);
            }
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

                string LowNote;
                string HighNote;
                string HighExtras;
                int HighFourDelta;
                if (Index1 > Index2)
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
                string NoteValue = "";
                if (Index1 == Index2) {
                    _ = SoloLineageDict.TryGetValue(((UnityEngine.Random.Range(0, 3) == 0) ? LowNote : HighNote), out NoteValue);
                }
                else {
                    _ = DuoLineageDict.TryGetValue($"{LowNote} {HighNote}", out NoteValue);
                }
                float bias = Mathf.Pow(-Mathf.Cos(plopmachine.fichtean * Mathf.PI), 0.52f) / 2 + 0.5f;
                float does_the_church_allow_it = UnityEngine.Random.Range(1f, 0f);
                int MartinLutherKing = (bias > does_the_church_allow_it) ? 1 : 0;
                string[] heaven_or_hell = NoteValue.Split('|');
                string which_one_will_you_choose = heaven_or_hell[MartinLutherKing];
                string[] the_begotten = which_one_will_you_choose.Split(' ');
                string the_One = the_begotten[UnityEngine.Random.Range(0, the_begotten.Length)]; 
                string[] FinalNoteParts = the_One.Split('-');
                //added "clarity"... god really was with me when i made this

                int FinalOct = int.Parse(FinalNoteParts[0]) + HighFourDelta;
                if (FinalOct > 7) { FinalOct = UnityEngine.Random.Range(3, 7); }
                string FinalNote = $"{FinalOct}-{FinalNoteParts[1]}{HighExtras}";

                if (!InNoteList.Contains(FinalNote)) InNoteList.Add(FinalNote);
                if (!OutNoteList.Contains(FinalNote)) OutNoteList.Add(FinalNote);
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

        public static class DrumMachine
        {
            private static float linearToDb(float linear) { return Mathf.Pow(linear / 20f, 10); }
            private static float dbToLinear(float db) { return Mathf.Log(db) * 20f; }
            //private static float velocityToDb(float velocity) { return }
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
                public Track(Fill[] sequence, SoundID sample, int inttrack)
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
                    track = -1;
                    timer = 1;
                    sequenceindex = 0;
                }

                public Fill[] sequence;
                public SoundID sample;
                public int track;
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
                    tracks[i] = track; //Hi henp is there an easier way to modify this list shit?
                    if (step != null && step.Value.velocity != 0)
                    {
                        if (step.Value.chance != 1f && step.Value.chance < (UnityEngine.Random.Range(0f, 1f)))
                        {
                            continue;
                        }
                        float trackvol = track.track switch
                        {
                            0 => Mathf.Clamp01(plopMachine.currentagora * 0.4f - 0.5f),
                            1 => Mathf.Clamp01(plopMachine.currentagora * 0.5f - 1f),
                            2 => Mathf.Clamp01(plopMachine.currentagora * 0.35f - 1f),
                            3 => Mathf.Clamp01(plopMachine.currentagora * 0.27f - 1.6f),
                            _ => 0f,
                        };
                        plopMachine.PlayThing(track.sample, step.Value.velocity * trackvol, 1, mic); 
                    }
                }
                //impulse from a main loop will trigger a random fill of x length
                //impulse from a main thingy will trigger every loop to reset.
            }
            static List<Track> tracks = new();
            public static void StartthefuckingWaitDicthehe()
            {
                tracks.Add(new Track (new Fill[] {
                    new(1f, "1/4", 3),
                    new(0.4f, "1/4", 1, 0.6f) }, 
                Kick, 0));

                tracks.Add(new Track(new Fill[] {
                    new(0f, "1/2", 1), 
                    new(0.85f, "1/2", 1) }, 
                Snare, 1));

                tracks.Add(new Track(new Fill[] {
                    new(0.3f, "1/8", 1),
                    new(0.55f, "1/8", 1, 0.8f)},
                HiHat, 2));

                tracks.Add(new Track(new Fill[] {
                    new(0.66f, "1/16", 6), 
                    new(0.57f, "1/16", 3, 0.3f) }, 
                Perc1, 3));
            }
        }

        static Dictionary<string, int> WaitDict = new();
        public void StartthefuckingWaitDict()
        {
            DrumMachine.StartthefuckingWaitDicthehe();
            ChitChat.steppattern = new bool[4] { true, true, true, false };

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


        bool ol1;
        bool ol2 = true;
        bool ol3;
        private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);

            //if is meadowgamemode if not return lmao
            if (OnlineManager.lobby?.gameMode is MeadowGameMode)
            {
                var mic = self.cameras[0].virtualMicrophone;
                CurrentRegion = self.world?.region?.name ?? "sl";

                currentagora = Mathf.Lerp(currentagora, agora, 0.008f);
                if (MeadowMusic.AllowPlopping)
                {
                    debugstopwatch++;
                    float x = Mathf.PerlinNoise(debugstopwatch / 1000f, debugstopwatch / 4000f);
                    fichtean = (Mathf.Pow(x, 1 / (currentagora / 2 + 1)) + x) / 2;
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

        public static readonly SoundID twentysecsilence = new SoundID("twentysecsilence", register: true);
        public static readonly SoundID Kick  = new SoundID("Kick", register: true);
        public static readonly SoundID Snare = new SoundID("Snare", register: true);
        public static readonly SoundID HiHat = new SoundID("HiHat", register: true);
        public static readonly SoundID Perc1 = new SoundID("Perc1", register: true);
    }
}
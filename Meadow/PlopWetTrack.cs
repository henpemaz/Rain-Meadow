using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using UnityEngine;

namespace RainMeadow
{
    internal class PlopWetTrack : VirtualMicrophone.DisembodiedLoop
    {
        public PlopWetTrack(VirtualMicrophone mic, SoundLoader.SoundData sData, DisembodiedLoopEmitter controller, float pan, float volume, float pitch, bool startAtRandomTime) : base(mic, sData, controller, pan, volume, pitch, startAtRandomTime)
        {
            PlopInititationVelocity = MeadowMusic.vibeIntensity ?? 0;

            this.gameObject.AddComponent<AudioLowPassFilter>();
            this.gameObject.GetComponent<AudioLowPassFilter>().cutoffFrequency = 23000;
            /*
            WetLoop.gameObject.AddComponent<AudioReverbFilter>();
            WetLoop.gameObject.GetComponent<AudioReverbFilter>().reverbPreset = AudioReverbPreset.Dizzy;

            WetLoop.gameObject.GetComponent<AudioReverbFilter>().decayHFRatio = 0.8f;
            WetLoop.gameObject.GetComponent<AudioReverbFilter>().room = 0f;
            WetLoop.gameObject.GetComponent<AudioReverbFilter>().decayTime = 8f;
            //WetLoop.gameObject.GetComponent<AudioReverbFilter>().density = 100.0100f;
            //WetLoop.gameObject.GetComponent<AudioReverbFilter>().diffusion = 100.0100f;
            WetLoop.gameObject.GetComponent<AudioReverbFilter>().dryLevel = 0;
            WetLoop.gameObject.GetComponent<AudioReverbFilter>().hfReference = - 20000f;
            WetLoop.gameObject.GetComponent<AudioReverbFilter>().lfReference = - 1000f;
            //WetLoop.gameObject.GetComponent<AudioReverbFilter>().reflectionsDelay = -2000f;
            WetLoop.gameObject.GetComponent<AudioReverbFilter>().reflectionsLevel = -200f;
            WetLoop.gameObject.GetComponent<AudioReverbFilter>().reverbDelay = 0.2f;
            //WetLoop.gameObject.GetComponent<AudioReverbFilter>().reverbLevel = 0f;
            WetLoop.gameObject.GetComponent<AudioReverbFilter>().room = -1600f;
            WetLoop.gameObject.GetComponent<AudioReverbFilter>().roomHF = -270f;
            WetLoop.gameObject.GetComponent<AudioReverbFilter>().roomLF = -180f;
            */
            //reverb is weird but echo is nice

            this.gameObject.AddComponent<AudioEchoFilter>();
            this.gameObject.GetComponent<AudioEchoFilter>().wetMix = 0.4f;
            this.gameObject.GetComponent<AudioEchoFilter>().decayRatio = 0.6f;
            this.gameObject.GetComponent<AudioEchoFilter>().delay = 600f;

            this.gameObject.AddComponent<AudioChorusFilter>();
            this.gameObject.GetComponent<AudioChorusFilter>().depth = 0.4f;
            this.gameObject.GetComponent<AudioChorusFilter>().dryMix = 0.6f;
            this.gameObject.GetComponent<AudioChorusFilter>().delay = 7f;
            this.gameObject.GetComponent<AudioChorusFilter>().rate = 1.34f;
            this.gameObject.GetComponent<AudioChorusFilter>().wetMix1 = 0.6f;
            this.gameObject.GetComponent<AudioChorusFilter>().wetMix2 = 0.6f;
            this.gameObject.GetComponent<AudioChorusFilter>().wetMix3 = 0.6f;
        }
        public enum Wavetype
        {
            sineiloveyousineohmygodhavemybabies,
            square,
            triangleohmyfuckinggodyouthebestsidebitchmainbitchdudesisfuckbitchfuck,
            smoothsquareiwouldeatmyarmforthishoe,
            sawwaveiguesswhyareyouherewearenotevendubsteprndude,
            BandNoiseOhhhhManYourTheBasicButTheBest
        }
        public List<Plop> plops = new();
        //public List<SamplePlop> sampleplops = new();
        static public List<Plop> plopstoremove = new();
        //static public List<SamplePlop> sampleplopstoremove = new();
        public bool Fading = false;
        static public float PlopInititationVelocity;
        static public float PlopInititationPan;
        static public int? datadeletemarker;
        static public int flusheswithoutplop = 0;
        static int? LastTrackQuarter;
        static int LastTime;
        public override void Update(float timeStacker, float timeSpeed)
        {
            controller.volume = MeadowMusic.defaultMusicVolume * MeadowMusic.defaultPlopVolume;
            base.Update(timeStacker, timeSpeed); //RainMeadow.Debug(timeStacker + "    " +  timeSpeed);
            int dt = DateTime.Now.Millisecond - LastTime;
            float power = 1;
            if (dt / 25f < 0.5f) power = 0.5f;
            else if (dt / 25f > 2f) power = 2f;
            if (this.mic.camera.game.rainWorld.OptionsReady)
            {
                this.audioSource.volume = Mathf.Clamp01(Mathf.Pow(controller.volume * this.soundData.vol * this.mic.volumeGroups[this.volumeGroup] * this.mic.camera.game.rainWorld.options.musicVolume, this.mic.soundLoader.volumeExponent));
            }
            else { audioSource.volume = 0f; }
            if (flusheswithoutplop > 4) return; //reminder to self, you're starting wetloop off as zero
            CheckWetTrack();
            PlopInititationVelocity = MeadowMusic.vibeIntensity ?? 0;
            PlopInititationPan = PlopInititationVelocity == 0 ? 0 : ((MeadowMusic.vibePan ?? 0f) * Mathf.Pow((1f - PlopInititationVelocity) * 0.7f + 0.125f, 1.65f));
            if (plops.Count > 0)
            {
                int float1 = DateTime.Now.Millisecond;
                for (int i = plops.Count - 1; i >= 0; --i)
                {
                    Plop plop = plops[i];
                    plop.Update(power);
                };
                int float2 = DateTime.Now.Millisecond;
                if ((float2 - float1) > 15) RainMeadow.Debug($"Big amount of elapsed time in plop ({plops.Count}) calculations: " + (float2 - float1));
            }

            if (plopstoremove.Count > 0)
            {
                foreach (Plop plop in plopstoremove)
                {
                    plops.Remove(plop);
                }
                plopstoremove.Clear();
            }
        }
        private void CheckWetTrack() //flushes the previous quarter of the loop when entering a new quarter.
        {
            var TrackClip = this.audioSource.clip;
            int TrackCurrentSampleTime = this.audioSource.timeSamples;
            int TrackSamples = (TrackClip.samples * TrackClip.channels);
            int TrackQuarter = (int)((float)TrackCurrentSampleTime * 2f * 4f / (float)TrackSamples);

            LastTrackQuarter ??= TrackQuarter;

            if (LastTrackQuarter != TrackQuarter)
            {
                flusheswithoutplop += 1;
                //RainMeadow.Debug(TrackCurrentQuarterBuffer + "     " + TrackCurrentQuarter);
                int deletethissection = LastTrackQuarter.Value == 0 ? 3 : LastTrackQuarter.Value - 1;
                int sectionstartoffset = TrackSamples * deletethissection / 4;

                float[] TrackClipData = new float[TrackSamples];
                TrackClip.GetData(TrackClipData, 0);
                //RainMeadow.Debug(TrackSamples + "That many times it has done it stuff");
                for (int i = 0; i < TrackSamples / 4; ++i) //that'll be 5 seconds (2 channels)
                {
                    TrackClipData[i + sectionstartoffset] = 0;
                }
                LastTrackQuarter = TrackQuarter;
                TrackClip.SetData(TrackClipData, 0);
            }
        }
        public void WetPlop(string length, int octave, int semitone, float volume, float pan)
        {
            flusheswithoutplop = 0;
            if ((MeadowMusic.vibeIntensity ?? 0) == 0) return;
            plops.Add(new Plop(this, length, octave, semitone, volume, pan));
            //RainMeadow.Debug("Should play " + "   " + length + "   " + octave + "   " + semitone + "   " + volume + "   " + pan + "   " + plops.Count);
        }
        public class Plop
        {
            public float Frequency;
            public Wavetype Wavetype;
            public int TrackSampleStartsAt;
            public int plopattackmonosamples;
            public int plopreleasemonosamples;
            public int ploptotallength;
            public int ploprendered;
            public float volume;
            public float pan; //-1 for left, 1 for right
            public float lan;
            public float ran;
            public float phase;
            public float tremolofreq;
            public int oct;
            PlopWetTrack owner;
            public Plop(PlopWetTrack owner, string length, int octave, int semitone, float volume, float pan)
            {
                this.owner = owner;
                string acronym = (PlopMachine.CurrentRegion ?? "sl").ToUpper();
                bool diditwork = MeadowMusic.vibeZonesDict.TryGetValue(acronym, out MeadowMusic.VibeZone[] newthings);
                Wavetype = (diditwork ? newthings[0].sampleUsed : "Trisaw") switch
                {

                    "Trisaw" => Wavetype.smoothsquareiwouldeatmyarmforthishoe,
                    "Clar" => Wavetype.BandNoiseOhhhhManYourTheBasicButTheBest,
                    "Litri" => Wavetype.triangleohmyfuckinggodyouthebestsidebitchmainbitchdudesisfuckbitchfuck,
                    "Sine" => Wavetype.sineiloveyousineohmygodhavemybabies,
                    "Bell" => Wavetype.sawwaveiguesswhyareyouherewearenotevendubsteprndude,
                    _ => Wavetype.square
                };
                //var TrackClip = WetLoop.audioSource.clip;
                TrackSampleStartsAt = owner.audioSource.timeSamples;
                TrackSampleStartsAt += UnityEngine.Random.Range(1, 1000 * length switch { "L" => 3, "M" => 2, "S" => 1, _ => 1}); //initial delay
                this.oct = octave;
                this.Frequency = 440f * Mathf.Pow(2, octave - 5) * Mathf.Pow(2, (float)(semitone + 3) / (float)12);

                //These times could dictated by a perlin noise 
                //Todo: let regions customize this.
                //Mathf.PerlinNoise(debugstopwatch / 1000f, debugstopwatch / 4000f);
                float attacktime = 0.004f;
                float releasetime = length switch { "L" => 5.98f, "M" => 2.98f, "S" => 0.98f, _ => 3.98f };
                plopattackmonosamples = (int)(attacktime * 44100);
                plopreleasemonosamples = (int)(releasetime * 44100);
                ploptotallength = 2 * (plopattackmonosamples + plopreleasemonosamples);
                ploprendered = 0;
                this.tremolofreq = ((Mathf.Pow(UnityEngine.Random.Range(0f, 1f), 2.5f)) + 0.2f) * 10f * Mathf.PI * 2f;
                this.phase = UnityEngine.Random.Range(0f, 1f);
                this.volume = volume * 0.2f * Mathf.Min(PlopInititationVelocity, 1f); //mathf just for safetly
                this.pan = pan + PlopInititationPan;

                lan = Mathf.Pow(Mathf.Clamp01(1f - this.pan), 2);
                ran = Mathf.Pow(Mathf.Clamp01(1f + this.pan), 2);
            }

            public void Update(float power)
            {
                int samplestorender = (int)(8820 * power);
                //0.1 second a tick
                //RainMeadow.Debug("Rendering " + ploprendered + "  " + (ploprendered + samplestorender));
                //RainMeadow.Debug(ploprendered + "    " + plopattackmonosamples + "   " + ploptotallength + "   " + TrackClipData.Length + "   " + TotalWetSamples);
                Wavetype type = this.Wavetype;

                if (samplestorender + (ploprendered * 2) > ploptotallength)
                {
                    samplestorender = ploptotallength - (ploprendered * 2);
                    if (samplestorender <= 0)
                    {
                        //RainMeadow.Debug("Removed a plop");
                        plopstoremove.Add(this);
                        return;
                    }
                }
                AudioClip TrackClip = owner.audioSource.clip;
                float[] TrackClipData = new float[samplestorender * 2];
                TrackClip.GetData(TrackClipData, TrackSampleStartsAt + ploprendered - ((ploprendered + TrackSampleStartsAt) < TrackClip.samples ? 0 : TrackClip.samples));
                float attenuation = (1f - (float)oct / 20);
                float atan3 = Mathf.Atan(3);
                Parallel.For(ploprendered * 2, ploprendered * 2 + TrackClipData.Length, i =>
                {
                    int ii = (i % 2 == 0 ? i / 2 : (i - 1) / 2);
                    float ipan = (i % 2 == 0 ? lan : ran);
                    float CurrentAmplitude;
                    float iPhase = (ii * Mathf.PI * 2f * Frequency / 44100f) + phase;

                    if (ii < plopattackmonosamples)
                    {
                        CurrentAmplitude = (float)ii / (float)plopattackmonosamples;

                    }
                    else //we have no decay nor sustain here to worry about bro we're just impulses
                    {
                        float amp = ((float)(ii - plopattackmonosamples)) / plopreleasemonosamples;
                        CurrentAmplitude = Mathf.Pow((1.0f - amp), 3);
                        CurrentAmplitude *= (Mathf.Clamp01(1.0f - (amp * 7 * Mathf.Sin(amp * tremolofreq + phase))));
                    }
                    float iValue = volume * CurrentAmplitude * ipan * attenuation;

                    TrackClipData[i - (ploprendered * 2)] += type switch
                    {
                        Wavetype.sineiloveyousineohmygodhavemybabies => Mathf.Sin(iPhase) * iValue,
                        Wavetype.smoothsquareiwouldeatmyarmforthishoe => Mathf.Atan(Mathf.Sin(iPhase) * 3) / atan3 * iValue,
                        Wavetype.triangleohmyfuckinggodyouthebestsidebitchmainbitchdudesisfuckbitchfuck => Mathf.Asin(Mathf.Cos(iPhase)) * iValue,
                        Wavetype.square => ((Mathf.Sin(iPhase) > 0) ? iValue : -iValue) * 0.75f,
                        //TrackClipData[i - (ploprendered * 2)] += ((2*Mathf.Atan(Mathf.Tan(iPhase/2f)))/Mathf.PI) * iValue * 0.8f;
                        //TrackClipData[i - (ploprendered * 2)] += sum of ((2/(n*mathf.pi)) * Mathf.Sin(iPhase*n))
                        //TrackClipData[i - (ploprendered * 2)] += (Mathf.Sin(iPhase) + Mathf.Sin(iPhase * 2) / 2f + Mathf.Sin(iPhase * 3) / 3f) * (2 / (Mathf.PI)) * iValue;
                        //TrackClipData[i - (ploprendered * 2)] += (((ii * Frequency / 44100f) - Mathf.Floor(ii * Frequency / 44100f)) * 2f - 1f) * iValue;
                        //(Mathf.Sin(iPhase) + Mathf.Sin(iPhase * 2) / 2f + Mathf.Sin(iPhase * 3) / 3f) * (2 / (Mathf.PI)) * iValue,//TrackClipData[i - (ploprendered * 2)] += ((2*Mathf.Atan(Mathf.Tan(iPhase/2f)))/Mathf.PI) * iValue * 0.8f;
                        //mod(x, 1)
                        Wavetype.sawwaveiguesswhyareyouherewearenotevendubsteprndude => ((((iPhase / (Mathf.PI * 2)) % 1f) * 2) - 1f) * iValue,
                        Wavetype.BandNoiseOhhhhManYourTheBasicButTheBest => Mathf.Sin(iPhase) * iValue,//Todo :D Make something cool
                        _ => Mathf.Sin(iPhase) * iValue,
                    };
                    //Previous Formulas for Amplitudes
                    //CurrentAmplitude = (Mathf.Pow(2, 16f * attackexponent * ((float)ii / (float)MonoSamplesOfAttack)) - 1f) / (Mathf.Pow(2, 16.0f * attackexponent) - 1f);
                    //CurrentAmplitude = 1.0f - (Mathf.Pow(2, 16.0f * releaseexponent * ((float)(ii - MonoSamplesOfAttack) / (float)(WaveData.Length - MonoSamplesOfAttack))) - 1) / (Mathf.Pow(2, 16.0f * releaseexponent) - 1f);

                    //Makeshift compression that doesn't work due to divisions per sample leading to inprecice cuts. We would have to compress *area* by its normalisation. 
                    //while (TrackClipData[i - (ploprendered * 2)] >= 0.3)
                    //{
                    //    TrackClipData[i - (ploprendered * 2)] /= 1.1f;
                    //} 

                    //clipper, for safety! 
                    while (TrackClipData[i - (ploprendered * 2)] >= 0.9)
                    {
                        TrackClipData[i - (ploprendered * 2)] = 0.9f;
                    }
                });
                TrackClip.SetData(TrackClipData, TrackSampleStartsAt + ploprendered - ((ploprendered + TrackSampleStartsAt) < TrackClip.samples ? 0 : TrackClip.samples));
                ploprendered += samplestorender;
                if (ploprendered * 2 >= ploptotallength)
                {
                    //RainMeadow.Debug("Removed a plop");
                    plopstoremove.Add(this);
                }
            }
        }
    }
}
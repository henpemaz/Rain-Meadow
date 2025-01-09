using UnityEngine;

namespace RainMeadow
{
    public class DisenbodiedWetLoop : VirtualMicrophone.DisembodiedLoop
    {
        public DisenbodiedWetLoop(VirtualMicrophone mic, SoundLoader.SoundData sData, DisembodiedLoopEmitter controller, float pan, float volume, float pitch, bool startAtRandomTime) : base(mic, sData, controller, pan, volume, pitch, startAtRandomTime)
        {

        }
        public override void Update(float timeStacker, float timeSpeed)
        {
            base.Update(timeStacker, timeSpeed);
            if (this.mic.camera.game.rainWorld.OptionsReady)
            {
                this.audioSource.volume = Mathf.Clamp01(Mathf.Pow(controller.volume * this.soundData.vol * this.mic.volumeGroups[this.volumeGroup] * this.mic.camera.game.rainWorld.options.musicVolume, this.mic.soundLoader.volumeExponent));
                return;
            }
            this.audioSource.volume = 0f;
        }
    }
}
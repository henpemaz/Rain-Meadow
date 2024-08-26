using System;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowVoice
    {
        CreatureController owner;

        public MeadowVoice(CreatureController owner)
        {
            this.owner = owner;
            volume = 1f;
            myId = owner.customization.characterData.voiceId;
        }

        SoundID myId;
        float volume;
        float spam;
        float silence;

        internal void Call()
        {
            RainMeadow.Debug(owner.onlineCreature);
            owner.creature.room.PlaySound(myId, owner.creature.firstChunk, false, volume, 1.0f);
            spam = Mathf.Min(20, spam + 1f);
            silence = 0;
        }

        internal void Update()
        {
            volume = 1f - 0.75f * RWCustom.Custom.SCurve(Mathf.InverseLerp(2, 12, spam), 0.25f);
            silence = Mathf.Min(4f, silence + 1f / 40f);
            spam = RWCustom.Custom.LerpAndTick(spam, 0, silence / 400f, silence * 0.5f / 40f);
        }
    }
}

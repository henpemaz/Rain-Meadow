﻿using System;
using UnityEngine;
using static RainMeadow.MeadowProgression;

namespace RainMeadow
{
    public class MeadowAvatarData : AvatarData
    {
        public Skin skin;
        public SkinData skinData;
        public Character character;
        public CharacterData characterData;
        public Color tint;
        public float tintAmount;
        public float effectiveTintAmount;

        private HSLColor hslTint;
        private Color emoteBgColor;
        private Color symbolBgColor;
        private Color emoteColor;
        private Color symbolColor;

        private State lastState;

        internal string EmoteAtlas => skinData.emoteAtlasOverride ?? characterData.emoteAtlas;
        internal string EmotePrefix => skinData.emotePrefixOverride ?? characterData.emotePrefix;
        public SoundID VoiceId => skinData.voiceIdOverride ?? characterData.voiceId;

        internal override void ModifyBodyColor(ref Color originalBodyColor)
        {
            if (skinData.baseColor.HasValue) originalBodyColor = skinData.baseColor.Value;
            if (effectiveTintAmount > 0f)
            {
                originalBodyColor = Color.Lerp(HSLColor.Lerp(originalBodyColor.ToHSL(), hslTint, effectiveTintAmount).rgb,
                    Color.Lerp(originalBodyColor, tint, effectiveTintAmount), 0.5f); // lerp in average of hsl and rgb, neither is good on its own
            }
        }

        internal override void ModifyEyeColor(ref Color originalEyeColor)
        {
            if (skinData.eyeColor.HasValue) originalEyeColor = skinData.eyeColor.Value;
        }

        internal string GetEmote(Emote emote)
        {
            return (emote.value.StartsWith("emote") ? EmotePrefix + emote.value : emote.value).ToLowerInvariant();
        }

        internal string GetBackground(Emote emote)
        {
            return (emote.value.StartsWith("emote") ? "emote_background" : "symbols_background");
        }

        internal Color EmoteBackgroundColor(Emote emote)
        {
            return emote.value.StartsWith("emote") ? emoteBgColor : symbolBgColor;
        }

        internal Color EmoteColor(Emote emote)
        {
            return emote.value.StartsWith("emote") ? emoteColor : symbolColor;
        }

        public override EntityDataState MakeState(OnlineEntity onlineEntity, OnlineResource inResource)
        {
            if (inResource is Lobby || inResource is WorldSession)
            {
                return new State(this);
            }
            return null;
        }

        public class State : AvatarDataState
        {
            [OnlineField]
            public Skin skin;
            [OnlineField]
            public byte tintAmount;
            [OnlineFieldColorRgb]
            public Color tint;

            public State() : base() { }
            public State(MeadowAvatarData onlineEntity) : base(onlineEntity)
            {
                skin = onlineEntity.skin;
                tintAmount = (byte)(onlineEntity.tintAmount * 255f);
                tint = onlineEntity.tint;
            }

            public override Type GetDataType()
            {
                return typeof(MeadowAvatarData);
            }

            public override void ReadTo(OnlineEntity.EntityData entityData, OnlineEntity onlineEntity)
            {
                base.ReadTo(entityData, onlineEntity);
                var meadowAvatarSettings = (MeadowAvatarData)entityData;
                meadowAvatarSettings.skin = skin;
                meadowAvatarSettings.tintAmount = tintAmount / 255f;
                meadowAvatarSettings.tint = tint;

                meadowAvatarSettings.NewState(this);
            }
        }

        private void NewState(State state)
        {
            if (state != lastState) // ref uneq
            {
                Updated();
            }
            lastState = state;
        }

        internal void Updated()
        {
            this.skinData = MeadowProgression.skinData[skin];
            this.character = skinData.character;
            this.characterData = MeadowProgression.characterData[skinData.character];

            this.tint = new(tint.r, tint.g, tint.b);
            this.hslTint = tint.ToHSL();
            this.effectiveTintAmount = tintAmount * skinData.tintFactor;

            emoteBgColor = Color.Lerp(skinData.emoteColorOverride ?? characterData.emoteColor, this.tint, this.effectiveTintAmount);
            symbolBgColor = Color.white;
            emoteColor = Color.white;
            symbolColor = Color.Lerp(Color.white, this.tint, this.effectiveTintAmount);
        }
    }
}
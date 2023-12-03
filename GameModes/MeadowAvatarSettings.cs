using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowAvatarSettings : AvatarSettingsEntity
    {
        internal MeadowProgression.Skin skin;
        internal Color tint;
        internal float tintAmount;

        public static ConditionalWeakTable<OnlinePlayer, MeadowAvatarSettings> map = new();

        public MeadowAvatarSettings(EntityDefinition entityDefinition) : base(entityDefinition)
        {
            RainMeadow.Debug(this);
            map.Remove(owner);
            map.Add(owner, this);
        }

        public static MeadowAvatarSettings FromDefinition(MeadowPersonaSettingsDefinition meadowPersonaSettingsDefinition , OnlineResource inResource)
        {
            RainMeadow.Debug(meadowPersonaSettingsDefinition);
            return new MeadowAvatarSettings(meadowPersonaSettingsDefinition);
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new MeadowAvatarSettingsState(this, inResource, tick);
        }

        internal MeadowCustomization.CreatureCustomization MakeCustomization()
        {
            return new MeadowCustomization.CreatureCustomization(skin, tint, tintAmount);
        }

        public class MeadowAvatarSettingsState : AvatarSettingsState
        {
            [OnlineField]
            public short skin;
            [OnlineField]
            public byte tintAmount;
            [OnlineField]
            public byte tintR;
            [OnlineField]
            public byte tintG;
            [OnlineField]
            public byte tintB;

            public MeadowAvatarSettingsState() : base() { }

            public MeadowAvatarSettingsState(OnlineEntity onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
            {
                var meadowAvatarSettings = (MeadowAvatarSettings)onlineEntity;
                skin = (short)(meadowAvatarSettings.skin?.Index ?? -1);
                tintAmount = (byte)(meadowAvatarSettings.tintAmount * 255);
                tintR = (byte)(meadowAvatarSettings.tint.r * 255);
                tintG = (byte)(meadowAvatarSettings.tint.g * 255);
                tintB = (byte)(meadowAvatarSettings.tint.b * 255);
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                var meadowAvatarSettings = (MeadowAvatarSettings)onlineEntity;
                meadowAvatarSettings.skin = new MeadowProgression.Skin(MeadowProgression.Skin.values.GetEntry(skin));
                meadowAvatarSettings.tintAmount = tintAmount / 255f;
                meadowAvatarSettings.tint.r = tintR / 255f;
                meadowAvatarSettings.tint.g = tintG / 255f;
                meadowAvatarSettings.tint.b = tintB / 255f;
            }
        }
    }
}
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowAvatarSettings : AvatarSettingsEntity
    {
        internal float tintAmount;
        internal MeadowProgression.Skin skin;
        internal Color tint;

        public static ConditionalWeakTable<OnlinePlayer, MeadowAvatarSettings> map = new();

        public MeadowAvatarSettings(EntityDefinition entityDefinition) : base(entityDefinition)
        {
            RainMeadow.Debug(this);
            map.Add(owner, this);
        }

        public static MeadowAvatarSettings FromDefinition(MeadowPersonaSettingsDefinition meadowPersonaSettingsDefinition , OnlineResource inResource)
        {
            RainMeadow.Debug(meadowPersonaSettingsDefinition);
            return new MeadowAvatarSettings(meadowPersonaSettingsDefinition);
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new MeadowAvatarSettingsState(this, tick);
        }

        internal override void ApplyCustomizations(object subject, OnlineEntity oe)
        {
            Creature creature = (Creature)subject;
            MeadowCustomization.CreatureCustomization customization = MeadowCustomization.creatureCustomizations.GetOrCreateValue(creature);
            customization.skinData = MeadowProgression.skinData[skin];
            customization.tint = new(tint.r, tint.g, tint.b);
            customization.tintAmount = tintAmount * MeadowProgression.skinData[skin].tintFactor;
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

            public MeadowAvatarSettingsState(OnlineEntity onlineEntity, uint ts) : base(onlineEntity, ts)
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
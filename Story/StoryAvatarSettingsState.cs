using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    public class StoryAvatarSettings : AvatarSettingsEntity
    {
        internal SkinSelection.Skin skin;
        internal Color tint;
        internal float tintAmount;

        public static ConditionalWeakTable<OnlinePlayer, StoryAvatarSettings> map = new();

        public StoryAvatarSettings(EntityDefinition entityDefinition) : base(entityDefinition)
        {
            RainMeadow.Debug(this);
            map.Remove(owner);
            map.Add(owner, this);
        }

        public static StoryAvatarSettings FromDefinition(StoryPersonaSettingsDefintion meadowPersonaSettingsDefinition, OnlineResource inResource)
        {
            RainMeadow.Debug(meadowPersonaSettingsDefinition);
            return new StoryAvatarSettings(meadowPersonaSettingsDefinition);
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new StoryAvatarSettingsState(this, inResource, tick);
        }

        internal StoryCustomization.CreatureCustomization MakeCustomization()
        {
            return new StoryCustomization.CreatureCustomization(skin, tint, tintAmount);
        }

        public class StoryAvatarSettingsState : AvatarSettingsState
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

            public StoryAvatarSettingsState() : base() { }

            public StoryAvatarSettingsState(OnlineEntity onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
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
                var meadowAvatarSettings = (StoryAvatarSettings)onlineEntity;
                meadowAvatarSettings.skin = new SkinSelection.Skin(SkinSelection.Skin.values.GetEntry(skin));
                meadowAvatarSettings.tintAmount = tintAmount / 255f;
                meadowAvatarSettings.tint = new(tintR / 255f, tintG / 255f, tintB / 255f);
            }
        }
    }
}
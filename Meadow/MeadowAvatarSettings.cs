using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowAvatarSettings : ClientSettings
    {
        new public class Definition : ClientSettings.Definition
        {
            public Definition() : base() { }
            public Definition(OnlineEntity.EntityId entityId, OnlinePlayer owner) : base(entityId, owner) { }

            public override OnlineEntity MakeEntity(OnlineResource inResource)
            {
                return MeadowAvatarSettings.FromDefinition(this, inResource);
            }
        }

        // this could as well be in an EntityData bound to the entity, rather than at lobby level
        // however this limits displaying that data to when we're in a world with the creature in question
        // ie no lobby list with player colors
        internal MeadowProgression.Skin skin;
        internal Color tint;
        internal float tintAmount;

        public MeadowAvatarSettings(EntityDefinition entityDefinition) : base(entityDefinition)
        {
            RainMeadow.Debug(this);
        }

        public static MeadowAvatarSettings FromDefinition(Definition meadowPersonaSettingsDefinition, OnlineResource inResource)
        {
            RainMeadow.Debug(meadowPersonaSettingsDefinition);
            return new MeadowAvatarSettings(meadowPersonaSettingsDefinition);
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new State(this, inResource, tick);
        }

        internal override AvatarCustomization MakeCustomization()
        {
            return new MeadowAvatarCustomization(skin, tint, tintAmount);
        }

        public class State : ClientSettings.State
        {
            [OnlineField]
            public short skin;
            [OnlineField]
            public byte tintAmount;
            [OnlineFieldColorRgb]
            public Color tint;

            public State() : base() { }

            public State(MeadowAvatarSettings onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
            {
                skin = (short)(onlineEntity.skin?.Index ?? -1);
                tintAmount = (byte)(onlineEntity.tintAmount * 255);
                tint = onlineEntity.tint;
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                var meadowAvatarSettings = (MeadowAvatarSettings)onlineEntity;
                meadowAvatarSettings.skin = new MeadowProgression.Skin(MeadowProgression.Skin.values.GetEntry(skin));
                meadowAvatarSettings.tintAmount = tintAmount / 255f;
                meadowAvatarSettings.tint = tint;
            }
        }
    }
}
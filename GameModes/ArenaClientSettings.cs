using System;
using UnityEngine;

namespace RainMeadow.GameModes
{
    public class ArenaClientSettings : ClientSettings
    {
        public class Definition : ClientSettings.Definition
        {
            public Definition() { }

            public Definition(ClientSettings clientSettings, OnlineResource inResource) : base(clientSettings, inResource) { }

            public override OnlineEntity MakeEntity(OnlineResource inResource, EntityState initialState)
            {
                return new ArenaClientSettings(this, inResource, (State)initialState);
            }
        }

        public Color bodyColor;
        public Color eyeColor;
        public SlugcatStats.Name? playingAs;

        public ArenaClientSettings(Definition entityDefinition, OnlineResource inResource, State initialState) : base(entityDefinition, inResource, initialState)
        {
            RainMeadow.Debug(this);
            bodyColor = entityDefinition.owner == 2 ? Color.white : PlayerGraphics.DefaultSlugcatColor(SlugcatStats.Name.White);
            eyeColor = Color.black;

        }

        public ArenaClientSettings(EntityId id, OnlinePlayer owner) : base(id, owner)
        {

        }

        internal override EntityDefinition MakeDefinition(OnlineResource onlineResource)
        {
            return new Definition(this, onlineResource);
        }

        internal override AvatarCustomization MakeCustomization()
        {
            return new ArenaAvatarCustomization(this);
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new State(this, inResource, tick);
        }

        internal Color SlugcatColor()
        {
            return bodyColor;
        }

        public class State : ClientSettings.State
        {
            [OnlineFieldColorRgb]
            public Color bodyColor;
            [OnlineFieldColorRgb]
            public Color eyeColor;
            [OnlineField(nullable = true)]
            public string? playingAs;
            public State() { }
            public State(ArenaClientSettings onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
            {
                bodyColor = onlineEntity.bodyColor;
                eyeColor = onlineEntity.eyeColor;
                playingAs = onlineEntity.playingAs?.value;
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                var avatarSettings = (ArenaClientSettings)onlineEntity;
                avatarSettings.bodyColor = bodyColor;
                avatarSettings.eyeColor = eyeColor;
                if (playingAs != null)
                {
                    ExtEnumBase.TryParse(typeof(SlugcatStats.Name), playingAs, false, out var rawEnumBase);
                    avatarSettings.playingAs = rawEnumBase as SlugcatStats.Name;
                }
            }
        }
        public class ArenaAvatarCustomization : AvatarCustomization
        {
            public readonly ArenaClientSettings settings;

            public ArenaAvatarCustomization(ArenaClientSettings slugcatAvatarSettings)
            {
                this.settings = slugcatAvatarSettings;
            }


            internal override void ModifyBodyColor(ref Color bodyColor)
            {
                bodyColor = new Color(Mathf.Clamp(settings.bodyColor.r, 0.004f, 0.996f), Mathf.Clamp(settings.bodyColor.g, 0.004f, 0.996f), Mathf.Clamp(settings.bodyColor.b, 0.004f, 0.996f));
            }

            internal override void ModifyEyeColor(ref Color eyeColor)
            {
                eyeColor = new Color(Mathf.Clamp(settings.eyeColor.r, 0.004f, 0.996f), Mathf.Clamp(settings.eyeColor.g, 0.004f, 0.996f), Mathf.Clamp(settings.eyeColor.b, 0.004f, 0.996f));
            }

            internal override Color GetBodyColor()
            {
                return settings.bodyColor;
            }

        }
    }
}
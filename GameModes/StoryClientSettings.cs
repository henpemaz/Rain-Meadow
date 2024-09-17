using System;
using UnityEngine;

namespace RainMeadow
{
    public class StoryClientSettings : ClientSettings
    {
        public class Definition : ClientSettings.Definition
        {
            public Definition() { }
            public Definition(ClientSettings clientSettings, OnlineResource inResource) : base(clientSettings, inResource) { }

            public override OnlineEntity MakeEntity(OnlineResource inResource, EntityState initialState)
            {
                return new StoryClientSettings(this, inResource, (State)initialState);
            }
        }

        public Color bodyColor;
        public Color eyeColor; // unused
        public SlugcatStats.Name? playingAs;
        public bool readyForWin;
        public string? myLastDenPos = null;
        public bool isDead;

        public StoryClientSettings(Definition entityDefinition, OnlineResource inResource, State initialState) : base(entityDefinition, inResource, initialState)
        {

        }

        public StoryClientSettings(EntityId id, OnlinePlayer owner) : base(id, owner)
        {

        }

        internal override EntityDefinition MakeDefinition(OnlineResource onlineResource)
        {
            return new Definition(this, onlineResource);
        }

        internal override AvatarCustomization MakeCustomization()
        {
            return new SlugcatCustomization(this);
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
            [OnlineField(group = "game")]
            public bool readyForWin;
            [OnlineField(group = "game")]
            public bool isDead;

            public State() { }
            public State(StoryClientSettings onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
            {
                bodyColor = onlineEntity.bodyColor;
                eyeColor = onlineEntity.eyeColor;
                playingAs = onlineEntity.playingAs?.value;
                readyForWin = onlineEntity.readyForWin;
                isDead = onlineEntity.isDead;
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                var avatarSettings = (StoryClientSettings)onlineEntity;
                avatarSettings.bodyColor = bodyColor;
                avatarSettings.eyeColor = eyeColor;
                if (playingAs != null) {
                    ExtEnumBase.TryParse(typeof(SlugcatStats.Name), playingAs, false, out var rawEnumBase);
                    avatarSettings.playingAs = rawEnumBase as SlugcatStats.Name;
                }
                avatarSettings.readyForWin = readyForWin;
                avatarSettings.isDead = isDead;
            }
        }
        public class SlugcatCustomization : AvatarCustomization
        {
            public readonly StoryClientSettings settings;

            public SlugcatCustomization(StoryClientSettings slugcatAvatarSettings)
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
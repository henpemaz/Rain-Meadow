using System;
using UnityEngine;

namespace RainMeadow
{
    public class StoryAvatarSettings : AvatarSettings
    {
        public class Definition : AvatarSettings.Definition
        {
            public Definition() { }

            public Definition(EntityId entityId, OnlinePlayer owner) : base(entityId, owner) { }

            public override OnlineEntity MakeEntity(OnlineResource inResource)
            {
                return new StoryAvatarSettings(this);
            }
        }

        public Color bodyColor;
        public Color eyeColor; // unused
        public SlugcatStats.Name playingAs; // not implemented
        public bool readyForWin;
        public string myLastDenPos;

        public StoryAvatarSettings(Definition entityDefinition) : base(entityDefinition)
        {
            RainMeadow.Debug(this);
            // todo de-dummy
            // I'm accessible through gamemode.avatarsettings change me
            playingAs = SlugcatStats.Name.White;
            myLastDenPos = "SU_C04";
            bodyColor = entityDefinition.owner == 2 ? Color.cyan : PlayerGraphics.DefaultSlugcatColor(playingAs);
            eyeColor = Color.black;
        }

        internal override AvatarCustomization MakeCustomization()
        {
            return new SlugcatCustomization(this);
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new State(this, inResource, tick);
        }

        public class State : AvatarSettings.State
        {
            [OnlineFieldColorRgb]
            public Color bodyColor;
            [OnlineFieldColorRgb]
            public Color eyeColor;
            [OnlineField]
            public SlugcatStats.Name playingAs;
            [OnlineField(group ="win")]
            public bool readyForWin;

            public State() { }
            public State(StoryAvatarSettings onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
            {
                bodyColor = onlineEntity.bodyColor;
                eyeColor = onlineEntity.eyeColor;
                playingAs = onlineEntity.playingAs;
                readyForWin = onlineEntity.readyForWin;
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                var avatarSettings = (StoryAvatarSettings)onlineEntity;
                avatarSettings.bodyColor = bodyColor;
                avatarSettings.eyeColor = eyeColor;
                avatarSettings.playingAs = playingAs;
                avatarSettings.readyForWin = readyForWin;
            }
        }
        public class SlugcatCustomization : AvatarCustomization
        {
            private readonly StoryAvatarSettings settings;

            public SlugcatCustomization(StoryAvatarSettings slugcatAvatarSettings)
            {
                this.settings = slugcatAvatarSettings;
            }

            internal override void ModifyBodyColor(ref Color bodyColor)
            {
                bodyColor = settings.bodyColor;
            }

            internal override void ModifyEyeColor(ref Color eyeColor)
            {
                // no op
            }
        }
    }
}
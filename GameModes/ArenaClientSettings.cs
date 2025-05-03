using System;
using System.Drawing;

namespace RainMeadow
{
    public class ArenaClientSettings : OnlineEntity.EntityData
    {
        public SlugcatStats.Name? playingAs;
        public SlugcatStats.Name? randomPlayingAs;

        public ArenaClientSettings() { }

        public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
        {
            return new State(this);
        }

        public class State : EntityDataState
        {
            [OnlineField(nullable = true)]
            public SlugcatStats.Name? playingAs;
            [OnlineField(nullable = true)]
            public SlugcatStats.Name? randomPlayingAs;

            public State() { }
            public State(ArenaClientSettings onlineEntity) : base()
            {
                playingAs = onlineEntity.playingAs;
                randomPlayingAs = onlineEntity.randomPlayingAs;
            }

            public override void ReadTo(OnlineEntity.EntityData entityData, OnlineEntity onlineEntity)
            {
                var avatarSettings = (ArenaClientSettings)entityData;
                avatarSettings.playingAs = playingAs;
                avatarSettings.randomPlayingAs = randomPlayingAs;
            }

            public override Type GetDataType() => typeof(ArenaClientSettings);
        }
    }
}
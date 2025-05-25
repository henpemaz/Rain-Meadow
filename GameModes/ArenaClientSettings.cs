using System;
using System.Drawing;

namespace RainMeadow
{
    public class ArenaClientSettings : OnlineEntity.EntityData
    {
        public SlugcatStats.Name playingAs;

        public ArenaClientSettings() { }

        public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
        {
            return new State(this);
        }

        public class State : EntityDataState
        {
            [OnlineField]
            public SlugcatStats.Name? playingAs;
            public State() { }
            public State(ArenaClientSettings onlineEntity) : base()
            {
                playingAs = onlineEntity.playingAs;

            }

            public override void ReadTo(OnlineEntity.EntityData entityData, OnlineEntity onlineEntity)
            {
                var avatarSettings = (ArenaClientSettings)entityData;
                avatarSettings.playingAs = playingAs;
            }

            public override Type GetDataType() => typeof(ArenaClientSettings);
        }
    }
}
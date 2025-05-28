using System;
using UnityEngine;

namespace RainMeadow
{
    public class ArenaClientSettings : OnlineEntity.EntityData
    {
        public SlugcatStats.Name playingAs = SlugcatStats.Name.White;
        public bool selectingSlugcat;
        public Color slugcatColor = Color.black;

        public ArenaClientSettings() { }

        public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
        {
            return new State(this);
        }

        public class State : EntityDataState
        {
            [OnlineField(group = "arenaClientData")]
            public SlugcatStats.Name playingAs = SlugcatStats.Name.White;
            [OnlineFieldColorRgb(group = "arenaClientData")]
            public Color slugcatColor = Color.black;
            [OnlineField(group = "arenaClientData")]
            public bool selectingSlugcat;

            public State() { }

            public State(ArenaClientSettings onlineEntity) : base()
            {
                playingAs = onlineEntity.playingAs;
                selectingSlugcat = onlineEntity.selectingSlugcat;
                slugcatColor = onlineEntity.slugcatColor;
            }

            public override void ReadTo(OnlineEntity.EntityData entityData, OnlineEntity onlineEntity)
            {
                var clientSettings = (ArenaClientSettings)entityData;
                clientSettings.playingAs = playingAs;
                clientSettings.selectingSlugcat = selectingSlugcat;
                clientSettings.slugcatColor = slugcatColor;
            }

            public override Type GetDataType() => typeof(ArenaClientSettings);
        }
    }
}
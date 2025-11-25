using System;
using UnityEngine;

namespace RainMeadow
{
    public class ArenaClientSettings : OnlineEntity.EntityData
    {
        public SlugcatStats.Name playingAs = SlugcatStats.Name.White;
        public SlugcatStats.Name? randomPlayingAs;
        public bool selectingSlugcat, ready, gotSlugcat, weaverTail;
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
            [OnlineField(group = "arenaClientData", nullable = true)]
            public SlugcatStats.Name? randomPlayingAs;
            [OnlineFieldColorRgb(group = "arenaClientData")]
            public Color slugcatColor = Color.black;

            [OnlineField(group = "arenaClientData")]
            public bool selectingSlugcat;
            [OnlineField(group = "arenaClientData")]
            public bool ready;
            [OnlineField(group = "arenaClientData")]
            public bool gotSlugcat;
            [OnlineField(group = "arenaClientData")]
            public bool weaverTail;
            public State() { }

            public State(ArenaClientSettings onlineEntity) : base()
            {
                playingAs = onlineEntity.playingAs;
                randomPlayingAs = onlineEntity.randomPlayingAs;
                selectingSlugcat = onlineEntity.selectingSlugcat;
                slugcatColor = onlineEntity.slugcatColor;
                ready = onlineEntity.ready;
                gotSlugcat = onlineEntity.gotSlugcat;
                weaverTail = onlineEntity.weaverTail;
            }

            public override void ReadTo(OnlineEntity.EntityData entityData, OnlineEntity onlineEntity)
            {
                var avatarSettings = (ArenaClientSettings)entityData;
                avatarSettings.playingAs = playingAs;
                var clientSettings = (ArenaClientSettings)entityData;
                clientSettings.playingAs = playingAs;
                clientSettings.randomPlayingAs = randomPlayingAs;
                clientSettings.selectingSlugcat = selectingSlugcat;
                clientSettings.slugcatColor = slugcatColor;
                clientSettings.ready = ready;
                clientSettings.gotSlugcat = gotSlugcat;
                clientSettings.weaverTail = weaverTail;
            }

            public override Type GetDataType() => typeof(ArenaClientSettings);
        }
    }
}
using System;
using UnityEngine;

namespace RainMeadow
{
    public class ArenaTeamClientSettings : OnlineEntity.EntityData
    {
        public int team;

        public ArenaTeamClientSettings() { }

        public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
        {
            return new State(this);
        }

        public class State : EntityDataState
        {
            [OnlineField]
            public int team;
            public State() { }

            public State(ArenaTeamClientSettings onlineEntity) : base()
            {
                team = onlineEntity.team;
            }

            public override void ReadTo(OnlineEntity.EntityData entityData, OnlineEntity onlineEntity)
            {
                var avatarSettings = (ArenaTeamClientSettings)entityData;
                avatarSettings.team = team;
            }

            public override Type GetDataType() => typeof(ArenaTeamClientSettings);
        }
    }
}
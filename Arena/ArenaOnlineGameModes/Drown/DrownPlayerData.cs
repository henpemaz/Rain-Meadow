using System;
using UnityEngine;
using RainMeadow;
namespace RainMeadow.Arena.ArenaOnlineGameModes.Drown
{
    public class ArenaDrownClientSettings : OnlineEntity.EntityData
    {
        public int score;
        public int teamScore;
        public bool isInStore;
        public bool iOpenedDen;

        public ArenaDrownClientSettings() { }

        public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
        {
            return new State(this);
        }

        public class State : EntityDataState
        {
            [OnlineField]
            public int score;
            [OnlineField]
            public int teamScore;
            [OnlineField]
            public bool isInStore;
            [OnlineField]
            public bool iOpenedDen;
            public State() { }

            public State(ArenaDrownClientSettings onlineEntity) : base()
            {
                if (RainMeadow.isArenaMode(out var arena) && arena != null &&  DrownMode.isDrownMode(arena, out var drown) && drown != null)
                {
                    score = onlineEntity.score;
                    teamScore = onlineEntity.teamScore;
                    isInStore = drown.isInStore;
                    iOpenedDen = onlineEntity.iOpenedDen;
                }
            }

            public override void ReadTo(OnlineEntity.EntityData entityData, OnlineEntity onlineEntity)
            {
                var avatarSettings = (ArenaDrownClientSettings)entityData;
                avatarSettings.score = score;
                avatarSettings.isInStore = isInStore;
                avatarSettings.iOpenedDen = iOpenedDen;
                avatarSettings.teamScore = teamScore;

            }

            public override Type GetDataType() => typeof(ArenaDrownClientSettings);
        }
    }
}
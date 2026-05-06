using System;
using UnityEngine;
using RainMeadow;
namespace Drown
{
    public class ArenaDrownClientSettings : OnlineEntity.EntityData
    {
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
            public bool isInStore;
            [OnlineField]
            public bool iOpenedDen;
            public State() { }

            public State(ArenaDrownClientSettings onlineEntity) : base()
            {
                if (RainMeadow.RainMeadow.isArenaMode(out var arena) && arena != null && DrownMode.isDrownMode(arena, out var drown) && drown != null)
                {
                    isInStore = drown.isInStore;
                    iOpenedDen = onlineEntity.iOpenedDen;
                }
            }

            public override void ReadTo(OnlineEntity.EntityData entityData, OnlineEntity onlineEntity)
            {
                var avatarSettings = (ArenaDrownClientSettings)entityData;
                avatarSettings.isInStore = isInStore;
                avatarSettings.iOpenedDen = iOpenedDen;

            }

            public override Type GetDataType() => typeof(ArenaDrownClientSettings);
        }
    }
}
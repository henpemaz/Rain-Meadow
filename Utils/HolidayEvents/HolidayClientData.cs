using System;

namespace RainMeadow
{
    public class HolidayClientSettingsData : OnlineEntity.EntityData
    {
        public int meadowCoins = RainMeadow.rainMeadowOptions.MeadowCoins.Value;

        public override EntityDataState MakeState(OnlineEntity onlineEntity, OnlineResource inResource)
        {
            return new State(this);
        }

        public class State : EntityDataState
        {
            [OnlineField]
            public int meadowCoins;


            public State() { }
            public State(HolidayClientSettingsData hClient) : base()
            {
                meadowCoins = hClient.meadowCoins;
            }

            public override Type GetDataType()
            {
                return typeof(HolidayClientSettingsData);
            }

            public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
            {
                var hClientData = (HolidayClientSettingsData)data;
                hClientData.meadowCoins = meadowCoins;

            }
        }
    }
}

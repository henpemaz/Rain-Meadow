using System;

namespace RainMeadow
{
    public class StoryClientSettingsData : OnlineEntity.EntityData
    {
        public bool readyForWin;
        public bool isDead;

        public override EntityDataState MakeState(OnlineEntity onlineEntity, OnlineResource inResource)
        {
            return new State(this);
        }

        public class State : EntityDataState
        {
            [OnlineField(group = "game")]
            public bool readyForWin;
            [OnlineField(group = "game")]
            public bool isDead;

            public State() { }
            public State(StoryClientSettingsData storyClient) : base()
            {
                readyForWin = storyClient.readyForWin;
                isDead = storyClient.isDead;
            }

            public override Type GetDataType()
            {
                return typeof(StoryClientSettingsData);
            }

            public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
            {
                var storyClientData = (StoryClientSettingsData)data;
                storyClientData.readyForWin = readyForWin;
                storyClientData.isDead = isDead;
            }
        }
    }
}

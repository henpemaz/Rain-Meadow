using System;
using System.Linq;

namespace RainMeadow
{
    internal class MeadowMusicData : OnlineEntity.EntityData
    {
        //public int inGroup = -1;
        //public bool isDJ = true;
        public string providedSong;
        public float startedPlayingAt;
        public MeadowMusicData() : base() { }

        public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
        {
            if (inResource is WorldSession)
            {
                RainMeadow.Trace($"{this} for {entity} making state in {inResource}");
                return new State(this);
            }
            RainMeadow.Trace($"{this} for {entity} skipping state in {inResource}");
            return null;
        }

        public class State : EntityDataState
        {
            //[OnlineField(group = "music")]
            //public int inGroup;
            //[OnlineField(group = "music")]
            //public bool isDJ;
            [OnlineField(group = "music", nullable = true)]
            public string providedSong;
            [OnlineField(group = "music")]
            public float startedPlayingAt;

            public State() { }
            public State(MeadowMusicData mcd)
            {
                RainMeadow.Trace("From Data to State " + mcd); 
                //Copy From data to state
                //state = data;
                //inGroup = mcd.inGroup;
                //isDJ = mcd.isDJ;
                providedSong = mcd.providedSong;
                startedPlayingAt = mcd.startedPlayingAt;
                //RainMeadow.Debug("Sent: " + inGroup + " " + isDJ + " " + providedSong + " " + startedPlayingAt);
            }

            public override Type GetDataType() => typeof(MeadowMusicData);

            public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
            {
                var mcd = (MeadowMusicData)data;
                //Read from state to data
                //RainMeadow.Debug("Recieved: " + inGroup + " " + isDJ + " " + providedSong + " " + startedPlayingAt);
                //mcd.inGroup = inGroup;
                //mcd.isDJ = isDJ;
                mcd.providedSong = providedSong;
                mcd.startedPlayingAt = startedPlayingAt;
            }
        }
    }
}
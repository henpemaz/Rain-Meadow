using System.Linq;

namespace RainMeadow
{
    internal class MeadowMusicData : OnlineEntity.EntityData
    {
        private OnlineCreature oc;
        public int inGroup = -1;
        public bool isDJ = true;
        public string providedSong;
        public float startedPlayingAt;
        public MeadowMusicData(OnlineCreature oc)
        {
            this.oc = oc;
        }

        internal override EntityDataState MakeState(OnlineResource inResource)
        {
            if (inResource is RoomSession)
            {
                RainMeadow.Trace($"{this} for {oc} making state in {inResource}");
                return new State(this); //todo, don't send every fucking frame dude.
            }
            RainMeadow.Trace($"{this} for {oc} skipping state in {inResource}");
            return null;
        }

        public class State : EntityDataState
        {
            [OnlineField(group = "music")]
            public int inGroup;
            [OnlineField(group = "music")]
            public bool isDJ;
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
                inGroup = mcd.inGroup;
                isDJ = mcd.isDJ;
                providedSong = mcd.providedSong;
                startedPlayingAt = mcd.startedPlayingAt;
                //RainMeadow.Debug("Sent: " + inGroup + " " + isDJ + " " + providedSong + " " + startedPlayingAt);
            }

            internal override void ReadTo(OnlineEntity onlineEntity)
            {
                RainMeadow.Trace("From state to data " + onlineEntity);
                if (onlineEntity is OnlineCreature oc && oc.TryGetData<MeadowMusicData>(out var mcd))
                {
                    //Read from state to data
                    //RainMeadow.Debug("Recieved: " + inGroup + " " + isDJ + " " + providedSong + " " + startedPlayingAt);
                    mcd.inGroup = inGroup;
                    mcd.isDJ = isDJ;
                    mcd.providedSong = providedSong;
                    mcd.startedPlayingAt = startedPlayingAt;
                }
                else
                {
                    RainMeadow.Error("Failed to read MeadowMusicDataState into " + onlineEntity);
                }
            }
        }
    }
}
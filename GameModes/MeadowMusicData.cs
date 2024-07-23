namespace RainMeadow
{
    internal class MeadowMusicData : OnlineEntity.EntityData
    {
        private OnlineCreature oc;
        public int inGroup;
        public bool isDJ;
        public string? providedSong;
        public float? startedPlayingAt;
        public MeadowMusicData(OnlineCreature oc)
        {
            this.oc = oc;
        }

        internal override EntityDataState MakeState(OnlineResource inResource)
        {
            throw new System.NotImplementedException();
        }
    }
}
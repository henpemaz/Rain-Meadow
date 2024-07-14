namespace RainMeadow
{
    internal class MeadowMusicData : OnlineEntity.EntityData
    {
        private OnlineCreature oc;
        bool isDJ;
        int ingroup;
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
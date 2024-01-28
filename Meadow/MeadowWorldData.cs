namespace RainMeadow
{
    internal class MeadowWorldData : OnlineResource.ResourceData
    {
        internal int spawnedItems;
        internal ushort[] itemsPerRoom;
        private WorldSession ws;
        ushort itemsInRegion;

        public MeadowWorldData(WorldSession ws)
        {
            this.ws = ws;
        }

        internal override OnlineResource.ResourceDataState MakeState(OnlineResource inResource)
        {
            return new MeadowWorldState(ws);
        }

        internal class MeadowWorldState : OnlineResource.ResourceDataState
        {
            [OnlineField]
            bool placeholder;
            public MeadowWorldState() { }
            public MeadowWorldState(WorldSession ws)
            {

            }

            internal override void ReadTo(OnlineResource onlineResource)
            {

            }
        }
    }
}
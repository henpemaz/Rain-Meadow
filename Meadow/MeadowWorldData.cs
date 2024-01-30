namespace RainMeadow
{
    internal class MeadowWorldData : OnlineResource.ResourceData
    {
        internal ushort spawnedItems;

        internal override OnlineResource.ResourceDataState MakeState(OnlineResource inResource)
        {
            return new MeadowWorldState(this);
        }

        internal class MeadowWorldState : OnlineResource.ResourceDataState
        {
            [OnlineField]
            ushort spawnedItems;

            public MeadowWorldState() { }
            public MeadowWorldState(MeadowWorldData meadowWorldData)
            {
                spawnedItems = meadowWorldData.spawnedItems;
            }

            internal override void ReadTo(OnlineResource onlineResource)
            {
                onlineResource.GetData<MeadowWorldData>().spawnedItems = spawnedItems;
            }
        }
    }
}
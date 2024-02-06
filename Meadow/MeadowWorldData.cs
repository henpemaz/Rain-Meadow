using System;

namespace RainMeadow
{
    internal class MeadowWorldData : OnlineResource.ResourceData
    {
        internal ushort spawnedItems;

        public MeadowWorldData(OnlineResource resource) : base(resource) { }

        internal override ResourceDataState MakeState()
        {
            return new State(this);
        }

        internal class State : ResourceDataState
        {
            [OnlineField]
            ushort spawnedItems;

            public State() { }
            public State(MeadowWorldData meadowWorldData)
            {
                spawnedItems = meadowWorldData.spawnedItems;
            }

            internal override Type GetDataType() => typeof(MeadowWorldData);

            internal override void ReadTo(OnlineResource.ResourceData data)
            {
                (data as MeadowWorldData).spawnedItems = spawnedItems;
            }
        }
    }
}
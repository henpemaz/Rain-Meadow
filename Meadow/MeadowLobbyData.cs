using System;
using System.Linq;

namespace RainMeadow
{
    internal class MeadowLobbyData : OnlineResource.ResourceData
    {
        public ushort[] itemsPerRegion;

        public MeadowLobbyData(OnlineResource resource) : base(resource) { }

        internal override ResourceDataState MakeState()
        {
            return new State(this);
        }

        internal class State : ResourceDataState
        {
            [OnlineField]
            Generics.AddRemoveSortedUshorts itemsPerRegion;
            public State() { }
            public State(MeadowLobbyData meadowLobbyData)
            {
                itemsPerRegion = new(meadowLobbyData.itemsPerRegion.ToList());
            }

            internal override Type GetDataType() => typeof(MeadowLobbyData);

            internal override void ReadTo(OnlineResource.ResourceData data)
            {
                (data as MeadowLobbyData).itemsPerRegion = itemsPerRegion.list.ToArray();
            }
        }
    }
}
using System;
using System.Linq;

namespace RainMeadow
{
    internal class MeadowLobbyData : OnlineResource.ResourceData
    {
        public ushort[] itemsPerRegion;

        internal override OnlineResource.ResourceDataState MakeState(OnlineResource inResource)
        {
            return new MeadowLobbyState(this);
        }

        internal class MeadowLobbyState : OnlineResource.ResourceDataState
        {
            [OnlineField]
            Generics.AddRemoveSortedUshorts itemsPerRegion;
            public MeadowLobbyState() { }
            public MeadowLobbyState(MeadowLobbyData meadowLobbyData)
            {
                itemsPerRegion = new(meadowLobbyData.itemsPerRegion.ToList());
            }

            internal override void ReadTo(OnlineResource onlineResource)
            {
                onlineResource.GetData<MeadowLobbyData>().itemsPerRegion = itemsPerRegion.list.ToArray();
            }
        }
    }
}
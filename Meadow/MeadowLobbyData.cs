using System;
using System.Linq;

namespace RainMeadow
{
    internal class MeadowRegionData : OnlineResource.ResourceData
    {
        public float[] regionSpawnWeights;
        public ushort[] regionRedTokensGoal;
        public ushort[] regionBlueTokensGoal;
        public ushort[] regionGoldTokensGoal;
        public ushort[] regionGhostsGoal;

        public int redTokensGoal;
        public int blueTokensGoal;
        public int goldTokensGoal;
        public int ghostsGoal;
        public override ResourceDataState MakeState(OnlineResource resource)
        {
            return new State(this);
        }

        internal class State : ResourceDataState
        {
            [OnlineField]
            Generics.FixedOrderedUshorts regionRedTokensGoal;
            [OnlineField]
            Generics.FixedOrderedUshorts regionBlueTokensGoal;
            [OnlineField]
            Generics.FixedOrderedUshorts regionGoldTokensGoal;
            [OnlineField]
            Generics.FixedOrderedUshorts regionGhostsGoal;
            public State() { }
            public State(MeadowRegionData meadowLobbyData)
            {
                regionRedTokensGoal = new(meadowLobbyData.regionRedTokensGoal.ToList());
                regionBlueTokensGoal = new(meadowLobbyData.regionBlueTokensGoal.ToList());
                regionGoldTokensGoal = new(meadowLobbyData.regionGoldTokensGoal.ToList());
                regionGhostsGoal = new(meadowLobbyData.regionGhostsGoal.ToList());
            }

            public override Type GetDataType() => typeof(MeadowRegionData);

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                MeadowRegionData? meadowLobbyData = (data as MeadowRegionData);
                meadowLobbyData.regionRedTokensGoal = regionRedTokensGoal.list.ToArray();
                meadowLobbyData.regionBlueTokensGoal = regionBlueTokensGoal.list.ToArray();
                meadowLobbyData.regionGoldTokensGoal = regionGoldTokensGoal.list.ToArray();
                meadowLobbyData.regionGhostsGoal = regionGhostsGoal.list.ToArray();
            }
        }
    }
}
using System;
using System.Linq;

namespace RainMeadow
{
    internal class MeadowLobbyData : OnlineResource.ResourceData
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

        public MeadowLobbyData(OnlineResource resource) : base(resource) { }

        internal override ResourceDataState MakeState()
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
            public State(MeadowLobbyData meadowLobbyData)
            {
                regionRedTokensGoal = new(meadowLobbyData.regionRedTokensGoal.ToList());
                regionBlueTokensGoal = new(meadowLobbyData.regionBlueTokensGoal.ToList());
                regionGoldTokensGoal = new(meadowLobbyData.regionGoldTokensGoal.ToList());
                regionGhostsGoal = new(meadowLobbyData.regionGhostsGoal.ToList());
            }

            internal override Type GetDataType() => typeof(MeadowLobbyData);

            internal override void ReadTo(OnlineResource.ResourceData data)
            {
                MeadowLobbyData? meadowLobbyData = (data as MeadowLobbyData);
                meadowLobbyData.regionRedTokensGoal = regionRedTokensGoal.list.ToArray();
                meadowLobbyData.regionBlueTokensGoal = regionBlueTokensGoal.list.ToArray();
                meadowLobbyData.regionGoldTokensGoal = regionGoldTokensGoal.list.ToArray();
                meadowLobbyData.regionGhostsGoal = regionGhostsGoal.list.ToArray();
            }
        }
    }
}
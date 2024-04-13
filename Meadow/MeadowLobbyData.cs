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

        public int redTokensGoal;
        public int blueTokensGoal;
        public int goldTokensGoal;

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
            public State() { }
            public State(MeadowLobbyData meadowLobbyData)
            {
                regionRedTokensGoal = new(meadowLobbyData.regionRedTokensGoal.ToList());
                regionBlueTokensGoal = new(meadowLobbyData.regionBlueTokensGoal.ToList());
                regionGoldTokensGoal = new(meadowLobbyData.regionGoldTokensGoal.ToList());
            }

            internal override Type GetDataType() => typeof(MeadowLobbyData);

            internal override void ReadTo(OnlineResource.ResourceData data)
            {
                (data as MeadowLobbyData).regionRedTokensGoal = regionRedTokensGoal.list.ToArray();
                (data as MeadowLobbyData).regionBlueTokensGoal = regionBlueTokensGoal.list.ToArray();
                (data as MeadowLobbyData).regionGoldTokensGoal = regionGoldTokensGoal.list.ToArray();
            }
        }
    }
}
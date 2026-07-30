using System;
using RainMeadow.Arena.ArenaOnlineGameModes.ArenaChallengeModeNS;

namespace RainMeadow
{
    internal class ChallengeLobbyData : OnlineResource.ResourceData
    {
        public ChallengeLobbyData() { }

        public override ResourceDataState MakeState(OnlineResource resource)
        {
            return new State(this, resource);
        }

        internal class State : ResourceDataState
        {
            [OnlineField]
            public int challengeID;

            public State() { }

            public State(ChallengeLobbyData lobbyData, OnlineResource onlineResource)
            {
                if (ArenaChallengeMode.IsChallengeMode(out ArenaChallengeMode challenge))
                    challengeID = challenge.challengeID;
            }

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                if (ArenaChallengeMode.IsChallengeMode(out ArenaChallengeMode challenge))
                    challenge.challengeID = challengeID;
            }

            public override Type GetDataType() => typeof(TeamBattleLobbyData);
        }
    }
}

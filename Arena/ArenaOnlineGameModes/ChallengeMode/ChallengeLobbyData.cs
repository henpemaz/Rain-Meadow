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

            public State(ChallengeLobbyData arenaLobbyData, OnlineResource onlineResource)
            {
                bool isCh = ArenaChallengeMode.IsChallengeMode(out var chMode);
                if (isCh && chMode != null)
                {
                    challengeID = chMode.challengeID;
                }
            }

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                bool isCh = ArenaChallengeMode.IsChallengeMode(out var chMode);
                if (isCh && chMode != null)
                {
                    chMode.challengeID = challengeID;
                }
            }

            public override Type GetDataType() => typeof(TeamBattleLobbyData);
        }
    }
}

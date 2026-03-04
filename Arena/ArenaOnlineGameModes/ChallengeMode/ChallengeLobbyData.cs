using System;
using System.Collections.Generic;
using System.Linq;
using RainMeadow.Arena.ArenaOnlineGameModes.ArenaChallengeModeNS;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using UnityEngine;

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
                ArenaOnlineGameMode arena =
                    (onlineResource as Lobby).gameMode as ArenaOnlineGameMode;
                if (arena != null)
                {
                    bool isCh = ArenaChallengeMode.isChallengeMode(arena, out var chMode);
                    if (isCh && chMode != null)
                    {
                        challengeID = chMode.challengeID;
                    }
                }
            }

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                var lobby = (resource as Lobby);
                var arena = (lobby.gameMode as ArenaOnlineGameMode);
                if (arena != null)
                {
                    bool isCh = ArenaChallengeMode.isChallengeMode(arena, out var chMode);
                    if (isCh && chMode != null)
                    {
                        chMode.challengeID = challengeID;
                    }
                }
            }

            public override Type GetDataType() => typeof(TeamBattleLobbyData);
        }
    }
}

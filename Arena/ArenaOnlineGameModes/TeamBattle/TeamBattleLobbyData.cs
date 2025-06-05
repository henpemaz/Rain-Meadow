using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    internal class TeamBattleLobbyData : OnlineResource.ResourceData
    {
        public TeamBattleLobbyData() { }

        public override ResourceDataState MakeState(OnlineResource resource)
        {
            return new State(this, resource);

        }

        internal class State : ResourceDataState
        {
            [OnlineField]
            public int winningTeam;
            public State() { }
            public State(TeamBattleLobbyData arenaLobbyData, OnlineResource onlineResource)
            {
                ArenaOnlineGameMode arena = (onlineResource as Lobby).gameMode as ArenaOnlineGameMode;
                bool isTb = TeamBattleMode.isTeamBattleMode(arena, out var teamBattleMode);
                if (isTb && teamBattleMode != null)
                {
                    winningTeam = teamBattleMode.winningTeam;
                }
            }

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                var lobby = (resource as Lobby);
                var arena = (lobby.gameMode as ArenaOnlineGameMode);
                bool cachedTb = TeamBattleMode.isTeamBattleMode(arena, out var teamBattleMode);
                if (cachedTb && teamBattleMode != null)
                {
                    teamBattleMode.winningTeam = winningTeam;
                }



            }

            public override Type GetDataType() => typeof(TeamBattleLobbyData);
        }
    }
}
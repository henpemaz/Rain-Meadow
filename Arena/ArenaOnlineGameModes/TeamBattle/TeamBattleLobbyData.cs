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
            [OnlineField]
            public int martyrs;
            [OnlineField]
            public int outlaws;
            [OnlineField]
            public int dragonslayers;
            [OnlineField]
            public int chieftains;
            //[OnlineField]
            //public string martyrsName;
            //[OnlineField]
            //public string chieftainsName;
            //[OnlineField]
            //public string dragonslayersName;
            //[OnlineField]
            //public string outlawsName;


            [OnlineField]
            public int roundSpawnPointCycler;
            public State() { }
            public State(TeamBattleLobbyData arenaLobbyData, OnlineResource onlineResource)
            {
                ArenaOnlineGameMode arena = (onlineResource as Lobby).gameMode as ArenaOnlineGameMode;
                bool isTb = TeamBattleMode.isTeamBattleMode(arena, out var teamBattleMode);
                if (isTb && teamBattleMode != null)
                {
                    winningTeam = teamBattleMode.winningTeam;
                    martyrs = teamBattleMode.martyrsSpawn;
                    outlaws = teamBattleMode.outlawsSpawn;
                    dragonslayers = teamBattleMode.dragonslayersSpawn;
                    chieftains = teamBattleMode.chieftainsSpawn;

                    roundSpawnPointCycler = teamBattleMode.roundSpawnPointCycler;

                    //martyrsName = RainMeadow.rainMeadowOptions.MartyrTeamName.Value;
                    //chieftainsName = RainMeadow.rainMeadowOptions.ChieftainTeamName.Value;
                    //dragonslayersName = RainMeadow.rainMeadowOptions.DragonSlayersTeamName.Value;
                    //outlawsName = RainMeadow.rainMeadowOptions.OutlawsTeamName.Value;

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
                    teamBattleMode.martyrsSpawn = martyrs;
                    teamBattleMode.outlawsSpawn = outlaws;
                    teamBattleMode.dragonslayersSpawn = dragonslayers;
                    teamBattleMode.outlawsSpawn = outlaws;

                    teamBattleMode.roundSpawnPointCycler = roundSpawnPointCycler;

                    //teamBattleMode.martyrsTeamName = martyrsName;
                    //teamBattleMode.dragonSlayersTeamNames = dragonslayersName;
                    //teamBattleMode.chieftainsTeamNames = chieftainsName;
                    //teamBattleMode.outlawTeamNames = outlawsName;

                }



            }

            public override Type GetDataType() => typeof(TeamBattleLobbyData);
        }
    }
}
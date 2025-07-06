using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
            [OnlineFieldHalf]
            public float lerp;
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
            [OnlineField(nullable =true)]
            public string martyrsName;
            [OnlineField(nullable = true)]
            public string chieftainsName;
            [OnlineField(nullable = true)]
            public string dragonslayersName;
            [OnlineField(nullable = true)]
            public string outlawsName;
            [OnlineFieldColorRgb]
            public Color martyrColors;
            [OnlineFieldColorRgb]
            public Color chieftainColors;
            [OnlineFieldColorRgb]
            public Color dragonslayerColors;
            [OnlineFieldColorRgb]
            public Color outlawColors;


            [OnlineField]
            public int roundSpawnPointCycler;
            public State() { }
            public State(TeamBattleLobbyData arenaLobbyData, OnlineResource onlineResource)
            {
                ArenaOnlineGameMode arena = (onlineResource as Lobby).gameMode as ArenaOnlineGameMode;
                if (arena != null)
                {
                    martyrColors = TeamBattleMode.teamColors[0];
                    outlawColors = TeamBattleMode.teamColors[1];
                    dragonslayerColors = TeamBattleMode.teamColors[2];
                    chieftainColors = TeamBattleMode.teamColors[3];

                    bool isTb = TeamBattleMode.isTeamBattleMode(arena, out var teamBattleMode);
                    if (isTb && teamBattleMode != null)
                    {
                        martyrsName = teamBattleMode.teamNames[0];
                        chieftainsName = teamBattleMode.teamNames[1];
                        dragonslayersName = teamBattleMode.teamNames[2];
                        outlawsName = teamBattleMode.teamNames[3];
                        winningTeam = teamBattleMode.winningTeam;
                        martyrs = teamBattleMode.martyrsSpawn;
                        outlaws = teamBattleMode.outlawsSpawn;
                        dragonslayers = teamBattleMode.dragonslayersSpawn;
                        chieftains = teamBattleMode.chieftainsSpawn;
                        roundSpawnPointCycler = teamBattleMode.roundSpawnPointCycler;
                        lerp = teamBattleMode.lerp;

                    }
                }
            }

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                var lobby = (resource as Lobby);
                var arena = (lobby.gameMode as ArenaOnlineGameMode);
                if (arena != null)
                {
                    TeamBattleMode.teamColors[0] = martyrColors;
                    TeamBattleMode.teamColors[1] = outlawColors;
                    TeamBattleMode.teamColors[2] = dragonslayerColors;
                    TeamBattleMode.teamColors[3] = chieftainColors;


                    bool cachedTb = TeamBattleMode.isTeamBattleMode(arena, out var teamBattleMode);
                    if (cachedTb && teamBattleMode != null)
                    {
                        teamBattleMode.teamNames[0] = martyrsName;
                        teamBattleMode.teamNames[1] = chieftainsName;
                        teamBattleMode.teamNames[2] = outlawsName;
                        teamBattleMode.teamNames[3] = dragonslayersName;
                        teamBattleMode.winningTeam = winningTeam;
                        teamBattleMode.martyrsSpawn = martyrs;
                        teamBattleMode.outlawsSpawn = outlaws;
                        teamBattleMode.dragonslayersSpawn = dragonslayers;

                        teamBattleMode.roundSpawnPointCycler = roundSpawnPointCycler;
                        teamBattleMode.lerp = lerp;


                    }
                }
            }

            public override Type GetDataType() => typeof(TeamBattleLobbyData);
        }
    }
}
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
            [OnlineFieldColorRgb(nullable = true)]
            public Color martyrColors;
            [OnlineFieldColorRgb(nullable = true)]
            public Color chieftainColors;
            [OnlineFieldColorRgb(nullable = true)]
            public Color dragonslayerColors;
            [OnlineFieldColorRgb(nullable = true)]
            public Color outlawColors;


            [OnlineField]
            public int roundSpawnPointCycler;
            public State() { }
            public State(TeamBattleLobbyData arenaLobbyData, OnlineResource onlineResource)
            {
                ArenaOnlineGameMode arena = (onlineResource as Lobby).gameMode as ArenaOnlineGameMode;
                if (arena != null)
                {
                    bool isTb = TeamBattleMode.isTeamBattleMode(arena, out var teamBattleMode);
                    if (isTb && teamBattleMode != null)
                    {
                        martyrColors = teamBattleMode.teamColors[0];
                        outlawColors = teamBattleMode.teamColors[1];
                        dragonslayerColors = teamBattleMode.teamColors[2];
                        chieftainColors = teamBattleMode.teamColors[3];
                        martyrsName = teamBattleMode.teamNames[0];
                        outlawsName = teamBattleMode.teamNames[1];
                        dragonslayersName = teamBattleMode.teamNames[2];
                        chieftainsName = teamBattleMode.teamNames[3];
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


                    bool cachedTb = TeamBattleMode.isTeamBattleMode(arena, out var teamBattleMode);
                    if (cachedTb && teamBattleMode != null)
                    {
                        teamBattleMode.teamColors[0] = martyrColors;
                        teamBattleMode.teamColors[1] = outlawColors;
                        teamBattleMode.teamColors[2] = dragonslayerColors;
                        teamBattleMode.teamColors[3] = chieftainColors;
                        teamBattleMode.teamNames[0] = martyrsName;
                        teamBattleMode.teamNames[1] = outlawsName;
                        teamBattleMode.teamNames[2] = dragonslayersName;
                        teamBattleMode.teamNames[3] = chieftainsName;
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
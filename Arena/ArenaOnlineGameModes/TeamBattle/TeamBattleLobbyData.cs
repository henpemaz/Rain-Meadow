using System;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using UnityEngine;

namespace RainMeadow
{
    internal class TeamBattleLobbyData : OnlineResource.ResourceData
    {
        public override ResourceDataState MakeState(OnlineResource resource)
        {
            return new State(this, resource);
        }

        internal class State : ResourceDataState
        {
            [OnlineFieldHalf]
            public float lerp;
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

            public State(TeamBattleLobbyData lobbyData, OnlineResource onlineResource)
            {
                if (!TeamBattleMode.IsTeamBattleMode(out TeamBattleMode teamBattle))
                    return;

                martyrColors = teamBattle.teamColors[0];
                outlawColors = teamBattle.teamColors[1];
                dragonslayerColors = teamBattle.teamColors[2];
                chieftainColors = teamBattle.teamColors[3];
                martyrsName = teamBattle.teamNames[0];
                outlawsName = teamBattle.teamNames[1];
                dragonslayersName = teamBattle.teamNames[2];
                chieftainsName = teamBattle.teamNames[3];
                martyrs = teamBattle.martyrsSpawn;
                outlaws = teamBattle.outlawsSpawn;
                dragonslayers = teamBattle.dragonslayersSpawn;
                chieftains = teamBattle.chieftainsSpawn;
                roundSpawnPointCycler = teamBattle.roundSpawnPointCycler;
                lerp = teamBattle.lerp;
            }

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                if (!TeamBattleMode.IsTeamBattleMode(out TeamBattleMode teamBattle))
                    return;

                teamBattle.teamColors[0] = martyrColors;
                teamBattle.teamColors[1] = outlawColors;
                teamBattle.teamColors[2] = dragonslayerColors;
                teamBattle.teamColors[3] = chieftainColors;
                teamBattle.teamNames[0] = martyrsName;
                teamBattle.teamNames[1] = outlawsName;
                teamBattle.teamNames[2] = dragonslayersName;
                teamBattle.teamNames[3] = chieftainsName;
                teamBattle.martyrsSpawn = martyrs;
                teamBattle.outlawsSpawn = outlaws;
                teamBattle.dragonslayersSpawn = dragonslayers;
                teamBattle.roundSpawnPointCycler = roundSpawnPointCycler;
                teamBattle.lerp = lerp;
            }

            public override Type GetDataType() => typeof(TeamBattleLobbyData);
        }
    }
}

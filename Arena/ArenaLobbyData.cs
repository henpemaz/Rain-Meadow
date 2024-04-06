using System;
using System.Linq;

namespace RainMeadow
{
    internal class ArenaLobbyData : OnlineResource.ResourceData
    {
        public ArenaLobbyData(OnlineResource resource) : base(resource) { }

        internal override ResourceDataState MakeState()
        {
            return new State(this, resource);
        }

        internal class State : ResourceDataState
        {
            [OnlineField]
            public bool dummyTest;


            public State() { }
            public State(ArenaLobbyData arenaLobbyData, OnlineResource onlineResource)
            {
                ArenaCompetitiveGameMode arenaGameMode = (onlineResource as Lobby).gameMode as ArenaCompetitiveGameMode;
                dummyTest = arenaGameMode.dummyTest;


            }

            internal override Type GetDataType() => typeof(ArenaLobbyData);

            internal override void ReadTo(OnlineResource.ResourceData data)
            {
                var lobby = (data.resource as Lobby);
                (lobby.gameMode as ArenaCompetitiveGameMode).dummyTest = dummyTest;
            }
        }
    }
}
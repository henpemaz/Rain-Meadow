using System;
using System.Collections.Generic;
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
            public List<string> arenaPlaylist;


            public State() { }
            public State(ArenaLobbyData arenaLobbyData, OnlineResource onlineResource)
            {
                ArenaCompetitiveGameMode arenaGameMode = (onlineResource as Lobby).gameMode as ArenaCompetitiveGameMode;
                arenaPlaylist = arenaGameMode.playList;


            }

            internal override Type GetDataType() => typeof(ArenaLobbyData);

            internal override void ReadTo(OnlineResource.ResourceData data)
            {
                var lobby = (data.resource as Lobby);
                (lobby.gameMode as ArenaCompetitiveGameMode).playList = arenaPlaylist;
            }
        }
    }
}
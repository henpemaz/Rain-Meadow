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
            public List<string> playlist = new List<string>(); // possibly convert to a ushort (map index)


            public State() { }
            public State(ArenaLobbyData arenaLobbyData, OnlineResource onlineResource)
            {
                ArenaCompetitiveGameMode arenaGameMode = (onlineResource as Lobby).gameMode as ArenaCompetitiveGameMode;

				var process = RWCustom.Custom.rainWorld.processManager.currentMainLoop;
				if (process is not ArenaLobbyMenu)
				{
					return;
				}

				var menu = process as ArenaLobbyMenu;

                playlist = menu.mm.GetGameTypeSetup.playList;

            }



			internal override Type GetDataType() => typeof(ArenaLobbyData);

            internal override void ReadTo(OnlineResource.ResourceData data)
            {
                var lobby = (data.resource as Lobby);

				var process = RWCustom.Custom.rainWorld.processManager.currentMainLoop;
				if (process is not ArenaLobbyMenu)
				{
					return;
				}

				var menu = process as ArenaLobbyMenu;

				menu.mm.GetGameTypeSetup.playList = playlist;
                
            }
        }
    }
}
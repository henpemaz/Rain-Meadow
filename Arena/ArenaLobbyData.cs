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

            [OnlineField]
            public bool evilAI;

            [OnlineField]
            public byte rainTime;

            [OnlineField]
            public byte repeats;

            [OnlineField]
            public byte wildlife;

            [OnlineField]
            public bool shufflePlaylist;

            public State() { }
            public State(ArenaLobbyData arenaLobbyData, OnlineResource onlineResource)
            {
                ArenaCompetitiveGameMode arenaGameMode = (onlineResource as Lobby).gameMode as ArenaCompetitiveGameMode;

                var process = RWCustom.Custom.rainWorld.processManager.currentMainLoop;
                if (process is not ArenaLobbyMenu menu)
                {
                    return;
                }

                playlist = menu.mm.GetGameTypeSetup.playList;
                repeats = (byte)menu.mm.GetGameTypeSetup.levelRepeats;
                rainTime = (byte)menu.mm.arenaSettingsInterface.rainTimer.CheckedButton;
                evilAI = menu.mm.arenaSettingsInterface.evilAICheckBox.Checked;
                wildlife = (byte)menu.mm.arenaSettingsInterface.wildlifeArray.CheckedButton;
                shufflePlaylist = menu.mm.GetGameTypeSetup.shufflePlaylist;
            }



            internal override Type GetDataType() => typeof(ArenaLobbyData);

            internal override void ReadTo(OnlineResource.ResourceData data)
            {
                var lobby = (data.resource as Lobby);

                var process = RWCustom.Custom.rainWorld.processManager.currentMainLoop;
                if (process is not ArenaLobbyMenu menu)
                {
                    return;
                }

                menu.mm.GetGameTypeSetup.playList = playlist;
                menu.mm.GetGameTypeSetup.levelRepeats = repeats;
                menu.mm.arenaSettingsInterface.rainTimer.CheckedButton = rainTime;
                menu.mm.arenaSettingsInterface.evilAICheckBox.Checked = evilAI;
                menu.mm.arenaSettingsInterface.wildlifeArray.CheckedButton = wildlife;
                menu.mm.GetGameTypeSetup.shufflePlaylist = shufflePlaylist;
            }
        }
    }
}
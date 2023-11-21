﻿using Menu;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        public class Ext_ProcessID
        {
            public static ProcessManager.ProcessID OnlineManager = new("MeadowOnlineManager", true);
            public static ProcessManager.ProcessID LobbySelectMenu = new("MeadowLobbySelectMenu", true);
            public static ProcessManager.ProcessID LobbyMenu = new("MeadowLobbyMenu", true);
            public static ProcessManager.ProcessID ArenaLobbyMenu = new("MeadowArenaLobbyMenu", true);
            public static ProcessManager.ProcessID MeadowMenu = new("MeadowMenu", true);
        }

        public class Ext_SlugcatStatsName
        {
            public static SlugcatStats.Name OnlineSessionPlayer = new("MeadowOnline", true);
            public static SlugcatStats.Name OnlineSessionRemotePlayer = new("MeadowOnlineRemote", true);
        }

        public class Ext_SceneID
        {
            // MeadowSlugcat => Slugcat_White
            public static MenuScene.SceneID Slugcat_MeadowSquidcicada = new("Slugcat_MeadowSquidcicada", true);
            public static MenuScene.SceneID Slugcat_MeadowLizard = new("Slugcat_MeadowLizard", true);
        }
    }
}

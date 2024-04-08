using Menu;

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
            public static ProcessManager.ProcessID StoryMenu = new("StoryMenu", true);
        }

        public class Ext_SlugcatStatsName
        {
            public static SlugcatStats.Name OnlineSessionPlayer = new("MeadowOnline", true);
            public static SlugcatStats.Name OnlineSessionRemotePlayer = new("MeadowOnlineRemote", true);

            public static SlugcatStats.Name OnlineStoryWhite = new("OnlineStoryWhite", true);
            public static SlugcatStats.Name OnlineStoryYellow = new("OnlineStoryYellow", true);
            public static SlugcatStats.Name OnlineStoryRed = new("OnlineStoryRed", true);

        }

        public class Ext_SceneID
        {
            // MeadowSlugcat => Slugcat_White
            internal static MenuScene.SceneID Slugcat_MeadowSquidcicada = new("Slugcat_MeadowSquidcicada", true);
            internal static MenuScene.SceneID Slugcat_MeadowLizard = new("Slugcat_MeadowLizard", true);
            internal static MenuScene.SceneID Slugcat_MeadowScav = new("Slugcat_MeadowScav", true);
            internal static MenuScene.SceneID Slugcat_MeadowEggbug = new("Slugcat_MeadowEggbug", true);
        }

        public class Ext_PhysicalObjectType
        {
            public static AbstractPhysicalObject.AbstractObjectType MeadowPlant = new("MeadowPlant", true);
            // public static AbstractPhysicalObject.AbstractObjectType MeadowToken = new("MeadowToken", true);
            public static AbstractPhysicalObject.AbstractObjectType MeadowTokenRed = new("MeadowTokenRed", true);
            public static AbstractPhysicalObject.AbstractObjectType MeadowTokenBlue = new("MeadowTokenBlue", true);
            public static AbstractPhysicalObject.AbstractObjectType MeadowTokenGold = new("MeadowTokenGold", true);
        }
    }
}

using Steamworks;

namespace RainMeadow
{
    // trimmed down version for listing lobbies in menus
    public class LobbyInfo
    {
        public CSteamID id;
        public string name;
        public string mode;
        public int playerCount;

        public LobbyInfo(CSteamID id, string name, string mode, int playerCount)
        {
            this.id = id;
            this.name = name;
            this.mode = mode;
            this.playerCount = playerCount;
        }
    }
}

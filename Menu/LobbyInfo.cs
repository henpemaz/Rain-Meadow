using Steamworks;
using System.Net;

namespace RainMeadow
{
    // trimmed down version for listing lobbies in menus
    public class LobbyInfo
    {
        public IPEndPoint? ipEndpoint;
        public CSteamID id;
        public string name;
        public string mode;
        public string[] mods;
        public bool hasPassword;
        public int playerCount;
        public int maxPlayerCount;

        public LobbyInfo(CSteamID id, string name, string mode, string[] mods, bool hasPassword, int playerCount, int? maxPlayerCount)
        {
            this.id = id;
            this.name = name;
            this.mode = mode;
            this.mods = mods;
            this.playerCount = playerCount;
            this.hasPassword = hasPassword;
            this.maxPlayerCount = (int)maxPlayerCount;
        }

        public LobbyInfo(IPEndPoint id, string name, string mode, string[] mods, bool hasPassword, int playerCount, int? maxPlayerCount)
        {
            this.ipEndpoint = ipEndpoint;

            this.id = default;
            this.name = name;
            this.mode = mode;
            this.playerCount = playerCount;
            this.hasPassword = hasPassword;
            this.maxPlayerCount = (int)maxPlayerCount;
        }
    }
}

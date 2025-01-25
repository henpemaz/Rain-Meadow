using Steamworks;
using System.Net;

namespace RainMeadow
{
    // trimmed down version for listing lobbies in menus
    public class LobbyInfo
    {
        public CSteamID id;
        public string name;
        public string mode;
        public int playerCount;
        public bool hasPassword;
        public int maxPlayerCount;
        public string requiredMods;

        public IPEndPoint? ipEndpoint;

        public LobbyInfo(CSteamID id, string name, string mode, int playerCount, bool hasPassword, int? maxPlayerCount, string highImpactMods = "")
        {
            this.id = id;
            this.name = name;
            this.mode = mode;
            this.playerCount = playerCount;
            this.hasPassword = hasPassword;
            this.maxPlayerCount = (int)maxPlayerCount;
            this.requiredMods = highImpactMods;
        }

        public LobbyInfo(IPEndPoint ipEndpoint, string name, string mode, int playerCount, bool hasPassword, int? maxPlayerCount, string highImpactMods = "")
        {
            this.ipEndpoint = ipEndpoint;

            this.id = default;
            this.name = name;
            this.mode = mode;
            this.playerCount = playerCount;
            this.hasPassword = hasPassword;
            this.maxPlayerCount = (int)maxPlayerCount;
            this.requiredMods = highImpactMods;
        }
    }
}

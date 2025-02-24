using System.Net;

namespace RainMeadow
{
    // trimmed down version for listing lobbies in menus
    public abstract class LobbyInfo
    {
        public string name;
        public string mode;
        public int playerCount;
        public bool hasPassword;
        public int maxPlayerCount;
        public string requiredMods;
        public string bannedMods;

        public LobbyInfo(string name, string mode, int playerCount, bool hasPassword, int? maxPlayerCount, string highImpactMods = "", string bannedMods = "")
        {
            this.name = name;
            this.mode = mode;
            this.playerCount = playerCount;
            this.hasPassword = hasPassword;
            this.maxPlayerCount = (int)maxPlayerCount;
            this.requiredMods = highImpactMods;
            this.bannedMods = bannedMods;
        }
        
    }
}

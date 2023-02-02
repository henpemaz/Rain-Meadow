using Steamworks;

namespace RainMeadow
{
    // trimmed down version for listing lobbies in menus
    public class LobbyInfo
    {
        public CSteamID id;
        public string name;

        public LobbyInfo(CSteamID id)
        {
            this.id = id;
            this.name = SteamMatchmaking.GetLobbyData(id, OnlineManager.NAME_KEY);
        }
    }
}

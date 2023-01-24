using Steamworks;

namespace RainMeadow
{
    public class Lobby
    {
        public CSteamID id;

        public Lobby(CSteamID id)
        {
            this.id = id;
            UpdateInfo();
        }

        public void UpdateInfo()
        {
            owner = new OnlinePlayer(SteamMatchmaking.GetLobbyOwner(id));
            name = SteamMatchmaking.GetLobbyData(id, OnlineManager.NAME_KEY);
        }

        public void SetupNew()
        {
            SteamMatchmaking.SetLobbyData(id, OnlineManager.CLIENT_KEY, OnlineManager.CLIENT_VAL);
            SteamMatchmaking.SetLobbyData(id, OnlineManager.NAME_KEY, SteamFriends.GetPersonaName());
        }

        public OnlinePlayer owner;
        public string name;
    }
}

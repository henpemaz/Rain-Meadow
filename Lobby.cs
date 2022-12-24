using Steamworks;

namespace RainMeadow
{
    public class Lobby
    {
        public CSteamID id;

        public Lobby(CSteamID id)
        {
            this.id = id;
            UpdateInfoShort();
        }

        public void UpdateInfoShort()
        {
            owner = new OnlinePlayer(SteamMatchmaking.GetLobbyOwner(id));
            name = SteamMatchmaking.GetLobbyData(id, LobbyManager.NAME_KEY);
        }

        public void UpdateInfoFull()
        {
            
        }

        public void SetupNew()
        {
            SteamMatchmaking.SetLobbyData(id, LobbyManager.CLIENT_KEY, LobbyManager.CLIENT_VAL);
            SteamMatchmaking.SetLobbyData(id, LobbyManager.NAME_KEY, SteamFriends.GetPersonaName());
        }

        public OnlinePlayer owner;
        public string name;
    }
}

using Steamworks;
using System;

namespace RainMeadow
{
    class Lobby {
        public static string CLIENT_KEY = "client";
        public static string CLIENT_VAL = "Meadow";
        public static string NAME_KEY = "name";

        public Steamworks.CSteamID id;
        public Steamworks.CSteamID owner;

        public OnlinePlayer[] players;

        public Lobby(CSteamID cSteamID)
        {
            this.id = cSteamID;

            UpdateOwner();
        }

        public void UpdateOwner()
        {
            owner = SteamMatchmaking.GetLobbyOwner(id);
        }

        internal void SetupNew()
        {
            SteamMatchmaking.SetLobbyData(id, CLIENT_KEY, CLIENT_VAL);
            SteamMatchmaking.SetLobbyData(id, NAME_KEY, SteamFriends.GetPersonaName());
        }
    }
}

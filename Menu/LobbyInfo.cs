using Steamworks;
using System;
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

        public IPEndPoint? ipEndpoint;

        public LobbyInfo(CSteamID id, string name, string mode, int playerCount)
        {
            this.id = id;
            this.name = name;
            this.mode = mode;
            this.playerCount = playerCount;
        }

        public LobbyInfo(IPEndPoint ipEndpoint, string name, string mode, int playerCount)
        {
            this.ipEndpoint = ipEndpoint;

            this.id = default;
            this.name = name;
            this.mode = mode;
            this.playerCount = playerCount;
        }
    }
}

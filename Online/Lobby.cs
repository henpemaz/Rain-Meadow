using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    // Lobby is tightly bound to a SteamMatchmaking Lobby
    public class Lobby : OnlineResource
    {
        public CSteamID id;
        public List<OnlinePlayer> players;
        public Dictionary<Region, WorldSession> worldSessions;

        public Lobby(CSteamID id)
        {
            this.id = id;
            owner = new OnlinePlayer(SteamMatchmaking.GetLobbyOwner(id));
            if (isOwner)
            {
                SteamMatchmaking.SetLobbyData(id, OnlineManager.CLIENT_KEY, OnlineManager.CLIENT_VAL);
                SteamMatchmaking.SetLobbyData(id, OnlineManager.NAME_KEY, SteamFriends.GetPersonaName());
            }

            players = new List<OnlinePlayer>() { OnlineManager.mePlayer};
            UpdatePlayersList();
            Request(); // Everyone auto-subscribes this resource
        }

        public void UpdatePlayersList()
        {
            var n = SteamMatchmaking.GetNumLobbyMembers(OnlineManager.lobby.id);
            var oldplayers = players.Cast<CSteamID>().ToArray();
            var newplayers = new CSteamID[n];
            for (int i = 0; i < n; i++)
            {
                newplayers[i] = SteamMatchmaking.GetLobbyMemberByIndex(id, i);
            }
            foreach (var p in oldplayers)
            {
                if (!newplayers.Contains(p)) PlayerLeft(p);
            }
            foreach (var p in newplayers)
            {
                if (!oldplayers.Contains(p)) PlayerJoined(p);
            }
        }

        private void PlayerJoined(CSteamID p)
        {
            if (p == OnlineManager.me) return;
            players.Add(new OnlinePlayer(p));
        }

        private void PlayerLeft(CSteamID p)
        {
            players.RemoveAll(op=>op.id == p);
        }
    }
}

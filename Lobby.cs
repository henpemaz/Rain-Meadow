using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;

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
    // Lobby is tightly bound to a SteamMatchmaking Lobby
    // Steam mediates lobby ownership
    // Should this be merged with OnlineSession from onlineresources?
    public class Lobby
    {
        public CSteamID id;
        public OnlinePlayer owner;
        public string name;
        public OnlineSession onlineSession;
        public List<OnlinePlayer> players;

        internal bool isOwner => owner.id == OnlineManager.me;

        public Lobby(CSteamID id)
        {
            this.id = id;
            owner = new OnlinePlayer(SteamMatchmaking.GetLobbyOwner(id));
            name = SteamMatchmaking.GetLobbyData(id, OnlineManager.NAME_KEY);

            players = new List<OnlinePlayer>() { OnlineManager.mePlayer};
            onlineSession = new OnlineSession(this, owner);
            if (isOwner)
            {
                SteamMatchmaking.SetLobbyData(id, OnlineManager.CLIENT_KEY, OnlineManager.CLIENT_VAL);
                SteamMatchmaking.SetLobbyData(id, OnlineManager.NAME_KEY, SteamFriends.GetPersonaName());
            }
            UpdatePlayers();
        }

        public void UpdatePlayers()
        {
            var n = SteamMatchmaking.GetNumLobbyMembers(OnlineManager.lobby.id);
            var oldplayers = players.Cast<CSteamID>();
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

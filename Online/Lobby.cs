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
        public OnlineGameSession session;
        //public Dictionary<Region, WorldSession> worldSessions;

        public Lobby(CSteamID id)
        {
            this.id = id;
            var ownerId = SteamMatchmaking.GetLobbyOwner(id);
            owner = OnlineManager.me == ownerId ? OnlineManager.mePlayer : new OnlinePlayer(ownerId);
            if (isOwner)
            {
                SteamMatchmaking.SetLobbyData(id, OnlineManager.CLIENT_KEY, OnlineManager.CLIENT_VAL);
                SteamMatchmaking.SetLobbyData(id, OnlineManager.NAME_KEY, SteamFriends.GetPersonaName());
            }

            players = new List<OnlinePlayer>() { OnlineManager.mePlayer };
            UpdatePlayersList();
            if (isOwner)
            {
                Activate();
            }
            else
            {
                Request(); // Everyone auto-subscribes this resource
            }
        }

        public override void ReadState(ResourceState newState, long ts)
        {
            throw new NotImplementedException();
        }

        public void UpdatePlayersList()
        {
            var n = SteamMatchmaking.GetNumLobbyMembers(id);
            var oldplayers = players.Select(p =>p.id).ToArray();
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

        protected override ResourceState MakeState(long ts)
        {
            return new LobbyState(ts);
        }

        internal override string Identifier()
        {
            return ".";
        }

        private void PlayerJoined(CSteamID p)
        {
            RainMeadow.Debug($"PlayerJoined:{p} - {SteamFriends.GetPlayerNickname(p)}");
            if (p == OnlineManager.me) return;
            players.Add(new OnlinePlayer(p));
        }

        private void PlayerLeft(CSteamID p)
        {
            RainMeadow.Debug($"PlayerLeft:{p} - {SteamFriends.GetPlayerNickname(p)}");
            players.RemoveAll(op=>op.id == p);
        }

        private class LobbyState : ResourceState
        {
            public LobbyState(long ts)
            {
                this.ts = ts;
            }
        }
    }
}

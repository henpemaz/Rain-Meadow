using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    // Lobby is tightly bound to a SteamMatchmaking Lobby, also the top-level resource
    public class Lobby : OnlineResource
    {
        public CSteamID id;
        public OnlineGameSession session;
        private Region[] loadedRegions;

        public Dictionary<string, WorldSession> worldSessions = new();

        public Lobby(CSteamID id)
        {
            this.id = id;
            PlayersManager.UpdatePlayersList(id);
            var ownerId = SteamMatchmaking.GetLobbyOwner(id); // Steam decides
            owner = OnlineManager.PlayerFromId(ownerId);
            if (isOwner)
            {
                SteamMatchmaking.SetLobbyData(id, OnlineManager.CLIENT_KEY, OnlineManager.CLIENT_VAL);
                SteamMatchmaking.SetLobbyData(id, OnlineManager.NAME_KEY, SteamFriends.GetPersonaName());
            }

            if (isOwner)
            {
                Activate();
            }
            else
            {
                Request(); // Everyone auto-subscribes this resource
            }
        }

        public override void Activate()
        {
            base.Activate();
            this.loadedRegions = Region.LoadAllRegions(RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer);
            foreach (var r in loadedRegions)
            {
                worldSessions[r.name] = new WorldSession(r, this);
            }
        }

        protected override ResourceState MakeState(long ts)
        {
            return new LobbyState(ts);
        }

        public override void ReadState(ResourceState newState, long ts)
        {
            throw new NotImplementedException();
        }

        internal override string Identifier()
        {
            return ".";
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

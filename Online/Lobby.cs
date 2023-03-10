using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using static RainMeadow.WorldSession;

namespace RainMeadow
{
    // Lobby is tightly bound to a SteamMatchmaking Lobby, also the top-level resource
    public class Lobby : OnlineResource
    {
        public CSteamID id;
        public OnlineGameSession session;
        private Region[] loadedRegions;

        public Dictionary<string, WorldSession> worldSessions = new();
        protected override World World => throw new NotSupportedException(); // Lobby can't add world entities

        public event Action OnLobbyAvailable;

        public Lobby(CSteamID id)
        {
            this.id = id;
            PlayersManager.UpdatePlayersList(id);
            var ownerId = SteamMatchmaking.GetLobbyOwner(id); // Steam decides
            NewOwner(OnlineManager.PlayerFromId(ownerId));
            if (owner == null) throw new Exception("Couldnt find lobby owner in player list");
            if (isOwner)
            {
                SteamMatchmaking.SetLobbyData(id, OnlineManager.CLIENT_KEY, OnlineManager.CLIENT_VAL);
                SteamMatchmaking.SetLobbyData(id, OnlineManager.NAME_KEY, SteamFriends.GetPersonaName());
            }

            if (isOwner)
            {
                Available();
            }
            else
            {
                Request(); // Everyone auto-subscribes this resource
            }
        }

        protected override void ActivateImpl()
        {
            this.loadedRegions = Region.LoadAllRegions(RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer);
            foreach (var r in loadedRegions)
            {
                var ws = new WorldSession(r, this);
                worldSessions.Add(r.name, ws);
                subresources.Add(ws);
            }
        }

        protected override void AvailableImpl()
        {
            base.AvailableImpl();
            OnLobbyAvailable?.Invoke();
            OnLobbyAvailable = null;
        }

        protected override void DeactivateImpl()
        {
            throw new InvalidOperationException("cant deactivate");
        }

        protected override ResourceState MakeState(ulong ts)
        {
            return new LobbyState(this, ts);
        }

        public override void ReadState(ResourceState newState, ulong ts)
        {
            base.ReadState(newState, ts);
            if (newState is LobbyState newLobbyState)
            {
                // no op
            }
            else
            {
                throw new InvalidCastException("not a LobbyState");
            }
        }

        internal override string Identifier()
        {
            return ".";
        }

        // State has the current lease state of worldsessions
        public class LobbyState : ResourceState
        {
            public LobbyState() : base() { }
            public LobbyState(Lobby lobby, ulong ts) : base(lobby, ts)
            {

            }

            public override StateType stateType => StateType.LobbyState;
        }
    }
}

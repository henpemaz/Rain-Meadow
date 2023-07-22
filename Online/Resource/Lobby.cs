using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using static RainMeadow.OnlineGameMode;

namespace RainMeadow
{
    // Lobby is tightly bound to a SteamMatchmaking Lobby, also the top-level resource
    public class Lobby : OnlineResource
    {
        public OnlineGameMode gameMode;
        public Dictionary<string, WorldSession> worldSessions = new();

        public override World World => throw new NotSupportedException(); // Lobby can't add world entities

        public event Action OnLobbyAvailable;

        public Lobby(OnlineGameModeType mode, OnlinePlayer owner)
        {
            this.super = this;

            this.gameMode = OnlineGameMode.FromType(mode, this);
            if(gameMode == null) throw new Exception($"Invalid game mode {mode}");

            if (owner == null) throw new Exception("No lobby owner");
            NewOwner(owner);

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
            foreach (var r in Region.LoadAllRegions(RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer))
            {
                var ws = new WorldSession(r, this);
                worldSessions.Add(r.name, ws);
                subresources.Add(ws);
            }
        }

        protected override void AvailableImpl()
        {
            OnLobbyAvailable?.Invoke();
            OnLobbyAvailable = null;

            Activate();
        }

        protected override void DeactivateImpl()
        {
            throw new InvalidOperationException("cant deactivate");
        }

        protected override ResourceState MakeState(ulong ts)
        {
            return new LobbyState(this, ts);
        }

        public override void ReadState(ResourceState newState)
        {
            base.ReadState(newState);
            if (newState is LobbyState newLobbyState)
            {
                // no op
            }
            else
            {
                throw new InvalidCastException("not a LobbyState");
            }
        }

        public override string Id()
        {
            return ".";
        }

        public override ushort ShortId()
        {
            throw new NotImplementedException();
        }

        public override OnlineResource SubresourceFromShortId(ushort shortId)
        {
            return this.subresources[shortId];
        }

        public class LobbyState : ResourceState
        {
            public LobbyState() : base() { }
            public LobbyState(Lobby lobby, ulong ts) : base(lobby, ts)
            {

            }

            public override StateType stateType => StateType.LobbyState;
        }

        public override string ToString() {
            return "Lobby";
        }

        internal OnlinePlayer PlayerFromId(ushort id)
        {
            return LobbyManager.players.First(p => p.inLobbyId == id);
        }
    }
}

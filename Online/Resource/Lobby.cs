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

        public event Action OnLobbyAvailable; // for menus

        public Lobby(OnlineGameModeType mode, OnlinePlayer owner)
        {
            this.super = this;

            this.gameMode = FromType(mode, this);
            if (gameMode == null) throw new Exception($"Invalid game mode {mode}");

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

        protected override ResourceState MakeState(uint ts)
        {
            return new LobbyState(this, ts);
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

        protected ushort nextId = 1;

        public class LobbyState : ResourceWithSubresourcesState
        {
            public ushort nextId;
            public Generics.AddRemoveSortedPlayerIDs players;
            public Generics.AddRemoveSortedUshorts inLobbyIds;
            public LobbyState() : base() { }
            public LobbyState(Lobby lobby, uint ts) : base(lobby, ts)
            {
                nextId = lobby.nextId;
                players = new(lobby.participants.Keys.Select(p => p.id).ToList());
                inLobbyIds = new(lobby.participants.Keys.Select(p => p.inLobbyId).ToList());
            }
            protected override ResourceState NewInstance() => new LobbyState();

            public override StateType stateType => StateType.LobbyState;

            public override OnlineState ApplyDelta(OnlineState _newState)
            {
                var newState = _newState as LobbyState;
                var result = (LobbyState)base.ApplyDelta(newState);
                result.nextId = newState?.nextId ?? nextId;
                result.players = (Generics.AddRemoveSortedPlayerIDs)players.ApplyDelta(newState.players);
                result.inLobbyIds = (Generics.AddRemoveSortedUshorts)inLobbyIds.ApplyDelta(newState.inLobbyIds);
                return result;
            }

            public override OnlineState Delta(OnlineState lastAcknoledgedState)
            {
                var delta = (LobbyState)base.Delta(lastAcknoledgedState);
                delta.nextId = nextId;
                delta.players = (Generics.AddRemoveSortedPlayerIDs)players.Delta((lastAcknoledgedState as LobbyState).players);
                delta.inLobbyIds = (Generics.AddRemoveSortedUshorts)inLobbyIds.Delta((lastAcknoledgedState as LobbyState).inLobbyIds);
                return delta;
            }

            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                serializer.Serialize(ref nextId);
                serializer.SerializeNullable(ref players);
                serializer.SerializeNullable(ref inLobbyIds);
            }

            public override void ReadTo(OnlineResource resource)
            {
                var lobby = (Lobby)resource;
                lobby.nextId = nextId;
                for (int i = 0; i < players.list.Count; i++)
                {
                    if (MatchmakingManager.instance.GetPlayer(players.list[i]) is OnlinePlayer p)
                    {
                        if (p.inLobbyId != inLobbyIds.list[i]) RainMeadow.Debug($"Setting player {p} to lobbyId {inLobbyIds.list[i]}");
                        p.inLobbyId = inLobbyIds.list[i];
                    }
                    else
                    {
                        RainMeadow.Error("Player not found! " + players.list[i]);
                    }
                }
                lobby.UpdateParticipants(players.list.Select(MatchmakingManager.instance.GetPlayer).ToList());
                base.ReadTo(resource);
            }
        }

        public override string ToString()
        {
            return "Lobby";
        }

        public OnlinePlayer PlayerFromId(ushort id)
        {
            if (id == 0) return null;
            return OnlineManager.players.First(p => p.inLobbyId == id);
        }

        protected override void NewParticipantImpl(OnlinePlayer player)
        {
            base.NewParticipantImpl(player);
            player.inLobbyId = nextId;
            RainMeadow.Debug($"Assigned inLobbyId of {nextId} to player {player}");
            nextId++;
            // todo overflows and repeats
        }
    }
}

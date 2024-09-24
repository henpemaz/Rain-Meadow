using RainMeadow.Generics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    // Lobby is tightly bound to a SteamMatchmaking Lobby, also the top-level resource
    public class Lobby : OnlineResource
    {
        public OnlineGameMode gameMode;
        public OnlineGameMode.OnlineGameModeType gameModeType;
        public Dictionary<string, WorldSession> worldSessions = new();
        public Dictionary<OnlinePlayer, ClientSettings> clientSettings = new();
        public Dictionary<OnlinePlayer, OnlineEntity.EntityId> playerAvatars = new(); // should maybe be in GameMode

        public string[] mods = RainMeadowModManager.GetActiveMods();
        public List<string> bannedUsers = new List<string>();
        
        public bool modsChecked;

        public string? password;
        public bool hasPassword => password != null;
        public Lobby(OnlineGameMode.OnlineGameModeType mode, OnlinePlayer owner, string? password) : base(null)
        {
            OnlineManager.lobby = this; // needed for early entity processing

            this.gameMode = OnlineGameMode.FromType(mode, this);
            this.gameModeType = mode;
            if (gameMode == null) throw new Exception($"Invalid game mode {mode}");

            if (owner == null) throw new Exception("No lobby owner");
            isNeeded = true;
            NewOwner(owner);
            if (isOwner)
            {
                this.password = password;
            }
            else
            {
                RequestLobby(password);
            }
        }

        public void RequestLobby(string? key)
        {
            RainMeadow.Debug(this);
            if (isPending) throw new InvalidOperationException("pending");
            if (isAvailable) throw new InvalidOperationException("available");
            ClearIncommingBuffers();
            isRequesting = true;
            supervisor.InvokeRPC(RequestedLobby, key).Then(ResolveLobbyRequest);
        }

        [RPCMethod]
        public void RequestedLobby(RPCEvent request, string? key)
        {
            if (this.hasPassword)
            {
                if (this.password != key)
                {
                    request.from.QueueEvent(new GenericResult.Fail(request));
                    return;
                }
            }
            Requested(request);
        }

        public void ResolveLobbyRequest(GenericResult requestResult)
        {
            RainMeadow.Debug(this);
            isRequesting = false;
            if (requestResult is GenericResult.Ok)
            {
                MatchmakingManager.instance.JoinLobby(true);
                if (!isAvailable) // this was transfered to me because the previous owner left
                {
                    WaitingForState();
                    if (isOwner)
                    {
                        Available();
                    }
                }
            }
            else if (requestResult is GenericResult.Fail) // I didn't have the right key for this resource
            {
                RainMeadow.Error("locked request for " + this);
                MatchmakingManager.instance.JoinLobby(false);
            }
            else if (requestResult is GenericResult.Error) // I should retry
            {
                RequestLobby((requestResult.referencedEvent as RPCEvent).args[0] as string);
                RainMeadow.Error("request failed for " + this);
            }
        }

        internal override void Tick(uint tick)
        {
            clientSettings = activeEntities.Where(e => e is ClientSettings).ToDictionary(e => e.owner, e => e as ClientSettings);
            playerAvatars = clientSettings.ToDictionary(e => e.Key, e => e.Value.avatarId);
            gameMode.LobbyTick(tick);
            base.Tick(tick);
        }

        protected override void ActivateImpl()
        {
            if (RainMeadow.isArenaMode(out var _)) // Arena
            {
                Region arenaRegion = new Region("arena", 0, 0, RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer);

                var ws = new WorldSession(arenaRegion, this);
                worldSessions.Add(arenaRegion.name, ws);
                subresources.Add(ws);

                RainMeadow.Debug(subresources.Count);
            }
            else
            {
                foreach (var r in Region.LoadAllRegions(RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer))
                {
                    RainMeadow.Debug(r.name);
                    var ws = new WorldSession(r, this);
                    worldSessions.Add(r.name, ws);
                    subresources.Add(ws);
                }
                RainMeadow.Debug(subresources.Count);
            }
        }

        protected override void AvailableImpl()
        {

        }

        protected override void DeactivateImpl()
        {
            throw new InvalidOperationException("cant deactivate");
        }

        protected override void UnavailableImpl()
        {

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
            throw new NotImplementedException(); // Lobby cannot be a subresource
        }

        public override OnlineResource SubresourceFromShortId(ushort shortId)
        {
            return this.subresources[shortId];
        }

        protected ushort nextId = 1;

        public class LobbyState : ResourceWithSubresourcesState
        {
            [OnlineField]
            public ushort nextId;
            [OnlineField]
            public string[] mods;
            [OnlineField]
            public List<string> bannedUsers;
            [OnlineField(nullable = true)]
            public Generics.DynamicOrderedPlayerIDs players;
            [OnlineField(nullable = true)]
            public Generics.DynamicOrderedUshorts inLobbyIds;
            public LobbyState() : base() { }
            public LobbyState(Lobby lobby, uint ts) : base(lobby, ts)
            {
                nextId = lobby.nextId;
                players = new(lobby.participants.Select(p => p.id).ToList());
                inLobbyIds = new(lobby.participants.Select(p => p.inLobbyId).ToList());
                mods = lobby.mods;
                bannedUsers = lobby.bannedUsers;

            }

            public override void ReadTo(OnlineResource resource)
            {
                var lobby = (Lobby)resource;
                lobby.nextId = nextId;

                for (int i = 0; i < players.list.Count; i++)
                {
                    if (bannedUsers.Contains(players.list[i].name)) {

                        OnlineManager.LeaveLobby();
                        BanHammer.ShowBan((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame).manager);
                        return;

                    }

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
                lobby.UpdateParticipants(players.list.Select(MatchmakingManager.instance.GetPlayer).Where(p => p != null).ToList());

                if (!lobby.modsChecked)
                {
                    RainMeadowModManager.CheckMods(this.mods, lobby.mods);
                    lobby.modsChecked = true;
                }

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
            return OnlineManager.players.FirstOrDefault(p => p.inLobbyId == id);
        }

        protected override void NewParticipantImpl(OnlinePlayer player)
        {
            if (isOwner)
            {
                player.inLobbyId = nextId;
                RainMeadow.Debug($"Assigned inLobbyId of {nextId} to player {player}");
                nextId++;
                // todo overflows and repeats (unrealistic but it's a ushort)
            }
            base.NewParticipantImpl(player);
            gameMode.NewPlayerInLobby(player);
        }

        protected override void ParticipantLeftImpl(OnlinePlayer player)
        {
            base.ParticipantLeftImpl(player);
            gameMode.PlayerLeftLobby(player);
        }
    }
}

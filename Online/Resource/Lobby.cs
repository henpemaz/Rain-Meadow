using RainMeadow.Generics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMeadow
{
    // Lobby is tightly bound to a SteamMatchmaking Lobby, also the top-level resource
    public class Lobby : OnlineResource
    {
        public OnlineGameMode gameMode;
        public OnlineGameMode.OnlineGameModeType gameModeType;
        public Dictionary<string, WorldSession> worldSessions = new();
        public Dictionary<OnlinePlayer, ClientSettings> clientSettings = new();
        public Dictionary<Type, List<(byte, byte)>> enumMap = new();
        public List<KeyValuePair<OnlinePlayer, OnlineEntity.EntityId>> playerAvatars = new(); // guess we can support multiple avatars per client

        public string[] requiredmods;
        public string[] bannedmods;
        public DynamicOrderedPlayerIDs bannedUsers = new();

        public bool modsChecked;
        public bool bannedUsersChecked = false;

        public Dictionary<string, bool> configurableBools;
        public Dictionary<string, float> configurableFloats;
        public Dictionary<string, int> configurableInts;

        public string? password;
        public bool hasPassword => password != null;

        public Lobby(OnlineGameMode.OnlineGameModeType mode, OnlinePlayer owner, string? password) : base(null)
        {
            OnlineManager.lobby = this; // needed for early entity processing
            bannedUsers.list = new List<MeadowPlayerId>();

            RainMeadowModInfoManager.RefreshDebugModInfo();

            requiredmods = RainMeadowModManager.GetRequiredMods();
            bannedmods = RainMeadowModManager.GetBannedMods();

            this.gameMode = OnlineGameMode.FromType(mode, this);
            this.gameModeType = mode;
            if (gameMode == null) throw new Exception($"Invalid game mode {mode}");

            if (owner == null) throw new Exception("No lobby owner");
            isNeeded = true;
            NewOwner(owner);

            configurableBools = new Dictionary<string, bool>();
            configurableFloats = new Dictionary<string, float>();
            configurableInts = new Dictionary<string, int>();

            if (isOwner)
            {
                this.password = password;
                (configurableBools, configurableFloats, configurableInts) = OnlineGameMode.GetHostRemixSettings(this.gameMode);
                BuildEnumMap();
            }
            else
            {
                RainMeadow.Debug("Requesting lobby");
                RequestLobby(password);
            }
        }

        private OutgoingDataChunk enumMapChunkTemplate;
        public void BuildEnumMap()
        {
            enumMap = new();
            foreach (Type key in Serializer.serializedExtEnums)
            {
                
                // TODO: some ExtEnums don't effect gameplay exp: MatchmakingManager.MatchMakingDomain. 
                // we need to make a list of those to ignore.
                if (ExtEnumBase.valueDictionary[key].entries.Count > byte.MaxValue) throw new InvalidOperationException("too many ext enums");
                for (byte i = 0; i < ExtEnumBase.valueDictionary[key].Count; i++)
                {
                    enumMap[key].Add((i, i));
                }
            }

            checked
            {
                using (MemoryStream stream = new())
                using (BinaryWriter writer = new(stream))
                {
                    writer.Write(Encoding.ASCII.GetBytes("ENUM"));
                    writer.Write((byte)enumMap.Keys.Count);
                    foreach (Type key in enumMap.Keys)
                    {
                        var typename = Encoding.UTF8.GetBytes(key.AssemblyQualifiedName);
                        writer.Write((byte)typename.Length);
                        writer.Write(typename);
                        writer.Write((byte)enumMap[key].Count);
                        foreach (byte enumkey in enumMap[key].Select(x => x.Item1))
                        {
                            var entry = Encoding.UTF8.GetBytes(ExtEnumBase.GetExtEnumType(key).GetEntry(enumkey));
                            writer.Write((byte)entry.Length);
                            writer.Write(entry);
                        }
                    }
                    enumMapChunkTemplate = new(1, this, new ArraySegment<byte>((byte[])stream.GetBuffer().Clone(), 0, (int)stream.Position), null, 4096);
                }
            }
        }

        public override void ProcessEntireChunkImpl(IncomingDataChunk chunk)
        {
            checked
            {
                using (MemoryStream stream = new(chunk.GetData()))
                using (BinaryReader reader = new(stream))
                {
                    if (Encoding.ASCII.GetString(reader.ReadBytes(4)) == "ENUM")
                    {
                        Dictionary<Type, List<(byte, byte)>> newEnumMap = new();
                        byte typeCount = reader.ReadByte();
                        for (byte i = 0; i < typeCount; i++)
                        {
                            string typeName = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadByte()));
                            Type t = null;
                            try
                            {
                                t = Type.GetType(typeName);
                            }
                            catch (Exception except)
                            {
                                RainMeadow.Error(except);
                            }

                            if (t is null || !ExtEnumBase.valueDictionary.Keys.Contains(t))
                            {
                                // missing enum type error.
                                RainMeadow.Error($"Missing enum type {typeName}");
                                MatchmakingManager.currentInstance.JoinLobby(false);
                                return;
                            }
                            byte entryCount = reader.ReadByte();
                            RainMeadow.Debug($"{t.FullName}: {entryCount}:{ExtEnumBase.GetExtEnumType(t).Count}");

                            for (byte j = 0; j < entryCount; j++)
                            {
                                string entry = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadByte()));
                                if (!ExtEnumBase.TryParse(t, entry, false, out var result))
                                {
                                    // missing entry error.
                                    RainMeadow.Error($"Missing enum type entry: {entry}");
                                    MatchmakingManager.currentInstance.JoinLobby(false);
                                    return;
                                }

                                newEnumMap[t].Add((j, (byte)result.Index));
                            }
                        }
                        enumMap = newEnumMap;
                        enumMapChunkTemplate = new(1, this, new ArraySegment<byte>(chunk.GetData()), null, 4096);
                    }
                }
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

            request.from.QueueChunk(new OutgoingDataChunk(enumMapChunkTemplate, request.from, request.from.NextChunkId())).Then(_ => Requested(request));
        }

        public void ResolveLobbyRequest(GenericResult requestResult)
        {
            RainMeadow.Debug(this);
            isRequesting = false;
            if (requestResult is GenericResult.Ok)
            {
                MatchmakingManager.currentInstance.JoinLobby(true);
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
                MatchmakingManager.currentInstance.JoinLobby(false);
            }
            else if (requestResult is GenericResult.Error) // I should retry
            {
                RequestLobby((requestResult.referencedEvent as RPCEvent).args[0] as string);
                RainMeadow.Error("request failed for " + this);
            }
        }

        public override void Tick(uint tick)
        {
            clientSettings = activeEntities.Where(e => e is ClientSettings).ToDictionary(e => e.owner, e => e as ClientSettings);
            playerAvatars = clientSettings.SelectMany(e => e.Value.avatars.Select(a => new KeyValuePair<OnlinePlayer, OnlineEntity.EntityId>(e.Key, a))).ToList();
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
            public string[] requiredmods;
            [OnlineField]
            public string[] bannedmods;
            [OnlineField(nullable = true)]
            public Generics.DynamicOrderedPlayerIDs bannedUsers;
            [OnlineField(nullable = true)]
            public Generics.DynamicOrderedPlayerIDs players;
            [OnlineField(nullable = true)]
            public Generics.DynamicOrderedUshorts inLobbyIds;
            [OnlineField]
            public Dictionary<string, bool> onlineBoolRemixSettings;
            [OnlineField]
            public Dictionary<string, float> onlineFloatRemixSettings;
            [OnlineField]
            public Dictionary<string, int> onlineIntRemixSettings;
            public LobbyState() : base() { }
            public LobbyState(Lobby lobby, uint ts) : base(lobby, ts)
            {
                nextId = lobby.nextId;
                players = new(lobby.participants.Select(p => p.id).ToList());
                inLobbyIds = new(lobby.participants.Select(p => p.inLobbyId).ToList());
                requiredmods = lobby.requiredmods;
                bannedmods = lobby.bannedmods;
                bannedUsers = lobby.bannedUsers;
                onlineBoolRemixSettings = lobby.configurableBools;
                onlineFloatRemixSettings = lobby.configurableFloats;
                onlineIntRemixSettings = lobby.configurableInts;
            }

            public override void ReadTo(OnlineResource resource)
            {
                var lobby = (Lobby)resource;
                lobby.nextId = nextId;

                for (int i = 0; i < players.list.Count; i++)
                {

                    if (MatchmakingManager.currentInstance.GetPlayer(players.list[i]) is OnlinePlayer p)
                    {
                        if (p.inLobbyId != inLobbyIds.list[i]) RainMeadow.Debug($"Setting player {p} to lobbyId {inLobbyIds.list[i]}");
                        p.inLobbyId = inLobbyIds.list[i];
                    }
                    else
                    {
                        RainMeadow.Error("Player not found! " + players.list[i]);
                    }


                }
                lobby.UpdateParticipants(players.list.Select(MatchmakingManager.currentInstance.GetPlayer).Where(p => p is not null).ToList());
                if (lobby.bannedUsersChecked == false)
                {
                    // Need to get the participants before we check
                    if (this.bannedUsers != null && this.bannedUsers.list.Contains(OnlineManager.mePlayer.id))
                    {

                        BanHammer.BanUser(OnlineManager.mePlayer);
                        if (lobby.participants.Contains(OnlineManager.mePlayer))
                        {
                            lobby.OnPlayerDisconnect(lobby.PlayerFromMeadowID(OnlineManager.mePlayer.id));
                        }
                        lobby.bannedUsersChecked = true;
                        return;

                    }

                    lobby.bannedUsersChecked = true;
                }

                if (!lobby.modsChecked)
                {
                    //Made asyncronous so that the game doesn't get totally frozen
                    Task.Run(() => RainMeadowModManager.CheckMods(requiredmods, bannedmods, null, true));

                    lobby.requiredmods = requiredmods;
                    lobby.bannedmods = bannedmods;
                    if (ModManager.MMF && lobby.gameMode.nonGameplayRemixSettings != null)
                    {
                        OnlineGameMode.SetClientRemixSettings(onlineBoolRemixSettings, onlineFloatRemixSettings, onlineIntRemixSettings);
                    }
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

        public OnlinePlayer PlayerFromMeadowID(MeadowPlayerId id)
        {
            return OnlineManager.players.FirstOrDefault(p => p.id == id);
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

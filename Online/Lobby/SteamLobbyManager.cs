using Steamworks;
using System;
using System.Linq;

namespace RainMeadow
{
    public class SteamLobbyManager : LobbyManager
    {
        public class SteamPlayerId : MeadowPlayerId
        {
            public CSteamID steamID;
            public SteamNetworkingIdentity oid;

            public SteamPlayerId() { }
            public SteamPlayerId(CSteamID steamID) : base(SteamFriends.GetFriendPersonaName(steamID) ?? string.Empty)
            {
                this.steamID = steamID;
                oid = new SteamNetworkingIdentity();
                oid.SetSteamID(steamID);
            }

            public override void CustomSerialize(Serializer serializer)
            {
                serializer.Serialize(ref steamID.m_SteamID);
            }

            public override bool Equals(MeadowPlayerId other)
            {
                return other is SteamPlayerId otherS && steamID == otherS.steamID;
            }

            public override int GetHashCode()
            {
                return steamID.GetHashCode();
            }
        }

        public override MeadowPlayerId GetEmptyId()
        {
            return new SteamPlayerId();
        }

#pragma warning disable IDE0052 // Remove unread private members
        private CallResult<LobbyMatchList_t> m_RequestLobbyListCall;
        private CallResult<LobbyCreated_t> m_CreateLobbyCall;
        private CallResult<LobbyEnter_t> m_JoinLobbyCall;
        private Callback<LobbyDataUpdate_t> m_LobbyDataUpdate;
        private Callback<LobbyChatUpdate_t> m_LobbyChatUpdate;
        private Callback<SteamNetworkingMessagesSessionRequest_t> m_SessionRequest;
#pragma warning restore IDE0052 // Remove unread private members

        private CSteamID me;
        private CSteamID lobbyID;

        public SteamLobbyManager()
        {
            RainMeadow.DebugMe();
            m_RequestLobbyListCall = CallResult<LobbyMatchList_t>.Create(LobbyListReceived);
            m_CreateLobbyCall = CallResult<LobbyCreated_t>.Create(LobbyCreated);
            m_JoinLobbyCall = CallResult<LobbyEnter_t>.Create(LobbyJoined);
            m_LobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(LobbyUpdated);
            m_LobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(LobbyChatUpdated);
            m_SessionRequest = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(SessionRequest);

            me = SteamUser.GetSteamID();
            mePlayer = new OnlinePlayer(new SteamPlayerId(me)) { isMe = true };
        }

        public override event LobbyListReceived_t OnLobbyListReceived;
        public override event LobbyJoined_t OnLobbyJoined;

        public override void RequestLobbyList()
        {
            SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
            SteamMatchmaking.AddRequestLobbyListStringFilter(CLIENT_KEY, CLIENT_VAL, ELobbyComparison.k_ELobbyComparisonEqual);
            m_RequestLobbyListCall.Set(SteamMatchmaking.RequestLobbyList());
        }

        private void LobbyListReceived(LobbyMatchList_t pCallback, bool bIOFailure)
        {
            try
            {
                RainMeadow.DebugMe();
                LobbyInfo[] lobbies = new LobbyInfo[pCallback.m_nLobbiesMatching];
                if (!bIOFailure)
                {
                    for (int i = 0; i < pCallback.m_nLobbiesMatching; i++)
                    {
                        CSteamID id = SteamMatchmaking.GetLobbyByIndex(i);
                        lobbies[i] = new LobbyInfo(id, SteamMatchmaking.GetLobbyData(id, NAME_KEY), SteamMatchmaking.GetLobbyData(id, MODE_KEY));
                    }
                }

                OnLobbyListReceived?.Invoke(!bIOFailure, lobbies);
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        public override void CreateLobby(LobbyVisibility visibility, string gameMode)
        {
            creatingWithMode = gameMode;
            ELobbyType eLobbyTypeeLobbyType = visibility switch
            {
                LobbyVisibility.Private => ELobbyType.k_ELobbyTypePrivate,
                LobbyVisibility.Public => ELobbyType.k_ELobbyTypePublic,
                LobbyVisibility.FriendsOnly => ELobbyType.k_ELobbyTypeFriendsOnly,
                _ => throw new ArgumentException()
            };
            m_CreateLobbyCall.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 16));
        }

        public override void JoinLobby(LobbyInfo lobby)
        {
            m_JoinLobbyCall.Set(SteamMatchmaking.JoinLobby(lobby.id));
        }

        private static string creatingWithMode;
        private void LobbyCreated(LobbyCreated_t param, bool bIOFailure)
        {
            try
            {
                RainMeadow.DebugMe();
                if (!bIOFailure && param.m_eResult == EResult.k_EResultOK)
                {
                    RainMeadow.Debug("success");
                    lobbyID = new CSteamID(param.m_ulSteamIDLobby);
                    SteamMatchmaking.SetLobbyData(lobbyID, CLIENT_KEY, CLIENT_VAL);
                    SteamMatchmaking.SetLobbyData(lobbyID, NAME_KEY, SteamFriends.GetPersonaName() + "'s Lobby");
                    SteamMatchmaking.SetLobbyData(lobbyID, MODE_KEY, creatingWithMode);
                    lobby = new Lobby(new OnlineGameMode.OnlineGameModeType(creatingWithMode), mePlayer);
                    OnLobbyJoined?.Invoke(true);
                }
                else
                {
                    RainMeadow.Debug("failure, error code is " + param.m_eResult);
                    lobby = null;
                    lobbyID = default;
                    OnLobbyJoined?.Invoke(false);
                }
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        private void LobbyJoined(LobbyEnter_t param, bool bIOFailure)
        {
            try
            {
                if (!bIOFailure)
                {
                    RainMeadow.Debug("success");
                    lobbyID = new CSteamID(param.m_ulSteamIDLobby);
                    UpdatePlayersList();
                    var mode = new OnlineGameMode.OnlineGameModeType(SteamMatchmaking.GetLobbyData(lobbyID, MODE_KEY));
                    var owner = GetLobbyOwner();
                    if (owner == mePlayer)
                    {
                        SteamMatchmaking.SetLobbyData(lobbyID, CLIENT_KEY, CLIENT_VAL);
                        SteamMatchmaking.SetLobbyData(lobbyID, NAME_KEY, SteamFriends.GetPersonaName() + "'s Lobby");
                    }
                    lobby = new Lobby(mode, owner);
                    OnLobbyJoined?.Invoke(true);
                }
                else
                {
                    RainMeadow.Debug("failure");
                    lobby = null;
                    OnLobbyJoined?.Invoke(false);
                }
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        public override void UpdatePlayersList()
        {
            try
            {
                RainMeadow.DebugMe();
                var oldplayers = players.Select(p => (p.id as SteamPlayerId).steamID).ToArray();
                var n = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
                var newplayers = new CSteamID[n];
                for (int i = 0; i < n; i++)
                {
                    newplayers[i] = SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i);
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
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        private void PlayerJoined(CSteamID p)
        {
            RainMeadow.Debug($"PlayerJoined:{p} - {SteamFriends.GetFriendPersonaName(p)}");
            if (p == me) return;
            SteamFriends.RequestUserInformation(p, true);
            players.Add(new OnlinePlayer(new SteamPlayerId(p)));
        }

        private void PlayerLeft(CSteamID p)
        {
            RainMeadow.Debug($"{p} - {SteamFriends.GetFriendPersonaName(p)}");

            if (players.FirstOrDefault(op => (op.id as SteamPlayerId).steamID == p) is OnlinePlayer player)
            {
                RainMeadow.Debug($"Handling player disconnect:{player}");
                player.hasLeft = true;
                lobby?.OnPlayerDisconnect(player);
                while (player.HasUnacknoledgedEvents())
                {
                    player.AbortUnacknoledgedEvents();
                    lobby?.OnPlayerDisconnect(player);
                }
                RainMeadow.Debug($"Actually removing player:{player}");
                players.Remove(player);
            }
        }

        private void LobbyUpdated(LobbyDataUpdate_t param)
        {
            try
            {
                RainMeadow.Debug($"{param.m_ulSteamIDLobby} : {param.m_ulSteamIDMember} : {param.m_bSuccess}");
                if (lobby == null)
                {
                    RainMeadow.Error("got lobby event with no lobby!");
                    return;
                }
                if ((CSteamID)param.m_ulSteamIDLobby != lobbyID)
                {
                    RainMeadow.Error("got lobby event for wrong lobby!");
                    return;
                }
                if (param.m_bSuccess > 0)
                {
                    if (lobby != null && lobbyID == new CSteamID(param.m_ulSteamIDLobby))
                    {
                        // lobby event, check for possible changes
                        UpdatePlayersList();
                    }
                }
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        private void LobbyChatUpdated(LobbyChatUpdate_t param)
        {
            try
            {
                RainMeadow.Debug($"{param.m_ulSteamIDLobby} : {param.m_ulSteamIDUserChanged} : {param.m_ulSteamIDMakingChange} : {param.m_rgfChatMemberStateChange}");
                if (lobby == null)
                {
                    RainMeadow.Error("got lobby event with no lobby!");
                    return;
                }

                if ((CSteamID)param.m_ulSteamIDLobby != lobbyID)
                {
                    RainMeadow.Error("got lobby event for wrong lobby!");
                    return;
                }

                UpdatePlayersList();
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        private void SessionRequest(SteamNetworkingMessagesSessionRequest_t param)
        {
            try
            {
                var id = new CSteamID(param.m_identityRemote.GetSteamID64());
                RainMeadow.Debug("session request from " + id);
                if (lobby != null)
                {
                    if (players.FirstOrDefault(op => (op.id as SteamPlayerId).steamID == id) is OnlinePlayer p)
                    {
                        RainMeadow.Debug("accepted session from " + p.id.name);
                        SteamNetworkingMessages.AcceptSessionWithUser(ref param.m_identityRemote);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        public override void LeaveLobby()
        {
            RainMeadow.DebugMe();
            if (lobby != null)
            {
                SteamMatchmaking.LeaveLobby(lobbyID);
                OnlineManager.Reset();
            }
        }

        public override OnlinePlayer GetPlayer(MeadowPlayerId id)
        {
            return players.FirstOrDefault(p => (p.id as SteamPlayerId).steamID == (id as SteamPlayerId).steamID);
        }

        public override OnlinePlayer GetLobbyOwner()
        {
            return GetPlayer(new SteamPlayerId(SteamMatchmaking.GetLobbyOwner(lobbyID)));
        }

        internal OnlinePlayer GetPlayerSteam(ulong steamID)
        {
            return players.FirstOrDefault(p => (p.id as SteamPlayerId).steamID.m_SteamID == steamID);
        }
    }
}
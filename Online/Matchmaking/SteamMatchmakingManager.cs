using RWCustom;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public class SteamLobbyInfo : LobbyInfo {
        public CSteamID iD;
        public SteamLobbyInfo(CSteamID id, string name, string mode, int playerCount, bool hasPassword, int? maxPlayerCount) : 
            base(name, mode, playerCount, hasPassword, maxPlayerCount) {
            iD = id;
        }
    }

    public class SteamMatchmakingManager : MatchmakingManager
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

            public override string GetPersonaName() {
                return SteamFriends.GetFriendPersonaName(steamID);
            }

            public override bool canOpenProfileLink { get => true; }
            public override void OpenProfileLink() {
                string url = $"https://steamcommunity.com/profiles/{steamID}";
                SteamFriends.ActivateGameOverlayToWebPage(url);
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
        private Callback<GameLobbyJoinRequested_t> m_GameLobbyJoinRequested;
        private Callback<LobbyChatMsg_t> m_LobbyChatMsgCall;
#pragma warning restore IDE0052 // Remove unread private members

        private CSteamID me;
        public CSteamID lobbyID { get; private set; }

        public SteamMatchmakingManager()
        {
            SteamNetworkingUtils.InitRelayNetworkAccess();
            RainMeadow.DebugMe();
            m_RequestLobbyListCall = CallResult<LobbyMatchList_t>.Create(LobbyListReceived);
            m_CreateLobbyCall = CallResult<LobbyCreated_t>.Create(LobbyCreated);
            m_JoinLobbyCall = CallResult<LobbyEnter_t>.Create(LobbyConnected);
            m_LobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(LobbyUpdated);
            m_LobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(LobbyChatUpdated);
            m_SessionRequest = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(SessionRequest);
            m_GameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(GameLobbyJoinRequested);
            m_LobbyChatMsgCall = Callback<LobbyChatMsg_t>.Create(LobbyChatMessageReceived);

            me = SteamUser.GetSteamID();
        }

        public override void initializeMePlayer()
        {
            RainMeadow.DebugMe();
            OnlineManager.mePlayer = new OnlinePlayer(new SteamPlayerId(me)) { isMe = true };
        }
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
                        string? passwordKeyStr = SteamMatchmaking.GetLobbyData(id, PASSWORD_KEY);
                        lobbies[i] = new SteamLobbyInfo(id, Utils.GetTranslatedLobbyName(SteamMatchmaking.GetLobbyData(id, USERNAME_KEY)), SteamMatchmaking.GetLobbyData(id, MODE_KEY), SteamMatchmaking.GetNumLobbyMembers(id), passwordKeyStr != null ? bool.Parse(passwordKeyStr) : false, SteamMatchmaking.GetLobbyMemberLimit(id));
                    }
                }

                OnLobbyListReceivedEvent(!bIOFailure, lobbies);
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        public override void CreateLobby(LobbyVisibility visibility, string gameMode, string? password, int? maxPlayerCount)
        {
            creatingWithMode = gameMode;
            lobbyPassword = password;
            MAX_LOBBY = (int)maxPlayerCount;
            ELobbyType eLobbyTypeeLobbyType = visibility switch
            {
                LobbyVisibility.Private => ELobbyType.k_ELobbyTypePrivate,
                LobbyVisibility.Public => ELobbyType.k_ELobbyTypePublic,
                LobbyVisibility.FriendsOnly => ELobbyType.k_ELobbyTypeFriendsOnly,
                _ => throw new ArgumentException()
            };
            m_CreateLobbyCall.Set(SteamMatchmaking.CreateLobby(eLobbyTypeeLobbyType, 16));
        }

        public override void RequestJoinLobby(LobbyInfo lobby, string? password)
        {
            lobbyPassword = password;
            m_JoinLobbyCall.Set(SteamMatchmaking.JoinLobby((lobby as SteamLobbyInfo).iD));
        }

        public override void JoinLobby(bool success)
        {
            if (success)
            {
                OnLobbyJoinedEvent(true);
            }
            else
            {
                LeaveLobby();
                RainMeadow.Debug("Failed to join local game. Wrong Password");
                OnLobbyJoinedEvent(false, Utils.Translate("Wrong password!"));
            }
        }

        private static string creatingWithMode;
        private static string? lobbyPassword;
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
                    SteamMatchmaking.SetLobbyData(lobbyID, USERNAME_KEY, SteamFriends.GetPersonaName());
                    SteamMatchmaking.SetLobbyData(lobbyID, MODE_KEY, creatingWithMode);
                    SteamMatchmaking.SetLobbyData(lobbyID, PASSWORD_KEY, lobbyPassword != null ? "true" : "false");
                    SteamMatchmaking.SetLobbyMemberLimit(lobbyID, MAX_LOBBY);
                    OnlineManager.lobby = new Lobby(new OnlineGameMode.OnlineGameModeType(creatingWithMode), OnlineManager.mePlayer, lobbyPassword);
                    SteamFriends.SetRichPresence("connect", lobbyID.ToString());
                    OnLobbyJoinedEvent(true);
                }
                else
                {
                    RainMeadow.Debug("failure, error code is " + param.m_eResult);
                    OnlineManager.lobby = null;
                    lobbyID = default;
                    OnLobbyJoinedEvent(false, param.m_eResult.ToString());
                }
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        private void LobbyConnected(LobbyEnter_t param, bool bIOFailure)
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
                    if (owner == OnlineManager.mePlayer)
                    {
                        SteamMatchmaking.SetLobbyData(lobbyID, CLIENT_KEY, CLIENT_VAL);
                        SteamMatchmaking.SetLobbyData(lobbyID, USERNAME_KEY, SteamFriends.GetPersonaName());
                    }
                    SteamFriends.SetRichPresence("connect", lobbyID.ToString());

                    OnlineManager.lobby = new Lobby(mode, owner, lobbyPassword);
                }
                else
                {
                    RainMeadow.Debug("failure");
                    OnlineManager.lobby = null;
                    OnLobbyJoinedEvent(false, ((EChatRoomEnterResponse)param.m_EChatRoomEnterResponse).ToString());
                }
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        public override List<PlayerInfo> playerList => OnlineManager.players.Select(player => new PlayerInfo(() => player.id.OpenProfileLink(), player.id.name)).ToList();

        public void UpdatePlayersList()
        {
            try
            {
                RainMeadow.DebugMe();
                var oldplayers = OnlineManager.players.Select(p => (p.id as SteamPlayerId).steamID).ToArray();
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
                // OnlineManager.players will have been updated to match newplayers
                foreach (CSteamID player in newplayers)
                {
                    playerList.Add(new PlayerInfo(() => SteamFriends.ActivateGameOverlayToWebPage($"https://steamcommunity.com/profiles/{player}"), SteamFriends.GetFriendPersonaName(player)));
                }
                OnPlayerListReceivedEvent(playerList.ToArray());
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }
        public override bool canSendChatMessages => true;
        public override void SendChatMessage(string message) {
            byte[] msgBytes = System.Text.Encoding.UTF8.GetBytes(message);
            bool outputted = SteamMatchmaking.SendLobbyChatMsg(lobbyID, msgBytes, msgBytes.Length);

            if (!outputted) RainMeadow.Debug($"Failed to send message: {msgBytes} {msgBytes.Length}");
        }
        
        private void LobbyChatMessageReceived(LobbyChatMsg_t callback)
        {
            CSteamID senderID;
            byte[] msgData = new byte[1024];
            int msgDataLength = SteamMatchmaking.GetLobbyChatEntry((CSteamID)callback.m_ulSteamIDLobby, (int)callback.m_iChatID, out senderID, msgData, msgData.Length, out EChatEntryType _);

            string message = System.Text.Encoding.UTF8.GetString(msgData, 0, msgDataLength);
            RainMeadow.Debug($"Message from {SteamFriends.GetFriendPersonaName(senderID)}: {message}");
            RecieveChatMessage(GetPlayerSteam(senderID.m_SteamID), message);
        }

        private void PlayerJoined(CSteamID p)
        {
            RainMeadow.Debug($"PlayerJoined:{p} - {SteamFriends.GetFriendPersonaName(p)}");
            if (p == me) return;
            SteamFriends.RequestUserInformation(p, true);
            var player = new OnlinePlayer(new SteamPlayerId(p));
            OnlineManager.players.Add(player);

            HandleJoin(player);
        }

        private void PlayerLeft(CSteamID p)
        {
            RainMeadow.Debug($"{p} - {SteamFriends.GetFriendPersonaName(p)}");

            if (OnlineManager.players.FirstOrDefault(op => (op.id as SteamPlayerId).steamID == p) is OnlinePlayer player)
            {
                HandleDisconnect(player);
            }
        }

        private void LobbyUpdated(LobbyDataUpdate_t param)
        {
            try
            {
                RainMeadow.Debug($"{param.m_ulSteamIDLobby} : {param.m_ulSteamIDMember} : {param.m_bSuccess}");
                if (OnlineManager.lobby == null)
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
                    if (OnlineManager.lobby != null && lobbyID == new CSteamID(param.m_ulSteamIDLobby))
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
                if (OnlineManager.lobby == null)
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
                if (OnlineManager.lobby != null)
                {
                    if (OnlineManager.players.FirstOrDefault(op => (op.id as SteamPlayerId).steamID == id) is OnlinePlayer p)
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

        private void GameLobbyJoinRequested(GameLobbyJoinRequested_t param)
        {
            if (MatchmakingManager.currentDomain != MatchMakingDomain.Steam) {
                OnlineManager.LeaveLobby();
                MatchmakingManager.currentDomain = MatchMakingDomain.Steam;
            }

            try
            {
                if (param.m_steamIDLobby.m_SteamID == lobbyID.m_SteamID)
                {
                    RainMeadow.Debug("trying to rejoin same lobby, ignoring, id: " + param.m_steamIDLobby);
                    return;
                }

                RainMeadow.Debug("trying to join lobby from steam with id: " + param.m_steamIDLobby);

                if (lobbyID != default)
                {
                    LeaveLobby();
                }

                OnlineManager.currentlyJoiningLobby = new SteamLobbyInfo(param.m_steamIDLobby, "", "", 0, false, MAX_LOBBY);
                Custom.rainWorld.processManager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);

                m_JoinLobbyCall.Set(SteamMatchmaking.JoinLobby(param.m_steamIDLobby));
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
            if (lobbyID != default)
            {
                SteamMatchmaking.LeaveLobby(lobbyID);
            }
            lobbyID = default;
            SteamFriends.ClearRichPresence();
        }

        public override OnlinePlayer GetPlayer(MeadowPlayerId id)
        {
            return OnlineManager.players.FirstOrDefault(p => (p.id as SteamPlayerId).steamID == (id as SteamPlayerId).steamID);
        }

        public override OnlinePlayer GetLobbyOwner()
        {
            return GetPlayer(new SteamPlayerId(SteamMatchmaking.GetLobbyOwner(lobbyID)));
        }

        public OnlinePlayer GetPlayerSteam(ulong steamID)
        {
            return OnlineManager.players.FirstOrDefault(p => (p.id as SteamPlayerId).steamID.m_SteamID == steamID);
        }

        public override string GetLobbyID()
        {
            return lobbyID.ToString();
        }

        public override void OpenInvitationOverlay() {
            SteamFriends.ActivateGameOverlayInviteDialog(lobbyID);
        }
    }
}

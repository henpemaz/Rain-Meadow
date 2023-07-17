using Steamworks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace RainMeadow
{
    public static class LobbyManager
    {
        public static CSteamID me;
        public static OnlinePlayer mePlayer;
        public static List<OnlinePlayer> players;
        public static Lobby lobby;

        public static string CLIENT_KEY = "client";
        public static string CLIENT_VAL = "Meadow_" + RainMeadow.MeadowVersionStr;
        public static string NAME_KEY = "name";
        public static string MODE_KEY = "mode";

#if !LOCAL_P2P
#pragma warning disable IDE0052 // Remove unread private members
        private static CallResult<LobbyMatchList_t> m_RequestLobbyListCall;
        private static CallResult<LobbyCreated_t> m_CreateLobbyCall;
        private static CallResult<LobbyEnter_t> m_JoinLobbyCall;
        private static Callback<LobbyDataUpdate_t> m_LobbyDataUpdate;
        private static Callback<LobbyChatUpdate_t> m_LobbyChatUpdate;
        private static Callback<SteamNetworkingMessagesSessionRequest_t> m_SessionRequest;
#pragma warning restore IDE0052 // Remove unread private members
#endif


        public static void InitLobbyManager()
        {
#if LOCAL_P2P
            UdpPeer.Startup();
            me = new CSteamID((ulong)Process.GetCurrentProcess().Id);
#else
            m_RequestLobbyListCall = CallResult<LobbyMatchList_t>.Create(LobbyListReceived);
            m_CreateLobbyCall = CallResult<LobbyCreated_t>.Create(LobbyCreated);
            m_JoinLobbyCall = CallResult<LobbyEnter_t>.Create(LobbyJoined);
            m_LobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(LobbyUpdated);
            m_LobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(LobbyChatUpdated);
            m_SessionRequest = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(SessionRequest);
            me = SteamUser.GetSteamID();
#endif
            mePlayer = new OnlinePlayer(me) { isMe = true };
        }

        public static void Reset()
        {
            lobby = null;
            players = new List<OnlinePlayer>() { mePlayer };
        }

        public enum LobbyVisibility
        {
            [Description("Public")]
            Public = 1,
            [Description("Friends Only")]
            FriendsOnly,
            [Description("Private")]
            Private
        }

        public static event LobbyListReceived_t OnLobbyListReceived;
        public static event LobbyJoined_t OnLobbyJoined;
        public delegate void LobbyListReceived_t(bool ok, LobbyInfo[] lobbies);
        public delegate void LobbyJoined_t(bool ok);

        public static void RequestLobbyList()
        {
            RainMeadow.DebugMe();
#if LOCAL_P2P
            OnLobbyListReceived?.Invoke(true, UdpPeer.isHost ? new LobbyInfo[0] { } : new LobbyInfo[1] { new LobbyInfo(default, "local", "local") });
#else
            SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
            SteamMatchmaking.AddRequestLobbyListStringFilter(LobbyManager.CLIENT_KEY, LobbyManager.CLIENT_VAL, ELobbyComparison.k_ELobbyComparisonEqual);
            m_RequestLobbyListCall.Set(SteamMatchmaking.RequestLobbyList());
#endif
        }

        private static void LobbyListReceived(LobbyMatchList_t pCallback, bool bIOFailure)
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
                        lobbies[i] = new LobbyInfo(id, SteamMatchmaking.GetLobbyData(id, LobbyManager.NAME_KEY), SteamMatchmaking.GetLobbyData(id, LobbyManager.MODE_KEY));
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

        public static void CreateLobby(LobbyVisibility visibility, string gameMode)
        {
            RainMeadow.Debug(visibility);
#if LOCAL_P2P
            lobby = new Lobby(default, gameMode);
            OnLobbyJoined?.Invoke(true);
#else
            creatingWithMode = gameMode;
            ELobbyType eLobbyTypeeLobbyType = visibility switch
            {
                LobbyVisibility.Private => ELobbyType.k_ELobbyTypePrivate,
                LobbyVisibility.Public => ELobbyType.k_ELobbyTypePublic,
                LobbyVisibility.FriendsOnly => ELobbyType.k_ELobbyTypeFriendsOnly,
                _ => throw new ArgumentException()
            };
            m_CreateLobbyCall.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 16));
#endif
        }

        public static void JoinLobby(LobbyInfo lobby)
        {
            RainMeadow.Debug(lobby);
#if LOCAL_P2P
            RainMeadow.Debug("Joining local game...");
            var memory = new MemoryStream(16);
            var writer = new BinaryWriter(memory);
            Packet.Encode(new RequestJoinPacket(), writer, default, new IPEndPoint(IPAddress.Loopback, UdpPeer.STARTING_PORT));
            UdpPeer.Send(new IPEndPoint(IPAddress.Loopback, UdpPeer.STARTING_PORT), memory.GetBuffer(), (int)memory.Position, UdpPeer.PacketType.Reliable);
#else
            m_JoinLobbyCall.Set(SteamMatchmaking.JoinLobby(lobby.id));
#endif
        }

#if !LOCAL_P2P
        private static string creatingWithMode;
        private static void LobbyCreated(LobbyCreated_t param, bool bIOFailure)
        {
            try
            {
                RainMeadow.DebugMe();
                if (!bIOFailure && param.m_eResult == EResult.k_EResultOK)
                {
                    RainMeadow.Debug("success");
                    LobbyManager.lobby = new Lobby(new CSteamID(param.m_ulSteamIDLobby), creatingWithMode);
                    OnLobbyJoined?.Invoke(true);
                }
                else
                {
                    RainMeadow.Debug("failure, error code is " + param.m_eResult);
                    LobbyManager.lobby = null;
                    OnLobbyJoined?.Invoke(false);
                }
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        private static void LobbyJoined(LobbyEnter_t param, bool bIOFailure)
        {
            try
            {
                if (!bIOFailure)
                {
                    RainMeadow.Debug("success");
                    var id = new CSteamID(param.m_ulSteamIDLobby);
                    LobbyManager.lobby = new Lobby(id, SteamMatchmaking.GetLobbyData(id, LobbyManager.MODE_KEY));
                    OnLobbyJoined?.Invoke(true);
                }
                else
                {
                    RainMeadow.Debug("failure");
                    LobbyManager.lobby = null;
                    OnLobbyJoined?.Invoke(false);
                }
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }
#endif

        public static void LeaveLobby()
        {
            RainMeadow.DebugMe();
            if (LobbyManager.lobby != null)
            {
#if LOCAL_P2P
                UdpPeer.Shutdown();
#else
                SteamMatchmaking.LeaveLobby(LobbyManager.lobby.id);
#endif
                OnlineManager.Reset();
            }
        }

#if !LOCAL_P2P
        private static void LobbyUpdated(LobbyDataUpdate_t param)
        {
            try
            {
                RainMeadow.Debug($"{param.m_ulSteamIDLobby} : {param.m_ulSteamIDMember} : {param.m_bSuccess}");
                if (LobbyManager.lobby == null)
                {
                    RainMeadow.Error("got lobby event with no lobby!");
                    return;
                }
                if ((CSteamID)param.m_ulSteamIDLobby != LobbyManager.lobby.id)
                {
                    RainMeadow.Error("got lobby event for wrong lobby!");
                    return;
                }
                if (param.m_bSuccess > 0)
                {
                    if (LobbyManager.lobby != null && LobbyManager.lobby.id == new CSteamID(param.m_ulSteamIDLobby))
                    {
                        // lobby event, check for possible changes
                        UpdatePlayersList(LobbyManager.lobby);
                    }
                }
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        private static void LobbyChatUpdated(LobbyChatUpdate_t param)
        {
            try
            {
                RainMeadow.Debug($"{param.m_ulSteamIDLobby} : {param.m_ulSteamIDUserChanged} : {param.m_ulSteamIDMakingChange} : {param.m_rgfChatMemberStateChange}");
                if (LobbyManager.lobby == null)
                {
                    RainMeadow.Error("got lobby event with no lobby!");
                    return;
                }

                if ((CSteamID)param.m_ulSteamIDLobby != LobbyManager.lobby.id)
                {
                    RainMeadow.Error("got lobby event for wrong lobby!");
                    return;
                }

                UpdatePlayersList(LobbyManager.lobby);
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        private static void SessionRequest(SteamNetworkingMessagesSessionRequest_t param)
        {
            try
            {
                var id = new CSteamID(param.m_identityRemote.GetSteamID64());
                RainMeadow.Debug("session request from " + id);
                if (LobbyManager.lobby != null)
                {
                    if (players.FirstOrDefault(op => op.id == id) is OnlinePlayer p)
                    {
                        RainMeadow.Debug("accepted session from " + p.name);
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
#endif


        public static void UpdatePlayersList(Lobby lobby)
        {
            try
            {
                RainMeadow.DebugMe();
                var oldplayers = players.Select(p => p.id).ToArray();
                var n = SteamMatchmaking.GetNumLobbyMembers(lobby.id);
                var newplayers = new CSteamID[n];
                for (int i = 0; i < n; i++)
                {
                    newplayers[i] = SteamMatchmaking.GetLobbyMemberByIndex(lobby.id, i);
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

        private static void PlayerJoined(CSteamID p)
        {
            RainMeadow.Debug($"PlayerJoined:{p} - {SteamFriends.GetFriendPersonaName(p)}");
            if (p == me) return;
            SteamFriends.RequestUserInformation(p, true);
            players.Add(new OnlinePlayer(p));
        }

        private static void PlayerLeft(CSteamID p)
        {
            RainMeadow.Debug($"{p} - {SteamFriends.GetFriendPersonaName(p)}");

            if (players.FirstOrDefault(op => op.id == p) is OnlinePlayer player)
            {
                RainMeadow.Debug($"Handling player disconnect:{player}");
                player.hasLeft = true;
                LobbyManager.lobby?.OnPlayerDisconnect(player);
                while (player.HasUnacknoledgedEvents())
                {
                    player.AbortUnacknoledgedEvents();
                    LobbyManager.lobby?.OnPlayerDisconnect(player);
                }
                RainMeadow.Debug($"Actually removing player:{player}");
                players.Remove(player);
            }
        }

        public static OnlinePlayer PlayerFromId(ulong id)
        {
            return LobbyManager.players.FirstOrDefault(p => p.id.m_SteamID == id);
        }

        public static OnlinePlayer BestTransferCandidate(OnlineResource onlineResource, Dictionary<OnlinePlayer, PlayerMemebership> subscribers)
        {
            if (subscribers.Keys.Contains(mePlayer)) return mePlayer;
            // todo pick by ping?
            if (subscribers.Count < 1) return null;
            return subscribers.First().Key;
        }
    }
}
using Steamworks;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;

namespace RainMeadow
{
    public static class LobbyManager
    {
#pragma warning disable IDE0052 // Remove unread private members
        private static CallResult<LobbyMatchList_t> m_RequestLobbyListCall;
        private static CallResult<LobbyCreated_t> m_CreateLobbyCall;
        private static CallResult<LobbyEnter_t> m_JoinLobbyCall;
        private static Callback<LobbyDataUpdate_t> m_LobbyDataUpdate;
        private static Callback<LobbyChatUpdate_t> m_LobbyChatUpdate;
#pragma warning restore IDE0052 // Remove unread private members

        public static void InitLobbyManager()
        {
            m_RequestLobbyListCall = CallResult<LobbyMatchList_t>.Create(LobbyListReceived);
            m_CreateLobbyCall = CallResult<LobbyCreated_t>.Create(LobbyCreated);
            m_JoinLobbyCall = CallResult<LobbyEnter_t>.Create(LobbyJoined);
            m_LobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(LobbyUpdated);
            m_LobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(LobbyChatUpdated);
#if LOCAL_P2P
            UdpPeer.Startup();
#endif
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
            OnLobbyListReceived?.Invoke(true, UdpPeer.isHost ? new LobbyInfo[0] { } : new LobbyInfo[1] { new LobbyInfo(default) { name = "dummy" } });
#else
            SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
            SteamMatchmaking.AddRequestLobbyListStringFilter(OnlineManager.CLIENT_KEY, OnlineManager.CLIENT_VAL, ELobbyComparison.k_ELobbyComparisonEqual);
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
                        lobbies[i] = new LobbyInfo(id);
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

        private static string creatingWithMode;
        public static void CreateLobby(LobbyVisibility visibility, string gameMode)
        {
            creatingWithMode = gameMode;
            RainMeadow.Debug(visibility);
#if LOCAL_P2P
            OnlineManager.lobby = new Lobby(default, creatingWithMode);
            OnLobbyJoined?.Invoke(true);
#else
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

        private static void LobbyCreated(LobbyCreated_t param, bool bIOFailure)
        {
            try
            {
                RainMeadow.DebugMe();
                if (!bIOFailure && param.m_eResult == EResult.k_EResultOK)
                {
                    RainMeadow.Debug("success");
                    OnlineManager.lobby = new Lobby(new CSteamID(param.m_ulSteamIDLobby), creatingWithMode);
                    OnLobbyJoined?.Invoke(true);
                }
                else
                {
                    RainMeadow.Debug("failure, error code is " + param.m_eResult);
                    OnlineManager.lobby = null;
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
                    OnlineManager.lobby = new Lobby(id, SteamMatchmaking.GetLobbyData(id, OnlineManager.MODE_KEY));
                    OnLobbyJoined?.Invoke(true);
                }
                else
                {
                    RainMeadow.Debug("failure");
                    OnlineManager.lobby = null;
                    OnLobbyJoined?.Invoke(false);
                }
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }

        public static void LeaveLobby()
        {
            RainMeadow.DebugMe();
            if (OnlineManager.lobby != null)
            {
#if LOCAL_P2P
                UdpPeer.Shutdown();
#else
                SteamMatchmaking.LeaveLobby(OnlineManager.lobby.id);
#endif
                OnlineManager.Reset();
            }
        }

        private static void LobbyUpdated(LobbyDataUpdate_t param)
        {
            try
            {
                RainMeadow.Debug($"{param.m_ulSteamIDLobby} : {param.m_ulSteamIDMember} : {param.m_bSuccess}");
                if (OnlineManager.lobby == null)
                {
                    RainMeadow.Error("got lobby event with no lobby!");
                    return;
                }
                if ((CSteamID)param.m_ulSteamIDLobby != OnlineManager.lobby.id)
                {
                    RainMeadow.Error("got lobby event for wrong lobby!");
                    return;
                }
                if (param.m_bSuccess > 0)
                {
                    if (OnlineManager.lobby != null && OnlineManager.lobby.id == new CSteamID(param.m_ulSteamIDLobby))
                    {
                        // lobby event, check for possible changes
                        PlayersManager.UpdatePlayersList(OnlineManager.lobby.id);
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
                if (OnlineManager.lobby == null)
                {
                    RainMeadow.Error("got lobby event with no lobby!");
                    return;
                }

                if ((CSteamID)param.m_ulSteamIDLobby != OnlineManager.lobby.id)
                {
                    RainMeadow.Error("got lobby event for wrong lobby!");
                    return;
                }

                PlayersManager.UpdatePlayersList(OnlineManager.lobby.id);
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                throw;
            }
        }
    }
}
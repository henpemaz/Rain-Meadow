using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;

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
        }

        public static event LobbyListReceived_t OnLobbyListReceived;
        public static event LobbyJoined_t OnLobbyJoined;
        public delegate void LobbyListReceived_t(bool ok, LobbyInfo[] lobbies);
        public delegate void LobbyJoined_t(bool ok);

        public static void RequestLobbyList()
        {
            RainMeadow.DebugMe();
            SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
            SteamMatchmaking.AddRequestLobbyListStringFilter(OnlineManager.CLIENT_KEY, OnlineManager.CLIENT_VAL, ELobbyComparison.k_ELobbyComparisonEqual);
            m_RequestLobbyListCall.Set(SteamMatchmaking.RequestLobbyList());
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

        public static void CreateLobby()
        {
            RainMeadow.DebugMe();
            m_CreateLobbyCall.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 10));
        }

        public static void JoinLobby(LobbyInfo lobby)
        {
            RainMeadow.DebugMe();
            m_JoinLobbyCall.Set(SteamMatchmaking.JoinLobby(lobby.id));
        }

        private static void LobbyCreated(LobbyCreated_t param, bool bIOFailure)
        {
            try
            {
                RainMeadow.DebugMe();
                if (!bIOFailure && param.m_eResult == EResult.k_EResultOK)
                {
                    RainMeadow.Debug("success");
                    OnlineManager.lobby = new Lobby(new CSteamID(param.m_ulSteamIDLobby));
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
                    OnlineManager.lobby = new Lobby(new CSteamID(param.m_ulSteamIDLobby));
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
                SteamMatchmaking.LeaveLobby(OnlineManager.lobby.id);
                OnlineManager.lobby = null;
            }
        }

        private static void LobbyUpdated(LobbyDataUpdate_t param)
        {
            try
            {
                RainMeadow.Debug($"{param.m_ulSteamIDLobby} : {param.m_ulSteamIDMember} : {param.m_bSuccess}");
                if (OnlineManager.lobby == null) {
                    RainMeadow.Error("got lobby event with no lobby!");
                    return;
                }
                if ((CSteamID)param.m_ulSteamIDLobby != OnlineManager.lobby.id) {
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
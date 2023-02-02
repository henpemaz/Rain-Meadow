using Steamworks;
using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public class LobbyManager
    {
#pragma warning disable IDE0052 // Remove unread private members
        private CallResult<LobbyMatchList_t> m_RequestLobbyListCall;
        private CallResult<LobbyCreated_t> m_CreateLobbyCall;
        private CallResult<LobbyEnter_t> m_JoinLobbyCall;
        private Callback<LobbyDataUpdate_t> m_LobbyDataUpdate;
        private Callback<LobbyChatUpdate_t> m_LobbyChatUpdate;
#pragma warning restore IDE0052 // Remove unread private members

        public LobbyManager()
        {
            m_RequestLobbyListCall = CallResult<LobbyMatchList_t>.Create(LobbyListReceived);
            m_CreateLobbyCall = CallResult<LobbyCreated_t>.Create(LobbyCreated);
            m_JoinLobbyCall = CallResult<LobbyEnter_t>.Create(LobbyJoined);
            m_LobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(LobbyUpdated);
            m_LobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(LobbyChatUpdated);
        }

        public event LobbyListReceived_t OnLobbyListReceived;
        public event LobbyJoined_t OnLobbyJoined;
        public delegate void LobbyListReceived_t(bool ok, LobbyInfo[] lobbies);
        public delegate void LobbyJoined_t(bool ok);

        public void RequestLobbyList()
        {
            RainMeadow.DebugMethodName();
            SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
            SteamMatchmaking.AddRequestLobbyListStringFilter(OnlineManager.CLIENT_KEY, OnlineManager.CLIENT_VAL, ELobbyComparison.k_ELobbyComparisonEqual);
            m_RequestLobbyListCall.Set(SteamMatchmaking.RequestLobbyList());
        }

        private void LobbyListReceived(LobbyMatchList_t pCallback, bool bIOFailure)
        {
            RainMeadow.DebugMethodName();
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

        public void CreateLobby()
        {
            RainMeadow.DebugMethodName();
            m_CreateLobbyCall.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 10));
        }

        public void JoinLobby(LobbyInfo lobby)
        {
            RainMeadow.DebugMethodName();
            m_JoinLobbyCall.Set(SteamMatchmaking.JoinLobby(lobby.id));
        }

        private void LobbyCreated(LobbyCreated_t param, bool bIOFailure)
        {
            RainMeadow.DebugMethodName();
            if (!bIOFailure && param.m_eResult == EResult.k_EResultOK)
            {
                OnlineManager.lobby = new Lobby(new CSteamID(param.m_ulSteamIDLobby));
                OnLobbyJoined?.Invoke(true);
            }
            else
            {
                OnlineManager.lobby = null;
                OnLobbyJoined?.Invoke(false);
            }
        }

        private void LobbyJoined(LobbyEnter_t param, bool bIOFailure)
        {
            RainMeadow.DebugMethodName();
            if (!bIOFailure)
            {
                OnlineManager.lobby = new Lobby(new CSteamID(param.m_ulSteamIDLobby));
                OnLobbyJoined?.Invoke(true);
            }
            else
            {
                OnlineManager.lobby = null;
                OnLobbyJoined?.Invoke(false);
            }
        }

        public void LeaveLobby()
        {
            RainMeadow.DebugMethodName();
            if (OnlineManager.lobby != null)
            {
                SteamMatchmaking.LeaveLobby(OnlineManager.lobby.id);
                OnlineManager.lobby = null;
            }
        }

        private void LobbyUpdated(LobbyDataUpdate_t e)
        {
            RainMeadow.DebugMethodName();
            if (OnlineManager.lobby == null) {
                RainMeadow.Error("got lobby event with no lobby!");
                return;
            }
            if ((CSteamID)e.m_ulSteamIDLobby != OnlineManager.lobby.id) {
                RainMeadow.Error("got lobby event for wrong lobby!");
                return;
            }
            if (e.m_bSuccess > 0)
            {
                if (OnlineManager.lobby != null && OnlineManager.lobby.id == new CSteamID(e.m_ulSteamIDLobby) && e.m_ulSteamIDLobby == e.m_ulSteamIDMember)
                {
                    // lobby event, check for possible changes
                    if (OnlineManager.lobby.owner.id != SteamMatchmaking.GetLobbyOwner(OnlineManager.lobby.id))
                    {
                        // owner changed
                        // panik?
                    }
                }
            }
        }

        private void LobbyChatUpdated(LobbyChatUpdate_t param)
        {
            RainMeadow.DebugMethodName();
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
        }
    }
}
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    public class OnlineManager : MainLoopProcess {

        public static string CLIENT_KEY = "client";
        public static string CLIENT_VAL = "Meadow";
        public static string NAME_KEY = "name";

        private CallResult<LobbyMatchList_t> m_RequestLobbyListCall;
        private CallResult<LobbyCreated_t> m_CreateLobbyCall;
        private CallResult<LobbyEnter_t> m_JoinLobbyCall;
        private Callback<LobbyDataUpdate_t> m_LobbyDataUpdate;


        public static OnlineManager instance;
        public OnlinePlayer me;
        public Lobby lobby;
        public OnlineSession session;

        public bool isOnlineSession;

        public OnlineManager(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.OnlineManager)
        {
            instance = this;

            m_RequestLobbyListCall = CallResult<LobbyMatchList_t>.Create(LobbyListReceived);
            m_CreateLobbyCall = CallResult<LobbyCreated_t>.Create(LobbyCreated);
            m_JoinLobbyCall = CallResult<LobbyEnter_t>.Create(LobbyJoined);
            m_LobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(LobbyUpdated);

            me = new OnlinePlayer(SteamUser.GetSteamID());
            RainMeadow.sLogger.LogInfo("OnlineManager Created");
        }

        public void RequestLobbyList()
        {
            RainMeadow.sLogger.LogInfo("OnlineManager RequestLobbyList");
            SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
            SteamMatchmaking.AddRequestLobbyListStringFilter(OnlineManager.CLIENT_KEY, OnlineManager.CLIENT_VAL, ELobbyComparison.k_ELobbyComparisonEqual);
            m_RequestLobbyListCall.Set(SteamMatchmaking.RequestLobbyList());
        }

        public delegate void LobbyListReceived_t(bool ok, Lobby[] lobbies);
        public event LobbyListReceived_t OnLobbyListReceived;

        void LobbyListReceived(LobbyMatchList_t pCallback, bool bIOFailure)
        {
            Debug.Log("LobbyListReceived");
            List<Lobby> list = new List<Lobby>();
            if (!bIOFailure)
            {
                for (int i = 0; i < pCallback.m_nLobbiesMatching; i++)
                {
                    list.Add(new Lobby(SteamMatchmaking.GetLobbyByIndex(i)));
                }
            }

            OnLobbyListReceived?.Invoke(!bIOFailure, list.ToArray());
        }

        public void CreateLobby()
        {
            Debug.Log("CreateLobby");
            m_CreateLobbyCall.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 10));
        }

        public delegate void LobbyJoined_t(bool ok, Lobby lobby);
        public event LobbyJoined_t OnLobbyJoined;
        void LobbyCreated(LobbyCreated_t param, bool bIOFailure)
        {
            Debug.Log("LobbyCreated");
            if (!bIOFailure && param.m_eResult == EResult.k_EResultOK)
            {
                this.lobby = new Lobby(new CSteamID(param.m_ulSteamIDLobby));
                lobby.SetupNew();
                OnLobbyJoined?.Invoke(true, lobby);
            }
            else
            {
                lobby = null;
                OnLobbyJoined?.Invoke(false, null);
            }
        }

        public void JoinLobby(Lobby lobby)
        {
            Debug.Log("JoinLobby");
            m_JoinLobbyCall.Set(SteamMatchmaking.JoinLobby(lobby.id));
        }

        private void LobbyJoined(LobbyEnter_t param, bool bIOFailure)
        {
            Debug.Log("LobbyJoined");
            if (!bIOFailure)
            {
                lobby = new Lobby(new CSteamID(param.m_ulSteamIDLobby));

                if (lobby.owner == me)
                {
                    lobby.SetupNew();
                }
                OnLobbyJoined?.Invoke(true, lobby);
            }
            else
            {
                lobby = null;
                OnLobbyJoined?.Invoke(false, null);
            }
        }

        void LobbyUpdated(LobbyDataUpdate_t e)
        {
            if (e.m_bSuccess > 0)
            {
                if (lobby != null && lobby.id == new CSteamID(e.m_ulSteamIDLobby) && e.m_ulSteamIDLobby == e.m_ulSteamIDMember)
                {
                    // lobby event, check for possible changes
                    if (lobby.owner.id != SteamMatchmaking.GetLobbyOwner(lobby.id))
                    {
                        // owner changed
                    }

                    lobby.UpdateInfo();
                }
            }
        }

        internal void BroadcastEvent(LobbyEvent lobbyEvent)
        {
            //throw new NotImplementedException();
        }

        internal void LeaveLobby()
        {
            if(lobby != null)
            {
                SteamMatchmaking.LeaveLobby(lobby.id);
            }
        }
    }
}

using Steamworks;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    public class LobbyManager {
        public static string CLIENT_KEY = "client";
        public static string CLIENT_VAL = "Meadow";
        public static string NAME_KEY = "name";

        private CallResult<LobbyMatchList_t> m_RequestLobbyListCall;
        private CallResult<LobbyCreated_t> m_CreateLobbyCall;
        private CallResult<LobbyEnter_t> m_JoinLobbyCall;

        public OnlinePlayer me;
        public Lobby currentLobby;

        static LobbyManager _instance;
        public static LobbyManager instance 
        { get
            {
                _instance ??= new LobbyManager();
                return _instance;
            }
        }

        public LobbyManager()
        {
            m_RequestLobbyListCall = CallResult<LobbyMatchList_t>.Create(LobbyListReceived);
            m_CreateLobbyCall = CallResult<LobbyCreated_t>.Create(LobbyCreated);
            m_JoinLobbyCall = CallResult<LobbyEnter_t>.Create(LobbyJoined);

            me = new OnlinePlayer(SteamUser.GetSteamID());

            RequestLobbyList();
        }

        public void RequestLobbyList()
        {
            SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
            SteamMatchmaking.AddRequestLobbyListStringFilter(LobbyManager.CLIENT_KEY, LobbyManager.CLIENT_VAL, ELobbyComparison.k_ELobbyComparisonEqual);
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
                this.currentLobby = new Lobby(new CSteamID(param.m_ulSteamIDLobby));
                currentLobby.SetupNew();
                currentLobby.UpdateInfoFull();
                OnLobbyJoined?.Invoke(true, currentLobby);
            }
            else
            {
                currentLobby = null;
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
                currentLobby = new Lobby(new CSteamID(param.m_ulSteamIDLobby));

                if (currentLobby.owner == me)
                {
                    currentLobby.SetupNew();
                }
                currentLobby.UpdateInfoFull();
                OnLobbyJoined?.Invoke(true, currentLobby);
            }
            else
            {
                currentLobby = null;
                OnLobbyJoined?.Invoke(false, null);
            }
        }
    }
}

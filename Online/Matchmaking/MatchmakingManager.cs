using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Services.Analytics.Internal;

namespace RainMeadow
{

    public abstract class MatchmakingManager
    {
        public class MatchMaker: ExtEnum<MatchMaker> {
            public MatchMaker(string name, bool register) : base(name, register) { }

            public static MatchMaker Local = new MatchMaker("Local", true);
            public static MatchMaker Steam = new MatchMaker("Steam", true);


        };


        public static event LobbyListReceived_t OnLobbyListReceived = delegate {};
        public static event PlayerListReceived_t OnPlayerListReceived = delegate {};
        public static event LobbyJoined_t OnLobbyJoined = delegate {};

        protected static void OnLobbyJoinedEvent(bool ok, string error = "") => OnLobbyJoined?.Invoke(ok, error);
        protected static void OnPlayerListReceivedEvent(PlayerInfo[] players) => OnPlayerListReceived?.Invoke(players);
        protected static void OnLobbyListReceivedEvent(bool ok, LobbyInfo[] lobbies) => OnLobbyListReceived?.Invoke(ok, lobbies);

        public static event ChangedMatchMaker_t changedMatchMaker = delegate { };
        public delegate void ChangedMatchMaker_t(MatchMaker last, MatchMaker current);

        private static MatchMaker _Matchmaker = MatchMaker.Local;

        public static MatchMaker currentMatchMaker { get { return _Matchmaker; } set { 
                        var last = _Matchmaker; 
                        _Matchmaker = value; 
                        changedMatchMaker.Invoke(last, _Matchmaker);  }} 
        public static MatchmakingManager currentInstance { get => instances[currentMatchMaker]; }
        public static Dictionary<MatchMaker, MatchmakingManager> instances = new Dictionary<MatchMaker, MatchmakingManager>();


        public static string CLIENT_KEY = "client";
        public static string CLIENT_VAL = "Meadow_" + RainMeadow.MeadowVersionStr;
        public static string NAME_KEY = "name";
        public static string MODE_KEY = "mode";
        public static string PASSWORD_KEY = "password";
        public static int MAX_LOBBY = 4;

        static public readonly List<MatchMaker> supported_matchmakers = new();

        public static void InitLobbyManager()
        {
            supported_matchmakers.Clear();
            instances.Clear();

            if (OnlineManager.netIO is SteamNetIO) {
                instances.TryAdd(MatchMaker.Steam, new SteamMatchmakingManager());
                supported_matchmakers.Add(MatchMaker.Steam);
            }

            supported_matchmakers.Add(MatchMaker.Local); 
            instances.TryAdd(MatchMaker.Local, new LANMatchmakingManager());
            currentMatchMaker = supported_matchmakers[0];
                
            currentInstance.initializeMePlayer();
            changedMatchMaker += (last, current) => {
                currentInstance.initializeMePlayer();
            };
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

        public delegate void LobbyListReceived_t(bool ok, LobbyInfo[] lobbies);
        public delegate void PlayerListReceived_t(PlayerInfo[] players);
        public delegate void LobbyJoined_t(bool ok, string error = "");

        public abstract void initializeMePlayer();
        public abstract void RequestLobbyList();

        public abstract void CreateLobby(LobbyVisibility visibility, string gameMode, string? password, int? maxPlayerCount);

        public abstract void RequestJoinLobby(LobbyInfo lobby, string? password);
        public abstract void JoinLobby(bool success);

        public abstract void LeaveLobby();

        public abstract OnlinePlayer GetLobbyOwner();

        public virtual OnlinePlayer GetPlayer(MeadowPlayerId id)
        {
            return OnlineManager.players.FirstOrDefault(p => p.id == id);
        }

        public virtual List<PlayerInfo> playerList => OnlineManager.players.Select(player => new PlayerInfo(() => player.id.OpenProfileLink(), player.id.name)).ToList();

        // the idea here was to decide by ping some day
        public virtual OnlinePlayer BestTransferCandidate(OnlineResource onlineResource, List<OnlinePlayer> subscribers)
        {
            if (onlineResource.isAvailable && onlineResource.isActive && subscribers.Contains(OnlineManager.mePlayer) && !OnlineManager.mePlayer.isActuallySpectating) return OnlineManager.mePlayer;
            if (subscribers.Count < 1) return null;
            return subscribers.FirstOrDefault(p => !p.hasLeft && OnlineManager.lobby.gameMode.PlayerCanOwnResource(p, onlineResource));
        }

        public void HandleDisconnect(OnlinePlayer player)
        {
            RainMeadow.Debug($"Handling player disconnect:{player}");
            player.hasLeft = true;
            OnlineManager.lobby?.OnPlayerDisconnect(player);
            while (player.HasUnacknoledgedEvents())
            {
                player.AbortUnacknoledgedEvents();
                OnlineManager.lobby?.OnPlayerDisconnect(player);
                OnlineManager.ForceLoadUpdate(); // process incoming data
            }
            RainMeadow.Debug($"Actually removing player:{player}");
            OnlineManager.players.Remove(player);

            ChatLogManager.LogMessage("", $"{player.id.name} left the game.");
        }

        public abstract MeadowPlayerId GetEmptyId();

        public abstract string GetLobbyID();
        public abstract void OpenInvitationOverlay();
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;

namespace RainMeadow
{

    public abstract class MatchmakingManager
    {
        public class MatchMakingDomain: ExtEnum<MatchMakingDomain> {
            public MatchMakingDomain(string name, bool register) : base(name, register) { }

            public static MatchMakingDomain LAN = new MatchMakingDomain("Local", true);
            public static MatchMakingDomain Steam = new MatchMakingDomain("Steam", true);


        };


        public static event LobbyListReceived_t OnLobbyListReceived = delegate {};
        public static event PlayerListReceived_t OnPlayerListReceived = delegate {};
        public static event LobbyJoined_t OnLobbyJoined = delegate {};

        protected static void OnLobbyJoinedEvent(bool ok, string error = "") => OnLobbyJoined?.Invoke(ok, error);
        protected static void OnPlayerListReceivedEvent(PlayerInfo[] players) => OnPlayerListReceived?.Invoke(players);
        protected static void OnLobbyListReceivedEvent(bool ok, LobbyInfo[] lobbies) => OnLobbyListReceived?.Invoke(ok, lobbies);

        public static event ChangedMatchMakingDomain_t changedMatchMaker = delegate { };
        public delegate void ChangedMatchMakingDomain_t(MatchMakingDomain last, MatchMakingDomain current);

        private static MatchMakingDomain _Domain = MatchMakingDomain.LAN;

        public static MatchMakingDomain currentDomain { get { return _Domain; } set { 
                        var last = _Domain; 
                        _Domain = value; 
                        changedMatchMaker.Invoke(last, _Domain);  }} 
        public static MatchmakingManager currentInstance { get => instances[currentDomain]; }
        public static Dictionary<MatchMakingDomain, MatchmakingManager> instances = new Dictionary<MatchMakingDomain, MatchmakingManager>();


        public static string CLIENT_KEY = "client";
        public static string CLIENT_VAL = "Meadow_" + RainMeadow.MeadowVersionStr;
        public static string NAME_KEY = "name";
        public static string MODE_KEY = "mode";
        public static string MODS_KEY = "mods";
        public static string BANNED_MODS_KEY = "banned_mods";
        public static string PASSWORD_KEY = "password";
        public static int MAX_LOBBY = 4;

        static public readonly List<MatchMakingDomain> supported_matchmakers = new();

        public static void InitLobbyManager()
        {
            supported_matchmakers.Clear();
            instances.Clear();

            if (OnlineManager.netIO is SteamNetIO) {
                instances.Add(MatchMakingDomain.Steam, new SteamMatchmakingManager());
                supported_matchmakers.Add(MatchMakingDomain.Steam);
            }

            supported_matchmakers.Add(MatchMakingDomain.LAN); 
            instances.Add(MatchMakingDomain.LAN, new LANMatchmakingManager());
            currentDomain = supported_matchmakers[0];
                
            OnlineManager.LeaveLobby();
            changedMatchMaker += (last, current) => {
                OnlineManager.LeaveLobby();
            };
        }

        public enum LobbyVisibility
        {
            [Description("Public")]
            Public = 1,
            [Description("Friends Only")]
            FriendsOnly,
            [Description("Invite Only")]
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

        public abstract void JoinLobbyUsingArgs(params string?[] args);
        public static void JoinLobbyUsingCode(string code) {
            RainMeadow.Debug($"Attempting to join lobby with code: {code}");

            string[] args = code.Split(' ');
            
            int connect_steam_idx = Array.IndexOf(args, "+connect_steam_lobby"),
                connect_lan_idx = Array.IndexOf(args, "+connect_lan_lobby"),
                password_idx = Array.IndexOf(args, "+lobby_password");

            //find password, if it exists
            string? password = null;
            if (password_idx >= 0 && args.Length > password_idx + 1)
                password = args[password_idx + 1];

            //connect to lobby
            if (connect_steam_idx >= 0)
            {
                if (args.Length > connect_steam_idx + 1)
                {
                    foreach (var domain in supported_matchmakers)
                    {
                        if (domain == MatchMakingDomain.Steam)
                        {
                            instances[domain].JoinLobbyUsingArgs(args[connect_steam_idx + 1], password);
                            break;
                        }
                    }
                }
                else
                    RainMeadow.Error("found +connect_steam_lobby but no valid lobby id in the command line");
            }
            else if (connect_lan_idx >= 0)
            {
                if (args.Length > connect_lan_idx + 2)
                {
                    foreach (var domain in supported_matchmakers)
                    {
                        if (domain == MatchMakingDomain.LAN)
                        {
                            instances[domain].JoinLobbyUsingArgs(args[connect_lan_idx + 1], args[connect_lan_idx + 2], password);
                            break;
                        }
                    }
                }
                else
                    RainMeadow.Error("found +connect_lan_lobby but no valid lobby address and port in the command line");
            }
        }

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

        public virtual bool canSendChatMessages => false;
        public virtual void SendChatMessage(string message) { }
        public virtual void RecieveChatMessage(OnlinePlayer player, string message) { 
            ChatLogManager.LogMessage($"{player.id.GetPersonaName()}", $"{message}");
        }

        public void HandleJoin(OnlinePlayer player) {
            ChatLogManager.LogMessage("Rain Meadow:", $"{player.id.GetPersonaName()} joined the game.");
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

            ChatLogManager.LogMessage("Rain Meadow:", $"{player.id.GetPersonaName()} left the game.");
        }

        public abstract MeadowPlayerId GetEmptyId();

        public abstract string GetLobbyID();
        public abstract void OpenInvitationOverlay();
    }
}

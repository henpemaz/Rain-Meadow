using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace RainMeadow
{

    public abstract class LobbyManager
    {
        public static LobbyManager instance;
        public static OnlinePlayer mePlayer;
        public static List<OnlinePlayer> players;
        public static Lobby lobby;

        public static string CLIENT_KEY = "client";
        public static string CLIENT_VAL = "Meadow_" + RainMeadow.MeadowVersionStr;
        public static string NAME_KEY = "name";
        public static string MODE_KEY = "mode";


        public static void InitLobbyManager()
        {
#if LOCAL_P2P
            instance = new LocalLobbyManager();
#else
            instance = new SteamLobbyManager();
#endif
        }

        public virtual void Reset()
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

        public abstract event LobbyListReceived_t OnLobbyListReceived;
        public abstract event LobbyJoined_t OnLobbyJoined;
        public delegate void LobbyListReceived_t(bool ok, LobbyInfo[] lobbies);
        public delegate void LobbyJoined_t(bool ok);

        public abstract void RequestLobbyList();

        public abstract void CreateLobby(LobbyVisibility visibility, string gameMode);

        public abstract void JoinLobby(LobbyInfo lobby);

        public abstract void LeaveLobby();

        public abstract OnlinePlayer GetLobbyOwner();

        public abstract void UpdatePlayersList(); // todo remove this

        public virtual OnlinePlayer GetPlayer(MeadowPlayerId id)
        {
            return players.FirstOrDefault(p => p.id == id);
        }

        public virtual OnlinePlayer BestTransferCandidate(OnlineResource onlineResource, Dictionary<OnlinePlayer, PlayerMemebership> subscribers)
        {
            if (subscribers.Keys.Contains(mePlayer)) return mePlayer;
            if (subscribers.Count < 1) return null;
            return subscribers.First().Key;
        }

        public abstract MeadowPlayerId GetEmptyId();
    }
}
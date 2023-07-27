using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace RainMeadow
{

    public abstract class MatchmakingManager
    {
        public static MatchmakingManager instance;
        public static string CLIENT_KEY = "client";
        public static string CLIENT_VAL = "Meadow_" + RainMeadow.MeadowVersionStr;
        public static string NAME_KEY = "name";
        public static string MODE_KEY = "mode";


        public static void InitLobbyManager()
        {
#if LOCAL_P2P
            instance = new LocalMatchmakingManager();
#else
            instance = new SteamLobbyManager();
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

        public abstract event LobbyListReceived_t OnLobbyListReceived;
        public abstract event LobbyJoined_t OnLobbyJoined;
        public delegate void LobbyListReceived_t(bool ok, LobbyInfo[] lobbies);
        public delegate void LobbyJoined_t(bool ok);

        public abstract void RequestLobbyList();

        public abstract void CreateLobby(LobbyVisibility visibility, string gameMode);

        public abstract void JoinLobby(LobbyInfo lobby);

        public abstract void LeaveLobby();

        public abstract OnlinePlayer GetLobbyOwner();

        public virtual OnlinePlayer GetPlayer(MeadowPlayerId id)
        {
            return OnlineManager.players.FirstOrDefault(p => p.id == id);
        }

        public virtual OnlinePlayer BestTransferCandidate(OnlineResource onlineResource, Dictionary<OnlinePlayer, PlayerMemebership> subscribers)
        {
            if (subscribers.Keys.Contains(OnlineManager.mePlayer)) return OnlineManager.mePlayer;
            if (subscribers.Count < 1) return null;
            return subscribers.First().Key;
        }

        public abstract MeadowPlayerId GetEmptyId();
    }
}




using System.Collections.Generic;
using System.Net;

namespace RainMeadow {
    class RouterPlayerId : MeadowPlayerId {
        public ulong RoutingId = 0;

        public RouterPlayerId() {}
        public RouterPlayerId(ulong id = 0) { RoutingId = id; }

        override public int GetHashCode() { unchecked { return (int)RoutingId; } }
        override public void CustomSerialize(Serializer serializer) {
            serializer.Serialize(ref RoutingId);
        }

        public override bool Equals(MeadowPlayerId other) {
            if (other is RouterPlayerId other_router_id) {
                return RoutingId == other_router_id.RoutingId;
            }

            return false;
        }
    }



    class ServerRouterPlayerId : RouterPlayerId {
        public IPEndPoint endPoint;

        public ServerRouterPlayerId() {}
        public ServerRouterPlayerId(ulong id, IPEndPoint endPoint) { RoutingId = id; this.endPoint = endPoint; }
    }

    public class RouterMatchmakingManager : MatchmakingManager {

        public override void initializeMePlayer() {
            OnlineManager.mePlayer = new OnlinePlayer( new RouterPlayerId());
            OnlineManager.players = new List<OnlinePlayer>{ OnlineManager.mePlayer };
        }

        public override void RequestLobbyList() {

        }

        LobbyVisibility visibility;
        int? maxPlayerCount;
        public override void CreateLobby(LobbyVisibility visibility, string gameMode, string? password, int? maxPlayerCount) {
            maxPlayerCount = maxPlayerCount ?? 4;
            OnlineManager.lobby = new Lobby(new OnlineGameMode.OnlineGameModeType(gameMode), OnlineManager.mePlayer, password);
            MatchmakingManager.OnLobbyJoinedEvent(true, "");
        }

        public override void RequestJoinLobby(LobbyInfo lobby, string? password) {

        }

        public override void JoinLobby(bool success) {

        }

        public override void LeaveLobby() {

        }

        public override OnlinePlayer GetLobbyOwner() {
            return null;
        }

        public override MeadowPlayerId GetEmptyId() {
            return new RouterPlayerId(0);
        }

        public override string GetLobbyID() {
            if (OnlineManager.lobby != null) {
                return OnlineManager.lobby.owner.id.GetPersonaName() ?? "Nobody" + "'s Lobby";
            }

            return "Unknown Lan Lobby";
        }


        public override bool canOpenInvitations => false;
    }
}
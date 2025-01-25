namespace RainMeadow
{
    public class JoinLobbyPacket : InformLobbyPacket
    {
        public JoinLobbyPacket() : base() { }
        public JoinLobbyPacket(int maxplayers, string name, bool passwordprotected, string mode, int currentplayercount) : base(maxplayers, name, passwordprotected, mode, currentplayercount) { }
        public override Type type => Type.JoinLobby;

        public override void Process()
        {
            if (MatchmakingManager.currentDomain != MatchmakingManager.MatchMakingDomain.LAN) return;
            var newLobbyInfo = MakeLobbyInfo();

            // If we don't have a lobby and we a currently joining a lobby
            if (OnlineManager.lobby is null && OnlineManager.currentlyJoiningLobby is not null) {
                // If the lobby we want to join is a lan lobby
                if (OnlineManager.currentlyJoiningLobby is LANMatchmakingManager.LANLobbyInfo oldLobbyInfo) {
                    // If the lobby we want to join is the lobby that allowed us to join.
                    if (UDPPeerManager.CompareIPEndpoints(oldLobbyInfo.endPoint, newLobbyInfo.endPoint)) {
                        OnlineManager.currentlyJoiningLobby = newLobbyInfo;
                        (MatchmakingManager.instances[MatchmakingManager.MatchMakingDomain.LAN] as LANMatchmakingManager).LobbyAcknoledgedUs(processingPlayer);
                    }
                }
            }
        }
    }
}
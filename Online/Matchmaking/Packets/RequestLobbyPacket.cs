namespace RainMeadow
{
    public class RequestLobbyPacket : Packet
    {
        public override Type type => Type.RequestLobby;

        public override void Process() {
            if (OnlineManager.lobby != null && OnlineManager.lobby.isOwner) {
                (MatchmakingManager.instances[MatchmakingManager.MatchMaker.Local] as LANMatchmakingManager).SendLobbyInfo(
                    (processingPlayer.id as LANMatchmakingManager.LANPlayerId).endPoint);
            }

        }
    }
}
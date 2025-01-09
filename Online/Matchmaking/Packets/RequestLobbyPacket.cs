namespace RainMeadow
{
    public class RequestLobbyPacket : Packet
    {
        public override Type type => Type.RequestLobby;

        public override void Process() {
            if (OnlineManager.lobby != null) {
                (MatchmakingManager.instances[MatchmakingManager.MatchMaker.Local] as LANMatchmakingManager).SendLobbyInfo(processingPlayer);
            }

        }
    }
}
namespace RainMeadow
{
    public class RequestLobbyPacket : Packet
    {
        public override Type type => Type.RequestLobby;

        public override void Process() {
            if (OnlineManager.lobby != null) {
                RainMeadow.DebugMe();
                (MatchmakingManager.instances[MatchmakingManager.MatchMakingDomain.LAN] as LANMatchmakingManager).SendLobbyInfo(processingPlayer);
            }

        }
    }
}
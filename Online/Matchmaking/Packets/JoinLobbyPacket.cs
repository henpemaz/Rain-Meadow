namespace RainMeadow
{
    public class JoinLobbyPacket : Packet
    {
        public override Type type => Type.JoinLobby;

        public override void Process()
        {
            (MatchmakingManager.instances[MatchmakingManager.MatchMaker.Local] as LANMatchmakingManager).LobbyAcknoledgedUs();
        }
    }
}
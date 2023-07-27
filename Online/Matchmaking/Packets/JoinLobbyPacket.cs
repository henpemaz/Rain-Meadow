namespace RainMeadow
{
    public class JoinLobbyPacket : Packet
    {
        public override Type type => Type.JoinLobby;

        public override void Process()
        {
            (MatchmakingManager.instance as LocalMatchmakingManager).LobbyJoined();
        }
    }
}
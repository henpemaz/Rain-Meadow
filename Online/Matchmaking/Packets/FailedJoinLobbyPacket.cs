using System.IO;

namespace RainMeadow
{
    public class FailedJoinLobbyPacket : Packet
    {
        public override Type type => Type.FailedJoinLobby;

        public override void Process()
        {
            (MatchmakingManager.instance as LocalMatchmakingManager).JoinLobby(false);
        }
    }
}
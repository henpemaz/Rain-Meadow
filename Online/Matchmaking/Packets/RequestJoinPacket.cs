using System.IO;

namespace RainMeadow
{
    public class RequestJoinPacket : Packet
    {
        public override Type type => Type.RequestJoin;
        private string password;

        public RequestJoinPacket() : base() { }
        public override void Process()
        {
            if (OnlineManager.lobby != null)
            {
                // Hello packet from joining peer
                (MatchmakingManager.instance as LocalMatchmakingManager).LocalPlayerJoined(processingPlayer);
                // Tell them they are in
                NetIO.SendP2P(processingPlayer, new JoinLobbyPacket(), NetIO.SendType.Reliable);
            }
        }
    }
}
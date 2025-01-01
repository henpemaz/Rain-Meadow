namespace RainMeadow
{
    public class RequestJoinPacket : Packet
    {
        public override Type type => Type.RequestJoin;

        public override void Process()
        {
            if (OnlineManager.lobby != null)
            {
                // Hello packet from joining peer
                (MatchmakingManager.instances[MatchmakingManager.MatchMaker.Local] as LANMatchmakingManager).AcknoledgeLANPlayer(processingPlayer);

                // Tell them they are in
                OnlineManager.netIO.SendP2P(processingPlayer, new JoinLobbyPacket(), NetIO.SendType.Reliable);
            }
        }
    }
}
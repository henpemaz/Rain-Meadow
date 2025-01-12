namespace RainMeadow
{
    public class RequestLeavePacket : Packet
    {
        public override Type type => Type.RequestLeave;

        public override void Process()
        {
            if (OnlineManager.lobby != null)
            {
                (MatchmakingManager.instances[MatchmakingManager.MatchMaker.LAN] as LANMatchmakingManager).RemoveLANPlayer(processingPlayer);
                OnlineManager.netIO.SendP2P(processingPlayer, new SessionEndPacket(), NetIO.SendType.Unreliable);
            }
        }
    }
}
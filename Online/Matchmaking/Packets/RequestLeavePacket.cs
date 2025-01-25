namespace RainMeadow
{
    public class RequestLeavePacket : Packet
    {
        public override Type type => Type.RequestLeave;

        public override void Process()
        {
            if (MatchmakingManager.currentDomain != MatchmakingManager.MatchMakingDomain.LAN) return;
            if (OnlineManager.lobby != null)
            {
                (MatchmakingManager.instances[MatchmakingManager.MatchMakingDomain.LAN] as LANMatchmakingManager).RemoveLANPlayer(processingPlayer);
                OnlineManager.netIO.SendP2P(processingPlayer, new SessionEndPacket(), NetIO.SendType.Unreliable);
            }
        }
    }
}
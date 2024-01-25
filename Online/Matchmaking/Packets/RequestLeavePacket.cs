namespace RainMeadow
{
    public class RequestLeavePacket : Packet
    {
        public override Type type => Type.RequestLeave;

        public override void Process()
        {
            if (OnlineManager.lobby != null)
            {
                (MatchmakingManager.instance as LocalMatchmakingManager).LocalPlayerLeft(processingPlayer);

                NetIO.SendP2P(processingPlayer, new SessionEndPacket(), NetIO.SendType.Unreliable);
            }
        }
    }
}
namespace RainMeadow
{
    public class SessionEndPacket : Packet
    {
        public override Type type => Type.SessionEnd;

        public override void Process()
        {
            if (MatchmakingManager.currentDomain == MatchmakingManager.MatchMakingDomain.LAN)
			{
            	OnlineManager.GetLANNetIO().ForgetPlayer(processingPlayer);
			}
        }
    }
}
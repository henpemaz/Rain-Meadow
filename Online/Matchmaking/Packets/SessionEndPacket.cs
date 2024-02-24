namespace RainMeadow
{
    public class SessionEndPacket : Packet
    {
        public override Type type => Type.SessionEnd;

        public override void Process()
        {
            if (OnlineManager.lobby != null)
            {
                (MatchmakingManager.instance as LocalMatchmakingManager).sessionShutdown();
            }
        }
    }
}
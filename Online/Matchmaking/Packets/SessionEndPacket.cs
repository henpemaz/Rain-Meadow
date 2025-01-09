namespace RainMeadow
{
    public class SessionEndPacket : Packet
    {
        public override Type type => Type.SessionEnd;

        public override void Process()
        {
            if (OnlineManager.lobby != null)
            {
            // (MatchmakingManager.instances[MatchmakingManager.MatchMaker.Local] as LANMatchmakingManager).sessionShutdown();
            }
        }
    }
}
namespace RainMeadow
{
    public class SessionEndPacket : Packet
    {
        public override Type type => Type.SessionEnd;

        public override void Process()
        {
            if (OnlineManager.lobby != null)
            {
                (OnlineManager.netIO as LANNetIO)?.ForgetPlayer(processingPlayer);
            }
        }
    }
}
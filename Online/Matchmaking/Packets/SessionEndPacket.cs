namespace RainMeadow
{
    public class SessionEndPacket : Packet
    {
        public override Type type => Type.SessionEnd;

        public override void Process()
        {
            OnlineManager.netIO.ForgetPlayer(processingPlayer);
        }
    }
}
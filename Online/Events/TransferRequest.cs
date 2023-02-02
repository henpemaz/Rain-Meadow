namespace RainMeadow
{
    public class TransferRequest : PlayerEvent
    {
        private OnlinePlayer mePlayer;
        private OnlinePlayer onlinePlayer;
        private OnlineResource onlineResource;

        public TransferRequest(OnlinePlayer mePlayer, OnlinePlayer onlinePlayer, OnlineResource onlineResource)
        {
            this.mePlayer = mePlayer;
            this.onlinePlayer = onlinePlayer;
            this.onlineResource = onlineResource;
        }
    }
}
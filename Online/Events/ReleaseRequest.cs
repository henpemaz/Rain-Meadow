namespace RainMeadow
{
    public class ReleaseRequest : PlayerEvent
    {
        public OnlinePlayer from;
        public OnlinePlayer to;
        public OnlineResource onlineResource;

        public ReleaseRequest(OnlinePlayer from, OnlinePlayer to, OnlineResource onlineResource)
        {
            this.from = from;
            this.to = to;
            this.onlineResource = onlineResource;
        }
    }
}
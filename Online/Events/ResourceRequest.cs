namespace RainMeadow
{
    public class ResourceRequest : PlayerEvent
    {
        public OnlinePlayer from;
        public OnlinePlayer to;
        public OnlineResource onlineResource;

        public ResourceRequest(OnlinePlayer from, OnlinePlayer to, OnlineResource onlineResource)
        {
            this.from = from;
            this.to = to;
            this.onlineResource = onlineResource;
        }
    }
}
using System.Collections.Generic;

namespace RainMeadow
{
    public class TransferRequest : PlayerEvent
    {
        public OnlinePlayer from;
        public OnlinePlayer to;
        public OnlineResource resource;
        public List<OnlinePlayer> subscribers;

        public TransferRequest(OnlinePlayer from, OnlinePlayer to, OnlineResource resource)
        {
            this.from = from;
            this.to = to;
            this.resource = resource;
        }
    }
}
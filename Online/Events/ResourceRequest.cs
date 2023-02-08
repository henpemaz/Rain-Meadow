namespace RainMeadow
{
    internal class ResourceRequest : ResourceEvent
    {
        public ResourceRequest(OnlinePlayer from, OnlinePlayer to, OnlineResource onlineResource) : base(from, to, onlineResource){}

        public override EventTypeId eventType => EventTypeId.ResourceRequest;
    }
}
namespace RainMeadow
{
    public class ReleaseRequest : ResourceEvent
    {
        public ReleaseRequest(OnlinePlayer from, OnlinePlayer to, OnlineResource onlineResource) : base(from, to, onlineResource){}

        public override EventTypeId eventType => EventTypeId.ReleaseRequest;
    }
}
namespace RainMeadow
{
    public class ResourceRequest : ResourceEvent
    {
        public ResourceRequest() { }

        public ResourceRequest(OnlineResource onlineResource) : base(onlineResource) { }

        public override EventTypeId eventType => EventTypeId.ResourceRequest;

        internal override void Process()
        {
            onlineResource.Requested(this);
        }
    }
}
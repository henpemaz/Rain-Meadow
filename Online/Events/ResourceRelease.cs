namespace RainMeadow
{
    public class ResourceRelease : ResourceEvent
    {
        public ResourceRelease() { }
        public ResourceRelease(OnlineResource resource) : base(resource) { }

        public override void Process()
        {
            onlineResource.Released(this);
        }

        public override EventTypeId eventType => EventTypeId.ResourceRelease;
    }
}
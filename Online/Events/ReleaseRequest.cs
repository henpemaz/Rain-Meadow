namespace RainMeadow
{
    public class ReleaseRequest : ResourceEvent
    {
        public ReleaseRequest(OnlineResource onlineResource) : base(onlineResource) { }

        public override EventTypeId eventType => EventTypeId.ReleaseRequest;

        internal override void Process()
        {
            onlineResource.Released(this);
        }
    }
}